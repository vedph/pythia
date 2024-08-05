﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Corpus.Core;
using Corpus.Core.Plugin.Analysis;
using Pythia.Core.Plugin.Analysis;
using Xunit;

namespace Pythia.Core.Plugin.Test.Analysis;

public sealed class XmlSentenceParserTest
{
    private static Document CreateDocument()
    {
        return new Document
        {
            Id = 1,
            Author = "Catullus",
            Title = "Carmina",
            DateValue = -54
        };
    }

    private static XmlSentenceParser CreateParser()
    {
        XmlSentenceParser parser = new();
        parser.Configure(new XmlSentenceParserOptions
        {
            StopTags =
            [
                "div",
                "head",
                "p",
                "body"
            ]
        });
        return parser;
    }

    private static async Task Tokenize(string text, IIndexRepository repository)
    {
        TeiTextFilter filter = new();
        StandardTokenizer tokenizer = new();
        tokenizer.Filters.Add(new ItalianTokenFilter());

        tokenizer.Start(await filter.ApplyAsync(new StringReader(text)), 1);

        List<TextSpan> tokens = [];
        while (await tokenizer.NextAsync())
        {
            tokenizer.CurrentToken.DocumentId = 1;
            tokens.Add(tokenizer.CurrentToken.Clone());
        }
        repository.AddSpans(tokens);
    }

    [Fact]
    public void Parse_Empty_None()
    {
        XmlSentenceParser parser = CreateParser();
        MockIndexRepository repository = new();

        parser.Parse(CreateDocument(), new StringReader(""), null, repository);

        Assert.Empty(repository.Spans);
    }

    [Fact]
    public async Task Parse_ImplicitStop_Ok()
    {
        const string text = "<TEI><text><body><p>Hello there</p></body></text></TEI>";
        XmlSentenceParser parser = CreateParser();
        MockIndexRepository repository = new();
        await Tokenize(text, repository);

        parser.Parse(CreateDocument(), new StringReader(text), null, repository);

        List<TextSpan> sentences = repository.Spans.Values
            .Where(s => s.Type == TextSpan.TYPE_SENTENCE)
            .OrderBy(s => s.P1)
            .ToList();
        Assert.Single(sentences);

        TextSpan sentence = sentences[0];
        Assert.Equal(TextSpan.TYPE_SENTENCE, sentence.Type);
        Assert.Equal(1, sentence.DocumentId);
        Assert.Equal(1, sentence.P1);
        Assert.Equal(2, sentence.P2);
        Assert.Null(sentence.Attributes);
    }

    [Fact]
    public async Task Parse_ExplicitStop_Ok()
    {
        const string text = "<TEI><text><body><p>Hello, Socrates. " +
                             "Do you know me?</p></body></text></TEI>";
        XmlSentenceParser parser = CreateParser();
        MockIndexRepository repository = new();
        await Tokenize(text, repository);

        parser.Parse(CreateDocument(), new StringReader(text), null, repository);

        List<TextSpan> sentences = repository.Spans.Values
            .Where(s => s.Type == TextSpan.TYPE_SENTENCE)
            .OrderBy(s => s.P1)
            .ToList();
        Assert.Equal(2, sentences.Count);

        TextSpan sentence = sentences[0];
        Assert.Equal(TextSpan.TYPE_SENTENCE, sentence.Type);
        Assert.Equal(1, sentence.DocumentId);
        Assert.Equal(1, sentence.P1);
        Assert.Equal(2, sentence.P2);
        Assert.Null(sentence.Attributes);

        sentence = sentences[1];
        Assert.Equal(TextSpan.TYPE_SENTENCE, sentence.Type);
        Assert.Equal(1, sentence.DocumentId);
        Assert.Equal(3, sentence.P1);
        Assert.Equal(6, sentence.P2);
        Assert.Null(sentence.Attributes);
    }

    [Fact]
    public async Task Parse_ExplicitStopWithNs_Ok()
    {
        const string text = "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\">" +
            "<text><body><p>Hello, Socrates. " +
            "Do you know me?</p></body></text></TEI>";

        XmlSentenceParser parser = new();
        parser.Configure(new XmlSentenceParserOptions
        {
            StopTags = new[]
            {
                "tei:div",
                "tei:head",
                "tei:p",
                "tei:body"
            },
            Namespaces = ["tei=http://www.tei-c.org/ns/1.0"]
        });

        MockIndexRepository repository = new();
        await Tokenize(text, repository);

        parser.Parse(CreateDocument(), new StringReader(text), null, repository);

        List<TextSpan> sentences = repository.Spans.Values
            .Where(s => s.Type == TextSpan.TYPE_SENTENCE)
            .OrderBy(s => s.P1)
            .ToList();
        Assert.Equal(2, sentences.Count);

        TextSpan sentence = sentences[0];
        Assert.Equal(TextSpan.TYPE_SENTENCE, sentence.Type);
        Assert.Equal(1, sentence.DocumentId);
        Assert.Equal(1, sentence.P1);
        Assert.Equal(2, sentence.P2);
        Assert.Null(sentence.Attributes);

        sentence = sentences[1];
        Assert.Equal(TextSpan.TYPE_SENTENCE, sentence.Type);
        Assert.Equal(1, sentence.DocumentId);
        Assert.Equal(3, sentence.P1);
        Assert.Equal(6, sentence.P2);
        Assert.Null(sentence.Attributes);
    }

    [Fact]
    public async Task Parse_ExplicitStopWithAbbr_Ok()
    {
        const string text =
            "<TEI><text><body><p>It is 5 <choice><abbr>P.M.</abbr>" +
            "<expan>post meridiem</expan></choice>. " +
            "Do you know me?</p></body></text></TEI>";
        XmlSentenceParser parser = new();
        parser.Configure(new XmlSentenceParserOptions
        {
            StopTags =
            [
                "div",
                "head",
                "p",
                "body"
            ],
            NoSentenceMarkerTags = ["abbr"]
        });
        MockIndexRepository repository = new();
        await Tokenize(text, repository);

        parser.Parse(CreateDocument(), new StringReader(text), null, repository);

        List<TextSpan> sentences = repository.Spans.Values
            .Where(s => s.Type == TextSpan.TYPE_SENTENCE)
            .OrderBy(s => s.P1)
            .ToList();
        Assert.Equal(2, sentences.Count);

        TextSpan sentence = sentences[0];
        Assert.Equal(TextSpan.TYPE_SENTENCE, sentence.Type);
        Assert.Equal(1, sentence.DocumentId);
        Assert.Equal(1, sentence.P1);
        Assert.Equal(5, sentence.P2);
        Assert.Null(sentence.Attributes);

        sentence = sentences[1];
        Assert.Equal(TextSpan.TYPE_SENTENCE, sentence.Type);
        Assert.Equal(1, sentence.DocumentId);
        Assert.Equal(6, sentence.P1);
        Assert.Equal(9, sentence.P2);
        Assert.Null(sentence.Attributes);
    }

    [Fact]
    public async Task Parse_BodyOnly_Ok()
    {
        const string text =
            "<TEI><teiHeader>Fake here. End.</teiHeader>" +
            "<text><body><lg>" +
            "<l>Hello, Socrates. This is a</l>" +
            "<l>test. The third sentence!</l>" +
            "</lg></body></text></TEI>";
        XmlSentenceParser parser = CreateParser();
        parser.Configure(new XmlSentenceParserOptions
        {
            RootXPath = "/TEI//body",
            StopTags = ["head"]
        });
        MockIndexRepository repository = new();
        await Tokenize(text, repository);

        parser.Parse(CreateDocument(), new StringReader(text), null, repository);

        List<TextSpan> sentences = repository.Spans.Values
            .Where(s => s.Type == TextSpan.TYPE_SENTENCE)
            .OrderBy(s => s.P1)
            .ToList();
        Assert.Equal(3, sentences.Count);

        TextSpan sentence = sentences[0];
        Assert.Equal(TextSpan.TYPE_SENTENCE, sentence.Type);
        Assert.Equal(1, sentence.DocumentId);
        Assert.Equal(1, sentence.P1);
        Assert.Equal(2, sentence.P2);
        Assert.Null(sentence.Attributes);

        sentence = sentences[1];
        Assert.Equal(TextSpan.TYPE_SENTENCE, sentence.Type);
        Assert.Equal(1, sentence.DocumentId);
        Assert.Equal(3, sentence.P1);
        Assert.Equal(6, sentence.P2);
        Assert.Null(sentence.Attributes);

        sentence = sentences[2];
        Assert.Equal(TextSpan.TYPE_SENTENCE, sentence.Type);
        Assert.Equal(1, sentence.DocumentId);
        Assert.Equal(7, sentence.P1);
        Assert.Equal(9, sentence.P2);
        Assert.Null(sentence.Attributes);
    }
}
