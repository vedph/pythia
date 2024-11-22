using Fusi.Tools.Data;
using System;
using System.Collections.Generic;

namespace Corpus.Core;

/// <summary>
/// Filter for documents.
/// </summary>
public sealed class DocumentFilter : PagingOptions
{
    /// <summary>
    /// Gets or sets the corpus identifier; if specified, documents must have
    /// a corpus ID equal to this value.
    /// </summary>
    public string? CorpusId { get; set; }

    /// <summary>
    /// Gets or sets the corpus ID prefix. If specified, documents must
    /// belong to a corpus whose ID starts with this value.
    /// </summary>
    public string? CorpusIdPrefix { get; set; }

    /// <summary>
    /// Gets or sets the author. If specified, documents must have
    /// an author containing this value.
    /// </summary>
    public string? Author { get; set; }

    /// <summary>
    /// Gets or sets the title. If specified, documents must have
    /// a title containing this value.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the source. If specified, documents must have
    /// a source containing this value.
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Gets or sets the profile identifier. If specified, documents must have
    /// a profile equal to this value.
    /// </summary>
    public string? ProfileId { get; set; }

    /// <summary>
    /// Gets or sets the profile ID prefix. If specified, documents must
    /// have a profile ID starting with this value.
    /// </summary>
    public string? ProfileIdPrefix { get; set; }

    /// <summary>
    /// Gets or sets the minimum date value.
    /// </summary>
    public double MinDateValue { get; set; }

    /// <summary>
    /// Gets or sets the maximum date value.
    /// </summary>
    public double MaxDateValue { get; set; }

    /// <summary>
    /// Gets or sets the minimum time modified.
    /// </summary>
    public DateTime? MinTimeModified { get; set; }

    /// <summary>
    /// Gets or sets the maximum time modified.
    /// </summary>
    public DateTime? MaxTimeModified { get; set; }

    /// <summary>
    /// Gets or sets the attributes to match. Each attribute filter
    /// is a tuple where 1=name and 2=value. The value must be contained
    /// in the attribute's value.
    /// </summary>
    public List<Tuple<string,string>>? Attributes { get; set; }

    /// <summary>
    /// Gets or sets the sort order.
    /// </summary>
    public DocumentSortOrder SortOrder { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether sort is descending rather
    /// than ascending.
    /// </summary>
    public bool IsSortDescending { get; set; }

    /// <summary>
    /// Gets or sets the user identifier to match.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Determines whether this filter is empty, i.e. has no filtering
    /// condition set.
    /// </summary>
    /// <returns>
    ///   <c>true</c> if this instance is empty; otherwise, <c>false</c>.
    /// </returns>
    public bool IsEmpty()
    {
        return string.IsNullOrEmpty(CorpusId)
               && string.IsNullOrEmpty(CorpusIdPrefix)
               && string.IsNullOrEmpty(Author)
               && string.IsNullOrEmpty(Title)
               && string.IsNullOrEmpty(Source)
               && string.IsNullOrEmpty(ProfileId)
               && string.IsNullOrEmpty(ProfileIdPrefix)
               && string.IsNullOrEmpty(UserId)
               && MinDateValue == 0
               && MaxDateValue == 0
               && !MinTimeModified.HasValue
               && !MaxTimeModified.HasValue
               && (Attributes == null || Attributes.Count == 0);
    }
}

/// <summary>
/// Document's sort order.
/// </summary>
public enum DocumentSortOrder
{
    /// <summary>Sort by the document's sort key.</summary>
    Default = 0,

    /// <summary>Sort first by the document's author.</summary>
    Author,

    /// <summary>Sort first by the document's title.</summary>
    Title,

    /// <summary>Sort first by the document's date value.</summary>
    Date
}
