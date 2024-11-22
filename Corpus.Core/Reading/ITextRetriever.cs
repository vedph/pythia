using System.Threading.Tasks;

namespace Corpus.Core.Reading;

/// <summary>
/// Full text retriever.
/// </summary>
/// <remarks>This is a generic interface implemented by any component
/// in charge of retrieving the full text of an indexed document, whatever
/// its location or format.</remarks>
public interface ITextRetriever
{
    /// <summary>
    /// Retrieve the text from the specified document.
    /// </summary>
    /// <param name="document">The document to retrieve text for.</param>
    /// <param name="context">The optional context object, whose type
    /// and function depend on the implementor.</param>
    /// <returns>Text, or null if not found.</returns>
    Task<string?> GetAsync(IDocument document, object? context = null);
}
