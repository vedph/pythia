namespace Corpus.Core.Analysis;

/// <summary>
/// Interface for components which build a document's sort key from its attributes.
/// </summary>
public interface IDocSortKeyBuilder
{
    /// <summary>
    /// Builds the sort key for the specified document.
    /// </summary>
    /// <returns>key</returns>
    string Build(IDocument document);
}
