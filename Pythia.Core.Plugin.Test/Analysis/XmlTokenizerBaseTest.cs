using Pythia.Core.Analysis;
using Pythia.Core.Plugin.Analysis;
using System.IO;
using Xunit;

namespace Pythia.Core.Plugin.Test.Analysis;

public sealed class XmlTokenizerBaseTest
{
    private NullXmlTokenizer GetTokenizer(bool filters = false)
    {
        ITokenizer inner = new StandardTokenizer();
        if (filters) inner.Filters.Add(new AlnumAposTokenFilter());

        NullXmlTokenizer tokenizer = new();
        tokenizer.SetInnerTokenizer(inner);
        return tokenizer;
    }

    [Fact]
    public void Tokenize_NoText_Ok()
    {
        NullXmlTokenizer tokenizer = GetTokenizer();
        tokenizer.Start(new StringReader("<TEI><text></text></TEI>"), 1);
        Assert.False(tokenizer.Next());
    }

    [Fact]
    public void Tokenize_SingleTextNode_Ok()
    {
        NullXmlTokenizer tokenizer = GetTokenizer();
        tokenizer.Start(new StringReader("<TEI><text>" +
            "<body>" +
            "<head>Hello, world!</head>" +
            "<div></div>" +
            "</body></text></TEI>"), 1);

        string[] expected = new[]
        {
            "Hello,", "world!"
        };
        int i = 0;
        while (tokenizer.Next())
        {
            Assert.Equal(expected[i], tokenizer.CurrentToken.Value);
            i++;
            Assert.Equal(i, tokenizer.CurrentToken.Position);
        }
    }

    [Fact]
    public void Tokenize_MultipleTextNodes_Ok()
    {
        NullXmlTokenizer tokenizer = GetTokenizer();
        tokenizer.Start(new StringReader("<TEI><text>" +
            "<body>" +
            "<head>Title</head>" +
            "<div>" +
            "<p>This is paragraph 1.</p>" +
            "<p>End.</p>" +
            "</div>" +
            "</body></text></TEI>"), 1);

        string[] expected = new[]
        {
            "Title", "This", "is", "paragraph", "1.",
            "End."
        };
        int i = 0;
        while (tokenizer.Next())
        {
            Assert.Equal(expected[i], tokenizer.CurrentToken.Value);
            i++;
            Assert.Equal(i, tokenizer.CurrentToken.Position);
        }
    }

    [Fact]
    public void Tokenize_MixedTextNodes_Ok()
    {
        NullXmlTokenizer tokenizer = GetTokenizer();
        tokenizer.Start(new StringReader("<TEI><text>" +
            "<body>" +
            "<head>Title</head>" +
            "<div>This is a <hi rend=\"bold\">test</hi>, stop.</div>" +
            "</body></text></TEI>"), 1);

        string[] expected = new[]
        {
            "Title", "This", "is", "a", "test", ",", "stop."
        };
        int i = 0;
        while (tokenizer.Next())
        {
            Assert.Equal(expected[i], tokenizer.CurrentToken.Value);
            i++;
            Assert.Equal(i, tokenizer.CurrentToken.Position);
        }
    }

    [Fact]
    public void Tokenize_MixedTextNodesWithFilters_Ok()
    {
        NullXmlTokenizer tokenizer = GetTokenizer(true);
        tokenizer.Start(new StringReader("<TEI><text>" +
            "<body>" +
            "<head>Title</head>" +
            "<div>This is a <hi rend=\"bold\">test</hi>, stop.</div>" +
            "</body></text></TEI>"), 1);

        string[] expected = new[]
        {
            "Title", "This", "is", "a", "test", "stop"
        };
        int i = 0;
        while (tokenizer.Next())
        {
            Assert.Equal(expected[i], tokenizer.CurrentToken.Value);
            i++;
            Assert.Equal(i, tokenizer.CurrentToken.Position);
        }
    }
}

internal sealed class NullXmlTokenizer : XmlTokenizerBase
{
}
