using System.Collections.Generic;

namespace Corpus.Core.Reading;

/// <summary>
/// Text mapper interface.
/// </summary>
public interface ITextMapper
{
    /// <summary>
    /// Maps the specified document text.
    /// </summary>
    /// <param name="text">The text to map.</param>
    /// <param name="attributes">The optional attributes of the document
    /// the text belongs to. These can be used by the mapper to decide
    /// the mapping strategy: for instance, a poetic text might be mapped
    /// differently from a prose text.</param>
    /// <returns>the root node of the generated map</returns>
    TextMapNode? Map(string text,
        IReadOnlyDictionary<string,string>? attributes = null);
}
