using System.Collections.Generic;

namespace Corpus.Core;

/// <summary>
/// Corpus. A corpus is an arbitrarily defined group of documents, inside
/// the same database. It can be used to delimit searches to a subset of
/// the database's documents.
/// </summary>
public class Corpus : ICorpus
{
    /// <summary>
    /// Gets or sets the identifier. This is a short string, arbitrarily
    /// defined by the corpus creator.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the title.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the user identifier optionally assigned to this corpus.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Gets the IDs of the document included in this corpus.
    /// </summary>
    public IList<int>? DocumentIds { get; set; }

    /// <summary>
    /// Create a new instance of the <see cref="Corpus"/> class.
    /// </summary>
    public Corpus()
    {
        DocumentIds = [];
    }

    /// <summary>
    /// Returns a <see cref="string" /> that represents this instance.
    /// </summary>
    /// <returns>
    /// A <see cref="string" /> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        return $"Corpus {Id}: {Title} ({DocumentIds?.Count ?? 0})";
    }
}
