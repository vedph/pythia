using System;
using System.Linq;
using System.Xml.Linq;
using Corpus.Core.Plugin.Reading;
using Corpus.Core.Reading;
using Fusi.Tools;
using Xunit;

namespace Corpus.Core.Plugin.Test.Reading;

public sealed class XmlTextPickerTest
{
    [Fact]
    public void Configure_WithNsPrefix_DoesNotThrow()
    {
        XmlTextPicker picker = new();
        Exception exception = Record.Exception(() => picker.Configure(
            new XmlTextPickerOptions
        {
            Namespaces = ["tei=http://www.tei-c.org/ns/1.0"],
            DefaultNsPrefix = "tei",
            HitElement = "<tei:hi rend=\"hit\"></tei:hi>"
        }));

        Assert.Null(exception);
    }

    private static XmlTextMapper GetXmlTextMapper()
    {
        XmlTextMapper mapper = new();
        mapper.Configure(new XmlTextMapperOptions
        {
            Definitions =
            [
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
                    ValueTemplateArgs =
                    [
                        new XmlStructureValueArg("type", "./@type"),
                        new XmlStructureValueArg("n", "./@n")
                    ]
                }
            ],
            Namespaces =
            [
                "tei=http://www.tei-c.org/ns/1.0"
            ],
            DefaultNsPrefix = "tei"
        });
        return mapper;
    }

    [Fact]
    public void PickNode_Body_Ok()
    {
        string xml = TestHelper.LoadResourceText("MappedDoc.xml");
        XmlTextMapper mapper = GetXmlTextMapper();
        TextMapNode root = mapper.Map(xml)!;
        XmlTextPicker picker = new();
        picker.Configure(new XmlTextPickerOptions
        {
            Namespaces = ["tei=http://www.tei-c.org/ns/1.0"],
            DefaultNsPrefix = "tei"
        });

        TextPiece? piece = picker.PickNode(xml, root, root);

        Assert.NotNull(piece);
        Assert.Equal("map", piece.DocumentMap.Label);

        XElement bodyElem = XElement.Parse(piece.Text,
            LoadOptions.PreserveWhitespace);
        Assert.Equal("body", bodyElem.Name.LocalName);
        Assert.NotNull(bodyElem);
        XNamespace ns = "http://www.tei-c.org/ns/1.0";
        Assert.Equal(2, bodyElem.Elements(ns + "div").Count());
    }

    [Fact]
    public void PickNode_Poem1_Ok()
    {
        string xml = TestHelper.LoadResourceText("MappedDoc.xml");
        XmlTextMapper mapper = GetXmlTextMapper();
        TextMapNode root = mapper.Map(xml)!;
        XmlTextPicker picker = new();
        picker.Configure(new XmlTextPickerOptions
        {
            Namespaces = ["tei=http://www.tei-c.org/ns/1.0"],
            DefaultNsPrefix = "tei"
        });

        TextPiece? piece = picker.PickNode(xml, root, root.GetDescendant("0")!);

        Assert.NotNull(piece);
        Assert.Equal("map", piece.DocumentMap.Label);

        XElement divElem = XElement.Parse(piece.Text, LoadOptions.PreserveWhitespace);
        Assert.Equal("div", divElem.Name.LocalName);
        Assert.Equal("poem", divElem.ReadOptionalAttribute("type", null));
        Assert.Equal("11", divElem.ReadOptionalAttribute("n", null));
    }

    [Fact]
    public void PickNode_Poem2_Ok()
    {
        string xml = TestHelper.LoadResourceText("MappedDoc.xml");
        XmlTextMapper mapper = GetXmlTextMapper();
        TextMapNode root = mapper.Map(xml)!;
        XmlTextPicker picker = new();
        picker.Configure(new XmlTextPickerOptions
        {
            Namespaces = ["tei=http://www.tei-c.org/ns/1.0"],
            DefaultNsPrefix = "tei"
        });

        TextPiece? piece = picker.PickNode(xml, root, root.GetDescendant("1")!);

        Assert.NotNull(piece);
        Assert.Equal("map", piece.DocumentMap.Label);

        XElement divElem = XElement.Parse(piece.Text, LoadOptions.PreserveWhitespace);
        Assert.Equal("div", divElem.Name.LocalName);
        Assert.Equal("poem", divElem.ReadOptionalAttribute("type", null));
        Assert.Equal("51", divElem.ReadOptionalAttribute("n", null));
    }

    private static XmlTextMapper GetTeiTextMapper()
    {
        XmlTextMapper mapper = new();
        mapper.Configure(new XmlTextMapperOptions
        {
            Definitions =
            [
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
                    ValueTemplateArgs =
                    [
                        new XmlStructureValueArg("t", ".")
                    ],
                    ValueMaxLength = 30,
                    ValueTrimming = true,
                    DiscardEmptyValue = true
                }
            ],
            Namespaces =
            [
                "tei=http://www.tei-c.org/ns/1.0"
            ],
            DefaultNsPrefix = "tei"
        });
        return mapper;
    }

    [Fact]
    public void PickNode_TeiBody_Ok()
    {
        string xml = TestHelper.LoadResourceText("TeiSampleDoc.xml");
        XmlTextMapper mapper = GetTeiTextMapper();

        TextMapNode root = mapper.Map(xml)!;
        XmlTextPicker picker = new();
        picker.Configure(new XmlTextPickerOptions
        {
            Namespaces = ["tei=http://www.tei-c.org/ns/1.0"],
            DefaultNsPrefix = "tei"
        });

        TextPiece? piece = picker.PickNode(xml, root, root);

        Assert.NotNull(piece);
        Assert.Equal("act", piece.DocumentMap.Label);

        XElement bodyElem = XElement.Parse(piece.Text,
            LoadOptions.PreserveWhitespace);
        Assert.Equal("body", bodyElem.Name.LocalName);
        Assert.NotNull(bodyElem);
        XNamespace ns = "http://www.tei-c.org/ns/1.0";
        Assert.Equal(14, bodyElem.Elements(ns + "p").Count());
    }

    [Fact]
    public void PickNode_TeiSecondChild_P2()
    {
        string xml = TestHelper.LoadResourceText("TeiSampleDoc.xml");
        XmlTextMapper mapper = GetTeiTextMapper();

        TextMapNode root = mapper.Map(xml)!;
        XmlTextPicker picker = new();
        picker.Configure(new XmlTextPickerOptions
        {
            Namespaces = ["tei=http://www.tei-c.org/ns/1.0"],
            DefaultNsPrefix = "tei"
        });

        TextPiece? piece = picker.PickNode(xml, root, root.Children[1]);

        Assert.NotNull(piece);
        Assert.Equal("Atto di Appello", piece.DocumentMap.Children[1].Label);

        XElement pElem = XElement.Parse(piece.Text,
            LoadOptions.PreserveWhitespace);
        Assert.Equal("p", pElem.Name.LocalName);
        Assert.NotNull(pElem);
    }

    [Fact]
    public void PickContext_TeiOffsetInP2_P2()
    {
        string xml = TestHelper.LoadResourceText("TeiSampleDoc.xml");
        XmlTextMapper mapper = GetTeiTextMapper();

        TextMapNode root = mapper.Map(xml)!;
        XmlTextPicker picker = new();
        picker.Configure(new XmlTextPickerOptions
        {
            Namespaces = ["tei=http://www.tei-c.org/ns/1.0"],
            DefaultNsPrefix = "tei"
        });

        TextPiece? piece = picker.PickContext(xml, root, 695, 702);

        Assert.NotNull(piece);
        Assert.Equal("Atto di Appello", piece.DocumentMap.Children[1].Label);

        XElement pElem = XElement.Parse(piece.Text,
            LoadOptions.PreserveWhitespace);
        Assert.Equal("p", pElem.Name.LocalName);
        Assert.NotNull(pElem);
    }

    [Fact]
    public void PickContext_TeiOffsetSpanningChidrenOfP4_P4()
    {
        string xml = TestHelper.LoadResourceText("TeiSampleDoc.xml");
        XmlTextMapper mapper = GetTeiTextMapper();

        TextMapNode root = mapper.Map(xml)!;
        XmlTextPicker picker = new();
        picker.Configure(new XmlTextPickerOptions
        {
            Namespaces = ["tei=http://www.tei-c.org/ns/1.0"],
            DefaultNsPrefix = "tei"
        });

        TextPiece? piece = picker.PickContext(xml, root, 856, 912);

        Assert.NotNull(piece);
        Assert.Equal("CANNOBBIO Guia", piece.DocumentMap.Children[3].Label);

        XElement pElem = XElement.Parse(piece.Text,
            LoadOptions.PreserveWhitespace);
        Assert.Equal("p", pElem.Name.LocalName);
        Assert.NotNull(pElem);
    }

    [Fact]
    public void PickContext_TeiOffsetSpanningP4P5_Body()
    {
        string xml = TestHelper.LoadResourceText("TeiSampleDoc.xml");
        XmlTextMapper mapper = GetTeiTextMapper();

        TextMapNode root = mapper.Map(xml)!;
        XmlTextPicker picker = new();
        picker.Configure(new XmlTextPickerOptions
        {
            Namespaces = ["tei=http://www.tei-c.org/ns/1.0"],
            DefaultNsPrefix = "tei",
            WrapperPrefix = "<div>",
            WrapperSuffix = "</div>"
        });

        TextPiece? piece = picker.PickContext(xml, root, 856, 1004);

        Assert.NotNull(piece);

        XElement divElem = XElement.Parse(piece.Text,
            LoadOptions.PreserveWhitespace);
        Assert.Equal("div", divElem.Name.LocalName);
        Assert.NotNull(divElem);
    }
}
