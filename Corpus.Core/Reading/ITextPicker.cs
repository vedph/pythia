namespace Corpus.Core.Reading;

/// <summary>
/// Text picker interface.
/// </summary>
/// <remarks>A text picker is used to pick pieces of text from a document,
/// for reading it, or showing occurrences into their context. The text is
/// partitioned according to a text map, rooted at a <see cref="TextMapNode"/>
/// and created by an <see cref="ITextMapper"/> implementation, which is
/// specific to each input format. When picking text, the map nodes
/// selection is updated so that all the nodes are deselected, except
/// the ones which were picked.
/// </remarks>
public interface ITextPicker
{
    /// <summary>
    /// Pick the text corresponding to the content of the specified document
    /// map node.
    /// This is typically used when browsing a document via its map.
    /// </summary>
    /// <param name="text">The source document text.</param>
    /// <param name="map">The source document full map (root node)</param>
    /// <param name="node">The node to pick.</param>
    /// <returns>text or null if not found</returns>
    TextPiece? PickNode(string text, TextMapNode map, TextMapNode node);

    /// <summary>
    /// Pick the context wrapping the specified range of character offsets.
    /// This is typically used when displaying the results of a search.
    /// </summary>
    /// <param name="text">The source document text.</param>
    /// <param name="map">The source document full map (root node)</param>
    /// <param name="startIndex">The start character index.</param>
    /// <param name="endIndex">The end character index.</param>
    /// <returns>text or null if not found</returns>
    TextPiece? PickContext(string text, TextMapNode map, int startIndex,
        int endIndex);
}
