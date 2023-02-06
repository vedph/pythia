namespace Pythia.Tagger.Lookup;

/// <summary>
/// Lookup entry.
/// </summary>
public class LookupEntry
{
    /// <summary>
    /// Gets or sets the lemma identifier, represented by an integer 
    /// number (1-N), unique in the resource. This number is assigned
    /// during import, and varies at each new import.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the text.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets the filtered value.
    /// </summary>
    /// <remarks>This property is the value to be matched by the token(s)
    /// being looked up. It is generated during resource import, by
    /// leveraging the input processing components.</remarks>
    public string? Value { get; set; }

    /// <summary>
    /// Gets or sets the optional morphological signature.
    /// </summary>
    public string? Signature { get; set; }

    /// <summary>
    /// Returns a <see cref="string" /> that represents this instance.
    /// </summary>
    /// <returns>
    /// A <see cref="string" /> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        return $"#{Id}: {Value} ({Text})";
    }
}
