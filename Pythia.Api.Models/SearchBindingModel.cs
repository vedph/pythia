﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Pythia.Api.Models;

/// <summary>
/// Search model.
/// </summary>
public class SearchBindingModel
{
    /// <summary>
    /// Page number (1-N).
    /// </summary>
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; }

    /// <summary>
    /// Page size (1-100).
    /// </summary>
    [Range(1, 100)]
    public int PageSize { get; set; }

    /// <summary>
    /// The search query.
    /// </summary>
    [Required]
    public string? Query { get; set; }

    /// <summary>
    /// The desired size of the KWIC context (1-10).
    /// </summary>
    [Range(1, 10)]
    public int? ContextSize { get; set; }

    /// <summary>
    /// The optional sort fields. If not specified, the query will sort
    /// by document's sort key. Otherwise, it will sort by all the fields
    /// specified here, in their order.
    /// </summary>
    public IList<string>? SortFields { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchBindingModel"/> class.
    /// </summary>
    public SearchBindingModel()
    {
        PageNumber = 1;
        PageSize = 20;
        ContextSize = 5;
    }

    /// <summary>
    /// Returns a <see cref="string" /> that represents this instance.
    /// </summary>
    /// <returns>
    /// A <see cref="string" /> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        return $"[{PageNumber}×{PageSize}]: {Query}";
    }
}
