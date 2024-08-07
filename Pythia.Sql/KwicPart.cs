﻿namespace Pythia.Sql;

/// <summary>
/// A part of a KWIC context.
/// </summary>
public class KwicPart
{
    /// <summary>
    /// Gets or sets the document identifier.
    /// </summary>
    public int DocumentId { get; set; }

    /// <summary>
    /// Gets or sets the token span ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the position.
    /// </summary>
    public int Position { get; set; }

    /// <summary>
    /// Gets or sets the value.
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// Gets or sets the unparsed text from which <see cref="Value"/> was derived.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Returns a <see cref="string" /> that represents this instance.
    /// </summary>
    /// <returns>
    /// A <see cref="string" /> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        return $"Context part: {DocumentId}@{Position}: {Value}";
    }
}
