namespace Corpus.Core.Reading;

/// <summary>
/// Document's text renderer.
/// </summary>
public interface ITextRenderer
{
    /// <summary>
    /// Renders the specified text.
    /// </summary>
    /// <param name="document">The document the text belongs to. This can
    /// be used by renderers to change their behavior according to the
    /// document's metadata.
    /// </param>
    /// <param name="text">The input text.</param>
    /// <returns>rendered text</returns>
    string Render(IDocument document, string text);
}
