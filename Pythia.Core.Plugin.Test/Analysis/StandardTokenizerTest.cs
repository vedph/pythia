using System.IO;
using System.Threading.Tasks;
using Pythia.Core.Analysis;
using Pythia.Core.Plugin.Analysis;
using Xunit;

namespace Pythia.Core.Plugin.Test.Analysis;

public sealed class StandardTokenizerTest
{
    [Fact]
    public async Task Next_Empty_False()
    {
        StandardTokenizer tokenizer = new();
        tokenizer.Start(new StringReader(""), 1);

        Assert.False(await tokenizer.NextAsync());
    }

    [Fact]
    public async Task Next_WhitespaceOnly_False()
    {
        StandardTokenizer tokenizer = new();
        tokenizer.Start(new StringReader("  "), 1);

        Assert.False(await tokenizer.NextAsync());
    }

    [Fact]
    public async Task Next_Simple_Ok()
    {
        StandardTokenizer tokenizer = new();
        tokenizer.Start(new StringReader("alpha"), 1);

        Assert.True(await tokenizer.NextAsync());

        Assert.Equal(0, tokenizer.CurrentToken.Index);
        Assert.Equal(5, tokenizer.CurrentToken.Length);
        Assert.Equal(1, tokenizer.CurrentToken.P1);
        Assert.Equal("alpha", tokenizer.CurrentToken.Value);
    }

    [Fact]
    public async Task Next_SimpleWithNonLetters_Ok()
    {
        StandardTokenizer tokenizer = new();
        tokenizer.Start(new StringReader("al[p]ha"), 1);

        Assert.True(await tokenizer.NextAsync());

        Assert.Equal(0, tokenizer.CurrentToken.Index);
        Assert.Equal(7, tokenizer.CurrentToken.Length);
        Assert.Equal(1, tokenizer.CurrentToken.P1);
        Assert.Equal("al[p]ha", tokenizer.CurrentToken.Value);
    }

    [Fact]
    public async Task Next_Two_Ok()
    {
        StandardTokenizer tokenizer = new();
        tokenizer.Start(new StringReader("alpha beta"), 1);

        // alpha
        Assert.True(await tokenizer.NextAsync());
        Assert.Equal(0, tokenizer.CurrentToken.Index);
        Assert.Equal(5, tokenizer.CurrentToken.Length);
        Assert.Equal(1, tokenizer.CurrentToken.P1);
        Assert.Equal("alpha", tokenizer.CurrentToken.Value);
        // beta
        Assert.True(await tokenizer.NextAsync());
        Assert.Equal(6, tokenizer.CurrentToken.Index);
        Assert.Equal(4, tokenizer.CurrentToken.Length);
        Assert.Equal(2, tokenizer.CurrentToken.P1);
        Assert.Equal("beta", tokenizer.CurrentToken.Value);
        // end
        Assert.False(await tokenizer.NextAsync());
    }

    [Fact]
    public async Task Next_TwoWithRightApostrophe_Ok()
    {
        StandardTokenizer tokenizer = new();
        tokenizer.Start(new StringReader("l'incenso"), 1);

        // l'
        Assert.True(await tokenizer.NextAsync());
        Assert.Equal(0, tokenizer.CurrentToken.Index);
        Assert.Equal(2, tokenizer.CurrentToken.Length);
        Assert.Equal(1, tokenizer.CurrentToken.P1);
        Assert.Equal("l'", tokenizer.CurrentToken.Value);
        // incenso
        Assert.True(await tokenizer.NextAsync());
        Assert.Equal(2, tokenizer.CurrentToken.Index);
        Assert.Equal(7, tokenizer.CurrentToken.Length);
        Assert.Equal(2, tokenizer.CurrentToken.P1);
        Assert.Equal("incenso", tokenizer.CurrentToken.Value);
        // end
        Assert.False(await tokenizer.NextAsync());
    }

    [Fact]
    public async Task Next_TwoWithLeftApostrophe_Ok()
    {
        StandardTokenizer tokenizer = new();
        tokenizer.Start(new StringReader("'l ponerò"), 1);

        // 'l
        Assert.True(await tokenizer.NextAsync());
        Assert.Equal(0, tokenizer.CurrentToken.Index);
        Assert.Equal(2, tokenizer.CurrentToken.Length);
        Assert.Equal(1, tokenizer.CurrentToken.P1);
        Assert.Equal("'l", tokenizer.CurrentToken.Value);
        // ponerò
        Assert.True(await tokenizer.NextAsync());
        Assert.Equal(3, tokenizer.CurrentToken.Index);
        Assert.Equal(6, tokenizer.CurrentToken.Length);
        Assert.Equal(2, tokenizer.CurrentToken.P1);
        Assert.Equal("ponerò", tokenizer.CurrentToken.Value);
        // end
        Assert.False(await tokenizer.NextAsync());
    }

    [Fact]
    public async Task Next_FilteredOutToken_Ok()
    {
        StandardTokenizer tokenizer = new();
        tokenizer.Filters.Add(new LoAlnumAposTokenFilter());
        //                                012345678901
        tokenizer.Start(new StringReader("alpha - beta"), 1);

        // alpha
        Assert.True(await tokenizer.NextAsync());
        Assert.Equal(0, tokenizer.CurrentToken.Index);
        Assert.Equal(5, tokenizer.CurrentToken.Length);
        Assert.Equal(1, tokenizer.CurrentToken.P1);
        Assert.Equal("alpha", tokenizer.CurrentToken.Value);
        // beta
        Assert.True(await tokenizer.NextAsync());
        Assert.Equal(8, tokenizer.CurrentToken.Index);
        Assert.Equal(4, tokenizer.CurrentToken.Length);
        Assert.Equal(2, tokenizer.CurrentToken.P1);
        Assert.Equal("beta", tokenizer.CurrentToken.Value);
        // end
        Assert.False(await tokenizer.NextAsync());
    }
}
