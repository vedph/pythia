using System;

namespace Corpus.Core;

/// <summary>
/// A document.
/// </summary>
/// <seealso cref="IHasAttributes" />
public interface IDocument : IHasAttributes
{
    /// <summary>
    /// Gets or sets the identifier.
    /// </summary>
    int Id { get; set; }

    /// <summary>
    /// Gets or sets the author.
    /// </summary>
    string? Author { get; set; }

    /// <summary>
    /// Gets or sets the title.
    /// </summary>
    string? Title { get; set; }

    /// <summary>
    /// Gets or sets the date value. This is a numeric value calculated
    /// according to the source documents metadata, and representing a numeric
    /// value which can be used to sort documents by their datation,
    /// even when approximate.
    /// </summary>
    double DateValue { get; set; }

    /// <summary>
    /// Gets or sets the sort key.
    /// </summary>
    string? SortKey { get; set; }

    /// <summary>
    /// Gets or sets the source to be used to retrieve this document's
    /// content; this might be a file path, an URI, etc.
    /// </summary>
    string? Source { get; set; }

    /// <summary>
    /// Gets or sets the optional identifier of the profile
    /// used for reading this document.
    /// </summary>
    string? ProfileId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who saved this document.
    /// </summary>
    string? UserId { get; set; }

    /// <summary>
    /// Gets or sets the last modified date and time (UTC).
    /// </summary>
    DateTime LastModified { get; set; }

    /// <summary>
    /// Gets or sets the document's text content, which can optionally be
    /// stored within the document itself using this property.
    /// </summary>
    string? Content { get; set; }
}