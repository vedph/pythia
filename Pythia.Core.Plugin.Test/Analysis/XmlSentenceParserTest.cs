using System.IO;
using System.Linq;
using Corpus.Core;
using Corpus.Core.Analysis;
using Corpus.Core.Plugin.Analysis;
using Pythia.Core.Analysis;
using Pythia.Core.Plugin.Analysis;
using Xunit;

namespace Pythia.Core.Plugin.Test.Analysis
{
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
            XmlSentenceParser parser = new XmlSentenceParser();
            parser.Configure(new XmlSentenceParserOptions
            {
                StopTags = new[]
                {
                    "div",
                    "head",
                    "p",
                    "body"
                }
            });
            return parser;
        }

        private static void Tokenize(string text, IIndexRepository repository)
        {
            ITextFilter filter = new TeiTextFilter();
            ITokenizer tokenizer = new StandardTokenizer();
            tokenizer.Filters.Add(new ItalianTokenFilter());

            tokenizer.Start(filter.Apply(new StringReader(text)), 1);

            while (tokenizer.Next())
            {
                tokenizer.CurrentToken.DocumentId = 1;
                repository.AddToken(tokenizer.CurrentToken.Clone());
            }
        }

        [Fact]
        public void Parse_Empty_None()
        {
            XmlSentenceParser parser = CreateParser();
            MockIndexRepository repository = new MockIndexRepository();

            parser.Parse(CreateDocument(), new StringReader(""), null, repository);

            Assert.Empty(repository.Structures);
        }

        [Fact]
        public void Parse_ImplicitStop_Ok()
        {
            const string text = "<TEI><text><body><p>Hello there</p></body></text></TEI>";
            XmlSentenceParser parser = CreateParser();
            MockIndexRepository repository = new MockIndexRepository();
            Tokenize(text, repository);

            parser.Parse(CreateDocument(), new StringReader(text), null, repository);

            Assert.Single(repository.Structures);
            Structure structure = repository.Structures.Values.First();
            Assert.Equal("sent", structure.Name);
            Assert.Equal(1, structure.DocumentId);
            Assert.Equal(1, structure.StartPosition);
            Assert.Equal(2, structure.EndPosition);
            Assert.Equal(0, structure.Attributes.Count);
        }

        [Fact]
        public void Parse_ExplicitStop_Ok()
        {
            const string text = "<TEI><text><body><p>Hello, Socrates. " +
                                 "Do you know me?</p></body></text></TEI>";
            XmlSentenceParser parser = CreateParser();
            MockIndexRepository repository = new MockIndexRepository();
            Tokenize(text, repository);

            parser.Parse(CreateDocument(), new StringReader(text), null, repository);

            Assert.Equal(2, repository.Structures.Count);

            Structure structure = repository.Structures.Values.First();
            Assert.Equal("sent", structure.Name);
            Assert.Equal(1, structure.DocumentId);
            Assert.Equal(1, structure.StartPosition);
            Assert.Equal(2, structure.EndPosition);
            Assert.Equal(0, structure.Attributes.Count);

            structure = repository.Structures.Values.Skip(1).First();
            Assert.Equal("sent", structure.Name);
            Assert.Equal(1, structure.DocumentId);
            Assert.Equal(3, structure.StartPosition);
            Assert.Equal(6, structure.EndPosition);
            Assert.Equal(0, structure.Attributes.Count);
        }

        [Fact]
        public void Parse_ExplicitStopWithNs_Ok()
        {
            const string text = "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\">" +
                "<text><body><p>Hello, Socrates. " +
                "Do you know me?</p></body></text></TEI>";

            XmlSentenceParser parser = new XmlSentenceParser();
            parser.Configure(new XmlSentenceParserOptions
            {
                StopTags = new[]
                {
                    "tei:div",
                    "tei:head",
                    "tei:p",
                    "tei:body"
                },
                Namespaces = new[] { "tei=http://www.tei-c.org/ns/1.0" }
            });

            MockIndexRepository repository = new MockIndexRepository();
            Tokenize(text, repository);

            parser.Parse(CreateDocument(), new StringReader(text), null, repository);

            Assert.Equal(2, repository.Structures.Count);

            Structure structure = repository.Structures.Values.First();
            Assert.Equal("sent", structure.Name);
            Assert.Equal(1, structure.DocumentId);
            Assert.Equal(1, structure.StartPosition);
            Assert.Equal(2, structure.EndPosition);
            Assert.Equal(0, structure.Attributes.Count);

            structure = repository.Structures.Values.Skip(1).First();
            Assert.Equal("sent", structure.Name);
            Assert.Equal(1, structure.DocumentId);
            Assert.Equal(3, structure.StartPosition);
            Assert.Equal(6, structure.EndPosition);
            Assert.Equal(0, structure.Attributes.Count);
        }

        [Fact]
        public void Parse_ExplicitStopWithAbbr_Ok()
        {
            const string text =
                "<TEI><text><body><p>It is 5 <choice><abbr>P.M.</abbr>" +
                "<expan>post meridiem</expan></choice>. " +
                "Do you know me?</p></body></text></TEI>";
            XmlSentenceParser parser = new XmlSentenceParser();
            parser.Configure(new XmlSentenceParserOptions
            {
                StopTags = new[]
                {
                    "div",
                    "head",
                    "p",
                    "body"
                },
                NoSentenceMarkerTags = new[] {"abbr"}
            });
            MockIndexRepository repository = new MockIndexRepository();
            Tokenize(text, repository);

            parser.Parse(CreateDocument(), new StringReader(text), null, repository);

            Assert.Equal(2, repository.Structures.Count);

            Structure structure = repository.Structures.Values.First();
            Assert.Equal("sent", structure.Name);
            Assert.Equal(1, structure.DocumentId);
            Assert.Equal(1, structure.StartPosition);
            Assert.Equal(5, structure.EndPosition);
            Assert.Equal(0, structure.Attributes.Count);

            structure = repository.Structures.Values.Skip(1).First();
            Assert.Equal("sent", structure.Name);
            Assert.Equal(1, structure.DocumentId);
            Assert.Equal(6, structure.StartPosition);
            Assert.Equal(9, structure.EndPosition);
            Assert.Equal(0, structure.Attributes.Count);
        }

        [Fact]
        public void Parse_BodyOnly_Ok()
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
                StopTags = new[] { "head" }
            });
            MockIndexRepository repository = new MockIndexRepository();
            Tokenize(text, repository);

            parser.Parse(CreateDocument(), new StringReader(text), null, repository);

            Assert.Equal(3, repository.Structures.Count);

            Structure structure = repository.Structures.Values.First();
            Assert.Equal("sent", structure.Name);
            Assert.Equal(1, structure.DocumentId);
            Assert.Equal(1, structure.StartPosition);
            Assert.Equal(2, structure.EndPosition);
            Assert.Equal(0, structure.Attributes.Count);

            structure = repository.Structures.Values.Skip(1).First();
            Assert.Equal("sent", structure.Name);
            Assert.Equal(1, structure.DocumentId);
            Assert.Equal(3, structure.StartPosition);
            Assert.Equal(6, structure.EndPosition);
            Assert.Equal(0, structure.Attributes.Count);

            structure = repository.Structures.Values.Skip(2).First();
            Assert.Equal("sent", structure.Name);
            Assert.Equal(1, structure.DocumentId);
            Assert.Equal(7, structure.StartPosition);
            Assert.Equal(9, structure.EndPosition);
            Assert.Equal(0, structure.Attributes.Count);
        }
    }
}
