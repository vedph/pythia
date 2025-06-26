using Pythia.Tagger.Lookup;
using System;
using Xunit;

namespace Pythia.Tagger.Ita.Plugin.Test;

public sealed class MorphitConverterTest
{
    private readonly MorphitConverter _converter = new(
        new RamLookupIndex());

    [Fact]
    public void ParseTag_NullInput_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _converter.ParseTag(null!));
    }

    [Fact]
    public void ParseTag_UnknownPos_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() =>
            _converter.ParseTag("UNKNOWN"));
        Assert.Throws<InvalidOperationException>(() =>
            _converter.ParseTag("UNKNOWN:feat1+feat2"));
    }

    [Fact]
    public void ParseTag_UnknownFeature_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() =>
            _converter.ParseTag("VER:unknown"));
    }

    [Fact]
    public void ParseTag_SimplePos_ReturnsCorrectBuilder()
    {
        PosTagBuilder result = _converter.ParseTag("ADV");

        Assert.NotNull(result);
        Assert.Equal(UDTags.ADV, result.Pos);
        Assert.Empty(result.Features);
    }

    [Fact]
    public void ParseTag_PosWithFeatures_ReturnsCorrectBuilder()
    {
        PosTagBuilder result = _converter.ParseTag("VER:cond+pres+3+p");

        Assert.NotNull(result);
        Assert.Equal(UDTags.VERB, result.Pos);
        Assert.Equal(4, result.Features.Count);
        Assert.Equal(UDTags.MOOD_CONDITIONAL, result.Features[UDTags.FEAT_MOOD]);
        Assert.Equal(UDTags.TENSE_PRESENT, result.Features[UDTags.FEAT_TENSE]);
        Assert.Equal(UDTags.PERSON_THIRD, result.Features[UDTags.FEAT_PERSON]);
        Assert.Equal(UDTags.NUMBER_PLURAL, result.Features[UDTags.FEAT_NUMBER]);
    }

    [Fact]
    public void ParseTag_PosWithFirstPersonSingular_ReturnsCorrectBuilder()
    {
        PosTagBuilder result = _converter.ParseTag("VER:cond+pres+1+s");

        Assert.NotNull(result);
        Assert.Equal(UDTags.VERB, result.Pos);
        Assert.Equal(4, result.Features.Count);
        Assert.Equal(UDTags.MOOD_CONDITIONAL, result.Features[UDTags.FEAT_MOOD]);
        Assert.Equal(UDTags.TENSE_PRESENT, result.Features[UDTags.FEAT_TENSE]);
        Assert.Equal(UDTags.PERSON_FIRST, result.Features[UDTags.FEAT_PERSON]);
        Assert.Equal(UDTags.NUMBER_SINGULAR, result.Features[UDTags.FEAT_NUMBER]);
    }

    [Fact]
    public void ParseTag_AdjWithFeatures_ReturnsCorrectBuilder()
    {
        PosTagBuilder result = _converter.ParseTag("ADJ:pos+f+p");

        Assert.NotNull(result);
        Assert.Equal(UDTags.ADJ, result.Pos);
        Assert.Equal(3, result.Features.Count);
        Assert.Equal(UDTags.DEGREE_POSITIVE, result.Features[UDTags.FEAT_DEGREE]);
        Assert.Equal(UDTags.GENDER_FEMININE, result.Features[UDTags.FEAT_GENDER]);
        Assert.Equal(UDTags.NUMBER_PLURAL, result.Features[UDTags.FEAT_NUMBER]);
    }

    [Fact]
    public void ParseTag_PosWithImpliedFeatures_ReturnsCorrectBuilder()
    {
        PosTagBuilder result = _converter.ParseTag("NOUN-M");

        Assert.NotNull(result);
        Assert.Equal(UDTags.NOUN, result.Pos);
        Assert.Single(result.Features);
        Assert.Equal(UDTags.GENDER_MASCULINE, result.Features[UDTags.FEAT_GENDER]);
    }

    [Fact]
    public void ParseTag_DeterminativeWithImpliedFeatures_ReturnsCorrectBuilder()
    {
        PosTagBuilder result = _converter.ParseTag("DET-DEMO");

        Assert.NotNull(result);
        Assert.Equal(UDTags.DET, result.Pos);
        Assert.Single(result.Features);
        Assert.Equal(UDTags.PRONTYPE_DEMONSTRATIVE,
            result.Features[UDTags.FEAT_PRONTYPE]);
    }

    [Fact]
    public void ParseTag_MultipleFeaturesWithRedundantTags_ReturnsCorrectBuilder()
    {
        // a feature might be specified both in the POS and in features
        PosTagBuilder result = _converter.ParseTag("NOUN-F:p");

        Assert.NotNull(result);
        Assert.Equal(UDTags.NOUN, result.Pos);
        Assert.Equal(2, result.Features.Count);
        Assert.Equal(UDTags.GENDER_FEMININE, result.Features[UDTags.FEAT_GENDER]);
        Assert.Equal(UDTags.NUMBER_PLURAL, result.Features[UDTags.FEAT_NUMBER]);
    }

    [Fact]
    public void ParseTag_VerbWithEnclitics_ReturnsCorrectBuilder()
    {
        string[] enclitics =
        [
            "mi", "ti", "lo", "le", "la", "li", "ci", "ne", "gli",
            "vi", "si",
            "melo", "mela", "meli", "mele", "mene",
            "telo", "tela", "teli", "tele", "tene",
            "glielo", "gliela", "glieli", "gliele", "gliene",
            "selo", "sela", "seli", "sele", "sene",
            "celo", "cela", "celi", "cele", "cene",
            "velo", "vela", "veli", "vele", "vene"
        ];

        foreach (string enclitic in enclitics)
        {
            PosTagBuilder result = _converter.ParseTag($"VER:inf+pres+{enclitic}");

            Assert.NotNull(result);
            Assert.Equal(UDTags.VERB, result.Pos);
            Assert.Equal(3, result.Features.Count);
            Assert.Equal(UDTags.VERBFORM_INFINITIVE,
                result.Features[UDTags.FEAT_VERBFORM]);
            Assert.Equal(UDTags.TENSE_PRESENT, result.Features[UDTags.FEAT_TENSE]);
            Assert.Equal(enclitic, result.Features[MorphitConverter.FEAT_ENCLITIC]);
        }
    }
}