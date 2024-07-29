using System;
using System.ComponentModel.DataAnnotations;
using Pythia.Core;

namespace Pythia.Api.Models;

/// <summary>
/// Lemmata filter model.
/// </summary>
public sealed class LemmaFilterBindingModel
{
    /// <summary>
    /// The page number (1-N).
    /// </summary>
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; }

    /// <summary>
    /// Page size (1-N).
    /// </summary>
    [Range(1, 100)]
    public int PageSize { get; set; }

    /// <summary>
    /// Gets or sets the language.
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// Gets or sets the value pattern. This can include wildcards <c>?</c>
    /// and <c>*</c>.
    /// </summary>
    public string? ValuePattern { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="ValuePattern"/>
    /// refers to the reversed version of the value.
    /// </summary>
    public bool IsValuePatternReversed { get; set; }

    /// <summary>
    /// Gets or sets the minimum length of the value.
    /// </summary>
    public int MinValueLength { get; set; }

    /// <summary>
    /// Gets or sets the maximum length of the value.
    /// </summary>
    public int MaxValueLength { get; set; }

    /// <summary>
    /// Gets or sets the token's minimum frequency; 0=not set.
    /// </summary>
    public int MinCount { get; set; }

    /// <summary>
    /// Gets or sets the token's maximum frequency; 0=not set.
    /// </summary>
    public int MaxCount { get; set; }

    /// <summary>
    /// Gets or sets the sort order.
    /// </summary>
    public WordSortOrder SortOrder { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether sort is descending
    /// rather than ascending.
    /// </summary>
    public bool IsSortDescending { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LemmaFilterBindingModel"/> class.
    /// </summary>
    public LemmaFilterBindingModel()
    {
        PageNumber = 1;
        PageSize = 50;
    }

    /// <summary>
    /// Converts this model to the corresponding Pythia filter.
    /// </summary>
    /// <returns>Filter.</returns>
    public LemmaFilter ToFilter()
    {
        return new LemmaFilter
        {
            PageNumber = PageNumber,
            PageSize = PageSize,
            Language = Language,
            ValuePattern = ValuePattern,
            IsValuePatternReversed = IsValuePatternReversed,
            MinValueLength = MinValueLength,
            MaxValueLength = MaxValueLength,
            MinCount = MinCount,
            MaxCount = MaxCount,
            SortOrder = SortOrder,
            IsSortDescending = IsSortDescending
        };
    }
}
