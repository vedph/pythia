using Corpus.Core;
using System.ComponentModel.DataAnnotations;

namespace Corpus.Api.Models;

/// <summary>
/// Corpus filter model.
/// </summary>
public class CorpusFilterBindingModel : PagingFilterBindingModel
{
    /// <summary>
    /// Any part of the ID text to be found.
    /// </summary>
    [MaxLength(50)]
    public string? Id { get; set; }

    /// <summary>
    /// Any part of the title text to be found.
    /// </summary>
    [MaxLength(100)]
    public string? Title { get; set; }

    /// <summary>
    /// The prefix to match for the corpus' ID.
    /// </summary>
    [MaxLength(50)]
    public string? Prefix { get; set; }

    /// <summary>
    /// The user identifier (name) to match.
    /// </summary>
    [MaxLength(256)]
    public string? UserId { get; set; }

    /// <summary>
    /// True to include documents counts instead of their IDs list.
    /// </summary>
    public bool Counts { get; set; }

    /// <summary>
    /// Converts this model into a corpus filter.
    //// </summary>
    public CorpusFilter ToCorpusFilter()
    {
        return new CorpusFilter
        {
            PageNumber = PageNumber,
            PageSize = PageSize,
            Id = Id,
            Title = Title,
            Prefix = Prefix,
            UserId = UserId
        };
    }
}
