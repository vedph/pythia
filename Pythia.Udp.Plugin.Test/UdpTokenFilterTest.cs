using Fusi.Tools;
using Pythia.Core.Plugin.Analysis;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using Xunit;

// NOTE: this test requires an internet connection

namespace Pythia.Udp.Plugin.Test;

public sealed class UdpTokenFilterTest
{
    private const string MODEL = "italian-isdt-ud-2.15-241121";

    private static readonly Regex _featRegex =
        new("(?<n>[^=]+)=(?<v>[^|]*)\\|?", RegexOptions.Compiled);

    private static Dictionary<string,string> ParseFeats(string feats)
    {
        Dictionary<string, string> dct = new();
        feats = feats.ToLowerInvariant();

        foreach (Match m in _featRegex.Matches(feats))
        {
            dct[m.Groups["n"].Value] = m.Groups["v"].Value;
        }
        return dct;
    }

    private static void AssertFeatsEqual(string expected,
        IList<Corpus.Core.Attribute> attributes)
    {
        foreach (var p in ParseFeats(expected))
        {
            var attr = attributes.FirstOrDefault(
                a => a.Name == p.Key &&
                a.Value == p.Value);
            if (attr == null)
            {
                Debug.WriteLine($"Missing expected {p.Key}={p.Value}");
            }
            Assert.NotNull(attr);
        }
    }

    [Fact]
    public async Task Apply_Ok()
    {
        // prepare text
        UdpTextFilter textFilter = new();
        textFilter.Configure(new UdpTextFilterOptions
        {
            Model = MODEL
        });
        DataDictionary context = new();
        const string text = "Questa è una prova. La fine è vicina.";
        await textFilter.ApplyAsync(new StringReader(text), context);

        // prepare token filter
        UdpTokenFilter filter = new();
        filter.Configure(new UdpTokenFilterOptions
        {
            Props = UdpTokenProps.All
        });

        // prepare tokenizer
        int n = 0;
        StandardTokenizer tokenizer = new();
        string[] expectedLemmas =
        [
            "questo", "essere", "uno", "prova",
            "il", "fine", "essere", "vicino"
        ];
        string[] expectedUpos =
        [
            "PRON", "AUX", "DET", "NOUN",
            "DET", "NOUN", "AUX", "ADJ"
        ];
        string[] expectedXpos =
        [
            "PD", "VA", "RI", "S",
            "RD", "S", "VA", "A"
        ];
        string[] expectedFeats =
        [
            "Gender=Fem|Number=Sing|PronType=Dem",
            "Mood=Ind|Number=Sing|Person=3|Tense=Pres|VerbForm=Fin",
            "Definite=Ind|Gender=Fem|Number=Sing|PronType=Art",
            "Gender=Fem|Number=Sing",
            "Definite=Def|Gender=Fem|Number=Sing|PronType=Art",
            "Gender=Fem|Number=Sing",
            "Mood=Ind|Number=Sing|Person=3|Tense=Pres|VerbForm=Fin",
            "Gender=Fem|Number=Sing"
        ];
        int[] expectedHeads =
        [
            4, 4, 4, 0,
            2, 4, 4, 0
        ];
        string[] expectedDeprels =
        [
            "nsubj", "cop", "det", "root",
            "det", "nsubj", "cop", "root"
        ];

        // tokenize and filter each token
        tokenizer.Start(new StringReader(text), 1, context);
        while (await tokenizer.NextAsync())
        {
            await filter.ApplyAsync(tokenizer.CurrentToken, ++n, context);

            // lemma
            Assert.NotNull(tokenizer.CurrentToken.Lemma);
            Assert.Equal(expectedLemmas[n - 1], tokenizer.CurrentToken.Lemma);

            // upos
            Assert.NotNull(tokenizer.CurrentToken.Pos);
            Assert.Equal(expectedUpos[n - 1], tokenizer.CurrentToken.Pos);

            // xpos
            Corpus.Core.Attribute? attr = tokenizer.CurrentToken.Attributes!
                .FirstOrDefault(a => a.Name == "xpos");
            Assert.NotNull(attr);
            Assert.Equal(expectedXpos[n - 1], attr.Value);

            // feats
            AssertFeatsEqual(expectedFeats[n - 1],
                tokenizer.CurrentToken.Attributes!);

            // head
            attr = tokenizer.CurrentToken.Attributes!
                .FirstOrDefault(a => a.Name == "head");
            Assert.NotNull(attr);
            Assert.Equal(expectedHeads[n - 1],
                int.Parse(attr.Value!, CultureInfo.InvariantCulture));

            // deprel
            attr = tokenizer.CurrentToken.Attributes!
                .FirstOrDefault(a => a.Name == "deprel");
            Assert.NotNull(attr);
            Assert.Equal(expectedDeprels[n - 1], attr.Value);

            // misc
            Assert.NotNull(tokenizer.CurrentToken.Attributes!
                .FirstOrDefault(a => a.Name == "misc"));
        }
    }

    [Fact]
    public async Task Apply_Della_Ok()
    {
        // prepare text
        UdpTextFilter textFilter = new();
        textFilter.Configure(new UdpTextFilterOptions
        {
            Model = MODEL
        });
        DataDictionary context = new();
        const string text = "Questo è della casa.";
        await textFilter.ApplyAsync(new StringReader(text), context);

        // prepare token filter
        UdpTokenFilter filter = new();
        filter.Configure(new UdpTokenFilterOptions
        {
            Props = UdpTokenProps.All,
            Multiwords =
            [
                new UdpMultiwordTokenOptions()
                {
                    MinCount = 2,
                    MaxCount = 2,
                    Tokens =
                    [
                        new UdpChildTokenOptions()
                        {
                             Upos = "ADP",
                             Xpos = "E"
                        },
                        new UdpChildTokenOptions()
                        {
                             Upos = "DET",
                             Xpos = "RD"
                        },
                    ],
                    Target = new UdpMultiwordTokenTargetOptions()
                    {
                        Upos = "DET",
                        Xpos = "RD",
                        Feats = new()
                        {
                            ["*"] = "2"
                        }
                    }
                }
            ]
        });

        // prepare tokenizer
        int n = 0;
        StandardTokenizer tokenizer = new();
        string[] expectedLemmas =
        [
            "questo", "essere", "della", "casa"
        ];
        string[] expectedUpos =
        [
            "PRON", "AUX", "DET", "NOUN"
        ];
        string[] expectedXpos =
        [
            "PD", "VA", "RD", "S",
        ];
        string[] expectedFeats =
        [
            "Gender=Masc|Number=Sing|PronType=Dem",
            "Mood=Ind|Number=Sing|Person=3|Tense=Pres|VerbForm=Fin",
            "Definite=Def|Gender=Fem|Number=Sing|PronType=Art",
            "Gender=Fem|Number=Sing"
        ];

        // tokenize and filter each token
        tokenizer.Start(new StringReader(text), 1, context);
        while (await tokenizer.NextAsync())
        {
            await filter.ApplyAsync(tokenizer.CurrentToken, ++n, context);

            // lemma
            Assert.NotNull(tokenizer.CurrentToken.Lemma);
            Assert.Equal(expectedLemmas[n - 1], tokenizer.CurrentToken.Lemma);

            // upos
            Assert.NotNull(tokenizer.CurrentToken.Pos);
            Assert.Equal(expectedUpos[n - 1], tokenizer.CurrentToken.Pos);

            // xpos
            Corpus.Core.Attribute? attr = tokenizer.CurrentToken.Attributes!
                .FirstOrDefault(a => a.Name == "xpos");
            Assert.NotNull(attr);
            Assert.Equal(expectedXpos[n - 1], attr.Value);

            // feats
            AssertFeatsEqual(expectedFeats[n - 1],
                tokenizer.CurrentToken.Attributes!);
        }
    }
}
