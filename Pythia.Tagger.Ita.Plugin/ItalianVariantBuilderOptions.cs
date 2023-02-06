namespace Pythia.Tagger.Ita.Plugin;

/// <summary>
/// Options for <see cref="ItalianVariantBuilder"/>.
/// </summary>
public sealed class ItalianVariantBuilderOptions
{
    /// <summary>
    /// The superlatives option, indicating whether to try building
    /// the positive degree from a word ending with a superlative suffix
    /// (e.g. <c>bello</c> from <c>bellissimo</c>).
    /// </summary>
    /// <value>Defaults to true.</value>
    public bool Superlatives { get; set; }

    /// <summary>
    /// The enclitics groups option, indicating whether to try building
    /// the word without enclitics from a group like <c>dammi</c>.
    /// </summary>
    /// <value>Defaults to true.</value>
    public bool EncliticGroups { get; set; }

    /// <summary>
    /// The truncated variants option, indicating whether to try building
    /// a full variant like <c>suora</c> from its truncated counterpart
    /// (<c>suor</c>).
    /// </summary>
    /// <value>Defaults to true.</value>
    public bool UntruncatedVariants { get; set; }

    /// <summary>
    /// The unelided variants option, indicating whether to try building
    /// a non-elided variant like <c>bello</c> from its elided counterpart
    /// (<c>bell'</c>).
    /// </summary>
    /// <value>Defaults to true.</value>
    public bool UnelidedVariants { get; set; }

    /// <summary>
    /// The apostrophe artifacts variants option, indicating whether to try
    /// try building variants without starting/ending apostrophes. Such variants
    /// are rather artifacts due to apostrophes misused as quotes (e.g. <c>'prova'</c>),
    /// and cause issues when the apostrophe is taken into account by tokenizers,
    /// which is the usual case for Italian.
    /// </summary>
    public bool ApostropheArtifacts { get; set; }

    /// <summary>
    /// The iota variants option, indicating whether variants with <c>i</c> instead
    /// of <c>j</c> should be found. This essentially happens with old texts (e.g.
    /// <c>effluvj</c>).
    /// </summary>
    public bool IotaVariants { get; set; }

    /// <summary>
    /// The isC- variants option, indicating whether variants of type isC- should
    /// be found (e.g. <c>iscoprire</c> instead of <c>scoprire</c>).
    /// </summary>
    public bool IscVariants { get; set; }

    /// <summary>
    /// The accentual variants option, indicating whether acute instead of grave
    /// or grave instead of acute should be searched. Such variants appear in
    /// inaccurate or old texts.
    /// </summary>
    public bool AccentedVariants { get; set; }

    /// <summary>
    /// The accent artifacts option, indicating whether to search for accented forms
    /// written with accent-like artifacts (plain final vowel + acute/grave accents
    /// or apostrophe, like <c>citta`</c> = <c>città</c>).
    /// </summary>
    public bool AccentArtifacts { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ItalianVariantBuilderOptions"/> class.
    /// </summary>
    public ItalianVariantBuilderOptions()
    {
        Superlatives = true;
        EncliticGroups = true;
        UntruncatedVariants = true;
        UnelidedVariants = true;
    }

    /// <summary>
    /// Sets all the options to the specified value.
    /// </summary>
    /// <param name="value">The value to set the options to.</param>
    public void SetAll(bool value)
    {
        Superlatives = value;
        EncliticGroups = value;
        UntruncatedVariants = value;
        UnelidedVariants = value;
        ApostropheArtifacts = value;
        IotaVariants = value;
        IscVariants = value;
        AccentedVariants = value;
        AccentArtifacts = value;
    }
}
