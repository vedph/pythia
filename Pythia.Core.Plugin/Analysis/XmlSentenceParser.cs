using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using Corpus.Core;
using Corpus.Core.Plugin.Analysis;
using Fusi.Tools;
using Fusi.Tools.Config;
using Fusi.Xml;
using Pythia.Core.Analysis;

namespace Pythia.Core.Plugin.Analysis
{
    /// <summary>
    /// Sentence structures parser for XML sources.
    /// Tag: <c>structure-parser.xml-sentence</c>.
    /// </summary>
    /// <seealso cref="IStructureParser" />
    [Tag("structure-parser.xml-sentence")]
    public sealed class XmlSentenceParser : StructureParserBase,
        IConfigurable<XmlSentenceParserOptions>
    {
        private const int BUFFER_SIZE = 50;

        private readonly HashSet<XName> _stopTags;
        private readonly HashSet<XName> _noMarkerTags;
        private readonly HashSet<char> _endMarkers;
        private readonly List<Structure> _structures;
        private readonly HashSet<int> _fakeStops;
        private readonly StringBuilder _tag;
        private Dictionary<string, string> _namespaces;
        private string _rootXPath;
        private XmlNamespaceManager _nsMgr;

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlSentenceParser"/>
        /// class.
        /// </summary>
        public XmlSentenceParser()
        {
            _fakeStops = new HashSet<int>();
            _endMarkers = new HashSet<char> { '.', '?', '!', '\u037E' };
            _stopTags = new HashSet<XName>();
            _noMarkerTags = new HashSet<XName>();
            _tag = new StringBuilder();
            _structures = new List<Structure>();
        }

        private static string ResolveTagName(string name,
            IDictionary<string, string> namespaces)
        {
            string resolved = XmlNsOptionHelper.ResolveTagName(name, namespaces);
            if (resolved == null)
            {
                throw new ApplicationException($"Tag name \"{name}\" " +
                    "has unknown namespace prefix");
            }
            return resolved;
        }

        /// <summary>
        /// Configures the object with the specified options.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException">options</exception>
        public void Configure(XmlSentenceParserOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            // document filters
            SetDocumentFilters(options.DocumentFilters);

            // end markers
            _endMarkers.Clear();
            foreach (char c in options.SentenceEndMarkers ?? ".?!\u037E")
                _endMarkers.Add(c);

            // read prefix=namespace pairs if any
            _namespaces = XmlNsOptionHelper.ParseNamespaces(options.Namespaces);

            // stop tags
            _stopTags.Clear();
            if (options.StopTags != null)
            {
                foreach (string s in options.StopTags)
                    _stopTags.Add(ResolveTagName(s, _namespaces));
            }

            // no-end-markers tags
            _noMarkerTags.Clear();
            if (options.NoSentenceMarkerTags != null)
            {
                foreach (string s in options.NoSentenceMarkerTags)
                    _noMarkerTags.Add(ResolveTagName(s, _namespaces));
            }

            // root path
            _rootXPath = options.RootXPath;
        }

        private bool IsEndSentencePunctuation(char c) => _endMarkers.Contains(c);

        private bool IsAfterEnd(StringBuilder sb, int index)
        {
            index--;
            while (index > 0)
            {
                if (IsEndSentencePunctuation(sb[index])) return true;
                if (!char.IsWhiteSpace(sb[index])) break;
                index--;
            }

            return false;
        }

        private XName GetXNameFromPrefixedName(string prefixed)
        {
            int i = prefixed.IndexOf(':');
            if (i == -1) return prefixed;
            string prefix = prefixed.Substring(0, i);
            string local = prefixed.Substring(i + 1);
            string ns = _nsMgr.LookupNamespace(prefix);
            return XName.Get(local, ns);
        }

        private Tuple<int, XName> FindTagEnd(StringBuilder sb, int index)
        {
            _tag.Clear();

            // skip </ or just <
            if (index + 1 < sb.Length && sb[index + 1] == '/') index += 2;
            else index++;
            int nameStart = index, nameEnd = -1;

            // reach > while keeping track of tag's name
            while (index < sb.Length && sb[index] != '>')
            {
                if (char.IsWhiteSpace(sb[index]) || sb[index] == '/')
                    nameEnd = index - 1;
                index++;
            }
            if (nameEnd == -1) nameEnd = index - 1;

            for (int i = nameStart; i <= nameEnd; i++) _tag.Append(sb[i]);

            // get XName from prefix:name
            XName name = GetXNameFromPrefixedName(_tag.ToString());
            return Tuple.Create(index, name);
        }

        private StringBuilder FillEndMarkers(string xml)
        {
            StringBuilder sb = new StringBuilder(xml);
            if (_noMarkerTags.Count == 0) return sb;

            XDocument doc = XDocument.Parse(xml,
                LoadOptions.PreserveWhitespace |
                LoadOptions.SetLineInfo);

            foreach (XName tag in _noMarkerTags)
            {
                foreach (XElement element in doc.Root.Descendants(tag))
                {
                    IXmlLineInfo info = element;

                    int offset = 1 + OffsetHelper.GetOffset(xml,
                        info.LineNumber,
                        info.LinePosition - 1);

                    string outerXml = element.OuterXml();
                    bool inTag = false;

                    for (int i = 0; i < outerXml.Length; i++)
                    {
                        if (outerXml[i] == '<')
                        {
                            inTag = true;
                            continue;
                        }
                        if (outerXml[i] == '>')
                        {
                            inTag = false;
                            continue;
                        }

                        if (!inTag && _endMarkers.Contains(sb[offset + i]))
                            sb[offset + i] = ' ';
                    }
                }
            }

            return sb;
        }

        private string PrepareCode(string xml)
        {
            StringBuilder sb = FillEndMarkers(xml);

            if (_stopTags.Count == 0) XmlFiller.FillTags(sb);
            else
            {
                int i = 0;
                while (i < sb.Length)
                {
                    if (sb[i] == '<')
                    {
                        // if it's a closing tag, not preceded by a stop:
                        if (i + 2 < sb.Length && sb[i + 1] == '/' && i > 0
                            && !IsAfterEnd(sb, i))
                        {
                            // read tag and find its end
                            var t = FindTagEnd(sb, i);
                            // if it's a stop-tag, fill with . + spaces,
                            // else just with spaces
                            if (_stopTags.Contains(t.Item2))
                            {
                                sb[i] = '.';
                                _fakeStops.Add(i++);
                            }
                            else sb[i++] = ' ';
                            while (i <= t.Item1) sb[i++] = ' ';
                        }
                        else
                        {
                            while (i < sb.Length && sb[i] != '>') sb[i++] = ' ';
                            if (i < sb.Length) sb[i++] = ' ';
                        }
                    } // <
                    else i++;
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Parses the structures in the specified document content.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="reader">The document's text reader.</param>
        /// <param name="progress">The optional progress reporter.</param>
        /// <param name="cancel">The optional cancellation token.</param>
        /// <exception cref="ArgumentNullException">reader or
        /// calculator or repository</exception>
        protected override void DoParse(Document document, TextReader reader,
            IProgress<ProgressReport> progress = null,
            CancellationToken? cancel = null)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));

            _structures.Clear();
            _fakeStops.Clear();

            string xml = reader.ReadToEnd();

            // load all the namespaces from the document, so that we can
            // get each namespace URI from its document-scoped prefix
            if (!string.IsNullOrWhiteSpace(xml))
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xml);
                _nsMgr = new XmlNamespaceManager(doc.NameTable);

                // add namespaces from options if any
                if (_namespaces?.Count > 0)
                {
                    foreach (var ns in _namespaces)
                        _nsMgr.AddNamespace(ns.Key, ns.Value);
                }
            }
            else _nsMgr = null;

            // keep the target element only if requested
            if (_rootXPath != null)
                xml = XmlFiller.GetFilledXml(xml, _rootXPath, _nsMgr);

            xml = PrepareCode(xml);

            // while preflighting the repository is null
            if (Repository == null) return;

            int i = 0, count = 0;
            ProgressReport report = progress != null ?
                new ProgressReport() : null;

            while (i < xml.Length)
            {
                // start with the first letter
                if (char.IsLetter(xml[i]))
                {
                    int start = i;
                    int j = i + 1;
                    while (j < xml.Length
                           && !IsEndSentencePunctuation(xml[j]))
                    {
                        j++;
                    }

                    // in case of a fake stop added at the end of a stop-tag,
                    // the region must stop before it, as it does not exist in
                    // the XML code, where it represents the < character of
                    // the ending tag.
                    var range = Repository.GetTokenPositionRange(
                        document.Id,
                        start,
                        _fakeStops.Contains(j)? j - 1 : j);

                    if (range != null)
                    {
                        // add the structure
                        _structures.Add(new Structure
                        {
                            StartPosition = range.Item1,
                            EndPosition = range.Item2,
                            DocumentId = document.Id,
                            Name = "sent"
                        });
                        if (_structures.Count >= BUFFER_SIZE)
                        {
                            Repository.AddStructures(_structures);
                            _structures.Clear();
                        }

                        // progress
                        if (progress != null && ++count % 10 == 0)
                            progress.Report(report);
                    }
                    i = j;
                }
                else i++;

                if (cancel.HasValue && cancel.Value.IsCancellationRequested)
                    break;
            }

            if (_structures.Count > 0) Repository?.AddStructures(_structures);
        }
    }

    #region XmlSentenceParserOptions
    /// <summary>
    /// Options for <see cref="XmlSentenceParser"/>.
    /// </summary>
    public sealed class XmlSentenceParserOptions : StructureParserOptions
    {
        /// <summary>
        /// Gets or sets the XPath 1.0 expression targeting the root path.
        /// This is the path to the element to be used as the root for this
        /// parser; when specified, sentences will be searched only whithin
        /// this element and all its descendants. For instance, in a TEI document
        /// you will probably want to limit sentences to the contents of the
        /// <c>body</c> (<c>/tei:TEI//tei:body</c>) or <c>text</c>
        /// (<c>/tei:TEI//tei:text</c>) element only. If not specified,
        /// the whole document will be parsed.
        /// You can use namespace prefixes in this expression, either from
        /// the document or from <see cref="Namespaces"/>.
        /// </summary>
        public string RootXPath { get; set; }

        /// <summary>
        /// Gets or sets the stop tags names. A "stop tag" is a tag implying a
        /// sentence stop when closed (e.g. <c>head</c> in a TEI document,
        /// as a title is anyway a "sentence", distinct from the following text,
        /// whether it ends with a stop or not).
        /// Each tag gets filled with spaces, while a stop tag gets filled with
        /// a full stop followed by spaces.
        /// When using namespaces, add a prefix (like <c>tei:body</c>) and
        /// ensure it is defined in <see cref="Namespaces"/>.
        /// </summary>
        public string[] StopTags { get; set; }

        /// <summary>
        /// Gets or sets the list of tag names whose content should be ignored
        /// when detecting sentence end markers. For instance, say you have
        /// a TEI document with abbr elements containing abbreviations with dot(s);
        /// in this case, you can add abbr to this list, so that all the
        /// dots inside it are ignored.
        /// When using namespaces, add a prefix (like <c>tei:abbr</c>) and
        /// ensure it is defined in <see cref="Namespaces"/>.
        /// </summary>
        public string[] NoSentenceMarkerTags { get; set; }

        /// <summary>
        /// Gets or sets a set of optional key=namespace URI pairs. Each string
        /// has format <c>prefix=namespace</c>. When dealing with documents with
        /// namespaces, add all the prefixes you will use in <see cref="RootXPath"/>
        /// or <see cref="StopTags"/> here, so that they will be expanded
        /// before processing.
        /// </summary>
        public string[] Namespaces { get; set; }

        /// <summary>
        /// Gets or sets the list of characters which are used as sentence end
        /// markers. The default value is <c>.?!</c> plus U+037E (Greek question
        /// mark).
        /// </summary>
        public string SentenceEndMarkers { get; set; }
    }
    #endregion
}
