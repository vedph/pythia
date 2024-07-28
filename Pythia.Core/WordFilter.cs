using Fusi.Tools.Data;

namespace Pythia.Core;

/// <summary>
/// Word browser filter.
/// </summary>
public class WordFilter : PagingOptions
{
    /// <summary>
    /// Gets or sets the language.
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// Gets or sets the lemma the word must belong to.
    /// </summary>
    public string? Lemma { get; set; }

    /// <summary>
    /// Gets or sets the part of speech the word must belong to.
    /// </summary>
    public string? Pos { get; set; }

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
}

/// <summary>
/// Terms list sort order.
/// </summary>
public enum WordSortOrder
{
    /// <summary>The default sort order (=by value).</summary>
    Default = 0,

    /// <summary>Sort by term's value.</summary>
    ByValue,

    /// <summary>Sort by reversed term's value.</summary>
    ByReversedValue,

    /// <summary>Sort by term's count.</summary>
    ByCount
}
