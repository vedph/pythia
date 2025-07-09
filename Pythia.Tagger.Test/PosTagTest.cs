using System;
using System.Collections.Generic;
using Xunit;

namespace Pythia.Tagger.Test;

public sealed class PosTagTest
{
    [Fact]
    public void Constructor_Default_SetsEmptyValues()
    {
        PosTag tag = new();

        Assert.Equal("", tag.Pos);
        Assert.Empty(tag.Features);
    }

    [Fact]
    public void Constructor_WithPosOnly_SetsPosCorrectly()
    {
        PosTag tag = new("NOUN");

        Assert.Equal("NOUN", tag.Pos);
        Assert.Empty(tag.Features);
    }

    [Fact]
    public void Constructor_WithPosAndFeatures_SetsValuesCorrectly()
    {
        Dictionary<string, string> features = new()
        {
            { "Number", "Plural" },
            { "Gender", "Masculine" }
        };

        PosTag tag = new("NOUN", features);

        Assert.Equal("NOUN", tag.Pos);
        Assert.Equal(2, tag.Features.Count);
        Assert.Equal("Plural", tag.Features["Number"]);
        Assert.Equal("Masculine", tag.Features["Gender"]);
    }

    [Fact]
    public void Constructor_WithNullPos_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new PosTag(null!));
    }

    [Fact]
    public void ToString_WithPosOnly_ReturnsPosOnly()
    {
        PosTag tag = new("VERB");
        Assert.Equal("VERB", tag.ToString());
    }

    [Fact]
    public void ToString_WithPosAndFeatures_ReturnsCorrectFormat()
    {
        Dictionary<string, string> features = new()
        {
            { "Number", "Singular" },
            { "Tense", "Present" }
        };

        PosTag tag = new("VERB", features);

        // features are ordered alphabetically
        Assert.Equal("VERB:Number=Singular|Tense=Present", tag.ToString());
    }

    [Theory]
    [InlineData("NOUN", true)]
    [InlineData("VERB", false)]
    public void IsMatch_WithPosOnly_ChecksPosOnly(string pos, bool expectedResult)
    {
        PosTag tag = new("NOUN");
        Assert.Equal(expectedResult, tag.IsMatch(pos));
    }

    [Fact]
    public void IsMatch_WithPosAndNoFeatures_ReturnsTrueForMatchingPos()
    {
        PosTag tag = new("ADJ", new Dictionary<string, string> {
            { "Degree", "Comparative" }
        });
        Assert.True(tag.IsMatch("ADJ"));
    }

    [Fact]
    public void IsMatch_WithPosAndFeatures_MatchesAllFeatures()
    {
        Dictionary<string, string> features = new()
        {
            { "Number", "Plural" },
            { "Gender", "Feminine" }
        };

        PosTag tag = new("NOUN", features);

        // matching all features
        Assert.True(tag.IsMatch("NOUN", "Number", "Plural", "Gender", "Feminine"));

        // missing one feature
        Assert.False(tag.IsMatch("NOUN", "Number", "Plural", "Gender", "Masculine"));

        // missing feature key
        Assert.False(tag.IsMatch("NOUN", "Number", "Plural", "Case", "Nominative"));
    }

    [Fact]
    public void IsMatch_WithQueryString_PositiveSimpleTests()
    {
        Dictionary<string, string> features = new()
        {
            { "Number", "Plural" },
            { "Gender", "Masculine" },
            { "Case", "Nominative" }
        };

        PosTag tag = new("NOUN", features);

        // simple equality
        Assert.True(tag.IsMatch("NOUN", "Number=Plural"));
        Assert.True(tag.IsMatch("NOUN", "Gender=Masculine"));

        // simple inequality
        Assert.True(tag.IsMatch("NOUN", "Number!=Singular"));
        Assert.False(tag.IsMatch("NOUN", "Number!=Plural"));

        // non-existent feature with equality (should be false)
        Assert.False(tag.IsMatch("NOUN", "Tense=Present"));

        // non-existent feature with inequality (should be true)
        Assert.True(tag.IsMatch("NOUN", "Tense!=Present"));
    }

    [Fact]
    public void IsMatch_WithQueryString_LogicalOperatorTests()
    {
        Dictionary<string, string> features = new()
        {
            { "Number", "Plural" },
            { "Gender", "Masculine" },
            { "Case", "Nominative" }
        };

        PosTag tag = new("NOUN", features);

        // AND operation (true && true)
        Assert.True(tag.IsMatch("NOUN", "Number=Plural AND Gender=Masculine"));

        // AND operation (true && false)
        Assert.False(tag.IsMatch("NOUN", "Number=Plural AND Gender=Feminine"));

        // OR operation (true || false)
        Assert.True(tag.IsMatch("NOUN", "Number=Plural OR Gender=Feminine"));

        // OR operation (false || false)
        Assert.False(tag.IsMatch("NOUN", "Number=Singular OR Gender=Feminine"));
    }

    [Fact]
    public void IsMatch_WithQueryString_GroupingTests()
    {
        Dictionary<string, string> features = new()
        {
            { "Number", "Plural" },
            { "Gender", "Masculine" },
            { "Case", "Nominative" }
        };

        PosTag tag = new("NOUN", features);

        // simple grouping
        Assert.True(tag.IsMatch("NOUN", "(Number=Plural)"));

        // complex grouping with AND and OR
        Assert.True(tag.IsMatch("NOUN",
            "(Number=Plural AND Gender=Masculine) OR Case=Accusative"));
        Assert.False(tag.IsMatch("NOUN",
            "(Number=Plural AND Gender=Feminine) OR Case=Accusative"));

        // nested grouping
        Assert.True(tag.IsMatch("NOUN",
            "(Number=Plural AND (Gender=Masculine OR Case=Accusative))"));
        Assert.False(tag.IsMatch("NOUN",
            "(Number=Singular AND (Gender=Masculine OR Case=Accusative))"));
    }

    [Fact]
    public void IsMatch_WithInvalidQueryString_ThrowsFormatException()
    {
        PosTag tag = new("NOUN", new Dictionary<string, string> {
            { "Number", "Plural" } });

        // Missing closing bracket
        Assert.Throws<FormatException>(() =>
            tag.IsMatch("NOUN", "(Number=Plural"));

        // Invalid operator
        Assert.Throws<FormatException>(() =>
            tag.IsMatch("NOUN", "Number!Plural"));

        // Incomplete condition
        Assert.Throws<FormatException>(() => tag.IsMatch("NOUN", "Number"));
    }

    [Fact]
    public void IsMatch_WithWhitespaceInQuery_HandlesCorrectly()
    {
        Dictionary<string, string> features = new()
        {
            { "Number", "Plural" },
            { "Gender", "Masculine" }
        };

        PosTag tag = new("NOUN", features);

        // whitespace around operators
        Assert.True(tag.IsMatch("NOUN", "Number = Plural"));
        Assert.True(tag.IsMatch("NOUN", "Number   =   Plural"));

        // whitespace around AND/OR
        Assert.True(tag.IsMatch("NOUN", "Number=Plural   AND   Gender=Masculine"));
        Assert.True(tag.IsMatch("NOUN", "Number=Singular   OR   Gender=Masculine"));

        // whitespace around brackets
        Assert.True(tag.IsMatch("NOUN", "  (  Number=Plural  )  "));
    }
}