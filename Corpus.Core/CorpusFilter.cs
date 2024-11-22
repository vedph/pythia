using Fusi.Tools.Data;

namespace Corpus.Core;

/// <summary>
/// Corpus filter.
/// </summary>
public sealed class CorpusFilter : PagingOptions
{
    /// <summary>
    /// Gets or sets the text to be included in the corpus' ID.
    /// As for any ID, this is not case insensitive.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the text to be included in the corpus' title.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the prefix to match for the corpus' ID.
    /// As for any ID, this is not case insensitive.
    /// </summary>
    public string? Prefix { get; set; }

    /// <summary>
    /// Gets or sets the user identifier to be matched.
    /// As for any ID, this is not case insensitive.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Returns a <see cref="string" /> that represents this instance.
    /// </summary>
    /// <returns>
    /// A <see cref="string" /> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        return base.ToString() +
               (Id != null ? $" ID={Id}" : "") +
               (Title != null ? $" title={Title}" : "");
    }
}
