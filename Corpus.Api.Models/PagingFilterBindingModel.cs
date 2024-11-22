using System;
using System.ComponentModel.DataAnnotations;

namespace Corpus.Api.Models;

/// <summary>
/// Paging filter model, used as a base for all the filter models requiring
/// paging.
/// </summary>
public class PagingFilterBindingModel
{
    /// <summary>
    /// Page number (1-N).
    /// </summary>
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; }

    /// <summary>
    /// Page size (0-N).
    /// </summary>
    [Range(0, 100)]
    public int PageSize { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PagingFilterBindingModel"/>
    /// class.
    /// </summary>
    public PagingFilterBindingModel()
    {
        PageNumber = 1;
        PageSize = 20;
    }
}
