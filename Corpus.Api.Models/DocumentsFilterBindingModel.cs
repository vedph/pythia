using Corpus.Core;
using System;
using System.ComponentModel.DataAnnotations;

namespace Corpus.Api.Models;

/// <summary>
/// Filter for a set of documents.
/// </summary>
/// <seealso cref="DocumentFilterBindingModel" />
public class DocumentsFilterBindingModel : DocumentFilterBindingModel
{
    /// <summary>
    /// Page number (1-N).
    /// </summary>
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; }

    /// <summary>
    /// Page size (1-N).
    /// </summary>
    [Range(1, 100)]
    public int PageSize { get; set; }

    /// <summary>
    /// The documents sort order (0=default, 1=author, 2=title, 3=date value).
    /// </summary>
    public DocumentSortOrder Sort { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether sort is descending rather than ascending.
    /// </summary>
    public bool Descending { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentFilterBindingModel"/> class.
    /// </summary>
    public DocumentsFilterBindingModel()
    {
        PageNumber = 1;
        PageSize = 20;
    }

    /// <summary>
    /// Converts to filter.
    /// </summary>
    /// <returns>Filter.</returns>
    public override DocumentFilter ToFilter()
    {
        DocumentFilter filter = base.ToFilter();

        filter.PageNumber = PageNumber;
        filter.PageSize = PageSize;
        filter.SortOrder = Sort;
        filter.IsSortDescending = Descending;
        return filter;
    }
}
