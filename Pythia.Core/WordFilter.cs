namespace Pythia.Core;

/// <summary>
/// Word browser filter.
/// </summary>
public class WordFilter : LemmaFilter
{
    /// <summary>
    /// Gets or sets the ID of the lemma the word must belong to.
    /// </summary>
    public int? LemmaId { get; set; }

    /// <summary>
    /// Gets or sets the part of speech the word must belong to.
    /// </summary>
    public string? Pos { get; set; }
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
