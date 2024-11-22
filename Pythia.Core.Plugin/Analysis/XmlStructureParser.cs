using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Corpus.Core;
using Corpus.Core.Plugin.Analysis;
using Fusi.Tools;
using Fusi.Tools.Configuration;
using Fusi.Tools.Text;
using Fusi.Xml;
using Pythia.Core.Analysis;

namespace Pythia.Core.Plugin.Analysis;

/// <summary>
/// A parser for element-based structures in XML documents.
/// <para>Tag: <c>structure-parser.xml</c>.</para>
/// </summary>
[Tag("structure-parser.xml")]
public sealed class XmlStructureParser : StructureParserBase,
    IConfigurable<XmlStructureParserOptions>
{
    private readonly List<TextSpan> _structures;
    private readonly TextCutterOptions _cutOptions;
    private IList<DroppableXmlStructureDefinition>? _definitions;
    private IDictionary<string, string>? _namespaces;
    private IDictionary<string, string>? _privilegedMappings;
    private int _bufferSize;

    private IProgress<ProgressReport>? _progress;
    private int _count;
    private ProgressReport? _report;

    /// <summary>
    /// Initializes a new instance of the <see cref="XmlStructureParser" />
    /// class.
    /// </summary>
    public XmlStructureParser()
    {
        _structures = [];
        _bufferSize = 100;
        _cutOptions = new TextCutterOptions
        {
            LineFlattening = true,
            Mode = TextCutterMode.Body,
            Limit = 100,
        };
    }

    /// <summary>
    /// Configures the object with the specified options.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <exception cref="ArgumentNullException">options</exception>
    public void Configure(XmlStructureParserOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        // document filters
        SetDocumentFilters(options.DocumentFilters);

        // read prefix=namespace pairs if any
        _namespaces = XmlNsOptionHelper.ParseNamespaces(options.Namespaces);

        // buffer size
        _bufferSize = options.BufferSize < 0? 100 : options.BufferSize;

        // definitions
        _definitions = options.Definitions;

        // mappings
        _privilegedMappings = options.PrivilegedMappings;
    }

    private string ApplyFilters(string text, TextSpan structure)
    {
        StringBuilder sb = new(text);
        foreach (IStructureValueFilter filter in Filters)
        {
            filter.Apply(sb, structure);
        }
        return sb.ToString();
    }

    private void AddStructure(int documentId, string text,
        DroppableXmlStructureDefinition definition, XElement target,
        XmlNamespaceManager nsmgr)
    {
        _count++;
        if (_progress != null && _count % 10 == 0)
        {
            _report!.Count = _count;
            _progress.Report(_report);
        }

        // get the structure's range
        IXmlLineInfo info = target;
        // line position refers to 1st char past <, so subtract 1 from it
        int index = IndexCalculator!.GetIndex(
            info.LineNumber, info.LinePosition - 1);
        int endIndex = OffsetHelper.GetElementEndOffset(text, index);

        Tuple<int, int>? range = null;
        if (Repository != null)
        {
            range = Repository.GetPositionRange(documentId, index, endIndex - 1);
        }
        if (range == null) return;

        // create the structure
        TextSpan structure = new()
        {
            P1 = range.Item1,
            P2 = range.Item2,
            DocumentId = documentId,
            Type = definition.Name!,
            // index and length are used for highlight and must overlap element
            Index = index,
            Length = endIndex - index,
            Text = TextCutter.Cut(target.Value, _cutOptions)!,
        };

        // get the structure's value if any
        string? value = definition.GetStructureValue(target, nsmgr);
        if (!string.IsNullOrEmpty(value))
        {
            structure.Value = ApplyFilters(value, structure);
        }

        // special case: if the structure targets a token, add the attribute
        // to all the tokens inside it, and discard the structure itself
        if (definition.TokenTargetName != null)
        {
            string? targetName = _privilegedMappings?.ContainsKey
                (definition.TokenTargetName) == true
                ? _privilegedMappings[definition.TokenTargetName]
                : definition.TokenTargetName;

            Repository?.AddSpanAttributes(documentId,
                structure.P1,
                structure.P2,
                targetName,
                value ?? "",
                definition.Type);

            if (definition.OverriddenPos != null)
            {
                // override POS
                Repository?.AddSpanAttributes(documentId,
                    structure.P1,
                    structure.P2,
                    "pos",
                    definition.OverriddenPos,
                    definition.Type);

                // remove other POS-dependent attributes if required
                if (definition.RemovedPosTags?.Count > 0)
                {
                    Repository?.DeleteSpanAttributes(documentId,
                        structure.P1,
                        structure.P2,
                        definition.RemovedPosTags);
                }
            }

            return;
        }

        // add to buffer and flush it to database if full
        _structures.Add(structure);
        if (_structures.Count >= _bufferSize)
        {
            Repository?.AddSpans(_structures);
            _structures.Clear();
        }
    }

    /// <summary>
    /// Parses the specified document content.
    /// </summary>
    /// <param name="document">The document.</param>
    /// <param name="reader">The document's text reader.</param>
    /// <param name="context">The optional context.</param>
    /// <param name="progress">The optional progress reporter.</param>
    /// <param name="cancel">The optional cancellation token.</param>
    /// <exception cref="ArgumentNullException">null reader or
    /// calculator</exception>
    protected override void DoParse(IDocument document, TextReader reader,
        IHasDataDictionary? context = null,
        IProgress<ProgressReport>? progress = null,
        CancellationToken? cancel = null)
    {
        ArgumentNullException.ThrowIfNull(reader);

        _count = 0;
        _structures.Clear();
        if (_definitions == null) return;

        try
        {
            _progress = progress;
            if (_progress != null) _report = new ProgressReport();
            cancel ??= CancellationToken.None;

            // parse XML from the received text
            string text = reader.ReadToEnd();
            XDocument doc = XDocument.Parse(text,
                LoadOptions.SetLineInfo | LoadOptions.PreserveWhitespace);
            if (doc.Root == null) return;

            // load namespaces from both document and options
            XmlNamespaceManager nsmgr = new(doc.CreateReader().NameTable);
            if (_namespaces?.Count > 0)
            {
                foreach (var ns in _namespaces)
                    nsmgr.AddNamespace(ns.Key, ns.Value);
            }

            // search each defined structure in the document
            foreach (DroppableXmlStructureDefinition def in _definitions)
            {
                foreach (XElement target in
                    doc.XPathSelectElements(def.XPath!, nsmgr))
                {
                    AddStructure(document.Id, text, def, target, nsmgr);
                }
                if (cancel?.IsCancellationRequested == true) break;
            }

            // empty the buffer
            if (_structures.Count > 0) Repository?.AddSpans(_structures);
        }
        finally
        {
            _progress = null;
        }
    }
}

#region XmlStructureParserOptions
/// <summary>
/// Options for <see cref="XmlStructureParser"/>.
/// </summary>
public class XmlStructureParserOptions : StructureParserOptions
{
    /// <summary>
    /// Gets or sets the definitions.
    /// </summary>
    public IList<DroppableXmlStructureDefinition>? Definitions { get; set; }

    /// <summary>
    /// Gets or sets a set of optional key=namespace URI pairs. Each string
    /// has format <c>prefix=namespace</c>. When dealing with documents with
    /// namespaces, add all the prefixes you will use in
    /// <see cref="Definitions"/> here, so that they will be expanded
    /// before processing.
    /// </summary>
    public IList<string>? Namespaces { get; set; }

    /// <summary>
    /// Gets or sets the size of the structures buffer. Structures
    /// are flushed to the database only when the buffer is filled,
    /// thus avoiding excessive pressure on the database. The default
    /// value is 100.
    /// </summary>
    public int BufferSize { get; set; }

    /// <summary>
    /// Gets or sets the mappings between ghost structure values and privileged
    /// attributes. This is used when you are indexing ghost structures which
    /// inject attributes into their tokens, and you want to map their value
    /// into a privileged attribute rather than just adding a generic one.
    /// For instance, you might have a <c>foreign</c> structure with a value
    /// equal to <c>lat</c>=Latin and you might want to map this value to
    /// <see cref="TextSpan.Language"/> (via the <c>language</c> privileged
    /// attribute). In this case, you would add a mapping foreign = language,
    /// so that the parser will recognize the target as a privileged attribute
    /// and behave accordingly.
    /// </summary>
    public IDictionary<string,string>? PrivilegedMappings { get; set; }

    /// <summary>
    /// Create a new instance of the <see cref="XmlStructureParserOptions"/>
    /// class.
    /// </summary>
    public XmlStructureParserOptions()
    {
        BufferSize = 100;
    }
}
#endregion
