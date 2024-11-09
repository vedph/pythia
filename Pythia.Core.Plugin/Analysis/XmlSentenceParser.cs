using System;
using System.Collections.Generic;
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
    private readonly HashSet<XName> _blankTags;
    private readonly HashSet<XName> _stopTags;
    private readonly HashSet<XName> _noEndMarkerTags;
    private readonly HashSet<XName> _observedTags;
    private readonly HashSet<char> _endMarkers;
    private readonly List<TextSpan> _structures;
    private readonly HashSet<int> _fakeStops;
    private readonly TextCutterOptions _cutOptions;
    private Dictionary<string, string>? _namespaces;

    /// <summary>
    /// Initializes a new instance of the <see cref="XmlSentenceParser"/>
    /// class.
    /// </summary>
    public XmlSentenceParser()
    {
        _emptyNs = [];
        _fakeStops = [];
        _endMarkers = ['.', '?', '!', '\u037E'];
        _blankTags = [];
        _stopTags = [];
        _noEndMarkerTags = [];
        _observedTags = [];
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
        return XmlNsOptionHelper.ResolveTagName(name, namespaces
            ?? _emptyNs)
            ?? throw new ArgumentException($"Tag name \"{name}\" " +
                "has unknown namespace prefix");
    }

    /// <summary>
    /// Configures the object with the specified options.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <exception cref="ArgumentNullException">options</exception>
    public void Configure(XmlSentenceParserOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        // read prefix=namespace pairs if any
        _namespaces = XmlNsOptionHelper.ParseNamespaces(options.Namespaces);

        // document filters
        SetDocumentFilters(options.DocumentFilters);

        // blank tags
        _blankTags.Clear();
        if (options.BlankTags != null)
        {
            foreach (string s in options.BlankTags)
                _blankTags.Add(ResolveTagName(s, _namespaces));
        }

        // end markers
        _endMarkers.Clear();
        foreach (char c in options.SentenceEndMarkers ?? ".?!\u037E")
            _endMarkers.Add(c);

        // stop tags
        _stopTags.Clear();
        if (options.StopTags != null)
        {
            foreach (string s in options.StopTags)
                _stopTags.Add(ResolveTagName(s, _namespaces));
        }

        // no-end-markers tags
        _noEndMarkerTags.Clear();
        if (options.NoSentenceMarkerTags != null)
        {
            foreach (string s in options.NoSentenceMarkerTags)
                _noEndMarkerTags.Add(ResolveTagName(s, _namespaces));
        }

        // observed tags = all the tags together
        _observedTags.Clear();
        _observedTags.UnionWith(_blankTags);
        _observedTags.UnionWith(_stopTags);
        _observedTags.UnionWith(_noEndMarkerTags);
    }

    private bool IsEndSentencePunctuation(char c) => _endMarkers.Contains(c);

    private (string cut, string full) GetSentenceText(int documentId,
        int p1, int p2, string xml)
    {
        TextSpan t1 = Repository!.GetSpansAt(documentId, p1, TextSpan.TYPE_TOKEN)[0];
        TextSpan t2 = Repository.GetSpansAt(documentId, p2, TextSpan.TYPE_TOKEN)[0];

        string full = xml[t1.Index..(t2.Index + t2.Length)];
        full = NormalizeWS(full);
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

    //private void LoadNamespaces(string xml)
    //{
    //    // load all the namespaces from the document, so that we can
    //    // get each namespace URI from its document-scoped prefix
    //    if (!string.IsNullOrWhiteSpace(xml))
    //    {
    //        XmlDocument doc = new();
    //        doc.LoadXml(xml);
    //        _nsMgr = new XmlNamespaceManager(doc.NameTable);

    //        // add namespaces from options if any
    //        if (_namespaces?.Count > 0)
    //        {
    //            foreach (var ns in _namespaces)
    //                _nsMgr.AddNamespace(ns.Key, ns.Value);
    //        }
    //    }
    //    else
    //    {
    //        _nsMgr = null;
    //    }
    //}

    private void AddFakeStops(StringBuilder xml, XmlTagRangeSet set)
    {
        foreach (XmlTagRange range in set.GetTagRanges()
            .Where(r => _stopTags.Contains(r.TagName)))
        {
            // find index of the closing tag
            int i = range.StartIndex + range.Length - 1;
            while (i > range.StartIndex && xml[i] != '<') i--;

            // add a fake stop at the closing tag, except when it is preceded
            // by any of the _endMarkers characters (ignoring whitespaces)
            int left = i - 1;
            while (left > range.StartIndex && char.IsWhiteSpace(xml[left]))
                left--;

            if (!IsEndSentencePunctuation(xml[left]))
            {
                _fakeStops.Add(i);
                xml[i] = '.';
            }
        }
    }

    private static void NormalizeWS(StringBuilder sb)
    {
        // normalize all whitespaces in sb into a single space and trim it
        bool inWS = false;
        for (int i = 0; i < sb.Length; i++)
        {
            char c = sb[i];
            if (char.IsWhiteSpace(c))
            {
                if (!inWS)
                {
                    sb[i] = ' ';
                    inWS = true;
                }
                else
                {
                    sb.Remove(i, 1);
                    i--;
                }
            }
            else
            {
                inWS = false;
            }
        }
        if (sb.Length > 0 && char.IsWhiteSpace(sb[0])) sb.Remove(0, 1);
        if (sb.Length > 0 && char.IsWhiteSpace(sb[^1]))
            sb.Remove(sb.Length - 1, 1);
    }

    private static string NormalizeWS(string s)
    {
        StringBuilder sb = new(s);
        NormalizeWS(sb);
        return sb.ToString();
    }

    private static string GetBlankedPlainText(string xml)
    {
        StringBuilder sb = new(xml);
        XmlFiller.FillTags(sb);
        return sb.ToString();
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

        // reset state
        _structures.Clear();
        _fakeStops.Clear();

        // load the XML content
        string xml = reader.ReadToEnd();

        // load all the namespaces from the document
        //LoadNamespaces(xml);

        // create a set of XML tag ranges for all the observed tags
        XmlTagRangeSet set = new(xml, _observedTags);

        // project the XML into plain text in 3 steps
        StringBuilder filledXml = new(xml);

        // 1. blank-fill unwanted tags (e.g. teiHeader) if required
        if (_blankTags.Count > 0) XmlFiller.FillTags(filledXml, _blankTags, set);

        // 2. blank-fill stop tags with a leading dot if required,
        // while also keeping track of their indexes
        if (_stopTags.Count > 0) AddFakeStops(filledXml, set);

        // 3. in each of the no-end-marker tags, replace end markers with spaces
        foreach (var range in set.GetTagRanges()
            .Where(range => _noEndMarkerTags.Contains(range.TagName)))
        {
            for (int j = 0; j < range.Length; j++)
            {
                int index = range.StartIndex + j;
                if (_endMarkers.Contains(filledXml[index]))
                    filledXml[index] = ' ';
            }
        }

        // while preflighting the repository is null
        if (Repository == null) return;

        int i = 0, count = 0;
        ProgressReport? report = progress != null ? new ProgressReport() : null;

        // remove all the tags and get the prepared plain text, where end markers
        // have been adjusted with reference to the XML tags
        XmlFiller.FillTags(filledXml);
        string prepared = filledXml.ToString();

        // get the plain text from the XML to be used as the source of the
        // sentence's text value - this is different from prepared, because
        // it retains the original text with its end markers
        string text = GetBlankedPlainText(xml);

        while (i < prepared.Length)
        {
            // start with the first letter/digit (e.g. a numbered list starts
            // with a number)
            if (char.IsLetterOrDigit(prepared[i]))
            {
                int start = i;
                int j = i + 1;
                while (j < prepared.Length
                       && !IsEndSentencePunctuation(prepared[j]))
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
                        range.Item1, range.Item2, text);

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

            if (cancel.HasValue && i % 100 == 0 &&
                cancel.Value.IsCancellationRequested)
            {
                break;
            }
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
    /// Gets or sets the names of the XML elements which internally should be
    /// blank-filled before parsing the sentence structures. For instance, a
    /// TEI header would be a good candidate, as it does not contain any text
    /// content.
    /// When using namespaces, add a prefix (like <c>tei:teiHeader</c>) and
    /// ensure it is defined in <see cref="Namespaces"/>.
    /// </summary>
    public HashSet<string>? BlankTags { get; set; }

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
    public HashSet<string>? StopTags { get; set; }

    /// <summary>
    /// Gets or sets the list of tag names whose content should be ignored
    /// when detecting sentence end markers. For instance, say you have
    /// a TEI document with abbr elements containing abbreviations with dot(s);
    /// in this case, you can add abbr to this list, so that all the
    /// dots inside it are ignored.
    /// When using namespaces, add a prefix (like <c>tei:abbr</c>) and
    /// ensure it is defined in <see cref="Namespaces"/>.
    /// </summary>
    public HashSet<string>? NoSentenceMarkerTags { get; set; }

    /// <summary>
    /// Gets or sets a set of optional key=namespace URI pairs. Each string
    /// has format <c>prefix=namespace</c>.
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
