using System.Xml.Linq;
using Xunit;
using Corpus.Core.Plugin.Reading;
using System;

namespace Corpus.Core.Plugin.Test.Reading;

public sealed class XmlHighlighterTest
{
    private static string GetNormalizedString(string text) =>
        text.Replace("\r\n", "").Replace("\n", "");

    [Fact]
    public void WrapHighlightedText_NoHighlights_Unchanged()
    {
        XmlHighlighter highlighter = new();
        const string xmlInput = "<div><p>Nothing to highlight here</p></div>";

        XDocument doc = XDocument.Parse(xmlInput, LoadOptions.PreserveWhitespace);

        highlighter.WrapHighlightedText(doc);

        Assert.Equal(xmlInput, doc.ToString(SaveOptions.DisableFormatting));
    }

    [Fact]
    public void WrapHighlightedText_SameTextNode()
    {
        XmlHighlighter highlighter = new();
        const string xmlInput = "<div><p>The {{fool}} said something</p></div>";
        XDocument doc = XDocument.Parse(xmlInput, LoadOptions.PreserveWhitespace);

        highlighter.WrapHighlightedText(doc);

        const string expectedXml =
            "<div><p>The <hi rend=\"hit\">fool</hi> said something</p></div>";
        Assert.Equal(expectedXml,
            GetNormalizedString(doc.ToString(SaveOptions.DisableFormatting)));
    }

    [Fact]
    public void WrapHighlightedText_MultipleInSameTextNode()
    {
        XmlHighlighter highlighter = new();
        const string xmlInput = "<div><p>The {{foo}} and {{bar}} " +
            "are here</p></div>";
        XDocument doc = XDocument.Parse(xmlInput, LoadOptions.PreserveWhitespace);

        highlighter.WrapHighlightedText(doc);

        const string expectedXml = "<div><p>The <hi rend=\"hit\">foo</hi> " +
            "and <hi rend=\"hit\">bar</hi> are here</p></div>";
        string actualXml = GetNormalizedString(doc.ToString(
            SaveOptions.DisableFormatting));
        Assert.Equal(expectedXml, actualXml);
    }

    [Fact]
    public void WrapHighlightedText_NestedElements()
    {
        XmlHighlighter highlighter = new();
        const string xmlInput = "<div><p>The {{fool <persName>Joe</persName> " +
            "said}} something</p></div>";
        XDocument doc = XDocument.Parse(xmlInput, LoadOptions.PreserveWhitespace);

        highlighter.WrapHighlightedText(doc);

        const string expectedXml = "<div><p>The <hi rend=\"hit\">fool </hi>" +
            "<persName><hi rend=\"hit\">Joe</hi></persName><hi rend=\"hit\"> said</hi> " +
            "something</p></div>";
        Assert.Equal(expectedXml,
            GetNormalizedString(doc.ToString(SaveOptions.DisableFormatting)));
    }

    [Fact]
    public void WrapHighlightedText_CustomEscapes()
    {
        XmlHighlighter highlighter = new()
        {
            OpeningEscape = "[[",
            ClosingEscape = "]]",
            HiElement = new XElement("highlight", new XAttribute("type", "custom"))
        };
        const string xmlInput = "<div><p>The [[foo]] said something</p></div>";
        XDocument doc = XDocument.Parse(xmlInput, LoadOptions.PreserveWhitespace);

        highlighter.WrapHighlightedText(doc);

        const string expectedXml = "<div><p>The <highlight type=\"custom\">" +
            "foo</highlight> said something</p></div>";
        Assert.Equal(expectedXml,
                     GetNormalizedString(doc.ToString(SaveOptions.DisableFormatting)));
    }

    [Fact]
    public void WrapHighlightedText_ComplexNesting()
    {
        XmlHighlighter highlighter = new();
        const string xmlInput = "<div>" +
            "<p>The {{fool <persName>Joe</persName> said: <q>Here I am!</q></p>" +
            "<p>And went}} away.</p>" +
            "</div>";

        XDocument doc = XDocument.Parse(xmlInput, LoadOptions.PreserveWhitespace);

        highlighter.WrapHighlightedText(doc);

        const string expectedXml = "<div>" +
            "<p>The <hi rend=\"hit\">fool </hi><persName>" +
            "<hi rend=\"hit\">Joe</hi></persName>" +
            "<hi rend=\"hit\"> said: </hi>" +
            "<q><hi rend=\"hit\">Here I am!</hi></q></p>" +
            "<p><hi rend=\"hit\">And went</hi> away.</p>" +
            "</div>";
        string actualXml = GetNormalizedString(doc.ToString(
            SaveOptions.DisableFormatting));
        Assert.Equal(expectedXml, actualXml);
    }

    [Fact]
    public void OpeningEscape_CannotBeNull()
    {
        XmlHighlighter highlighter = new();

        Assert.Throws<ArgumentNullException>(() =>
            highlighter.OpeningEscape = null!);
    }

    [Fact]
    public void ClosingEscape_CannotBeNull()
    {
        XmlHighlighter highlighter = new();

        Assert.Throws<ArgumentNullException>(() =>
            highlighter.ClosingEscape = null!);
    }

    [Fact]
    public void HiElement_CannotBeNull()
    {
        XmlHighlighter highlighter = new();

        Assert.Throws<ArgumentNullException>(()
            => highlighter.HiElement = null!);
    }
}
