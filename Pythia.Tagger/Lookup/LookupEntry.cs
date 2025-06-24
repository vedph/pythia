using System.Text;

namespace Pythia.Tagger.Lookup;

/// <summary>
/// A lookup index entry.
/// </summary>
public record LookupEntry
{
    /// <summary>
    /// Gets or sets the entry identifier, represented by an integer 
    /// number (1-N), unique in the resource. This number is assigned
    /// during import, and varies at each new import.
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// Gets or sets the text.
    /// </summary>
    public string? Text { get; init; }

    /// <summary>
    /// Gets or sets the filtered value.
    /// </summary>
    /// <remarks>This property is the value to be matched by the token(s)
    /// being looked up. It is generated during resource import, by
    /// leveraging the input processing components.</remarks>
    public string? Value { get; init; }

    /// <summary>
    /// Gets or sets the optional lemma this entry belongs to.
    /// </summary>
    public string? Lemma { get; init; }

    /// <summary>
    /// Gets or sets the optional full part of speech with its features.
    /// This can be parsed into a <see cref="PosTag"/> object using the
    /// corresponding <see cref="PosTagBuilder"/>.
    /// </summary>
    public string? Pos { get; init; }

    /// <summary>
    /// Returns a <see cref="string" /> that represents this instance.
    /// </summary>
    /// <returns>
    /// A <see cref="string" /> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        StringBuilder sb = new();
        sb.Append('#').Append(Id);
        sb.Append(' ').Append(Value);
        if (Text != Value)
            sb.Append(" < ").Append(Text);
        if (!string.IsNullOrEmpty(Pos))
            sb.Append(" [").Append(Pos).Append(']');

        if (!string.IsNullOrEmpty(Lemma))
            sb.Append(" (").Append(Lemma).Append(')');

        return sb.ToString();
    }
}
