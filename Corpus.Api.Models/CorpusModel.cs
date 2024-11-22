using Corpus.Core;
using System;
using System.Linq;

namespace Corpus.Api.Models;

/// <summary>
/// Corpus view model.
/// </summary>
public class CorpusModel
{
    /// <summary>
    /// The corpus identifier. This is a short string, arbitrarily defined by
    /// the corpus creator.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// The corpus title.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// The corpus description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the user identifier.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// The IDs of all the documents included in the corpus.
    /// </summary>
    public int[]? DocumentIds { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CorpusModel"/> class.
    /// </summary>
    /// <param name="corpus">The corpus.</param>
    /// <exception cref="ArgumentNullException">null corpus</exception>
    public CorpusModel(ICorpus corpus)
    {
        Id = corpus?.Id ?? throw new ArgumentNullException(nameof(corpus));
        Title = corpus.Title;
        Description = corpus.Description;
        UserId = corpus.UserId;
        if (corpus.DocumentIds?.Count > 0)
            DocumentIds = corpus.DocumentIds.ToArray();
    }
}
