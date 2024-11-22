using System;
using System.Collections.Generic;
using Corpus.Core.Plugin.Reading;
using Corpus.Core.Reading;
using Xunit;

namespace Corpus.Core.Plugin.Test.Reading;

public sealed class XmlTextMapperTest
{
    [Fact]
    public void Map_TextAndPoems_Ok()
    {
        string text = TestHelper.LoadResourceText("MappedDoc.xml");
        XmlTextMapper mapper = new();
        mapper.Configure(new XmlTextMapperOptions
        {
            Definitions = new HierarchicXmlStructureDefinition[]
            {
                // TEI/text/body (root)
                new HierarchicXmlStructureDefinition
                {
                    Name = "root",
                    XPath = "/tei:TEI/tei:text/tei:body",
                    ValueTemplate = "map"
                },
                // TEI/text/body/div (poems)
                new HierarchicXmlStructureDefinition
                {
                    Name = "poem",
                    ParentName = "root",
                    XPath = "./tei:div",
                    DefaultValue = "poem",
                    ValueTemplate = "{type}{$_}{n}",
                    ValueTemplateArgs = new[]
                    {
                        new XmlStructureValueArg("type", "./@type"),
                        new XmlStructureValueArg("n", "./@n")
                    }
                }
            },
            Namespaces = new[]
            {
                "tei=http://www.tei-c.org/ns/1.0"
            }
        });

        TextMapNode root = mapper.Map(text, new Dictionary<string, string>
        {
            ["title"] = "Sample"
        })!;

        string NL = Environment.NewLine;
        string dump = root.DumpTree();
        Assert.Equal(
            "map [330-913] /TEI[1]/text[1]/body[1]" + NL +
            ".poem 11 [340-629] /TEI[1]/text[1]/body[1]/div[1]" + NL +
            ".poem 51 [633-904] /TEI[1]/text[1]/body[1]/div[2]" + NL,
            dump);
    }

    [Fact]
    public void Map_TextPoemsStanzas_Ok()
    {
        string text = TestHelper.LoadResourceText("MappedDoc.xml");

        XmlTextMapper mapper = new();
        mapper.Configure(new XmlTextMapperOptions
        {
            Definitions = new HierarchicXmlStructureDefinition[]
            {
                // TEI/text/body (root)
                new HierarchicXmlStructureDefinition
                {
                    Name = "root",
                    XPath = "/tei:TEI/tei:text/tei:body",
                    ValueTemplate = "map"
                },
                // TEI/text/body/div (poems)
                new HierarchicXmlStructureDefinition
                {
                    Name = "poem",
                    ParentName = "root",
                    XPath = "./tei:div",
                    DefaultValue = "poem",
                    ValueTemplate = "{type}{$_}{n}",
                    ValueTemplateArgs = new[]
                    {
                        new XmlStructureValueArg("type", "./@type"),
                        new XmlStructureValueArg("n", "./@n")
                    }
                },
                // TEI/text/body/div/div (stanzas)
                new HierarchicXmlStructureDefinition
                {
                    Name = "stanza",
                    ParentName = "poem",
                    XPath = "./tei:div",
                    DefaultValue = "stanza",
                    ValueTemplate = "{type}{$_}{n}",
                    ValueTemplateArgs = new[]
                    {
                        new XmlStructureValueArg("type", "./@type"),
                        new XmlStructureValueArg("n", "./@n")
                    }
                }
            },
            Namespaces = new[]
            {
                "tei=http://www.tei-c.org/ns/1.0"
            }
        });

        TextMapNode root = mapper.Map(text, new Dictionary<string, string>
        {
            ["title"] = "Sample"
        })!;

        string NL = Environment.NewLine;
        string dump = root.DumpTree();
        Assert.Equal(
            "map [330-913] /TEI[1]/text[1]/body[1]" + NL +
            ".poem 11 [340-629] /TEI[1]/text[1]/body[1]/div[1]" + NL +
            "..stanza 6 [370-619] /TEI[1]/text[1]/body[1]/div[1]/div[1]" + NL +
            ".poem 51 [633-904] /TEI[1]/text[1]/body[1]/div[2]" + NL +
            "..stanza 1 [663-894] /TEI[1]/text[1]/body[1]/div[2]/div[1]" + NL,
            dump);
    }

    [Fact]
    public void Map_TeiP_Ok()
    {
        string text = TestHelper.LoadResourceText("TeiSampleDoc.xml");
        XmlTextMapper mapper = new();
        mapper.Configure(new XmlTextMapperOptions
        {
            Definitions = new HierarchicXmlStructureDefinition[]
            {
                // TEI/text/body (root)
                new HierarchicXmlStructureDefinition
                {
                    Name = "body",
                    XPath = "/tei:TEI/tei:text/tei:body",
                    ValueTemplate = "act"
                },
                // TEI/text/body/p (paragraphs)
                new HierarchicXmlStructureDefinition
                {
                    Name = "p",
                    ParentName = "body",
                    XPath = "./tei:p",
                    DefaultValue = "paragraph",
                    ValueTemplate = "{t}",
                    ValueTemplateArgs = new[]
                    {
                        new XmlStructureValueArg("t", ".")
                    },
                    ValueMaxLength = 30,
                    ValueTrimming = true,
                    DiscardEmptyValue = true
                }
            },
            Namespaces = new[]
            {
                "tei=http://www.tei-c.org/ns/1.0"
            },
            DefaultNsPrefix = "tei"
        });

        TextMapNode root = mapper.Map(text)!;

        string NL = Environment.NewLine;
        string dump = root.DumpTree();
        Assert.Equal(
            "act [404-2045] /tei:TEI[1]/tei:text[1]/tei:body[1]" + NL +
            ".ecc.ma eccellentissima CORTE D... [418-653] /tei:TEI[1]/tei:text[1]/tei:body[1]/tei:p[1]" + NL +
            ".Atto di Appello [661-719] /tei:TEI[1]/tei:text[1]/tei:body[1]/tei:p[2]" + NL +
            ".PER [727-782] /tei:TEI[1]/tei:text[1]/tei:body[1]/tei:p[3]" + NL +
            ".CANNOBBIO Guia [790-950] /tei:TEI[1]/tei:text[1]/tei:body[1]/tei:p[4]" + NL +
            ".APPELLANTE [958-1021] /tei:TEI[1]/tei:text[1]/tei:body[1]/tei:p[5]" + NL +
            ".CONTRO [1029-1087] /tei:TEI[1]/tei:text[1]/tei:body[1]/tei:p[6]" + NL +
            ".COMUNARDA S.p.A. Società per A... [1095-1381] /tei:TEI[1]/tei:text[1]/tei:body[1]/tei:p[7]" + NL +
            ".La sig.ra signora Cannobbio Gu... [1411-1679] /tei:TEI[1]/tei:text[1]/tei:body[1]/tei:p[9]" + NL +
            ".PROPONE [1687-1746] /tei:TEI[1]/tei:text[1]/tei:body[1]/tei:p[10]" + NL +
            ".Appello avverso la Sentenza. [1754-1807] /tei:TEI[1]/tei:text[1]/tei:body[1]/tei:p[11]" + NL +
            ".SVOLGIMENTO DEL PROCESSO [1815-1891] /tei:TEI[1]/tei:text[1]/tei:body[1]/tei:p[12]" + NL +
            ".Con atto di citazione del 20.6... [1899-1997] /tei:TEI[1]/tei:text[1]/tei:body[1]/tei:p[13]" + NL +
            ".Salvezze illimitate. [2005-2032] /tei:TEI[1]/tei:text[1]/tei:body[1]/tei:p[14]" + NL,
            dump);
    }
}
