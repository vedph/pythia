using System;

namespace Corpus.Core.Reading;

/// <summary>
/// A piece of text.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="TextPiece"/> class.
/// </remarks>
/// <param name="text">The text.</param>
/// <param name="map">The map.</param>
/// <exception cref="ArgumentNullException">null text or map</exception>
public sealed class TextPiece(string text, TextMapNode map)
{
    private string _text = text ?? throw new ArgumentNullException(nameof(text));

    /// <summary>
    /// Gets or sets the context text. This value can be empty but not null.
    /// </summary>
    public string Text
    {
        get => _text;
        set => _text = value ?? "";
    }

    /// <summary>
    /// Gets the whole document map.
    /// </summary>
    public TextMapNode DocumentMap { get; } = map
        ?? throw new ArgumentNullException(nameof(map));

    /// <summary>
    /// Gets or sets the start character index of the picked portion of
    /// the source document text, when known by the picker; otherwise,
    /// both this and <see cref="EndIndex"/> are 0.
    /// </summary>
    public int StartIndex { get; set; }

    /// <summary>
    /// Gets or sets the end character index (exclusive) of the picked
    /// portion of the source document text, when known by the picker;
    /// otherwise, both this and <see cref="StartIndex"/> are 0.
    /// </summary>
    public int EndIndex { get; set; }

    /// <summary>
    /// Determines whether this piece includes the whole document map, i.e.
    /// the picked portion of the text includes the range of the map's root
    /// node. This is false when the picked range is unknown.
    /// </summary>
    /// <returns>True if the whole map is included.</returns>
    public bool IncludesWholeMap()
    {
        if (EndIndex <= StartIndex) return false;

        TextMapNode root = DocumentMap;
        while (root.Parent != null) root = root.Parent;

        return StartIndex <= root.StartIndex && EndIndex >= root.EndIndex;
    }

    /// <summary>
    /// Returns a <see cref="string" /> that represents this instance.
    /// </summary>
    /// <returns>
    /// A <see cref="string" /> that represents this instance.</returns>
    public override string ToString()
    {
        return $"{DocumentMap}: {Text.Length} " +
               $"\"{(Text.Length > 50 ? Text.Substring(0, 50) : Text)}\"";
    }
}
