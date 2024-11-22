namespace Corpus.Core;

/// <summary>
/// A configuration profile.
/// </summary>
public class Profile : IProfile
{
    /// <summary>
    /// Gets or sets the identifier. This is a unique, user-defined short
    /// string.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the optional profile type.
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the content. This is usually JSON code.
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Gets or sets the user identifier optionally assigned to this profile.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Returns a <see cref="string" /> that represents this instance.
    /// </summary>
    /// <returns>
    /// A <see cref="string" /> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        return Id ?? base.ToString()!;
    }
}
