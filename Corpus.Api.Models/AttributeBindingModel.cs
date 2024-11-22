using Corpus.Core;
using System.ComponentModel.DataAnnotations;

namespace Corpus.Api.Models;

/// <summary>
/// Attribute binding model.
/// </summary>
public class AttributeBindingModel
{
    /// <summary>
    /// The name.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string? Name { get; set; }

    /// <summary>
    /// The value.
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string? Value { get; set; }

    /// <summary>
    /// The type.
    /// </summary>
    [Required]
    public AttributeType Type { get; set; }
}
