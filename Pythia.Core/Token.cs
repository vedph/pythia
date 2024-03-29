﻿using System.Collections.Generic;
using Corpus.Core;

namespace Pythia.Core;

/// <summary>
/// Token.
/// </summary>
/// <seealso cref="T:Corpus.Core.IHasAttributes" />
public class Token : IHasAttributes
{
    /// <summary>
    /// Gets or sets the identifier of the document this token belongs to.
    /// </summary>
    public int DocumentId { get; set; }

    /// <summary>
    /// Gets or sets the ordinal position of this token in the document (1-N).
    /// </summary>
    public int Position { get; set; }

    /// <summary>
    /// Gets or sets the character index of this token in the document.
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// Gets or sets the characters length of this token in the document.
    /// </summary>
    public short Length { get; set; }

    /// <summary>
    /// Gets or sets the language.
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// Gets or sets the token value.
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// Gets the token's attributes. At the analysis level a token is
    /// spit out whenever found in a text; at the storage level, typically
    /// the token is stored separately from its occurrences. So, attributes
    /// get assigned to occurrences.
    /// </summary>
    public IList<Attribute>? Attributes { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Token"/> class.
    /// </summary>
    public Token()
    {
        Attributes = new List<Attribute>();
    }

    /// <summary>
    /// Adds the specified attribute, ensuring that <see cref="Attributes"/>
    /// is not null.
    /// </summary>
    /// <param name="attribute">The attribute.</param>
    public void AddAttribute(Attribute attribute)
    {
        System.ArgumentNullException.ThrowIfNull(attribute);

        Attributes ??= new List<Attribute>();
        Attributes.Add(attribute);
    }

    /// <summary>
    /// Resets this instance.
    /// </summary>
    public void Reset()
    {
        DocumentId = 0;
        Position = 0;
        Index = 0;
        Length = 0;
        Language = null;
        Value = null;
        Attributes?.Clear();
    }

    /// <summary>
    /// Clones this instance.
    /// </summary>
    /// <returns>new token instance</returns>
    public Token Clone()
    {
        Token token = new()
        {
            DocumentId = DocumentId,
            Position = Position,
            Index = Index,
            Length = Length,
            Language = Language,
            Value = Value
        };
        if (Attributes != null)
        {
            foreach (Attribute a in Attributes)
                token.Attributes!.Add(a.Clone());
        }
        return token;
    }

    /// <summary>
    /// Copies data from the specified token.
    /// </summary>
    /// <param name="token">The token.</param>
    /// <exception cref="System.ArgumentNullException">token</exception>
    public void CopyFrom(Token token)
    {
        System.ArgumentNullException.ThrowIfNull(token);

        DocumentId = token.DocumentId;
        Position = token.Position;
        Index = token.Index;
        Length = token.Length;
        Language = token.Language;
        Value = token.Value;

        if (Attributes == null) Attributes = new List<Attribute>();
        else Attributes.Clear();
        if (token.Attributes != null)
        {
            foreach (Attribute a in token.Attributes)
                Attributes.Add(a.Clone());
        }
    }

    /// <summary>
    /// Returns a <see cref="string" /> that represents this instance.
    /// </summary>
    /// <returns>
    /// A <see cref="string" /> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        return $"\"{Value}\" [{Language}]: {DocumentId}.{Position}" +
               $"@{Index}×{Length} @{Attributes?.Count ?? 0}";
    }
}
