using Fusi.Tools;
using Pythia.Core.Analysis;
using Pythia.Core.Plugin.Analysis;
using Xunit;

namespace Pythia.Udp.Plugin.Test
{
    public sealed class UdpTokenFilterTest
    {
        [Fact]
        public async Task Apply_Ok()
        {
            // prepare text
            UdpTextFilter textFilter = new();
            textFilter.Configure(new UdpTextFilterOptions
            {
                Model = "italian-isdt-ud-2.10-220711"
            });
            DataDictionary context = new();
            const string text = "Questa è una prova. La fine è vicina.";
            await textFilter.ApplyAsync(new StringReader(text), context);

            // prepare token filter
            UdpTokenFilter filter = new();
            filter.Configure(new UdpTokenFilterOptions
            {
                Lemma = true,
                UPosTag = true,
                XPosTag = true,
            });

            // prepare tokenizer
            int n = 0;
            ITokenizer tokenizer = new StandardTokenizer();
            string[] expectedLemmas = new[]
            {
                "questo", "essere", "un", "prova",
                "il", "fine", "essere", "vicino"
            };
            string[] expectedUpos = new[]
            {
                "PRON", "AUX", "DET", "NOUN",
                "DET", "NOUN", "AUX", "ADJ"
            };
            string[] expectedXpos = new[]
            {
                "PD", "VA", "RI", "S",
                "RD", "S", "VA", "A"
            };

            // tokenize and filter each token
            tokenizer.Start(new StringReader(text), 1, context);
            while (tokenizer.Next())
            {
                filter.Apply(tokenizer.CurrentToken, ++n);

                // lemma
                Corpus.Core.Attribute? attr = tokenizer.CurrentToken.Attributes!
                    .FirstOrDefault(a => a.Name == "lemma");
                Assert.NotNull(attr);
                Assert.Equal(expectedLemmas[n - 1], attr.Value);

                // upos
                attr = tokenizer.CurrentToken.Attributes!
                    .FirstOrDefault(a => a.Name == "upos");
                Assert.NotNull(attr);
                Assert.Equal(expectedUpos[n - 1], attr.Value);

                // xpos
                attr = tokenizer.CurrentToken.Attributes!
                    .FirstOrDefault(a => a.Name == "xpos");
                Assert.NotNull(attr);
                Assert.Equal(expectedXpos[n - 1], attr.Value);
            }
        }
    }
}
