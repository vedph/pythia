using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Corpus.Core.Plugin.Analysis;
using Corpus.Core.Reading;
using Fusi.Tools.Configuration;
using Fusi.Xml.Extras;

namespace Corpus.Core.Plugin.Reading;

/// <summary>
/// XML text picker.
/// Tag: <c>text-picker.xml</c>.
/// </summary>
/// <seealso cref="T:Corpus.Core.Reading.ITextPicker" />
[Tag("text-picker.xml")]
public sealed class XmlTextPicker : ITextPicker,
    IConfigurable<XmlTextPickerOptions>
{
    private readonly Regex _tagStartRegex;
    private XmlTextPickerOptions? _options;
    private IDictionary<string, string>? _namespaces;
    private XmlHighlighter? _highlighter;

    /// <summary>
    /// Initializes a new instance of the <see cref="XmlTextPicker"/> class.
    /// </summary>
    public XmlTextPicker()
    {
        _tagStartRegex = new Regex(@"<[^\?]", RegexOptions.Compiled);
    }

    private XElement? ParseElement(string xml)
    {
        // if there is no namespace prefix, parse directly
        if (!xml.Contains(':')) return XElement.Parse(xml);
        // if there are prefixes but no namespace mappings, fail
        if (_namespaces == null) return null;

        // add namespace declarations to the XML string
        foreach (var ns in _namespaces)
        {
            string xmlnsAttr = $"xmlns:{ns.Key}=\"{ns.Value}\"";

            // check if the namespace is already declared
            if (!xml.Contains(xmlnsAttr))
            {
                // insert the namespace declaration into the root element
                int insertIndex = xml.IndexOf('>');
                xml = xml.Insert(insertIndex, $" {xmlnsAttr}");
            }
        }

        // load the XML string into an XElement
        return XElement.Parse(xml);
    }

    /// <summary>
    /// Configures the object with the specified options.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <exception cref="ArgumentNullException">options</exception>
    public void Configure(XmlTextPickerOptions options)
    {
        _options = options ??
            throw new ArgumentNullException(nameof(options));

        // read prefix=namespace pairs if any
        _namespaces = XmlNsOptionHelper.ParseNamespaces(options.Namespaces);

        // create highlighter if needed
        if (!string.IsNullOrEmpty(options.HitElement))
        {
            XElement? hiElement = ParseElement(options.HitElement) ??
                throw new InvalidOperationException(
                    $"Invalid hit element: {options.HitElement}");

            // the hit element can have a namespace prefix or not: in both
            // cases, the highlighter creates it in the namespace of the
            // element containing the hit. Highlighting requires both the
            // hit markers.
            _highlighter = !string.IsNullOrEmpty(options.HitOpen) &&
                !string.IsNullOrEmpty(options.HitClose)
                ? new XmlHighlighter
                {
                    OpeningEscape = options.HitOpen,
                    ClosingEscape = options.HitClose,
                    HiElement = hiElement
                }
                : null;
        }
        else
        {
            _highlighter = null;
        }
    }

    /// <summary>
    /// Pick the text corresponding to the content of the specified
    /// document map node.
    /// This is typically used when browsing a document via its map.
    /// </summary>
    /// <param name="text">The source document text.</param>
    /// <param name="map">The source document full map (root node)</param>
    /// <param name="node">The node to pick.</param>
    /// <returns>text or null if not found</returns>
    /// <exception cref="ArgumentNullException">null text or map or node
    /// </exception>
    public TextPiece? PickNode(string text, TextMapNode map, TextMapNode node)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(node);

        map.SelectAll(false);
        if (node.Location == null) return null;

        XDocument doc = XDocument.Parse(text, LoadOptions.PreserveWhitespace);

        // load namespaces from both document and options
        XmlNamespaceManager nsmgr = XmlNsOptionHelper.GetDocNamespacesManager(
            text, _options?.DefaultNsPrefix, _namespaces);

        XElement? element = doc.XPathSelectElement(node.Location, nsmgr);
        return element != null
            ? new TextPiece(element.ToString(SaveOptions.DisableFormatting), map)
            {
                StartIndex = node.StartIndex,
                EndIndex = node.EndIndex
            }
            : null;
    }

    private string EnsureDefaultNs(string xml, TextMapNode map,
        XmlNamespaceManager nsmgr)
    {
        if (string.IsNullOrEmpty(_options?.DefaultNsPrefix)) return xml;

        string? ns = nsmgr.LookupNamespace(_options.DefaultNsPrefix);
        if (ns == null) return xml;

        // find 1st tag (skipping declarations like <?...?>)
        Match m = _tagStartRegex.Match(xml);
        if (!m.Success) return xml;

        XmlTag? tag = XmlTag.Parse(xml, m.Index, true, nsmgr);
        if (tag?.Attributes?.ContainsKey("xmlns") != true)
        {
            // insert xmlns="..." as the first attribute
            // after the first opening tag. First, determine
            // the insertion point
            int insertionIndex = xml.IndexOfAny(
                [' ', '\n', '\t', '/', '>'], m.Index);
            Debug.Assert(insertionIndex > -1);
            string insertion;
            if (char.IsWhiteSpace(xml[insertionIndex]))
            {
                insertionIndex++;
                insertion = $"xmlns=\"{ns}\" ";
            }
            else
            {
                insertion = $" xmlns=\"{ns}\" ";
            }

            // insert into the picked fragment. Map nodes are not shifted,
            // as their offsets refer to the whole source text, not to the
            // fragment (shifting them by a fragment offset corrupted them)
            xml = xml.Insert(insertionIndex, insertion);
        }
        return xml;
    }

    private static bool IsAncestorOrSelf(TextMapNode ancestor, TextMapNode node)
    {
        for (TextMapNode? n = node; n != null; n = n.Parent)
            if (n == ancestor) return true;
        return false;
    }

    /// <summary>
    /// Align the first and last nodes of a context so that the text from
    /// the start of the first to the end of the last is well-formed: when
    /// either node includes the other one, both become the including node;
    /// otherwise, both are widened to their ancestors which are children of
    /// their nearest common ancestor, i.e. to a sequence of siblings.
    /// </summary>
    private static (TextMapNode First, TextMapNode Last) AlignNodes(
        TextMapNode first, TextMapNode last)
    {
        if (IsAncestorOrSelf(first, last)) return (first, first);
        if (IsAncestorOrSelf(last, first)) return (last, last);

        TextMapNode? common = first.GetNearestCommonAncestor(last);
        if (common == null) return (first, last);
        while (first.Parent != common) first = first.Parent!;
        while (last.Parent != common) last = last.Parent!;
        return (first, last);
    }

    /// <summary>
    /// Pick the context wrapping the specified range of character offsets,
    /// wrapping it in hit markers if specified. This is typically used
    /// when displaying the results of a search.
    /// </summary>
    /// <param name="text">The source document text.</param>
    /// <param name="map">The source document full map (root node).</param>
    /// <param name="startIndex">The start character index.</param>
    /// <param name="endIndex">The end character index (exclusive).</param>
    /// <returns>text piece, or null if not found</returns>
    /// <exception cref="ArgumentNullException">null text or map</exception>
    public TextPiece? PickContext(string text, TextMapNode map,
        int startIndex, int endIndex)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(map);

        TextMapNode? firstNode = null;
        TextMapNode? lastNode = null;
        TextMapNode? prevNode = null;

        map.Visit(node =>
        {
            if (firstNode == null)
            {
                if (startIndex == node.StartIndex) firstNode = node;
                else if (startIndex < node.StartIndex) firstNode = prevNode;
            }

            if (firstNode != null)
            {
                if (endIndex <= firstNode.EndIndex)
                {
                    lastNode = firstNode;
                    return false;
                }
                if (endIndex <= node.EndIndex)
                {
                    lastNode = node;
                    return false;
                }
                node.IsSelected = true;
            }
            prevNode = node;
            return true;
        });

        // when no node starts after the start index, the visit never set
        // the first node: this happens when the hit is inside the last
        // node in document order (e.g. the last paragraph). In this case,
        // pick the deepest node including the start index, widened to the
        // nearest ancestor including also the end index
        if (firstNode == null)
        {
            map.SelectAll(false);
            map.Visit(node =>
            {
                if (node.StartIndex <= startIndex && startIndex < node.EndIndex)
                    firstNode = node;
                return true;
            });

            // the range is outside the map: nothing to pick
            if (firstNode == null) return null;

            while (firstNode.Parent != null && endIndex > firstNode.EndIndex)
                firstNode = firstNode.Parent;
            lastNode = firstNode;
        }
        lastNode ??= prevNode;
        (firstNode, lastNode) = AlignNodes(firstNode, lastNode!);

        // extract the text piece
        int pieceStart = firstNode.StartIndex;
        int pieceEnd = lastNode!.EndIndex;
        StringBuilder sb = new(text[pieceStart..pieceEnd]);

        // add hit markers if specified, ensuring that they fall inside
        // the picked text
        bool highlight = false;
        if (!string.IsNullOrEmpty(_options?.HitOpen) ||
            !string.IsNullOrEmpty(_options?.HitClose))
        {
            // the markers must fall inside the content of the picked
            // element(s), i.e. after the first start tag and before the
            // last end tag, else the piece would not be well-formed
            string piece = sb.ToString();
            int contentStart = piece.IndexOf('>') + 1;
            int contentEnd = piece.LastIndexOf('<');
            if (contentStart > 0 && contentEnd >= contentStart)
            {
                int hitStart = Math.Clamp(startIndex - pieceStart,
                    contentStart, contentEnd);
                int hitEnd = Math.Clamp(endIndex - pieceStart,
                    hitStart, contentEnd);
                sb.Insert(hitEnd, _options.HitClose ?? "");
                sb.Insert(hitStart, _options.HitOpen ?? "");
                highlight = true;
            }
        }

        // if first and last nodes are different, wrap them
        if (firstNode != lastNode)
        {
            sb.Insert(0, _options!.WrapperPrefix ?? "");
            sb.Append(_options!.WrapperSuffix ?? "");
        }

        // add default namespace if set
        XmlNamespaceManager nsmgr = XmlNsOptionHelper.GetDocNamespacesManager(
            text, _options?.DefaultNsPrefix, _namespaces);
        string xml = EnsureDefaultNs(sb.ToString(), map, nsmgr);

        // highlight if needed
        if (highlight && _highlighter != null &&
            !string.IsNullOrEmpty(_options?.HitElement))
        {
            XDocument doc = XDocument.Parse(xml, LoadOptions.PreserveWhitespace);
            _highlighter.WrapHighlightedText(doc);
            xml = doc.ToString(SaveOptions.DisableFormatting |
                SaveOptions.OmitDuplicateNamespaces);
        }

        return new TextPiece(xml, map)
        {
            StartIndex = pieceStart,
            EndIndex = pieceEnd
        };
    }
}

/// <summary>
/// Options for <see cref="XmlTextPicker"/>.
/// </summary>
/// <seealso cref="TextPickerOptions" />
public class XmlTextPickerOptions : TextPickerOptions
{
    /// <summary>
    /// Gets or sets a set of optional key=namespace URI pairs. Each string
    /// has format <c>prefix=namespace</c>.
    /// </summary>
    public IList<string>? Namespaces { get; set; }

    /// <summary>
    /// Gets or sets the default namespace prefix. When this is set,
    /// and the document has a default empty-prefix namespace (xmlns="URI"),
    /// all the XPath queries get their empty-prefix names prefixed with
    /// this prefix, which in turn is mapped to the default namespace.
    /// </summary>
    /// <remarks>
    /// This is because XPath treats the empty prefix as the null namespace.
    /// In other words, only prefixes mapped to namespaces can be used in
    /// XPath queries. This means that if you want to query against a
    /// namespace in an XML document, even if it is the default namespace,
    /// you need to define a prefix for it.
    /// <para>So, if for instance you have a TEI document with a default
    /// namespace <c>xmlns="http://www.tei-c.org/ns/1.0"</c>, and you
    /// define mappings with XPath queries like <c>//body</c>, nothing
    /// will be found. If instead you set <see cref="DefaultNsPrefix"/>
    /// to <c>tei</c> and then use this prefix in the mappings, like
    /// <c>//tei:body</c>, this will find the element.</para>
    /// <para>See https://stackoverflow.com/questions/585812/using-xpath-with-default-namespace-in-c-sharp.
    /// </para>
    /// </remarks>
    public string? DefaultNsPrefix { get; set; }

    /// <summary>
    /// Gets or sets the hit element used to wrap highlighted text, e.g.
    /// <c>&lt;tei:hi rend="hit"&gt;&lt;/tei:hi&gt;</c>.
    /// </summary>
    public string? HitElement { get; set; }

    /// <summary>
    /// Gets or sets the wrapper prefix. This is used when the context spans
    /// for more than a single node, so that we may need a container element
    /// to wrap them into a single-rooted XML context.
    /// </summary>
    public string? WrapperPrefix { get; set; }

    /// <summary>
    /// Gets or sets the wrapper suffix, closing the wrapper element opened
    /// by <see cref="WrapperPrefix"/>.
    /// </summary>
    public string? WrapperSuffix { get; set; }
}
