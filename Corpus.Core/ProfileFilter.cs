using Fusi.Tools.Data;

namespace Corpus.Core;

/// <summary>
/// Profile filter.
/// </summary>
public class ProfileFilter : PagingOptions
{
    /// <summary>
    /// Any portion of the ID to be matched.
    /// As for any ID, this is not case insensitive.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// The initial portion of the ID to be matched. Typically, profile
    /// IDs begin with prefixes like <c>prefix-</c> when belonging to
    /// different categories.
    /// </summary>
    public string? Prefix { get; set; }

    /// <summary>
    /// Gets or sets the type to be matched.
    /// As for any ID, this is not case insensitive.
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the user identifier to be matched.
    /// As for any ID, this is not case insensitive.
    /// </summary>
    public string? UserId { get; set; }
}
