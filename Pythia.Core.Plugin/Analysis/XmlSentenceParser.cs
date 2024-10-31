using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using Corpus.Core;
using Corpus.Core.Plugin.Analysis;
using Fusi.Tools;
using Fusi.Tools.Configuration;
using Fusi.Tools.Text;
using Fusi.Xml;
using Pythia.Core.Analysis;

namespace Pythia.Core.Plugin.Analysis;

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

    private readonly Dictionary<string, string> _emptyNs;
    private readonly HashSet<XName> _stopTags;
    private readonly HashSet<XName> _noMarkerTags;
    private readonly HashSet<char> _endMarkers;
    private readonly List<TextSpan> _structures;
    private readonly HashSet<int> _fakeStops;
    private readonly StringBuilder _tag;
    private readonly TextCutterOptions _cutOptions;
    private Dictionary<string, string>? _namespaces;
    private string? _rootXPath;
    private XmlNamespaceManager? _nsMgr;

    /// <summary>
    /// Initializes a new instance of the <see cref="XmlSentenceParser"/>
    /// class.
    /// </summary>
    public XmlSentenceParser()
    {
        _emptyNs = [];
        _fakeStops = [];
        _endMarkers = ['.', '?', '!', '\u037E'];
        _stopTags = [];
        _noMarkerTags = [];
        _tag = new StringBuilder();
        _structures = [];
        _cutOptions = new TextCutterOptions
        {
            LineFlattening = true,
            Mode = TextCutterMode.Body,
            Limit = 100,
        };
    }

    private string ResolveTagName(string name,
        IDictionary<string, string>? namespaces)
    {
        string? resolved = XmlNsOptionHelper.ResolveTagName(name, namespaces
            ?? _emptyNs);
        if (resolved == null)
        {
            throw new ArgumentException($"Tag name \"{name}\" " +
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
        ArgumentNullException.ThrowIfNull(options);

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
        string prefix = prefixed[..i];
        string local = prefixed[(i + 1)..];
        string? ns = _nsMgr!.LookupNamespace(prefix);
        return XName.Get(local, ns ?? "");
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

    private static int FindElementEndOffset(XElement element, string xml)
    {
        IXmlLineInfo elementInfo = element;

        // navigate through the content to find the end tag position
        XNode? lastNode = element.Nodes().LastOrDefault();
        if (lastNode != null)
        {
            IXmlLineInfo lastNodeInfo = lastNode;
            // get to the end of the last node's content
            string lastNodeString = lastNode.ToString();
            int lastNodeOffset = OffsetHelper.GetOffset(xml,
                lastNodeInfo.LineNumber,
                lastNodeInfo.LinePosition - 1);

            return lastNodeOffset + lastNodeString.Length +
                element.Name.LocalName.Length + 3; // +3 for "</>"
        }
        else
        {
            // empty element - find the end of the start tag
            int startOffset = OffsetHelper.GetOffset(xml,
                elementInfo.LineNumber,
                elementInfo.LinePosition - 1);

            // find the closing '>'
            for (int i = startOffset; i < xml.Length; i++)
            {
                if (xml[i] == '>') return i + 1;
            }
        }

        return xml.Length; // fallback, should not happen with valid XML
    }

    private StringBuilder FillEndMarkers(string xml)
    {
        StringBuilder sb = new(xml);
        if (string.IsNullOrWhiteSpace(xml) || _noMarkerTags.Count == 0)
            return sb;

        XDocument doc = XDocument.Parse(xml,
            LoadOptions.PreserveWhitespace |
            LoadOptions.SetLineInfo);

        foreach (XName tag in _noMarkerTags)
        {
            foreach (XElement element in doc.Root!.Descendants(tag))
            {
                // get the starting position of the element
                IXmlLineInfo startInfo = element;
                int startOffset = OffsetHelper.GetOffset(xml,
                    startInfo.LineNumber,
                    startInfo.LinePosition - 1);

                // find the end position by getting info about the next sibling
                // or parent
                int endOffset = FindElementEndOffset(element, xml);

                // process the text between the start and end tags
                bool inTag = false;
                int currentDepth = 0;

                for (int i = startOffset; i < endOffset; i++)
                {
                    char c = xml[i];

                    // track XML tag depth
                    if (c == '<')
                    {
                        if (i + 1 < xml.Length && xml[i + 1] == '/')
                        {
                            currentDepth--;
                            inTag = true;
                        }
                        else
                        {
                            currentDepth++;
                            inTag = true;
                        }
                        continue;
                    }
                    if (c == '>')
                    {
                        inTag = false;
                        continue;
                    }

                    // only process text at the current element's depth
                    if (!inTag && currentDepth == 1 && _endMarkers.Contains(c))
                    {
                        sb[i] = ' ';
                    }
                }
            }
        }

        return sb;
    }

    private string PrepareCode(string xml)
    {
        StringBuilder sb = FillEndMarkers(xml);

        if (_stopTags.Count == 0)
        {
            XmlFiller.FillTags(sb);
        }
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
                        else
                        {
                            sb[i++] = ' ';
                        }

                        while (i <= t.Item1) sb[i++] = ' ';
                    }
                    else
                    {
                        while (i < sb.Length && sb[i] != '>') sb[i++] = ' ';
                        if (i < sb.Length) sb[i++] = ' ';
                    }
                } // <
                else
                {
                    i++;
                }
            }
        }

        return sb.ToString();
    }

    private (string cut, string full) GetSentenceText(int documentId,
        int p1, int p2, string xml)
    {
        TextSpan t1 = Repository!.GetSpansAt(documentId, p1, TextSpan.TYPE_TOKEN)[0];
        TextSpan t2 = Repository.GetSpansAt(documentId, p2, TextSpan.TYPE_TOKEN)[0];

        string full = xml[t1.Index..(t2.Index + t2.Length)];
        string cut = TextCutter.Cut(full, _cutOptions)!;

        return (cut, full);
    }

    /// <summary>
    /// Counts the characters in <paramref name="text"/> without considering
    /// whitespaces which would be normalized to a single space and whitespaces
    /// at beginning and start of the text.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <returns>Count.</returns>
    private static int GetNormalizedWSCharCount(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;

        int count = 0;
        bool inWS = true;

        // skip initial whitespaces
        int i = 0;
        while (i < text.Length && char.IsWhiteSpace(text[i])) i++;

        // find index to last non-whitespace in text
        int j = text.Length - 1;
        while (j >= 0 && char.IsWhiteSpace(text[j])) j--;

        // count characters between first and last non-whitespace
        while (i <= j)
        {
            char c = text[i++];
            if (char.IsWhiteSpace(c))
            {
                if (!inWS)
                {
                    count++;
                    inWS = true;
                }
            }
            else
            {
                count++;
                inWS = false;
            }
        }
        return count;
    }

    /// <summary>
    /// Parses the structures in the specified document content.
    /// </summary>
    /// <param name="document">The document.</param>
    /// <param name="reader">The document's text reader.</param>
    /// <param name="context">The optional context.</param>
    /// <param name="progress">The optional progress reporter.</param>
    /// <param name="cancel">The optional cancellation token.</param>
    /// <exception cref="ArgumentNullException">reader or
    /// calculator or repository</exception>
    protected override void DoParse(IDocument document, TextReader reader,
        IHasDataDictionary? context = null,
        IProgress<ProgressReport>? progress = null,
        CancellationToken? cancel = null)
    {
        ArgumentNullException.ThrowIfNull(reader);

        _structures.Clear();
        _fakeStops.Clear();

        string xml = reader.ReadToEnd();

        // load all the namespaces from the document, so that we can
        // get each namespace URI from its document-scoped prefix
        if (!string.IsNullOrWhiteSpace(xml))
        {
            XmlDocument doc = new();
            doc.LoadXml(xml);
            _nsMgr = new XmlNamespaceManager(doc.NameTable);

            // add namespaces from options if any
            if (_namespaces?.Count > 0)
            {
                foreach (var ns in _namespaces)
                    _nsMgr.AddNamespace(ns.Key, ns.Value);
            }
        }
        else
        {
            _nsMgr = null;
        }

        // keep the target element only if requested
        if (_rootXPath != null)
            xml = XmlFiller.GetFilledXml(xml, _rootXPath, _nsMgr)!;

        xml = PrepareCode(xml);

        // while preflighting the repository is null
        if (Repository == null) return;

        int i = 0, count = 0;
        ProgressReport? report = progress != null ? new ProgressReport() : null;

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
                int end = _fakeStops.Contains(j) ? j - 1 : j;
                Tuple<int, int>? range = Repository.GetPositionRange(document.Id,
                    start, end);

                if (range != null)
                {
                    // add the structure
                    (string cut, string full) = GetSentenceText(document.Id,
                        range.Item1, range.Item2, xml);

                    _structures.Add(new TextSpan
                    {
                        DocumentId = document.Id,
                        Type = TextSpan.TYPE_SENTENCE,
                        P1 = range.Item1,
                        P2 = range.Item2,
                        Index = start,
                        Length = end - start + 1,
                        Value = GetNormalizedWSCharCount(full).ToString(
                            CultureInfo.InvariantCulture),
                        Text = cut
                    });
                    if (_structures.Count >= BUFFER_SIZE)
                    {
                        Repository.AddSpans(_structures);
                        _structures.Clear();
                    }

                    // progress
                    if (progress != null && ++count % 10 == 0)
                        progress.Report(report!);
                }
                i = j;
            }
            else
            {
                i++;
            }

            if (cancel.HasValue && cancel.Value.IsCancellationRequested)
                break;
        }

        if (_structures.Count > 0) Repository?.AddSpans(_structures);
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
    public string? RootXPath { get; set; }

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
    public IList<string>? StopTags { get; set; }

    /// <summary>
    /// Gets or sets the list of tag names whose content should be ignored
    /// when detecting sentence end markers. For instance, say you have
    /// a TEI document with abbr elements containing abbreviations with dot(s);
    /// in this case, you can add abbr to this list, so that all the
    /// dots inside it are ignored.
    /// When using namespaces, add a prefix (like <c>tei:abbr</c>) and
    /// ensure it is defined in <see cref="Namespaces"/>.
    /// </summary>
    public IList<string>? NoSentenceMarkerTags { get; set; }

    /// <summary>
    /// Gets or sets a set of optional key=namespace URI pairs. Each string
    /// has format <c>prefix=namespace</c>. When dealing with documents with
    /// namespaces, add all the prefixes you will use in <see cref="RootXPath"/>
    /// or <see cref="StopTags"/> here, so that they will be expanded
    /// before processing.
    /// </summary>
    public IList<string>? Namespaces { get; set; }

    /// <summary>
    /// Gets or sets the list of characters which are used as sentence end
    /// markers. The default value is <c>.?!</c> plus U+037E (Greek question
    /// mark).
    /// </summary>
    public string? SentenceEndMarkers { get; set; }
}
#endregion
