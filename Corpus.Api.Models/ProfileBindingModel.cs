using System.ComponentModel.DataAnnotations;

namespace Corpus.Api.Models;

/// <summary>
/// Profile binding model.
/// </summary>
public sealed class ProfileBindingModel
{
    /// <summary>
    /// The profile ID.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the profile type.
    /// </summary>
    [MaxLength(50)]
    public string? Type { get; set; }

    /// <summary>
    /// The profile content (JSON).
    /// </summary>
    [Required]
    public string? Content { get; set; }
}
