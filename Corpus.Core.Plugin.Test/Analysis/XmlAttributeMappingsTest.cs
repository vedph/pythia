using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.XPath;
using Corpus.Core.Plugin.Analysis;
using Xunit;

namespace Corpus.Core.Plugin.Test.Analysis;

public sealed class XmlAttributeMappingsTest
{
    private static XPathDocument GetXPathDocument(string name)
    {
        return new XPathDocument(
            new StreamReader(typeof(XmlAttributeMappingsTest).GetTypeInfo()
            .Assembly.GetManifestResourceStream(
                $"Corpus.Core.Plugin.Test.Assets.{name}")!,
            Encoding.UTF8));
    }

    [Fact]
    public void Extract_TitleAndDate_Ok()
    {
        XmlAttributeMappingSet mappings = new();
        mappings.Mappings.Add(XmlAttributeMapping.Parse(
            "title=/TEI/teiHeader/fileDesc/titleStmt/title")!);
        mappings.Mappings.Add(XmlAttributeMapping.Parse(
            "year=/TEI/teiHeader/fileDesc/editionStmt/edition/date " +
            "[N] (\\d{4})\\s*$")!);

        XPathDocument doc = GetXPathDocument("SampleMetadata.xml");

        IList<Attribute> attributes = mappings.Extract(doc,
            XmlNsOptionHelper.GetDocNamespacesManager(
                doc.CreateNavigator().OuterXml));

        Attribute? attribute = attributes.FirstOrDefault(
            a => a.Name == "title");
        Assert.NotNull(attribute);
        Assert.Equal("La Scienza in cucina e l'arte di mangiar bene",
            attribute.Value);
        Assert.Equal(AttributeType.Text, attribute.Type);

        attribute = attributes.FirstOrDefault(a => a.Name == "year");
        Assert.NotNull(attribute);
        Assert.Equal("1891", attribute.Value);
        Assert.Equal(AttributeType.Number, attribute.Type);
    }

    [Fact]
    public void Extract_TitleAndDateWithNs_Ok()
    {
        XmlAttributeMappingSet mappings = new();

        mappings.Mappings.Add(XmlAttributeMapping.Parse(
            "title=/tei:TEI/tei:teiHeader/tei:fileDesc/tei:titleStmt/tei:title")!);
        mappings.Mappings.Add(XmlAttributeMapping.Parse(
            "year=/tei:TEI/tei:teiHeader/tei:fileDesc/tei:editionStmt/tei:edition/tei:date " +
            "[N] (\\d{4})\\s*$")!);

        XPathDocument doc = GetXPathDocument("SampleMetadataNs.xml");

        IList<Attribute> attributes = mappings.Extract(doc,
            XmlNsOptionHelper.GetDocNamespacesManager(
                doc.CreateNavigator().OuterXml, "tei"));

        Attribute? attribute = attributes.FirstOrDefault(
            a => a.Name == "title");
        Assert.NotNull(attribute);
        Assert.Equal("La Scienza in cucina e l'arte di mangiar bene",
            attribute.Value);
        Assert.Equal(AttributeType.Text, attribute.Type);

        attribute = attributes.FirstOrDefault(a => a.Name == "year");
        Assert.NotNull(attribute);
        Assert.Equal("1891", attribute.Value);
        Assert.Equal(AttributeType.Number, attribute.Type);
    }

    [Fact]
    public void Extract_FirstMatchingDate_Ok()
    {
        XmlAttributeMappingSet mappings = new();
        mappings.Mappings.Add(XmlAttributeMapping.Parse(
            "year=/TEI/teiHeader/fileDesc/titleStmt/date/@when " +
            "[N] \\b[12]\\d{3}\\b")!);
        mappings.Mappings.Add(XmlAttributeMapping.Parse(
            "year=/TEI/teiHeader/fileDesc/editionStmt/edition/date/@when " +
            "[N] \\b[12]\\d{3}\\b")!);

        XPathDocument doc = GetXPathDocument("SampleMetadata.xml");

        IList<Attribute> attributes = mappings.Extract(doc,
            XmlNsOptionHelper.GetDocNamespacesManager(
                doc.CreateNavigator().OuterXml));

        Attribute? attribute = attributes.SingleOrDefault(a => a.Name == "year");
        Assert.NotNull(attribute);
        Assert.Equal("1891", attribute.Value);
        Assert.Equal(AttributeType.Number, attribute.Type);
    }

    [Fact]
    public void Extract_FirstMatchingDateWithNs_Ok()
    {
        XmlAttributeMappingSet mappings = new();
        mappings.Mappings.Add(XmlAttributeMapping.Parse(
            "year=/tei:TEI/tei:teiHeader/tei:fileDesc/tei:titleStmt/tei:date/@when " +
            "[N] \\b[12]\\d{3}\\b")!);
        mappings.Mappings.Add(XmlAttributeMapping.Parse(
            "year=/tei:TEI/tei:teiHeader/tei:fileDesc/tei:editionStmt/tei:edition/tei:date/@when " +
            "[N] \\b[12]\\d{3}\\b")!);

        XPathDocument doc = GetXPathDocument("SampleMetadataNs.xml");

        IList<Attribute> attributes = mappings.Extract(doc,
            XmlNsOptionHelper.GetDocNamespacesManager(
                doc.CreateNavigator().OuterXml, "tei"));

        Attribute? attribute = attributes.SingleOrDefault(a => a.Name == "year");
        Assert.NotNull(attribute);
        Assert.Equal("1891", attribute.Value);
        Assert.Equal(AttributeType.Number, attribute.Type);
    }
}
