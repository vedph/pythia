using System;
using System.Collections.Generic;
using System.Linq;

namespace Corpus.Core;

/// <summary>
/// A document.
/// </summary>
/// <seealso cref="T:Corpus.Core.IHasAttributes" />
public class Document : IDocument
{
    /// <summary>
    /// The target ID (table name) for document attributes.
    /// </summary>
    public const string ATTR_TARGET_ID = "document_attribute";

    /// <summary>
    /// Gets or sets the identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the author.
    /// </summary>
    public string? Author { get; set; }

    /// <summary>
    /// Gets or sets the title.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the date value. This is a numeric value calculated
    /// according to the source documents metadata, and representing a numeric
    /// value which can be used to sort documents by their datation,
    /// even when approximate.
    /// </summary>
    public double DateValue { get; set; }

    /// <summary>
    /// Gets or sets the sort key.
    /// </summary>
    public string? SortKey { get; set; }

    /// <summary>
    /// Gets or sets the source to be used to retrieve this document's
    /// content; this might be a file path, an URI, etc.
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Gets or sets the optional identifier of the profile
    /// used for reading this document.
    /// </summary>
    public string? ProfileId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who saved this document.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Gets or sets the last modified date and time (UTC).
    /// </summary>
    public DateTime LastModified { get; set; }

    /// <summary>
    /// Gets the attributes.
    /// </summary>
    public IList<Attribute>? Attributes { get; set; }

    /// <summary>
    /// Gets or sets the document's text content, which can optionally be
    /// stored within the document itself using this property.
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Document"/> class.
    /// </summary>
    public Document()
    {
        Attributes = [];
        LastModified = DateTime.UtcNow;
    }

    /// <summary>
    /// Returns a <see cref="string" /> that represents this instance.
    /// </summary>
    /// <returns>
    /// A <see cref="string" /> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        return $"{Id}: {Author} - {Title}" +
               (Attributes?.Count > 0
                   ? " @" + string.Join(",", from a in Attributes select a.Name)
                   : "");
    }
}
