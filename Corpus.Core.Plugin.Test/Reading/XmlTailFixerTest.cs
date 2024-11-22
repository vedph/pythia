using Corpus.Core.Plugin.Reading;
using Xunit;

namespace Corpus.Core.Plugin.Test.Reading;

public sealed class XmlTailFixerTest
{
    [Fact]
    public void GetTail_Null_Empty()
    {
        string s = XmlTailFixer.GetTail(null);
        Assert.Equal("", s);
    }

    [Fact]
    public void GetTail_Empty_Empty()
    {
        string s = XmlTailFixer.GetTail("");
        Assert.Equal("", s);
    }

    [Fact]
    public void GetTail_Spaces_Empty()
    {
        string s = XmlTailFixer.GetTail("  ");
        Assert.Equal("", s);
    }

    [Fact]
    public void GetTail_Wellformed_Empty()
    {
        string s = XmlTailFixer.GetTail(
            "<html>\n" +
            "<head><title>Hello</title></head>\n" +
            "<body><p>world</p></body>\n" +
            "</html>");
        Assert.Equal("", s);
    }

    [Fact]
    public void GetTail_WellformedWithDeclaration_Ok()
    {
        string s = XmlTailFixer.GetTail(
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
            "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\">\n" +
            "<teiHeader></teiHeader>\n" +
            "<text><body><p>world</p></body></text>\n" +
            "</TEI>");
        Assert.Equal("", s);
    }

    [Fact]
    public void GetTail_Malformed_Ok()
    {
        string s = XmlTailFixer.GetTail(
            "<html>\n" +
            "<head><title>Hello</title></head>\n" +
            "<body><p>world");
        Assert.Equal("</p></body></html>", s);
    }
}
