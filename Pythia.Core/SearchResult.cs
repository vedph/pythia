namespace Pythia.Core;

/// <summary>
/// A simple search result. This represents the essential data returned
/// by searches.
/// </summary>
public class SearchResult
{
    /// <summary>
    /// Gets or sets the result identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the document identifier.
    /// </summary>
    public int DocumentId { get; set; }

    /// <summary>
    /// Gets or sets the start position.
    /// </summary>
    public int P1 { get; set; }

    /// <summary>
    /// Gets or sets the end position.
    /// </summary>
    public int P2 { get; set; }

    /// <summary>
    /// Gets or sets the character index.
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// Gets or sets the character length.
    /// </summary>
    public int Length { get; set; }

    /// <summary>
    /// Gets or sets the span type of the source for this result.
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the span value.
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// Gets or sets the original text <see cref="Value"/> was derived from.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets the author.
    /// </summary>
    public string? Author { get; set; }

    /// <summary>
    /// Gets or sets the title.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the document sort key.
    /// </summary>
    public string? SortKey { get; set; }

    /// <summary>
    /// Returns a <see cref="string" /> that represents this instance.
    /// </summary>
    /// <returns>
    /// A <see cref="string" /> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        return $"#{Id}@{P1}-{P2}: {Value}";
    }
}
