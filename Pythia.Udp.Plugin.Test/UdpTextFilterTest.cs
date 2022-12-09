using Conllu;
using Fusi.Tools;
using Xunit;

namespace Pythia.Udp.Plugin.Test;

// NOTE: this test requires an internet connection

public sealed class UdpTextFilterTest
{
    [Fact]
    public async Task ApplyAsync_Ok()
    {
        // create filter
        UdpTextFilter filter = new();
        filter.Configure(new UdpTextFilterOptions
        {
            Model = "italian-isdt-ud-2.10-220711"
        });
        // context and text
        DataDictionary context = new();
        const string text = "Questa è una prova. La fine è vicina.";

        // apply filter
        TextReader reader = await filter.ApplyAsync(
            new StringReader(text), context);

        // text did not change
        string result = reader.ReadToEnd();
        Assert.Equal(text, result);

        // 1 chunk is present
        Assert.True(context.Data.ContainsKey(UdpTextFilter.UDP_KEY));
        IList<UdpChunk> chunks = (IList<UdpChunk>)
            context.Data[UdpTextFilter.UDP_KEY];
        Assert.Single(chunks);

        // the chunk has 2 sentences
        IList<Sentence> sentences = chunks[0].Sentences;
        Assert.Equal(2, sentences.Count);

        // Questa
        Sentence sentence = sentences[0];
        Token token = sentence.Tokens[0];
        Assert.Equal("questo", token.Lemma);
        // è
        token = sentence.Tokens[1];
        Assert.Equal("essere", token.Lemma);
        // una
        token = sentence.Tokens[2];
        Assert.Equal("uno", token.Lemma);
        // prova.
        token = sentence.Tokens[3];
        Assert.Equal("prova", token.Lemma);

        sentence = sentences[1];
        // La
        token = sentence.Tokens[0];
        Assert.Equal("il", token.Lemma);
        // fine
        token = sentence.Tokens[1];
        Assert.Equal("fine", token.Lemma);
        // è
        token = sentence.Tokens[2];
        Assert.Equal("essere", token.Lemma);
        // vicina.
        token = sentence.Tokens[3];
        Assert.Equal("vicino", token.Lemma);
    }
}