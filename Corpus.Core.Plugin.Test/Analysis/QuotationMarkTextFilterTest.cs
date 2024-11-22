using System.IO;
using System.Threading.Tasks;
using Corpus.Core.Plugin.Analysis;
using Xunit;

namespace Corpus.Core.Plugin.Test.Analysis;

public sealed class QuotationMarkTextFilterTest
{
    [Fact]
    public async Task Apply_Empty_Unchanged()
    {
        QuotationMarkTextFilter filter = new();

        TextReader reader = await filter.ApplyAsync(new StringReader(""));
        string result = reader.ReadToEnd();

        Assert.Equal("", result);
    }

    [Fact]
    public async Task Apply_NoQuotation_Unchanged()
    {
        const string sample = "Hello I'm a sample";
        QuotationMarkTextFilter filter = new();

        TextReader reader = await filter.ApplyAsync(new StringReader(sample));
        string result = reader.ReadToEnd();

        Assert.Equal(sample, result);
    }

    [Fact]
    public async Task Apply_QuotationInitial_Unchanged()
    {
        const string sample = "\u2019Hello I'm a sample with quote";
        QuotationMarkTextFilter filter = new();

        TextReader reader = await filter.ApplyAsync(new StringReader(sample));
        string result = reader.ReadToEnd();

        Assert.Equal(sample, result);
    }

    [Fact]
    public async Task Apply_QuotationFinal_Unchanged()
    {
        const string sample = "Hello I'm a \u2018sample\u2019";
        QuotationMarkTextFilter filter = new();

        TextReader reader = await filter.ApplyAsync(new StringReader(sample));
        string result = reader.ReadToEnd();

        Assert.Equal(sample, result);
    }

    [Fact]
    public async Task Apply_QuotationAsQuote_Unchanged()
    {
        const string sample = "Hello I'm a \u2018sample\u2019 with quote";
        QuotationMarkTextFilter filter = new();

        TextReader reader = await filter.ApplyAsync(new StringReader(sample));
        string result = reader.ReadToEnd();

        Assert.Equal(sample, result);
    }

    [Fact]
    public async Task Apply_QuotationAsApostrophe_Changed()
    {
        const string sample = "Hello I\u2019m a sample";
        QuotationMarkTextFilter filter = new();

        TextReader reader = await filter.ApplyAsync(new StringReader(sample));
        string result = reader.ReadToEnd();

        Assert.Equal("Hello I'm a sample", result);
    }
}
