using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Corpus.Core.Plugin.Analysis;
using Corpus.Core.Reading;
using Fusi.Tools.Configuration;
using Fusi.Xml;
using Fusi.Xml.Extras.XPathDiscovery;

namespace Corpus.Core.Plugin.Reading;

/// <summary>
/// A generic XML text mapper based on XPath expressions.
/// <para>Tag: <c>text-mapper.xml</c>.</para>
/// </summary>
/// <seealso cref="ITextMapper" />
[Tag("text-mapper.xml")]
public sealed class XmlTextMapper : ITextMapper,
    IConfigurable<XmlTextMapperOptions>
{
    private IList<HierarchicXmlStructureDefinition>? _definitions;
    private HierarchicXmlStructureDefinition? _defRoot;
    private IDictionary<string, string>? _namespaces;
    private string? _defaultNsPrefix;

    /// <summary>
    /// Configures the object with the specified options.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <exception cref="ArgumentNullException">options</exception>
    /// <exception cref="InvalidOperationException">Invalid XML text mapper
    /// definitions</exception>
    public void Configure(XmlTextMapperOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        // read prefix=namespace pairs if any
        _namespaces = XmlNsOptionHelper.ParseNamespaces(options.Namespaces);

        _defaultNsPrefix = options.DefaultNsPrefix;

        // definitions
        _definitions = options.Definitions;

        // arrange the definitions in the tree which will be used to build the map
        if (_definitions?.Count > 0)
        {
            _defRoot = _definitions.FirstOrDefault(
                d => string.IsNullOrEmpty(d.ParentName));
            if (_defRoot == null)
            {
                throw new InvalidOperationException(
                    "Invalid XML text mapper definitions: missing root");
            }

            foreach (HierarchicXmlStructureDefinition child in _definitions
                .Where(d => !string.IsNullOrEmpty(d.ParentName)))
            {
                var parent = _definitions.FirstOrDefault(
                    d => d.Name == child.ParentName) ??
                    throw new InvalidOperationException(
                        "Invalid XML text mapper definitions: " +
                        $"{child.Name} has unknown parent {child.ParentName}");

                parent.Children.Add(child);
            }
        }
        else
        {
            _defRoot = null;
        }
    }

    private TextMapNode? AddNode(XDocument doc, string text,
        XElement target,
        HierarchicXmlStructureDefinition nodeDef,
        TextMapNode? parentNode,
        XmlNamespaceManager nsmgr)
    {
        // create and add node
        IXmlLineInfo info = target;
        int offset = OffsetHelper.GetOffset(text, info.LineNumber,
            info.LinePosition - 1);

        TextMapNode node = new()
        {
            Label = nodeDef.GetStructureValue(target, nsmgr),
            Location = target.GetXPath(_defaultNsPrefix),
            StartIndex = offset,
            EndIndex = OffsetHelper.GetElementEndOffset(text, offset)
        };
        if (nodeDef.DiscardEmptyValue && node.Label == null)
            return null;

        if (string.IsNullOrEmpty(node.Label))
            node.Label = nodeDef.DefaultValue ?? "";

        parentNode?.Add(node);

        // recursively process its children
        foreach (HierarchicXmlStructureDefinition childDef in nodeDef.Children)
        {
            foreach (XElement childElem in
                target.XPathSelectElements(childDef.XPath!, nsmgr))
            {
                AddNode(doc, text, childElem, childDef, node, nsmgr);
            }
        }

        return node;
    }

    /// <summary>
    /// Maps the specified document text.
    /// </summary>
    /// <param name="text">The text to map.</param>
    /// <param name="attributes">The optional attributes of the document
    /// the text belongs to. These can be used by the mapper to decide the
    /// mapping strategy: for instance, a poetic text might be mapped
    /// differently from a prose text.</param>
    /// <returns>the root node of the generated map, or null if mapper was
    /// not configured or the map was empty</returns>
    /// <exception cref="ArgumentNullException">text</exception>
    public TextMapNode? Map(string text,
        IReadOnlyDictionary<string, string>? attributes = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (_definitions == null || _definitions.Count == 0) return null;

        // parse XML from the received text
        XDocument doc = XDocument.Parse(text,
            LoadOptions.SetLineInfo | LoadOptions.PreserveWhitespace);
        if (doc.Root == null) return null;

        // load namespaces from both document and options
        XmlNamespaceManager nsmgr =
            XmlNsOptionHelper.GetDocNamespacesManager(
                text, _defaultNsPrefix, _namespaces);

        // search each defined structure in the document, starting
        // from the root element (which by definition must be single)
        XElement? target = doc.XPathSelectElement(_defRoot!.XPath!, nsmgr);
        if (target == null) return null;

        return AddNode(doc, text, target, _defRoot, null, nsmgr);
    }
}

/// <summary>
/// Options for <see cref="XmlTextMapper"/>'s.
/// </summary>
public class XmlTextMapperOptions
{
    /// <summary>
    /// Gets or sets the definitions.
    /// </summary>
    public IList<HierarchicXmlStructureDefinition>? Definitions { get; set; }

    /// <summary>
    /// Gets or sets a set of optional key=namespace URI pairs. Each string
    /// has format <c>prefix=namespace</c>. When dealing with documents with
    /// namespaces, add all the prefixes you will use in
    /// <see cref="Definitions"/> here, so that they will be expanded
    /// before processing.
    /// </summary>
    public IList<string>? Namespaces { get; set; }

    /// <summary>
    /// Gets or sets the default namespace prefix, used to build XPath
    /// locations for map nodes. This is required when you have documents
    /// with a default empty-prefix namespace (xmlns="URI"), because XPath
    /// requires a prefix anyway.
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
}
