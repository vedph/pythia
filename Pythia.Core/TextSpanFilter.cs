using System.Collections.Generic;

namespace Pythia.Core;

/// <summary>
/// A filter for <see cref="TextSpan"/>.
/// </summary>
public class TextSpanFilter
{
    /// <summary>
    /// Gets or sets the span's type.
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the minimum position. Leave it to 0 to ignore it.
    /// </summary>
    public int PositionMin { get; set; }

    /// <summary>
    /// Gets or sets the maximum position. Leave it to 0 to ignore it.
    /// </summary>
    public int PositionMax { get; set; }

    /// <summary>
    /// Gets or sets the document ID(s) to limit the spans to.
    /// </summary>
    public HashSet<int>? DocumentIds { get; set; }

    /// <summary>
    /// Gets or sets the span's attributes, each with its name and value.
    /// All the attributes must match.
    /// </summary>
    public Dictionary<string, string>? Attributes { get; set; }

    /// <summary>
    /// Gets a value indicating whether this filter is empty.
    /// </summary>
    public bool IsEmpty => string.IsNullOrEmpty(Type)
        && PositionMin == 0 && PositionMax == 0
        && (DocumentIds == null || DocumentIds.Count == 0)
        && (Attributes == null || Attributes.Count == 0);
}
