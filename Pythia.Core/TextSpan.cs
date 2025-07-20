using Corpus.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pythia.Core;

/// <summary>
/// A span of text. This is a generic class which can represent a token, a
/// sentence, a verse, or any other text span. The span is defined by its
/// ordinal positions in the document, but also its character index, and its
/// length are recorded.
/// </summary>
public class TextSpan : IHasAttributes
{
    #region Constants
    /// <summary>
    /// The privileged document attribute names (except <c>id</c>).
    /// </summary>
    private static readonly HashSet<string> _privilegedDocAttrs =
        new(
        [
            "author", "title", "date_value", "sort_key", "source", "profile_id"
        ]);
    /// <summary>
    /// The privileged span attribute names (except <c>id</c>).
    /// </summary>
    private static readonly HashSet<string> _privilegedSpanAttrs =
        new(
        [
            "p1", "p2", "index", "length", "language", "pos", "lemma",
            "value", "text", "lemma_id", "word_id"
        ]);
    private static readonly HashSet<string> _numPrivilegedSpanAttrs = new(
        [
            "p1", "p2", "index", "length", "lemma_id", "word_id"
        ]);

    /// <summary>
    /// The token type.
    /// </summary>
    public const string TYPE_TOKEN = "tok";

    /// <summary>
    /// The sentence type.
    /// </summary>
    public const string TYPE_SENTENCE = "snt";

    /// <summary>
    /// The paragraph type.
    /// </summary>
    public const string TYPE_PARAGRAPH = "par";

    /// <summary>
    /// The verse type.
    /// </summary>
    public const string TYPE_VERSE = "vrs";
    #endregion

    /// <summary>
    /// Gets or sets the identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the document this token belongs to.
    /// </summary>
    public int DocumentId { get; set; }

    /// <summary>
    /// Gets or sets the span type.
    /// </summary>
    public string Type { get; set; } = TYPE_TOKEN;

    /// <summary>
    /// Gets or sets the start ordinal position of this token in the document (1-N).
    /// </summary>
    public int P1 { get; set; }

    /// <summary>
    /// Gets or sets the end ordinal position of this token in the document (1-N).
    /// </summary>
    public int P2 { get; set; }

    /// <summary>
    /// Gets or sets the character index of this token in the document.
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// Gets or sets the characters length of this token in the document.
    /// </summary>
    public int Length { get; set; }

    /// <summary>
    /// Gets or sets the language, when applicable.
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// Gets or sets the POS identifier for this span when applicable.
    /// </summary>
    public string? Pos { get; set; }

    /// <summary>
    /// Gets or sets the lemma for this span when applicable.
    /// </summary>
    public string? Lemma { get; set; }

    /// <summary>
    /// Gets or sets the token value.
    /// </summary>
    public string Value { get; set; } = "";

    /// <summary>
    /// Gets or sets the unparsed text for this span.
    /// </summary>
    public string Text { get; set; } = "";

    /// <summary>
    /// Gets the span's attributes.
    /// </summary>
    public IList<Corpus.Core.Attribute>? Attributes { get; set; }

    /// <summary>
    /// Adds the specified attribute ensuring that <see cref="Attributes"/>
    /// is not null.
    /// </summary>
    /// <param name="attribute">The attribute.</param>
    /// <exception cref="ArgumentNullException">attribute</exception>
    public void AddAttribute(Corpus.Core.Attribute attribute)
    {
        ArgumentNullException.ThrowIfNull(attribute);

        Attributes ??= [];
        Attributes.Add(attribute);
    }

    /// <summary>
    /// Determines whether this span has an attribute with the specified
    /// name or name and value.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="value">The value. When null, only name is matched.</param>
    /// <returns>
    ///   <c>true</c> if the specified name has attribute; otherwise, <c>false</c>.
    /// </returns>
    public bool HasAttribute(string name, string? value = null)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (Attributes == null) return false;

        return value == null
            ? Attributes.Any(a => a.Name == name)
            : Attributes.Any(a => a.Name == name && a.Value == value);
    }

    /// <summary>
    /// Resets this instance.
    /// </summary>
    public void Reset()
    {
        Id = 0;
        DocumentId = 0;
        Type = TYPE_TOKEN;
        P1 = 0;
        P2 = 0;
        Index = 0;
        Length = 0;
        Language = null;
        Pos = null;
        Lemma = null;
        Value = "";
        Text = "";
        Attributes = null;
    }

    /// <summary>
    /// Sets the positions.
    /// </summary>
    /// <param name="value">value.</param>
    public void SetPositions(int value) => P1 = P2 = value;

    /// <summary>
    /// Deeply clones this span.
    /// </summary>
    /// <returns>New span.</returns>
    public TextSpan Clone()
    {
        return new TextSpan
        {
            Id = Id,
            DocumentId = DocumentId,
            Type = Type,
            P1 = P1,
            P2 = P2,
            Index = Index,
            Length = Length,
            Language = Language,
            Pos = Pos,
            Lemma = Lemma,
            Value = Value,
            Text = Text,
            Attributes = Attributes?.Select(a => a.Clone()).ToList()
        };
    }

    /// <summary>
    /// Copies data from the specified span.
    /// </summary>
    /// <param name="span">The source span.</param>
    /// <exception cref="ArgumentNullException">span</exception>
    public void CopyFrom(TextSpan span)
    {
        ArgumentNullException.ThrowIfNull(span);

        Id = span.Id;
        DocumentId = span.DocumentId;
        Type = span.Type;
        P1 = span.P1;
        P2 = span.P2;
        Index = span.Index;
        Length = span.Length;
        Language = span.Language;
        Pos = span.Pos;
        Lemma = span.Lemma;
        Value = span.Value;
        Text = span.Text;
        Attributes = span.Attributes?.Select(a => a.Clone()).ToList();
    }

    /// <summary>
    /// Converts to string.
    /// </summary>
    /// <returns>
    /// A <see cref="string" /> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        StringBuilder sb = new();

        sb.Append('[').Append(DocumentId).Append(' ').Append(Type).Append("] ");
        sb.Append(P1).Append('-').Append(P2).Append(": ");
        sb.Append(Value).Append(" #").Append(Id);

        return sb.ToString();
    }

    /// <summary>
    /// Determines whether the specified name is a the name of a privileged
    /// document attribute.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns>
    ///   <c>true</c> if is privileged document attribute; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsPrivilegedDocAttr(string name) =>
        _privilegedDocAttrs.Contains(name);

    /// <summary>
    /// Determines whether the specified name is a the name of a privileged
    /// span attribute.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns>
    ///   <c>true</c> if is privileged span attribute; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsPrivilegedSpanAttr(string name) =>
        _privilegedSpanAttrs.Contains(name);

    /// <summary>
    /// Determines whether the specified name is a the name of a numeric
    /// privileged span attribute.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns>
    ///   <c>true</c> if is numeric privileged span attribute; otherwise,
    ///   <c>false</c>.
    /// </returns>
    public static bool IsNumericPrivilegedSpanAttr(string name) =>
        _numPrivilegedSpanAttrs.Contains(name);

    /// <summary>
    /// Gets the list of privileged document or span attributes.
    /// </summary>
    /// <param name="span">if set to <c>true</c> get span attributes, else
    /// get document attributes.</param>
    /// <returns>List of attributes.</returns>
    public static HashSet<string> GetPrivilegedAttrs(bool span)
    {
        HashSet<string> source = span ? _privilegedSpanAttrs : _privilegedDocAttrs;
        HashSet<string> target = [];
        foreach (string name in source) target.Add(name);
        return target;
    }
}
