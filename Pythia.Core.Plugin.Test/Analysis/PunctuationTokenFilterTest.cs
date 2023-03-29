using Pythia.Core.Plugin.Analysis;
using System.Linq;
using Xunit;

namespace Pythia.Core.Plugin.Test.Analysis;

public sealed class PunctuationTokenFilterTest
{
    [Fact]
    public void Apply_Empty_NoAttr()
    {
        PunctuationTokenFilter filter = new();
        Token token = new()
        {
            DocumentId = 1,
            Position = 1,
            Length = 0,
            Value = ""
        };

        filter.Apply(token, token.Position);

        Assert.Empty(token.Attributes!);
    }

    [Fact]
    public void Apply_NoPunct_NoAttr()
    {
        PunctuationTokenFilter filter = new();
        Token token = new()
        {
            DocumentId = 1,
            Position = 1,
            Length = 3,
            Value = "abc"
        };

        filter.Apply(token, token.Position);

        Assert.Empty(token.Attributes!);
    }

    [Fact]
    public void Apply_LeftPunct_Ok()
    {
        PunctuationTokenFilter filter = new();
        Token token = new()
        {
            DocumentId = 1,
            Position = 1,
            Length = 4,
            Value = "(abc"
        };

        filter.Apply(token, token.Position);

        Assert.Single(token.Attributes!);
        Assert.NotNull(token.Attributes!.FirstOrDefault(
            a => a.Name == "lp" && a.Value == "("));
    }

    [Fact]
    public void Apply_LeftPuncts_Ok()
    {
        PunctuationTokenFilter filter = new();
        Token token = new()
        {
            DocumentId = 1,
            Position = 1,
            Length = 5,
            Value = "([abc"
        };

        filter.Apply(token, token.Position);

        Assert.Single(token.Attributes!);
        Assert.NotNull(token.Attributes!.FirstOrDefault(
            a => a.Name == "lp" && a.Value == "(["));
    }

    [Fact]
    public void Apply_RightPunct_Ok()
    {
        PunctuationTokenFilter filter = new();
        Token token = new()
        {
            DocumentId = 1,
            Position = 1,
            Length = 4,
            Value = "abc."
        };

        filter.Apply(token, token.Position);

        Assert.Single(token.Attributes!);
        Assert.NotNull(token.Attributes!.FirstOrDefault(
            a => a.Name == "rp" && a.Value == "."));
    }

    [Fact]
    public void Apply_RightPuncts_Ok()
    {
        PunctuationTokenFilter filter = new();
        Token token = new()
        {
            DocumentId = 1,
            Position = 1,
            Length = 5,
            Value = "abc?)"
        };

        filter.Apply(token, token.Position);

        Assert.Single(token.Attributes!);
        Assert.NotNull(token.Attributes!.FirstOrDefault(
            a => a.Name == "rp" && a.Value == "?)"));
    }

    [Fact]
    public void Apply_LeftAndRightPuncts_Ok()
    {
        PunctuationTokenFilter filter = new();
        Token token = new()
        {
            DocumentId = 1,
            Position = 1,
            Length = 6,
            Value = "(abc!)"
        };

        filter.Apply(token, token.Position);

        Assert.Equal(2, token.Attributes!.Count);
        Assert.NotNull(token.Attributes!.FirstOrDefault(
            a => a.Name == "lp" && a.Value == "("));
        Assert.NotNull(token.Attributes!.FirstOrDefault(
            a => a.Name == "rp" && a.Value == "!)"));
    }

    [Fact]
    public void Apply_OnlyPuncts_Ok()
    {
        PunctuationTokenFilter filter = new();
        Token token = new()
        {
            DocumentId = 1,
            Position = 1,
            Length = 5,
            Value = "(...)"
        };

        filter.Apply(token, token.Position);

        Assert.Single(token.Attributes!);
        Assert.NotNull(token.Attributes!.FirstOrDefault(
            a => a.Name == "lp" && a.Value == "(...)"));
    }
}
