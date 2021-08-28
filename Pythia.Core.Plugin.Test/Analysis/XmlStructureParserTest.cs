using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Corpus.Core;
using Corpus.Core.Plugin.Reading;
using Pythia.Core.Analysis;
using Pythia.Core.Plugin.Analysis;
using Xunit;

namespace Pythia.Core.Plugin.Test.Analysis
{
    public sealed class XmlStructureParserTest
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

        private static TextReader LoadResourceText(string name)
        {
            Stream stream = typeof(XmlStructureParserTest).GetTypeInfo()
                .Assembly
                .GetManifestResourceStream($"Pythia.Core.Plugin.Test.Assets.{name}");
            return new StreamReader(stream, Encoding.UTF8);
        }

        private static Tuple<int, int, string, string>[] LoadStructureData()
        {
            List<Tuple<int, int, string, string>> rows =
                new List<Tuple<int, int, string, string>>();
            char[] seps = { ' ', '\t' };
            using (TextReader reader = LoadResourceText("Structures.txt"))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Length == 0) continue;
                    string[] a = line.Split(seps, StringSplitOptions.RemoveEmptyEntries);
                    rows.Add(Tuple.Create(
                        int.Parse(a[0], CultureInfo.InvariantCulture),
                        int.Parse(a[1], CultureInfo.InvariantCulture),
                        a[2],
                        a[3]));
                }
            }

            return rows.ToArray();
        }

        [Fact]
        public void Parse_Ok()
        {
            const string DOC_NAME = "SampleDoc.xml";

            MockIndexRepository repository = new MockIndexRepository();
            // document
            repository.AddDocument(new Document { Id = 1 }, true, true);
            // tokens
            string text = LoadResourceText(DOC_NAME).ReadToEnd();
            text = Regex.Replace(text, "<[^>]+>", m => new string(' ', m.Length));

            WhitespaceTokenizer tokenizer = new WhitespaceTokenizer();
            tokenizer.Filters.Add(new LoAlnumAposTokenFilter());
            tokenizer.Start(new StringReader(text), 1);
            while (tokenizer.Next())
            {
                Token token = tokenizer.CurrentToken.Clone();
                repository.AddToken(token);
            }

            XmlStructureParser parser = new XmlStructureParser();
            var options = new XmlStructureParserOptions
            {
                Definitions = new DroppableXmlStructureDefinition[]
                {
                    new DroppableXmlStructureDefinition
                    {
                        Name = "poem",
                        XPath = "/text/div",
                        ValueTemplateArgs = new[]
                        {
                            new XmlStructureValueArg("n", "./@n")
                        },
                        ValueTemplate = "{n}"
                    },
                    new DroppableXmlStructureDefinition
                    {
                        Name = "stanza",
                        XPath = "//div/div",
                        ValueTemplateArgs = new[]
                        {
                            new XmlStructureValueArg("n", "./@n")
                        },
                        ValueTemplate = "{n}"
                    },
                    new DroppableXmlStructureDefinition
                    {
                        Name = "line",
                        XPath = "//l",
                        ValueTemplateArgs = new[]
                        {
                            new XmlStructureValueArg("n", "./@n")
                        },
                        ValueTemplate = "{n}"
                    }
                },
                Namespaces = new[]
                {
                    "tei=http://www.tei-c.org/ns/1.0"
                }
            };
            parser.Configure(options);

            // act
            parser.Parse(CreateDocument(),
                LoadResourceText(DOC_NAME),
                new CharIndexCalculator(LoadResourceText(DOC_NAME)),
                repository);

            // assert
            Tuple<int, int, string, string>[] rows = LoadStructureData();
            Assert.Equal(rows.Length, repository.Structures.Count);
            foreach (var t in rows)
            {
                Debug.WriteLine(t);
                Structure structure = repository.Structures.Values
                    .FirstOrDefault(s => s.StartPosition == t.Item1 &&
                                         s.EndPosition == t.Item2 &&
                                         s.Name == t.Item3 &&
                                         s.Attributes.Any(a => a.Name == t.Item3 &&
                                                               a.Value == t.Item4));
                Assert.NotNull(structure);
            }
        }

        [Fact]
        public void ParseWithNs_Ok()
        {
            const string DOC_NAME = "SampleDocNs.xml";

            MockIndexRepository repository = new MockIndexRepository();
            // document
            repository.AddDocument(new Document { Id = 1 }, true, true);
            // tokens
            string text = LoadResourceText(DOC_NAME).ReadToEnd();
            text = Regex.Replace(text, "<[^>]+>", m => new string(' ', m.Length));

            WhitespaceTokenizer tokenizer = new WhitespaceTokenizer();
            tokenizer.Filters.Add(new LoAlnumAposTokenFilter());
            tokenizer.Start(new StringReader(text), 1);
            while (tokenizer.Next())
            {
                Token token = tokenizer.CurrentToken.Clone();
                repository.AddToken(token);
            }

            XmlStructureParser parser = new XmlStructureParser();
            var options = new XmlStructureParserOptions
            {
                Definitions = new DroppableXmlStructureDefinition[]
                {
                    new DroppableXmlStructureDefinition
                    {
                        Name = "poem",
                        XPath = "/tei:text/tei:div",
                        ValueTemplateArgs = new[]
                        {
                            new XmlStructureValueArg("n", "./@n")
                        },
                        ValueTemplate = "{n}"
                    },
                    new DroppableXmlStructureDefinition
                    {
                        Name = "stanza",
                        XPath = "//tei:div/tei:div",
                        ValueTemplateArgs = new[]
                        {
                            new XmlStructureValueArg("n", "./@n")
                        },
                        ValueTemplate = "{n}"
                    },
                    new DroppableXmlStructureDefinition
                    {
                        Name = "line",
                        XPath = "//tei:l",
                        ValueTemplateArgs = new[]
                        {
                            new XmlStructureValueArg("n", "./@n")
                        },
                        ValueTemplate = "{n}"
                    }
                },
                Namespaces = new[]
                {
                    "tei=http://www.tei-c.org/ns/1.0"
                }
            };
            parser.Configure(options);

            // act
            parser.Parse(CreateDocument(),
                LoadResourceText(DOC_NAME),
                new CharIndexCalculator(LoadResourceText(DOC_NAME)),
                repository);

            // assert
            Tuple<int, int, string, string>[] rows = LoadStructureData();
            Assert.Equal(rows.Length, repository.Structures.Count);
            foreach (var t in rows)
            {
                Debug.WriteLine(t);
                Structure structure = repository.Structures.Values
                    .FirstOrDefault(s => s.StartPosition == t.Item1 &&
                                         s.EndPosition == t.Item2 &&
                                         s.Name == t.Item3 &&
                                         s.Attributes.Any(a => a.Name == t.Item3 &&
                                                               a.Value == t.Item4));
                Assert.NotNull(structure);
            }
        }

        /* TODO: uncomment once mock repo is compatible
        [Fact]
        public void Parse_Ghost_Ok()
        {
            const string DOC_NAME = "SampleDoc.xml";

            MockIndexRepository repository = new MockIndexRepository();
            // document
            repository.AddDocument(new Document { Id = 1 }, true, true);
            // tokens
            string text = LoadResourceText(DOC_NAME).ReadToEnd();
            text = Regex.Replace(text, "<[^>]+>", m => new string(' ', m.Length));

            WhitespaceTokenizer tokenizer = new WhitespaceTokenizer();
            tokenizer.Filters.Add(new LoAlnumAposTokenFilter());
            tokenizer.Start(new StringReader(text), 1);
            while (tokenizer.Next())
            {
                Token token = tokenizer.CurrentToken.Clone();
                repository.AddToken(token);
            }

            XmlStructureParser parser = new XmlStructureParser();
            var options = new XmlStructureParserOptions
            {
                Definitions = new XmlStructureDefinition[]
                {
                    new XmlStructureDefinition
                    {
                        Name = "line 27",
                        XPath = "//l[@n=27]",
                        ValueTemplate = "boo",
                        TokenTargetName = "ghost"
                    }
                },
                Namespaces = new[]
                {
                    "tei=http://www.tei-c.org/ns/1.0"
                }
            };
            parser.Configure(options);

            // act
            parser.Parse(CreateDocument(),
                LoadResourceText(DOC_NAME),
                new CharIndexCalculator(LoadResourceText(DOC_NAME)),
                repository);

            // assert
            Tuple<int, int, string, string>[] rows = LoadStructureData();
            Assert.Equal(rows.Length, repository.Structures.Count);
            foreach (var t in rows)
            {
                Debug.WriteLine(t);
                Structure structure = repository.Structures.Values
                    .FirstOrDefault(s => s.StartPosition == t.Item1 &&
                                         s.EndPosition == t.Item2 &&
                                         s.Name == t.Item3 &&
                                         s.Attributes.Any(a => a.Name == t.Item3 &&
                                                               a.Value == t.Item4));
                Assert.NotNull(structure);
            }
        }
        */
    }
}
