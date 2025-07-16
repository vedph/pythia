using System.Text;

namespace Pythia.Tools;

/// <summary>
/// A word form from the index to be checked by <see cref="WordChecker"/>.
/// </summary>
public sealed class WordToCheck
{
    /// <summary>
    /// Word ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Language.
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// Word's value.
    /// </summary>
    public required string Value { get; set; }

    /// <summary>
    /// Full part of speech including features.
    /// </summary>
    public string? Pos { get; set; }

    /// <summary>
    /// The ID of the lemma the word belongs to.
    /// </summary>
    public int LemmaId { get; set; }

    /// <summary>
    /// The value of the lemma the word belongs to.
    /// </summary>
    public string? Lemma { get; set; }

    /// <summary>
    /// Converts the word to a string representation.
    /// </summary>
    /// <returns>String.</returns>
    public override string ToString()
    {
        StringBuilder sb = new();
        sb.Append('#').Append(Id)
          .Append(' ')
          .Append(Value);

        if (!string.IsNullOrEmpty(Pos)) sb.Append(" [").Append(Pos).Append(']');

        if (!string.IsNullOrEmpty(Lemma))
        {
            sb.Append(" < ");
            if (LemmaId > 0) sb.Append("#").Append(LemmaId).Append(' ');
            sb.Append(Lemma);
        }

        return sb.ToString();
    }
}
