using System.IO;
using System.Threading.Tasks;
using Pythia.Core.Plugin.Analysis;
using Xunit;

namespace Pythia.Core.Plugin.Test.Analysis;

public sealed class WhitespaceTokenizerTest
{
    [Fact]
    public async Task Next_Empty_False()
    {
        WhitespaceTokenizer tokenizer = new();
        tokenizer.Start(new StringReader(""), 1);

        Assert.False(await tokenizer.NextAsync());
    }

    [Fact]
    public async Task Next_Whitespace_False()
    {
        WhitespaceTokenizer tokenizer = new();
        tokenizer.Start(new StringReader("  \r\n "), 1);

        Assert.False(await tokenizer.NextAsync());
    }

    [Fact]
    public async Task Next_TextWithSpace_Ok()
    {
        WhitespaceTokenizer tokenizer = new();
        tokenizer.Start(new StringReader("alpha beta"), 1);

        Assert.True(await tokenizer.NextAsync());

        TextSpan token = tokenizer.CurrentToken;
        Assert.Equal(1, token.P1);
        Assert.Equal(1, token.P2);
        Assert.Equal(0, token.Index);
        Assert.Equal(5, token.Length);
        Assert.Equal("alpha", token.Value);

        Assert.True(await tokenizer.NextAsync());

        token = tokenizer.CurrentToken;
        Assert.Equal(2, token.P1);
        Assert.Equal(2, token.P2);
        Assert.Equal(6, token.Index);
        Assert.Equal(4, token.Length);
        Assert.Equal("beta", token.Value);

        Assert.False(await tokenizer.NextAsync());
    }
}
