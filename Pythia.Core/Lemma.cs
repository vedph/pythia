namespace Pythia.Core;

/// <summary>
/// A lemma, representing a word's base form. Lemmata are available only
/// when using some kind of lemmatizer (typically when POS-tagging tokens).
/// </summary>
public class Lemma
{
    /// <summary>
    /// Gets or sets the word's identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the value.
    /// </summary>
    public string Value { get; set; } = "";

    /// <summary>
    /// Gets or sets the reversed value.
    /// </summary>
    public string ReversedValue { get; set; } = "";

    /// <summary>
    /// Gets or sets the language.
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// Gets or sets the part of speech this word form belongs to.
    /// </summary>
    public string? Pos { get; set; }

    /// <summary>
    /// Gets or sets the total count of occurrences of all the word forms
    /// belonging to this lemma.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Converts to string.
    /// </summary>
    /// <returns>
    /// A <see cref="string" /> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        return $"{Id}: {Value} [{Pos}]={Count}";
    }
}
