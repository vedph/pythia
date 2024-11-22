using System.ComponentModel.DataAnnotations;

namespace Corpus.Api.Models;

/// <summary>
/// Corpus binding model.
/// </summary>
public class CorpusBindingModel
{
    /// <summary>
    /// The corpus identifier. This is a short string, arbitrarily defined by
    /// the corpus creator.
    /// </summary>
    [Required(ErrorMessage = "Corpus ID is required")]
    [MaxLength(50, ErrorMessage = "ID too long")]
    [RegularExpression("^[a-zA-Z0-9][.-_a-zA-Z0-9]{0,49}$")]
    public string? Id { get; set; }

    /// <summary>
    /// The corpus title.
    /// </summary>
    [Required(ErrorMessage = "Title is required")]
    [MaxLength(100, ErrorMessage = "Title too long")]
    public string? Title { get; set; }

    /// <summary>
    /// The corpus description.
    /// </summary>
    [MaxLength(1000, ErrorMessage = "Description too long")]
    public string? Description { get; set; }

    /// <summary>
    /// The optional source corpus ID to clone its content into the target
    /// corpus.
    /// </summary>
    [MaxLength(50)]
    public string? SourceId { get; set; }
}
