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
