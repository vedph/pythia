using Corpus.Core;
using System.ComponentModel.DataAnnotations;

namespace Corpus.Api.Models;

/// <summary>
/// Profile filter.
/// </summary>
/// <seealso cref="PagingFilterBindingModel" />
public class ProfileFilterBindingModel : PagingFilterBindingModel
{
    /// <summary>
    /// Any portion of the ID to be matched.
    /// </summary>
    [MaxLength(50)]
    public string? Id { get; set; }

    /// <summary>
    /// The type to be matched.
    /// </summary>
    [MaxLength(50)]
    public string? Type { get; set; }

    /// <summary>
    /// The initial portion of the ID to be matched. Typically, profile
    /// IDs begin with prefixes like <c>prefix-</c> when belonging to
    /// different categories.
    /// </summary>
    [MaxLength(50)]
    public string? Prefix { get; set; }

    /// <summary>
    /// The user identifier (name) to match.
    /// </summary>
    [MaxLength(256)]
    public string? UserId { get; set; }

    /// <summary>
    /// Converts this model into the corresponding filter.
    /// </summary>
    /// <returns>Filter.</returns>
    public ProfileFilter ToProfileFilter()
    {
        return new ProfileFilter
        {
            PageNumber = PageNumber,
            PageSize = PageSize,
            Id = Id,
            Prefix = Prefix,
            Type = Type,
            UserId = UserId
        };
    }
}
