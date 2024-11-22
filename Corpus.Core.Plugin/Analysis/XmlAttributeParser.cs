using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.XPath;
using Corpus.Core.Analysis;
using Fusi.Tools.Configuration;

namespace Corpus.Core.Plugin.Analysis;

/// <summary>
/// XML document's attributes parser. This extracts document's metadata
/// from its XML content (e.g. a <c>teiHeader</c> in a TEI document).
/// <para>Tag: <c>attribute-parser.xml</c>.</para>
/// </summary>
/// <seealso cref="T:Corpus.Core.Analysis.IAttributeParser" />
[Tag("attribute-parser.xml")]
public sealed class XmlAttributeParser : IAttributeParser,
    IConfigurable<XmlAttributeParserOptions>
{
    private string? _defaultNsPrefix;
    private Dictionary<string, string>? _namespaces;

    /// <summary>
    /// Gets the mappings.
    /// </summary>
    public XmlAttributeMappingSet? Mappings { get; private set; }

    /// <summary>
    /// Configures the object with the specified options.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <exception cref="ArgumentNullException">options</exception>
    public void Configure(XmlAttributeParserOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _defaultNsPrefix = options.DefaultNsPrefix;

        // namespaces
        if (options.Namespaces?.Count > 0)
            _namespaces = XmlNsOptionHelper.ParseNamespaces(options.Namespaces);

        // mappings
        Mappings = new XmlAttributeMappingSet();
        foreach (string s in options.Mappings ?? Array.Empty<string>())
            Mappings.Mappings.Add(XmlAttributeMapping.Parse(s)!);
    }

    /// <summary>
    /// Parses the text from the specified reader.
    /// </summary>
    /// <param name="reader">The text reader.</param>
    /// <param name="document">The document being parsed. Not used.</param>
    /// <returns>List of attributes extracted from the text.</returns>
    /// <exception cref="ArgumentNullException">reader</exception>
    public IList<Attribute> Parse(TextReader reader, IDocument document)
    {
        ArgumentNullException.ThrowIfNull(reader);

        if (Mappings == null) return Array.Empty<Attribute>();

        string xml = reader.ReadToEnd();

        // get all the namespaces in document
        var nsmgr = XmlNsOptionHelper.GetDocNamespacesManager(
            xml, _defaultNsPrefix, _namespaces);

        using StringReader sr = new(xml);
        XPathDocument xdoc = new(
            XmlReader.Create(sr, new XmlReaderSettings()),
            XmlSpace.Preserve);
        return Mappings.Extract(xdoc, nsmgr);
    }

    /// <summary>
    /// Returns a <see cref="string" /> that represents this instance.
    /// </summary>
    /// <returns>
    /// A <see cref="string" /> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        return $"{nameof(XmlAttributeParser)}: {Mappings}";
    }
}

/// <summary>
/// XML attribute parser options.
/// </summary>
public class XmlAttributeParserOptions
{
    /// <summary>
    /// Gets or sets the mappings. You can use namespaces prefixes, as far
    /// as they are found in the documents, or added in <see cref="Namespaces"/>.
    /// </summary>
    public IList<string>? Mappings { get; set; }

    /// <summary>
    /// Gets or sets a set of optional key=namespace URI pairs. Each string
    /// has format <c>prefix=namespace</c>. When dealing with documents with
    /// namespaces, add all the prefixes you will use in <see cref="Mappings"/>,
    /// unless the prefixes with their namespaces can be got from the XML
    /// documents being processed.
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
}
