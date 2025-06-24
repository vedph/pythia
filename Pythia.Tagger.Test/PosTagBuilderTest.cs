using System;
using System.IO;

namespace Pythia.Tagger.Test;

public class PosTagBuilderTest
{
    [Fact]
    public void Build_EmptyPos_ReturnsNull()
    {
        PosTagBuilder builder = new();

        string? result = builder.Build();

        Assert.Null(result);
    }

    [Fact]
    public void Build_PosWithNoFeatures_ReturnsPosOnly()
    {
        PosTagBuilder builder = new()
        {
            Pos = "NOUN"
        };

        string? result = builder.Build();

        Assert.Equal("NOUN", result);
    }

    [Fact]
    public void Build_PosWithFeaturesNoProfile_ReturnsOrderedFeatures()
    {
        PosTagBuilder builder = new()
        {
            Pos = "NOUN"
        };
        builder.Features["Number"] = "Plural";
        builder.Features["Gender"] = "Feminine";

        string? result = builder.Build();

        Assert.Equal("NOUN:Gender=Feminine|Number=Plural", result);
    }

    [Fact]
    public void Build_PosWithFeaturesWithProfile_ReturnsProfileOrderedFeatures()
    {
        PosTagBuilder builder = new()
        {
            Pos = "NOUN"
        };
        builder.Profile["NOUN"] = ["Number", "Gender"];
        builder.Features["Number"] = "Plural";
        builder.Features["Gender"] = "Feminine";

        string? result = builder.Build();

        Assert.Equal("NOUN:Number=Plural|Gender=Feminine", result);
    }

    [Fact]
    public void Build_CustomSeparators_UsesCustomSeparators()
    {
        PosTagBuilder builder = new()
        {
            Pos = "NOUN",
            PosSeparator = "_",
            FeatSeparator = "#"
        };
        builder.Features["Number"] = "Plural";
        builder.Features["Gender"] = "Feminine";

        string? result = builder.Build();

        Assert.Equal("NOUN_Gender=Feminine#Number=Plural", result);
    }

    [Fact]
    public void Build_PrependKeyFalse_OmitsKeys()
    {
        PosTagBuilder builder = new()
        {
            Pos = "NOUN",
            PrependKey = false
        };
        builder.Profile["NOUN"] = ["Number", "Gender"];
        builder.Features["Number"] = "Plural";
        builder.Features["Gender"] = "Feminine";

        string? result = builder.Build();

        Assert.Equal("NOUN:Plural|Feminine", result);
    }

    [Fact]
    public void Build_PrependKeyFalseWithMissingFeatures_PreservesPositions()
    {
        PosTagBuilder builder = new()
        {
            Pos = "NOUN",
            PrependKey = false
        };
        builder.Profile["NOUN"] = ["Number", "Gender", "Case"];
        builder.Features["Number"] = "Plural";
        // Gender is missing
        builder.Features["Case"] = "Nominative";

        string? result = builder.Build();

        // should preserve position with empty value between Plural and Nominative
        Assert.Equal("NOUN:Plural||Nominative", result);
    }

    [Fact]
    public void Build_PrependKeyTrueWithMissingFeatures_SkipsMissingFeatures()
    {
        PosTagBuilder builder = new()
        {
            Pos = "NOUN",
            PrependKey = true
        };
        builder.Profile["NOUN"] = ["Number", "Gender", "Case"];
        builder.Features["Number"] = "Plural";
        // Gender is missing
        builder.Features["Case"] = "Nominative";

        string? result = builder.Build();

        // should skip missing Gender feature completely, no double separators
        Assert.Equal("NOUN:Number=Plural|Case=Nominative", result);
    }

    [Fact]
    public void Build_PrependKeyFalseNoProfileWithMissingFeatures_OmitsEmptyFeatures()
    {
        PosTagBuilder builder = new()
        {
            Pos = "NOUN",
            PrependKey = false
        };
        // No profile defined
        builder.Features["Number"] = "Plural";
        builder.Features["Gender"] = "";  // empty feature value
        builder.Features["Case"] = "Nominative";

        string? result = builder.Build();

        // without profile, should only include non-empty values in alphabetical order
        Assert.Equal("NOUN:Nominative|Plural", result);
    }

    [Fact]
    public void Build_PrependKeyFalseWithAllFeaturesEmpty_OnlyIncludesPosAndSeparator()
    {
        PosTagBuilder builder = new()
        {
            Pos = "NOUN",
            PrependKey = false
        };
        builder.Profile["NOUN"] = ["Number", "Gender", "Case"];
        // all features are missing

        string? result = builder.Build();

        // should include POS only
        Assert.Equal("NOUN", result);
    }

    [Fact]
    public void Parse_PosWithEmptyFeaturePositions_ParsesCorrectly()
    {
        PosTagBuilder builder = new() { PrependKey = false };
        builder.Profile["NOUN"] = ["Number", "Gender", "Case"];

        PosTag? result = builder.Parse("NOUN:Plural||Nominative");

        Assert.NotNull(result);
        Assert.Equal("NOUN", result.Pos);
        Assert.Equal(2, result.Features.Count);
        Assert.Equal("Plural", result.Features["Number"]);
        Assert.False(result.Features.ContainsKey("Gender"));
        Assert.Equal("Nominative", result.Features["Case"]);
    }

    [Fact]
    public void Parse_NullOrEmptyText_ReturnsNull()
    {
        PosTagBuilder builder = new();

        Assert.Null(builder.Parse(null));
        Assert.Null(builder.Parse(""));
    }

    [Fact]
    public void Parse_PosOnlyText_ReturnsPosTag()
    {
        PosTagBuilder builder = new();

        PosTag? result = builder.Parse("NOUN");

        Assert.NotNull(result);
        Assert.Equal("NOUN", result.Pos);
        Assert.Empty(result.Features);
    }

    [Fact]
    public void Parse_PosWithFeaturesWithPrependKey_ReturnsParsedTag()
    {
        PosTagBuilder builder = new() { PrependKey = true };

        PosTag? result = builder.Parse("NOUN:Number=Plural|Gender=Feminine");

        Assert.NotNull(result);
        Assert.Equal("NOUN", result.Pos);
        Assert.Equal(2, result.Features.Count);
        Assert.Equal("Plural", result.Features["Number"]);
        Assert.Equal("Feminine", result.Features["Gender"]);
    }

    [Fact]
    public void Parse_PosWithFeaturesWithoutPrependKeyUsingProfile_ReturnsParsedTag()
    {
        PosTagBuilder builder = new() { PrependKey = false };
        builder.Profile["NOUN"] = ["Number", "Gender"];

        PosTag? result = builder.Parse("NOUN:Plural|Feminine");

        Assert.NotNull(result);
        Assert.Equal("NOUN", result.Pos);
        Assert.Equal(2, result.Features.Count);
        Assert.Equal("Plural", result.Features["Number"]);
        Assert.Equal("Feminine", result.Features["Gender"]);
    }

    [Fact]
    public void Parse_FeatureWithoutEqualsSign_HandlesProperly()
    {
        PosTagBuilder builder = new() { PrependKey = true };

        PosTag? result = builder.Parse("NOUN:MissingEquals|Gender=Feminine");

        Assert.NotNull(result);
        Assert.Equal("NOUN", result.Pos);
        Assert.Equal(2, result.Features.Count);
        Assert.Equal("MissingEquals", result.Features["MissingEquals"]);
        Assert.Equal("Feminine", result.Features["Gender"]);
    }

    [Fact]
    public void LoadProfile_ValidReader_LoadsProfileCorrectly()
    {
        PosTagBuilder builder = new();
        string profileText = "NOUN,Number,Gender,Case\n" +
            "VERB,Person,Number,Tense,Aspect";
        StringReader reader = new(profileText);

        builder.LoadProfile(reader);

        Assert.Equal(2, builder.Profile.Count);
        Assert.Equal(new[] { "Number", "Gender", "Case" },
            builder.Profile["NOUN"]);
        Assert.Equal(new[] { "Person", "Number", "Tense", "Aspect" },
            builder.Profile["VERB"]);
    }

    [Fact]
    public void LoadProfile_EmptyLines_IgnoresEmptyLines()
    {
        PosTagBuilder builder = new();
        string profileText = "NOUN,Number,Gender\n\nVERB,Tense";
        StringReader reader = new(profileText);

        builder.LoadProfile(reader);

        Assert.Equal(2, builder.Profile.Count);
        Assert.Equal(new[] { "Number", "Gender" }, builder.Profile["NOUN"]);
        Assert.Equal(new[] { "Tense" }, builder.Profile["VERB"]);
    }

    [Fact]
    public void LoadProfile_InvalidLines_IgnoresInvalidLines()
    {
        PosTagBuilder builder = new();
        string profileText = "NOUN,Number,Gender\nADJ\nVERB,Tense";
        StringReader reader = new(profileText);

        builder.LoadProfile(reader);

        Assert.Equal(2, builder.Profile.Count);
        Assert.Equal(new[] { "Number", "Gender" }, builder.Profile["NOUN"]);
        Assert.Equal(new[] { "Tense" }, builder.Profile["VERB"]);
    }

    [Fact]
    public void LoadProfile_NullReader_ThrowsArgumentNullException()
    {
        PosTagBuilder builder = new();

        Assert.Throws<ArgumentNullException>(() => builder.LoadProfile(null!));
    }
}
