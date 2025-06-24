using System.Collections.Generic;
using Pythia.Tagger.Lookup;
using Xunit;

namespace Pythia.Tagger.Ita.Plugin.Test;

public sealed class ItalianVariantBuilderTest
{
    private static ItalianVariantBuilderOptions GetZeroOptions()
    {
        ItalianVariantBuilderOptions options = new();
        options.SetAll(false);
        return options;
    }

    #region Superlatives
    [Fact]
    public void Build_Bellissimo_Bello()
    {
        ItalianVariantBuilderOptions options = GetZeroOptions();
        options.Superlatives = true;
        ItalianVariantBuilder builder = new();
        builder.Configure(options);

        ILookupIndex index = new RamLookupIndex([new LookupEntry
        {
            Pos = UDTags.ADJ,
            Value = "bello",
            Text = "bello"
        }]);

        IList<Variant> variants = builder.Build("bellissimo", UDTags.ADJ, index);

        Assert.Single(variants);
        Variant variant = variants[0];
        Assert.Equal("bello", variant.Value);
        Assert.Equal(UDTags.ADJ, variant.Pos);
        Assert.Equal("bellissimo", variant.Source);
        Assert.Equal("super", variant.Type);
    }

    [Fact]
    public void Build_Pochissimo_Poco()
    {
        ItalianVariantBuilderOptions options = GetZeroOptions();
        options.Superlatives = true;
        ItalianVariantBuilder builder = new();
        builder.Configure(options);

        RamLookupIndex index = new([new LookupEntry
        {
            Pos = UDTags.ADJ,
            Value = "poco",
            Text = "poco"
        }]);

        IList<Variant> variants = builder.Build("pochissimo", UDTags.ADJ, index);

        Assert.Single(variants);
        Variant variant = variants[0];
        Assert.Equal("poco", variant.Value);
        Assert.Equal(UDTags.ADJ, variant.Pos);
        Assert.Equal("pochissimo", variant.Source);
        Assert.Equal("super", variant.Type);
    }

    [Fact]
    public void Build_Abilissimo_Abile()
    {
        ItalianVariantBuilderOptions options = GetZeroOptions();
        options.Superlatives = true;
        ItalianVariantBuilder builder = new();
        builder.Configure(options);

        RamLookupIndex index = new([new LookupEntry
        {
            Pos = UDTags.ADJ,
            Value = "abile",
            Text = "abile"
        }]);

        IList<Variant> variants = builder.Build("abilissimo", UDTags.ADJ, index);

        Assert.Single(variants);
        Variant variant = variants[0];
        Assert.Equal("abile", variant.Value);
        Assert.Equal(UDTags.ADJ, variant.Pos);
        Assert.Equal("abilissimo", variant.Source);
        Assert.Equal("super", variant.Type);
    }
    #endregion

    #region Enclitic groups
    [Fact]
    public void Build_Dammi_Da()
    {
        ItalianVariantBuilderOptions options = GetZeroOptions();
        options.EncliticGroups = true;
        ItalianVariantBuilder builder = new();
        builder.Configure(options);

        RamLookupIndex index = new([new LookupEntry
        {
            Pos = new ItalianPosTagBuilder(UDTags.VERB,
                UDTags.FEAT_MOOD, UDTags.MOOD_IMPERATIVE).Build(),
            Value = "da'",
            Text = "da'"
        }]);

        IList<Variant> variants = builder.Build("dammi", null, index);

        Assert.Single(variants);
        Variant variant = variants[0];
        Assert.Equal("da'", variant.Value);
        Assert.Equal($"{UDTags.VERB}:{UDTags.FEAT_MOOD}={UDTags.MOOD_IMPERATIVE}",
            variant.Pos);
        Assert.Equal("dammi", variant.Source);
        Assert.Equal("enclitic", variant.Type);
    }

    [Fact]
    public void Build_Leggilo_Leggi()
    {
        ItalianVariantBuilderOptions options = GetZeroOptions();
        options.EncliticGroups = true;
        ItalianVariantBuilder builder = new();
        builder.Configure(options);

        RamLookupIndex index = new([new LookupEntry
        {
            Pos = new ItalianPosTagBuilder(UDTags.VERB,
                UDTags.FEAT_MOOD, UDTags.MOOD_IMPERATIVE).Build(),
            Value = "leggi",
            Text = "leggi"
        }]);

        IList<Variant> variants = builder.Build("leggilo", null, index);

        Assert.Single(variants);
        Variant variant = variants[0];
        Assert.Equal("leggi", variant.Value);
        Assert.Equal($"{UDTags.VERB}:{UDTags.FEAT_MOOD}={UDTags.MOOD_IMPERATIVE}",
            variant.Pos);
        Assert.Equal("leggilo", variant.Source);
        Assert.Equal("enclitic", variant.Type);
    }

    [Fact]
    public void Build_Fermiamoci_Fermiamo()
    {
        ItalianVariantBuilderOptions options = GetZeroOptions();
        options.EncliticGroups = true;
        ItalianVariantBuilder builder = new();
        builder.Configure(options);

        RamLookupIndex index = new([new LookupEntry
        {
            Pos = new ItalianPosTagBuilder(UDTags.VERB,
                UDTags.FEAT_MOOD, UDTags.MOOD_IMPERATIVE,
                UDTags.FEAT_PERSON, UDTags.PERSON_FIRST,
                UDTags.FEAT_NUMBER, UDTags.NUMBER_PLURAL).Build(),
            Value = "fermiamo",
            Text = "fermiamo"
        }]);

        IList<Variant> variants = builder.Build("fermiamoci", null, index);

        Assert.Single(variants);
        Variant variant = variants[0];
        Assert.Equal("fermiamo", variant.Value);
        Assert.Equal($"{UDTags.VERB}:{UDTags.FEAT_MOOD}={UDTags.MOOD_IMPERATIVE}" +
            $"|{UDTags.FEAT_PERSON}={UDTags.PERSON_FIRST}" +
            $"|{UDTags.FEAT_NUMBER}={UDTags.NUMBER_PLURAL}",
            variant.Pos);
        Assert.Equal("fermiamoci", variant.Source);
        Assert.Equal("enclitic", variant.Type);
    }

    [Fact]
    public void Build_Andarci_Andare()
    {
        ItalianVariantBuilderOptions options = GetZeroOptions();
        options.EncliticGroups = true;
        ItalianVariantBuilder builder = new();
        builder.Configure(options);

        RamLookupIndex index = new([new LookupEntry
        {
            Pos = new ItalianPosTagBuilder(UDTags.VERB,
                UDTags.FEAT_VERBFORM, UDTags.VERBFORM_INFINITIVE).Build(),
            Value = "andare",
            Text = "andare"
        }]);

        IList<Variant> variants = builder.Build("andarci", null, index);

        Assert.Single(variants);
        Variant variant = variants[0];
        Assert.Equal("andare", variant.Value);
        Assert.Equal($"{UDTags.VERB}:{UDTags.FEAT_VERBFORM}={UDTags.VERBFORM_INFINITIVE}",
            variant.Pos);
        Assert.Equal("andarci", variant.Source);
        Assert.Equal("enclitic", variant.Type);
    }

    [Fact]
    public void Build_Porgli_Porre()
    {
        ItalianVariantBuilderOptions options = GetZeroOptions();
        options.EncliticGroups = true;
        ItalianVariantBuilder builder = new();
        builder.Configure(options);

        RamLookupIndex index = new([new LookupEntry
        {
            Pos = new ItalianPosTagBuilder(UDTags.VERB,
                UDTags.FEAT_VERBFORM, UDTags.VERBFORM_INFINITIVE).Build(),
            Value = "porre",
            Text = "porre"
        }]);

        IList<Variant> variants = builder.Build("porgli", null, index);

        Assert.Single(variants);
        Variant variant = variants[0];
        Assert.Equal("porre", variant.Value);
        Assert.Equal(
            $"{UDTags.VERB}:{UDTags.FEAT_VERBFORM}={UDTags.VERBFORM_INFINITIVE}",
            variant.Pos);
        Assert.Equal("porgli", variant.Source);
        Assert.Equal("enclitic", variant.Type);
    }

    [Fact]
    public void Build_Avendomi_Avendo()
    {
        ItalianVariantBuilderOptions options = GetZeroOptions();
        options.EncliticGroups = true;
        ItalianVariantBuilder builder = new();
        builder.Configure(options);

        RamLookupIndex index = new([new LookupEntry
        {
            Pos = new ItalianPosTagBuilder(UDTags.VERB,
                UDTags.FEAT_VERBFORM, UDTags.VERBFORM_GERUND).Build(),
            Value = "avendo",
            Text = "avendo"
        }]);

        IList<Variant> variants = builder.Build("avendomi", null, index);

        Assert.Single(variants);
        Variant variant = variants[0];
        Assert.Equal("avendo", variant.Value);
        Assert.Equal(
            $"{UDTags.VERB}:{UDTags.FEAT_VERBFORM}={UDTags.VERBFORM_GERUND}",
            variant.Pos);
        Assert.Equal("avendomi", variant.Source);
        Assert.Equal("enclitic", variant.Type);
    }

    [Fact]
    public void Build_Allontanatomi_Allontanato()
    {
        ItalianVariantBuilderOptions options = GetZeroOptions();
        options.EncliticGroups = true;
        ItalianVariantBuilder builder = new();
        builder.Configure(options);

        RamLookupIndex index = new([new LookupEntry
        {
            Pos = new ItalianPosTagBuilder(UDTags.VERB,
                UDTags.FEAT_VERBFORM, UDTags.VERBFORM_PARTICIPLE,
                UDTags.FEAT_TENSE, UDTags.TENSE_PAST).Build(),
            Value = "allontanato",
            Text = "allontanato"
        }]);

        IList<Variant> variants = builder.Build("allontanatomi", null, index);

        Assert.Single(variants);
        Variant variant = variants[0];
        Assert.Equal("allontanato", variant.Value);
        Assert.Equal(
            $"{UDTags.VERB}:{UDTags.FEAT_VERBFORM}={UDTags.VERBFORM_PARTICIPLE}" +
            $"|{UDTags.FEAT_TENSE}={UDTags.TENSE_PAST}",
            variant.Pos);
        Assert.Equal("allontanatomi", variant.Source);
        Assert.Equal("enclitic", variant.Type);
    }

    [Fact]
    public void Build_Intrecciantesi_Intrecciante()
    {
        ItalianVariantBuilderOptions options = GetZeroOptions();
        options.EncliticGroups = true;
        ItalianVariantBuilder builder = new();
        builder.Configure(options);

        RamLookupIndex index = new([new LookupEntry
        {
            Pos = new ItalianPosTagBuilder(UDTags.VERB,
                UDTags.FEAT_VERBFORM, UDTags.VERBFORM_PARTICIPLE,
                UDTags.FEAT_TENSE, UDTags.TENSE_PRESENT).Build(),
            Value = "intrecciante",
            Text = "intrecciante"
        }]);

        IList<Variant> variants = builder.Build("intrecciantesi", null, index);

        Assert.Single(variants);
        Variant variant = variants[0];
        Assert.Equal("intrecciante", variant.Value);
        Assert.Equal(
            $"{UDTags.VERB}:{UDTags.FEAT_VERBFORM}={UDTags.VERBFORM_PARTICIPLE}" +
            $"|{UDTags.FEAT_TENSE}={UDTags.TENSE_PRESENT}",
            variant.Pos);
        Assert.Equal("intrecciantesi", variant.Source);
        Assert.Equal("enclitic", variant.Type);
    }
    #endregion

    #region Untruncated
    [Fact]
    public void Build_Suor_Suora()
    {
        ItalianVariantBuilderOptions options = GetZeroOptions();
        options.UntruncatedVariants = true;
        ItalianVariantBuilder builder = new();
        builder.Configure(options);

        RamLookupIndex index = new([new LookupEntry
        {
            Pos = "S",
            Value = "suora",
            Text = "suora"
        }]);

        IList<Variant> variants = builder.Build("suor", null, index);

        Assert.Single(variants);
        Variant variant = variants[0];
        Assert.Equal("suora", variant.Value);
        Assert.Equal("S", variant.Pos);
        Assert.Equal("suor", variant.Source);
        Assert.Equal("untruncated", variant.Type);
    }

    [Fact]
    public void Build_Cuor_Cuore()
    {
        ItalianVariantBuilderOptions options = GetZeroOptions();
        options.UntruncatedVariants = true;
        ItalianVariantBuilder builder = new();
        builder.Configure(options);

        RamLookupIndex index = new([new LookupEntry
        {
            Pos = "S",
            Value = "cuore",
            Text = "cuore"
        }]);

        IList<Variant> variants = builder.Build("cuor", null, index);

        Assert.Single(variants);
        Variant variant = variants[0];
        Assert.Equal("cuore", variant.Value);
        Assert.Equal("S", variant.Pos);
        Assert.Equal("cuor", variant.Source);
        Assert.Equal("untruncated", variant.Type);
    }

    [Fact]
    public void Build_Tor_Torre()
    {
        ItalianVariantBuilderOptions options = GetZeroOptions();
        options.UntruncatedVariants = true;
        ItalianVariantBuilder builder = new();
        builder.Configure(options);

        RamLookupIndex index = new([new LookupEntry
        {
            Pos = "S",
            Value = "torre",
            Text = "torre"
        }]);

        IList<Variant> variants = builder.Build("tor", null, index);

        Assert.Single(variants);
        Variant variant = variants[0];
        Assert.Equal("torre", variant.Value);
        Assert.Equal("S", variant.Pos);
        Assert.Equal("tor", variant.Source);
        Assert.Equal("untruncated", variant.Type);
    }
    #endregion

    #region Unelided
    [Fact]
    public void Build_Bell_Bello()
    {
        ItalianVariantBuilderOptions options = GetZeroOptions();
        options.UnelidedVariants = true;
        ItalianVariantBuilder builder = new();
        builder.Configure(options);

        RamLookupIndex index = new([new LookupEntry
        {
            Pos = UDTags.ADJ,
            Value = "bello",
            Text = "bello"
        }]);

        IList<Variant> variants = builder.Build("bell'", null, index);

        Assert.Single(variants);
        Variant variant = variants[0];
        Assert.Equal("bello", variant.Value);
        Assert.Equal(UDTags.ADJ, variant.Pos);
        Assert.Equal("bell'", variant.Source);
        Assert.Equal("elided", variant.Type);
    }

    [Fact]
    public void Build_Bell_Bella()
    {
        ItalianVariantBuilderOptions options = GetZeroOptions();
        options.UnelidedVariants = true;
        ItalianVariantBuilder builder = new();
        builder.Configure(options);

        RamLookupIndex index = new([new LookupEntry
        {
            Pos = UDTags.ADJ,
            Value = "bella",
            Text = "bella"
        }]);

        IList<Variant> variants = builder.Build("bell'", null, index);

        Assert.Single(variants);
        Variant variant = variants[0];
        Assert.Equal("bella", variant.Value);
        Assert.Equal(UDTags.ADJ, variant.Pos);
        Assert.Equal("bell'", variant.Source);
        Assert.Equal("elided", variant.Type);
    }

    [Fact]
    public void Build_Bell_Belli()
    {
        ItalianVariantBuilderOptions options = GetZeroOptions();
        options.UnelidedVariants = true;
        ItalianVariantBuilder builder = new();
        builder.Configure(options);

        RamLookupIndex index = new([new LookupEntry
        {
            Pos = UDTags.ADJ,
            Value = "belli",
            Text = "belli"
        }]);

        IList<Variant> variants = builder.Build("bell'", null, index);

        Assert.Single(variants);
        Variant variant = variants[0];
        Assert.Equal("belli", variant.Value);
        Assert.Equal(UDTags.ADJ, variant.Pos);
        Assert.Equal("bell'", variant.Source);
        Assert.Equal("elided", variant.Type);
    }

    [Fact]
    public void Build_Bell_Belle()
    {
        ItalianVariantBuilderOptions options = GetZeroOptions();
        options.UnelidedVariants = true;
        ItalianVariantBuilder builder = new();
        builder.Configure(options);

        RamLookupIndex index = new([new LookupEntry
        {
            Pos = UDTags.ADJ,
            Value = "belle",
            Text = "belle"
        }]);

        IList<Variant> variants = builder.Build("bell'", UDTags.ADJ, index);

        Assert.Single(variants);
        Variant variant = variants[0];
        Assert.Equal("belle", variant.Value);
        Assert.Equal(UDTags.ADJ, variant.Pos);
        Assert.Equal("bell'", variant.Source);
        Assert.Equal("elided", variant.Type);
    }
    #endregion

    #region Apostrophe artifacts
    [Fact]
    public void Build_ApostropheLeft_NoApostrophe()
    {
        ItalianVariantBuilderOptions options = GetZeroOptions();
        options.ApostropheArtifacts = true;
        ItalianVariantBuilder builder = new();
        builder.Configure(options);

        RamLookupIndex index = new([new LookupEntry
        {
            Pos = "N",
            Value = "oh",
            Text = "oh"
        }]);

        IList<Variant> variants = builder.Build("'oh", null, index);

        Assert.Single(variants);
        Variant variant = variants[0];
        Assert.Equal("oh", variant.Value);
        Assert.Equal("N", variant.Pos);
        Assert.Equal("'oh", variant.Source);
        Assert.Equal("apostrophe", variant.Type);
    }

    [Fact]
    public void Build_ApostropheRight_NoApostrophe()
    {
        ItalianVariantBuilderOptions options = GetZeroOptions();
        options.ApostropheArtifacts = true;
        ItalianVariantBuilder builder = new();
        builder.Configure(options);

        RamLookupIndex index = new([new LookupEntry
        {
            Pos = "N",
            Value = "oh",
            Text = "oh"
        }]);

        IList<Variant> variants = builder.Build("oh'", null, index);

        Assert.Single(variants);
        Variant variant = variants[0];
        Assert.Equal("oh", variant.Value);
        Assert.Equal("N", variant.Pos);
        Assert.Equal("oh'", variant.Source);
        Assert.Equal("apostrophe", variant.Type);
    }

    [Fact]
    public void Build_ApostropheLeftAndRight_NoApostrophe()
    {
        ItalianVariantBuilderOptions options = GetZeroOptions();
        options.ApostropheArtifacts = true;
        ItalianVariantBuilder builder = new();
        builder.Configure(options);

        RamLookupIndex index = new([new LookupEntry
        {
            Pos = "N",
            Value = "oh",
            Text = "oh"
        }]);

        IList<Variant> variants = builder.Build("'oh'", null, index);

        Assert.Single(variants);
        Variant variant = variants[0];
        Assert.Equal("oh", variant.Value);
        Assert.Equal("N", variant.Pos);
        Assert.Equal("'oh'", variant.Source);
        Assert.Equal("apostrophe", variant.Type);
    }
    #endregion

    #region Accent artifacts
    [Fact]
    public void Build_CittaApostrophe_CittaGrave()
    {
        ItalianVariantBuilderOptions options = GetZeroOptions();
        options.AccentArtifacts = true;
        ItalianVariantBuilder builder = new();
        builder.Configure(options);

        RamLookupIndex index = new([new LookupEntry
        {
            Pos = "S",
            Value = "città",
            Text = "città"
        }]);

        IList<Variant> variants = builder.Build("citta'", null, index);

        Assert.Single(variants);
        Variant variant = variants[0];
        Assert.Equal("città", variant.Value);
        Assert.Equal("S", variant.Pos);
        Assert.Equal("citta'", variant.Source);
        Assert.Equal("accent", variant.Type);
    }

    [Fact]
    public void Build_CittaBacktick_CittaGrave()
    {
        ItalianVariantBuilderOptions options = GetZeroOptions();
        options.AccentArtifacts = true;
        ItalianVariantBuilder builder = new();
        builder.Configure(options);

        RamLookupIndex index = new([new LookupEntry
        {
            Pos = "S",
            Value = "città",
            Text = "città"
        }]);

        IList<Variant> variants = builder.Build("citta`", null, index);

        Assert.Single(variants);
        Variant variant = variants[0];
        Assert.Equal("città", variant.Value);
        Assert.Equal("S", variant.Pos);
        Assert.Equal("citta`", variant.Source);
        Assert.Equal("accent", variant.Type);
    }
    #endregion

    #region Iota
    [Fact]
    public void Build_Jeri_Ieri()
    {
        ItalianVariantBuilderOptions options = GetZeroOptions();
        options.IotaVariants = true;
        ItalianVariantBuilder builder = new();
        builder.Configure(options);

        RamLookupIndex index = new([new LookupEntry
        {
            Pos = "N",
            Value = "ieri",
            Text = "ieri"
        }]);

        IList<Variant> variants = builder.Build("jeri", null, index);

        Assert.Single(variants);
        Variant variant = variants[0];
        Assert.Equal("ieri", variant.Value);
        Assert.Equal("N", variant.Pos);
        Assert.Equal("jeri", variant.Source);
        Assert.Equal("iota", variant.Type);
    }
    #endregion

    #region Isc
    [Fact]
    public void Build_Iscuola_Scuola()
    {
        ItalianVariantBuilderOptions options = GetZeroOptions();
        options.IscVariants = true;
        ItalianVariantBuilder builder = new();
        builder.Configure(options);

        RamLookupIndex index = new([new LookupEntry
        {
            Pos = "S",
            Value = "scuola",
            Text = "scuola"
        }]);

        IList<Variant> variants = builder.Build("iscuola", null, index);

        Assert.Single(variants);
        Variant variant = variants[0];
        Assert.Equal("scuola", variant.Value);
        Assert.Equal("S", variant.Pos);
        Assert.Equal("iscuola", variant.Source);
        Assert.Equal("isc", variant.Type);
    }
    #endregion

    #region Accented
    [Fact]
    public void Build_CittaAcute_CittaGrave()
    {
        ItalianVariantBuilderOptions options = GetZeroOptions();
        options.AccentedVariants = true;
        ItalianVariantBuilder builder = new();
        builder.Configure(options);

        RamLookupIndex index = new([new LookupEntry
        {
            Pos = "S",
            Value = "città",
            Text = "città"
        }]);

        IList<Variant> variants = builder.Build("cittá", null, index);

        Assert.Single(variants);
        Variant variant = variants[0];
        Assert.Equal("città", variant.Value);
        Assert.Equal("S", variant.Pos);
        Assert.Equal("cittá", variant.Source);
        Assert.Equal("acute-grave", variant.Type);
    }
    #endregion
}
