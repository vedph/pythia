using System.Collections.Generic;

namespace Pythia.Core.Analysis;

/// <summary>
/// A generic cache for tokens, typically used to integrate third-party
/// analysis. In this scenario, the cache is filled by tokenizers, then
/// it gets new data from third-parties, and finally is reused during
/// tokenization with a cache-reading token filter.
/// </summary>
public interface ITokenCache
{
    /// <summary>
    /// Gets the list of token attributes allowed to be stored in the cache.
    /// When empty, any attribute is allowed; otherwise, only the attributes
    /// included in this list are allowed.
    /// </summary>
    HashSet<string> AllowedAttributes { get; }

    /// <summary>
    /// Opens or creates the cache at the specified source.
    /// </summary>
    /// <param name="source">The source. The meaning of this parameter
    /// varies according to the cache implemetation. For instance, in a
    /// file system it might just be a directory name.</param>
    void Open(string source);

    /// <summary>
    /// Closes this cache.
    /// </summary>
    void Close();

    /// <summary>
    /// Deletes the cache at the specified source.
    /// </summary>
    /// <param name="source">The source.</param>
    void Delete(string source);

    /// <summary>
    /// Checks if the cache at the specified source exists.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <returns>True if exists.</returns>
    bool Exists(string source);

    /// <summary>
    /// Deletes the document with the specified ID from the cache.
    /// </summary>
    /// <param name="id">The document identifier.</param>
    void DeleteDocument(int id);

    /// <summary>
    /// Adds the specified tokens to the cache. The tokens must all
    /// belong to the same document.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="spans">The spans.</param>
    /// <param name="content">The document's content. Pass this when you want
    /// to add a token attribute named <c>text</c> with value equal to the
    /// original token. This can be required in some scenarios, e.g. for
    /// deferred POS tagging.</param>
    void AddSpans(int documentId, IList<TextSpan> spans, string? content = null);

    /// <summary>
    /// Gets the specified token from the cache.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="position">The token's position.</param>
    /// <returns>Span, or null if not found.</returns>
    TextSpan? GetSpan(int documentId, int position);
}
