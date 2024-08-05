using Pythia.Core.Analysis;
using Pythia.Core.Plugin.Analysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Pythia.Core.Plugin.Test.Analysis;

public sealed class PosTaggingXmlTokenizerTest
{
    private static PosTaggingXmlTokenizer GetTokenizer(bool filters = false)
    {
        ITokenizer inner = new StandardTokenizer();
        if (filters) inner.Filters.Add(new AlnumAposTokenFilter());
        PosTaggingXmlTokenizer tokenizer = new();
        tokenizer.SetInnerTokenizer(inner);
        tokenizer.SetTagger(new MockTokenPosTagger());
        return tokenizer;
    }

    [Fact]
    public async Task Tokenize_NoText_Ok()
    {
        PosTaggingXmlTokenizer tokenizer = GetTokenizer();
        tokenizer.Start(new StringReader("<TEI><text></text></TEI>"), 1);
        Assert.False(await tokenizer.NextAsync());
    }

    [Fact]
    public async Task Tokenize_SingleTextNode_NoPunct_Ok()
    {
        PosTaggingXmlTokenizer tokenizer = GetTokenizer();
        const string xml = "<TEI><text>" +
            "<body>" +
            "<head>Hello, world</head>" +
            "<div></div>" +
            "</body></text></TEI>";
        tokenizer.Start(new StringReader(xml), 1);

        string[] expected =
        [
            "Hello,", "world"
        ];
        int i = 0;
        while (await tokenizer.NextAsync())
        {
            TextSpan token = tokenizer.CurrentToken;
            Assert.Equal(expected[i], token.Value);
            Assert.Equal(expected[i], xml.Substring(token.Index, token.Length));
            i++;
            Assert.Equal(i, tokenizer.CurrentToken.P1);
            Corpus.Core.Attribute? pos = tokenizer.CurrentToken.Attributes?
                .FirstOrDefault(a => a.Name == "pos");
            Assert.NotNull(pos);
            Assert.Equal($"1.{i}", pos.Value);
        }
    }

    [Fact]
    public async Task Tokenize_SingleTextNode_EndingWithPunct_Ok()
    {
        PosTaggingXmlTokenizer tokenizer = GetTokenizer();
        const string xml = "<TEI><text>" +
            "<body>" +
            "<head>Hello, world!</head>" +
            "<div></div>" +
            "</body></text></TEI>";
        tokenizer.Start(new StringReader(xml), 1);

        string[] expected =
        [
            "Hello,", "world!"
        ];
        int i = 0;
        while (await tokenizer.NextAsync())
        {
            TextSpan token = tokenizer.CurrentToken;
            Assert.Equal(expected[i], token.Value);
            Assert.Equal(expected[i], xml.Substring(token.Index, token.Length));
            i++;
            Assert.Equal(i, tokenizer.CurrentToken.P1);
            Corpus.Core.Attribute? pos = tokenizer.CurrentToken.Attributes?
                .FirstOrDefault(a => a.Name == "pos");
            Assert.NotNull(pos);
            Assert.Equal($"1.{i}", pos.Value);
        }
    }

    [Fact]
    public async Task Tokenize_MultipleTextNodes_Ok()
    {
        PosTaggingXmlTokenizer tokenizer = GetTokenizer();
        tokenizer.Configure(new PosTaggingXmlTokenizerOptions
        {
            StopTags = ["head"]
        });

        const string xml = "<TEI><text>" +
            "<body>" +
            "<head>Title</head>" +
            "<div>" +
            "<p>This is paragraph 1.</p>" +
            "<p>End.</p>" +
            "</div>" +
            "</body></text></TEI>";
        tokenizer.Start(new StringReader(xml), 1);

        Tuple<string,string>[] expected =
        [
            Tuple.Create("Title", "1.1"),
            Tuple.Create("This", "2.1"),
            Tuple.Create("is", "2.2"),
            Tuple.Create("paragraph", "2.3"),
            Tuple.Create("1.", "2.4"),
            Tuple.Create("End.", "3.1")
        ];
        int i = 0;
        while (await tokenizer.NextAsync())
        {
            TextSpan token = tokenizer.CurrentToken;
            Assert.Equal(expected[i].Item1, token.Value);
            Assert.Equal(expected[i].Item1, xml.Substring(token.Index, token.Length));

            Corpus.Core.Attribute? pos = tokenizer.CurrentToken.Attributes?
                .FirstOrDefault(a => a.Name == "pos");
            Assert.NotNull(pos);
            Assert.Equal(expected[i].Item2, pos.Value);
            i++;
            Assert.Equal(i, tokenizer.CurrentToken.P1);
        }
    }

    [Fact]
    public async Task Tokenize_MultipleTextNodesWithFilters_Ok()
    {
        PosTaggingXmlTokenizer tokenizer = GetTokenizer(true);
        tokenizer.Configure(new PosTaggingXmlTokenizerOptions
        {
            StopTags = ["head"]
        });
        const string xml = "<TEI><text>" +
            "<body>" +
            "<head>Title</head>" +
            "<div>" +
            "<p>This is paragraph 1.</p>" +
            "<p>End.</p>" +
            "</div>" +
            "</body></text></TEI>";
        tokenizer.Start(new StringReader(xml), 1);

        Tuple<string, string, string>[] expected =
        [
            Tuple.Create("Title", "Title", "1.1"),
            Tuple.Create("This", "This", "2.1"),
            Tuple.Create("is", "is", "2.2"),
            Tuple.Create("paragraph", "paragraph", "2.3"),
            Tuple.Create("1", "1.", "2.4"),
            Tuple.Create("End", "End.", "3.1")
        ];
        int i = 0;
        while (await tokenizer.NextAsync())
        {
            TextSpan token = tokenizer.CurrentToken;
            Assert.Equal(expected[i].Item1, token.Value);
            Assert.Equal(expected[i].Item2, xml.Substring(token.Index, token.Length));

            Corpus.Core.Attribute? pos = tokenizer.CurrentToken.Attributes?
                .FirstOrDefault(a => a.Name == "pos");
            Assert.NotNull(pos);
            Assert.Equal(expected[i].Item3, pos.Value);
            i++;
            Assert.Equal(i, tokenizer.CurrentToken.P1);
        }
    }

    [Fact]
    public async Task Tokenize_MultipleTextNodesWithEmptyToken_Ok()
    {
        PosTaggingXmlTokenizer tokenizer = GetTokenizer(true);
        tokenizer.Configure(new PosTaggingXmlTokenizerOptions
        {
            StopTags = ["head"]
        });
        const string xml = "<TEI><text>" +
            "<body>" +
            "<head>Title</head>" +
            "<div>" +
            "<p>This is a <hi>paragraph</hi>. End.</p>" +
            "</div>" +
            "</body></text></TEI>";
        tokenizer.Start(new StringReader(xml), 1);

        Tuple<string, string, string>[] expected =
        [
            Tuple.Create("Title", "Title", "1.1"),
            Tuple.Create("This", "This", "2.1"),
            Tuple.Create("is", "is", "2.2"),
            Tuple.Create("a", "a", "2.3"),
            Tuple.Create("paragraph", "paragraph", "2.4"),
            Tuple.Create("End", "End.", "3.1")
        ];
        int i = 0;
        while (await tokenizer.NextAsync())
        {
            TextSpan token = tokenizer.CurrentToken;
            Assert.Equal(expected[i].Item1, token.Value);
            Assert.Equal(expected[i].Item2, xml.Substring(token.Index, token.Length));

            Corpus.Core.Attribute? pos = tokenizer.CurrentToken.Attributes?
                .FirstOrDefault(a => a.Name == "pos");
            Assert.NotNull(pos);
            Assert.Equal(expected[i].Item3, pos.Value);
            i++;
            Assert.Equal(i, tokenizer.CurrentToken.P1);
        }
    }

    [Fact]
    public async Task Tokenize_MultipleTextNodes_Deferred_Ok()
    {
        PosTaggingXmlTokenizer tokenizer = GetTokenizer();
        tokenizer.SetTagger(null);

        tokenizer.Configure(new PosTaggingXmlTokenizerOptions
        {
            StopTags = ["head"]
        });

        const string xml = "<TEI><text>" +
            "<body>" +
            "<head>Title</head>" +
            "<div>" +
            "<p>This is paragraph 1.</p>" +
            "<p>End.</p>" +
            "</div>" +
            "</body></text></TEI>";
        tokenizer.Start(new StringReader(xml), 1);

        Tuple<string, string>[] expected =
        [
            Tuple.Create("Title", "1"),
            Tuple.Create("This", ""),
            Tuple.Create("is", ""),
            Tuple.Create("paragraph", ""),
            Tuple.Create("1.", "4"),
            Tuple.Create("End.", "1")
        ];
        int i = 0;
        while (await tokenizer.NextAsync())
        {
            TextSpan token = tokenizer.CurrentToken;
            Assert.Equal(expected[i].Item1, token.Value);
            Assert.Equal(expected[i].Item1, xml.Substring(token.Index, token.Length));

            Corpus.Core.Attribute? s0 = tokenizer.CurrentToken.Attributes?
                .FirstOrDefault(a => a.Name == "s0");
            if (expected[i].Item2.Length == 0) Assert.Null(s0);
            else Assert.Equal(expected[i].Item2, s0!.Value);

            i++;
            Assert.Equal(i, tokenizer.CurrentToken.P1);
        }
    }
}

internal sealed class MockTokenPosTagger : ITokenPosTagger
{
    private int _sentenceNr;

    public void Tag(IList<TextSpan> tokens, string tagName)
    {
        _sentenceNr++;
        int n = 0;
        foreach (TextSpan token in tokens)
        {
            token.AddAttribute(new Corpus.Core.Attribute
            {
                Name = "pos",
                Value = $"{_sentenceNr}.{++n}"
            });
        }
    }
}
