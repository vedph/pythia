using System.Collections.Generic;

namespace Corpus.Core;

/// <summary>
/// A corpus.
/// </summary>
public interface ICorpus
{
    /// <summary>
    /// Gets or sets the identifier. This is a short string, arbitrarily
    /// defined by the corpus creator.
    /// </summary>
    string? Id { get; set; }

    /// <summary>
    /// Gets or sets the title.
    /// </summary>
    string? Title { get; set; }

    /// <summary>
    /// Gets or sets the description.
    /// </summary>
    string? Description { get; set; }

    /// <summary>
    /// Gets or sets the user identifier optionally assigned to this corpus.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Gets the IDs of the document included in this corpus.
    /// </summary>
    IList<int>? DocumentIds { get; set; }
}
