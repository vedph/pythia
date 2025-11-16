using Fusi.Tools.Data;

namespace Pythia.Tagger.Lookup;

/// <summary>
/// Lookup filter. This is used to filter lookup index entries.
/// </summary>
public class LookupFilter : PagingOptions
{
    /// <summary>
    /// The value to filter by. This can be a full word, a prefix, suffix,
    /// substring, or a fuzzy match, according to the comparison type.
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// The type of comparison to perform on the value.
    /// </summary>
    public LookupEntryComparison Comparison { get; set; }

    /// <summary>
    /// The similarity threshold for fuzzy matching.
    /// </summary>
    public double Threshold { get; set; } = 0.8;

    /// <summary>
    /// The part of speech to filter by, e.g. "NOUN", "VERB", etc.
    /// </summary>
    public string? Pos { get; set; }

    /// <summary>
    /// The lemma to filter by.
    /// </summary>
    public string? Lemma { get; set; }
}

public enum LookupEntryComparison
{
    /// <summary>
    /// Exact match.
    /// </summary>
    Exact = 0,
    /// <summary>
    /// Prefix match.
    /// </summary>
    Prefix,
    /// <summary>
    /// Suffix match.
    /// </summary>
    Suffix,
    /// <summary>
    /// Substring match.
    /// </summary>
    Substring,
    /// <summary>
    /// Fuzzy match.
    /// </summary>
    Fuzzy
}
