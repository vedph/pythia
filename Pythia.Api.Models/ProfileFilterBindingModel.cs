using Corpus.Core;
using System;
using System.ComponentModel.DataAnnotations;

namespace Pythia.Api.Models;

/// <summary>
/// Profiles filter.
/// </summary>
public sealed class ProfileFilterBindingModel
{
    /// <summary>
    /// Page number (1-N).
    /// </summary>
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; }

    /// <summary>
    /// Page size (0-100).
    /// </summary>
    [Range(0, 100)]
    public int PageSize { get; set; }

    /// <summary>
    /// Any portion of the ID to be matched.
    /// </summary>
    [MaxLength(50)]
    public string? Id { get; set; }

    /// <summary>
    /// The initial portion of the ID to be matched. Typically, profile
    /// IDs begin with prefixes like <c>prefix-</c> when belonging to
    /// different categories.
    /// </summary>
    [MaxLength(50)]
    public string? Prefix { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileFilterBindingModel"/>
    /// class.
    /// </summary>
    public ProfileFilterBindingModel()
    {
        PageNumber = 1;
        PageSize = 20;
    }

    /// <summary>
    /// Converts this model to the corresponding Pythia filter.
    /// </summary>
    /// <returns>Filter.</returns>
    public ProfileFilter ToFilter()
    {
        return new ProfileFilter
        {
            PageNumber = PageNumber,
            PageSize = PageSize,
            Id = Id,
            Prefix = Prefix
        };
    }
}
