using Pythia.Core.Plugin.Analysis;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Pythia.Core.Plugin.Test.Analysis;

public sealed class PunctuationTokenFilterTest
{
    [Fact]
    public async Task Apply_Empty_NullAttr()
    {
        PunctuationTokenFilter filter = new();
        TextSpan token = new()
        {
            DocumentId = 1,
            P1 = 1,
            P2 = 1,
            Length = 0,
            Value = ""
        };

        await filter.ApplyAsync(token, token.P1);

        Assert.Null(token.Attributes);
    }

    [Fact]
    public async Task Apply_NoPunct_NullAttr()
    {
        PunctuationTokenFilter filter = new();
        TextSpan token = new()
        {
            DocumentId = 1,
            P1 = 1,
            P2 = 1,
            Length = 3,
            Value = "abc"
        };

        await filter.ApplyAsync(token, token.P1);

        Assert.Null(token.Attributes);
    }

    [Fact]
    public async Task Apply_LeftPunct_Ok()
    {
        PunctuationTokenFilter filter = new();
        TextSpan token = new()
        {
            DocumentId = 1,
            P1 = 1,
            P2 = 1,
            Length = 4,
            Value = "(abc"
        };

        await filter.ApplyAsync(token, token.P1);

        Assert.Single(token.Attributes!);
        Assert.NotNull(token.Attributes!.FirstOrDefault(
            a => a.Name == "lp" && a.Value == "("));
    }

    [Fact]
    public async Task Apply_LeftPuncts_Ok()
    {
        PunctuationTokenFilter filter = new();
        TextSpan token = new()
        {
            DocumentId = 1,
            P1 = 1,
            P2 = 1,
            Length = 5,
            Value = "([abc"
        };

        await filter.ApplyAsync(token, token.P1);

        Assert.Single(token.Attributes!);
        Assert.NotNull(token.Attributes!.FirstOrDefault(
            a => a.Name == "lp" && a.Value == "(["));
    }

    [Fact]
    public async Task Apply_LeftPunctsWithBlacks_Ok()
    {
        PunctuationTokenFilter filter = new();
        filter.Configure(
            new PunctuationTokenFilterOptions
            {
                Punctuations = "()[]\"",
                ListType = -1
            });
        TextSpan token = new()
        {
            DocumentId = 1,
            P1 = 1,
            P2 = 1,
            Length = 8,
            Value = "\"(abc!)\""
        };

        await filter.ApplyAsync(token, token.P1);

        Assert.Single(token.Attributes!);
        Assert.NotNull(token.Attributes!.FirstOrDefault(
            a => a.Name == "rp" && a.Value == "!"));
    }

    [Fact]
    public async Task Apply_LeftPunctsWithWhites_Ok()
    {
        PunctuationTokenFilter filter = new();
        filter.Configure(
            new PunctuationTokenFilterOptions
            {
                Punctuations = ",:;.!?",
                ListType = 1
            });
        TextSpan token = new()
        {
            DocumentId = 1,
            P1 = 1,
            P2 = 1,
            Length = 8,
            Value = "\"(abc!)\""
        };

        await filter.ApplyAsync(token, token.P1);

        Assert.Single(token.Attributes!);
        Assert.NotNull(token.Attributes!.FirstOrDefault(
            a => a.Name == "rp" && a.Value == "!"));
    }

    [Fact]
    public async Task Apply_RightPunct_Ok()
    {
        PunctuationTokenFilter filter = new();
        TextSpan token = new()
        {
            DocumentId = 1,
            P1 = 1,
            P2 = 1,
            Length = 4,
            Value = "abc."
        };

        await filter.ApplyAsync(token, token.P1);

        Assert.Single(token.Attributes!);
        Assert.NotNull(token.Attributes!.FirstOrDefault(
            a => a.Name == "rp" && a.Value == "."));
    }

    [Fact]
    public async Task Apply_RightPuncts_Ok()
    {
        PunctuationTokenFilter filter = new();
        TextSpan token = new()
        {
            DocumentId = 1,
            P1 = 1,
            P2 = 1,
            Length = 5,
            Value = "abc?)"
        };

        await filter.ApplyAsync(token, token.P1);

        Assert.Single(token.Attributes!);
        Assert.NotNull(token.Attributes!.FirstOrDefault(
            a => a.Name == "rp" && a.Value == "?)"));
    }

    [Fact]
    public async Task Apply_LeftAndRightPuncts_Ok()
    {
        PunctuationTokenFilter filter = new();
        TextSpan token = new()
        {
            DocumentId = 1,
            P1 = 1,
            P2 = 1,
            Length = 6,
            Value = "(abc!)"
        };

        await filter.ApplyAsync(token, token.P1);

        Assert.Equal(2, token.Attributes!.Count);
        Assert.NotNull(token.Attributes!.FirstOrDefault(
            a => a.Name == "lp" && a.Value == "("));
        Assert.NotNull(token.Attributes!.FirstOrDefault(
            a => a.Name == "rp" && a.Value == "!)"));
    }

    [Fact]
    public async Task Apply_OnlyPuncts_Ok()
    {
        PunctuationTokenFilter filter = new();
        TextSpan token = new()
        {
            DocumentId = 1,
            P1 = 1,
            P2 = 1,
            Length = 5,
            Value = "(...)"
        };

        await filter.ApplyAsync(token, token.P1);

        Assert.Single(token.Attributes!);
        Assert.NotNull(token.Attributes!.FirstOrDefault(
            a => a.Name == "lp" && a.Value == "(...)"));
    }
}
