namespace Pythia.Core;

/// <summary>
/// A simple search result. This represents the essential data returned
/// by searches.
/// </summary>
public class SearchResult
{
    /// <summary>
    /// Gets or sets the result identifier. This is calculated by
    /// concatenating <see cref="DocumentId"/> and <see cref="Position"/>,
    /// separated by a dash, and is scoped to the search results only.
    /// It can be used by client code to uniquely identify each result
    /// in the received set.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the document identifier.
    /// </summary>
    public int DocumentId { get; set; }

    /// <summary>
    /// Gets or sets the token position.
    /// </summary>
    public int Position { get; set; }

    /// <summary>
    /// Gets or sets the character index.
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// Gets or sets the character length.
    /// </summary>
    public short Length { get; set; }

    /// <summary>
    /// Gets or sets the type of the entity being the source for this result.
    /// This is <c>t</c>=token (occurrence) or <c>s</c>=structure.
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the entity being the source for this
    /// result. This is the PK of an occurrence or structure, according to
    /// <see cref="EntityType"/>.
    /// </summary>
    public int EntityId { get; set; }

    /// <summary>
    /// Gets or sets the token value.
    /// </summary>
    public string? Value { get; set; }

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
        return $"#{DocumentId}@{Position}: {Value}";
    }
}
