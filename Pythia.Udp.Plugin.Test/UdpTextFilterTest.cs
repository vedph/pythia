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
        UdpTextFilter filter = new();
        filter.Configure(new UdpTextFilterOptions
        {
            Model = "italian-isdt-ud-2.10.220711"
        });
        DataDictionary context = new();
        const string text = "Questa è una prova. La fine è vicina.";

        var reader = await filter.ApplyAsync(
            new StringReader(text), context);

        string result = reader.ReadToEnd();
        Assert.Equal(text, result);

        Assert.True(context.Data.ContainsKey(UdpTextFilter.SENTENCES_KEY));
        IList<Sentence> sentences = (IList<Sentence>)
            context.Data[UdpTextFilter.SENTENCES_KEY];
        Assert.Equal(2, sentences.Count);
        // TODO
    }
}