namespace Corpus.Core;

/// <summary>
/// A configuration profile.
/// </summary>
public interface IProfile
{
    /// <summary>
    /// Gets or sets the identifier. This is a unique, user-defined short
    /// string, e.g. <c>xml-liz</c> for XML documents for the LIZ.
    /// </summary>
    string? Id { get; set; }

    /// <summary>
    /// Gets or sets the optional profile type.
    /// </summary>
    string? Type { get; set; }

    /// <summary>
    /// Gets or sets the content. This is usually JSON code.
    /// </summary>
    string? Content { get; set; }

    /// <summary>
    /// Gets or sets the user identifier optionally assigned to this profile.
    /// </summary>
    string? UserId { get; set; }
}
