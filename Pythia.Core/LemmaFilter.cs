using Fusi.Tools.Data;

namespace Pythia.Core;

/// <summary>
/// A filter for browsing lemmata.
/// </summary>
/// <seealso cref="PagingOptions" />
public class LemmaFilter : PagingOptions
{
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
    /// Gets or sets the part of speech.
    /// </summary>
    public string? Pos { get; set; }

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
