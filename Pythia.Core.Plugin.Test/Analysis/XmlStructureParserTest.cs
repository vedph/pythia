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

        private static XmlStructureParserOptions CreateDefinitions()
        {
            return new XmlStructureParserOptions
            {
                RootPath = "/text",
                Definitions = new[]
                {
                    "poem=/div @n$",
                    "stanza#=/div/div @n$",
                    "line#=/div/div/l @n$"
                }
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
            MockIndexRepository repository = new MockIndexRepository();
            // document
            repository.AddDocument(new Document { Id = 1 }, true, true);
            // tokens
            string text = LoadResourceText("SampleDoc.xml").ReadToEnd();
            text = Regex.Replace(text, @"<[^>]+>", m => new string(' ', m.Length));

            WhitespaceTokenizer tokenizer = new WhitespaceTokenizer();
            tokenizer.Filters.Add(new LoAlnumAposTokenFilter());
            tokenizer.Start(new StringReader(text), 1);
            while (tokenizer.Next())
            {
                Token token = tokenizer.CurrentToken.Clone();
                repository.AddToken(token);
            }

            XmlStructureParser parser = new XmlStructureParser();
            parser.Configure(CreateDefinitions());

            // act
            parser.Parse(CreateDocument(),
                LoadResourceText("SampleDoc.xml"),
                new CharIndexCalculator(LoadResourceText("SampleDoc.xml")),
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
    }
}
