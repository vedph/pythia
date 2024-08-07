using System.Text;

namespace Pythia.Core;

/// <summary>
/// A word form derived from grouping tokens.
/// </summary>
public class Word
{
    /// <summary>
    /// Gets or sets the word's identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the lemma identifier if any.
    /// </summary>
    public int? LemmaId { get; set; }

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
    /// Gets or sets the lemma this word form belongs to.
    /// </summary>
    public string? Lemma { get; set; }

    /// <summary>
    /// Gets or sets the total count of occurrences of this word form.
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
        StringBuilder sb = new(Id);
        sb.Append(": ");

        if (!string.IsNullOrEmpty(Pos))
            sb.Append('[').Append(Pos).Append("] ");

        sb.Append(Value);

        if (!string.IsNullOrEmpty(Lemma))
            sb.Append(" (").Append(Lemma).Append(") ");

        sb.Append(": ").Append(Count);

        return sb.ToString();
    }
}
