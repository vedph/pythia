using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Corpus.Core;
using Fusi.Tools;
using Fusi.Tools.Data;
using Pythia.Core.Analysis;

namespace Pythia.Core;

/// <summary>
/// Repository for tokens, structures, and documents, with their attributes.
/// </summary>
public interface IIndexRepository : ICorpusRepository
{
    /// <summary>
    /// Gets information about all the documents attribute types.
    /// </summary>
    /// <param name="privileged">True to include also the privileged attribute
    /// names in the list.</param>
    /// <returns>Sorted list of unique names and types.</returns>
    IList<AttributeInfo> GetDocAttributeInfo(bool privileged);

    /// <summary>
    /// Gets the spans starting at the specified position.
    /// </summary>
    /// <param name="p1">The start position (P1).</param>
    /// <param name="type">The optional type filter.</param>
    /// <param name="attributes">True to include span attributes.</param>
    /// <returns>Spans.</returns>
    IList<TextSpan> GetSpansAt(int p1, string? type = null,
        bool attributes = false);

    /// <summary>
    /// Adds all the specified span.
    /// </summary>
    /// <param name="spans">The spans.</param>
    void AddSpans(IEnumerable<TextSpan> spans);

    /// <summary>
    /// Adds the specified attribute to all the tokens included in the
    /// specified range of the specified document. This is typically used
    /// when adding attributes which come from structures encompassing them.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="start">The start position.</param>
    /// <param name="end">The end position (inclusive).</param>
    /// <param name="name">The attribute name. This can be either a
    /// non-privileged or a privileged attribute name.</param>
    /// <param name="value">The attribute value.</param>
    /// <param name="type">The attribute type.</param>
    void AddSpanAttributes(int documentId, int start, int end,
        string name, string value, AttributeType type);

    /// <summary>
    /// Deletes all the spans of the document with the specified ID.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="type">The type of span to delete, or null to delete any
    /// types.</param>
    /// <param name="negatedType">True to delete any type except the specified
    /// one.</param>
    void DeleteDocumentSpans(int documentId, string? type = null,
        bool negatedType = false);

    /// <summary>
    /// Gets the range of token positions starting from the specified range of
    /// token character indexes. This is used by structure parsers, which often
    /// must determine positional ranges starting from character indexes.
    /// </summary>
    /// <param name="documentId">The document ID.</param>
    /// <param name="startIndex">The start index.</param>
    /// <param name="endIndex">The end index.</param>
    /// <returns>range or null</returns>
    Tuple<int, int>? GetPositionRange(int documentId, int startIndex,
        int endIndex);

    /// <summary>
    /// Searches the index using the specified query.
    /// </summary>
    /// <param name="request">The query request.</param>
    /// <param name="literalFilters">The optional filters to apply to literal
    /// values in the query text.</param>
    /// <returns>The results page.</returns>
    DataPage<SearchResult> Search(SearchRequest request,
        IList<ILiteralFilter>? literalFilters = null);

    /// <summary>
    /// Gets the context for the specified result(s).
    /// </summary>
    /// <param name="results">The results to get context for.</param>
    /// <param name="contextSize">Size of the context: e.g. if 5, you will
    /// get 5 tokens to the left and 5 to the right.</param>
    /// <returns>results with context</returns>
    IList<KwicSearchResult> GetResultContext(IList<SearchResult> results,
        int contextSize);

    /// <summary>
    /// Gets the specified page of words.
    /// </summary>
    /// <param name="filter">The words filter.</param>
    /// <returns>The results page.</returns>
    DataPage<Word> GetWords(WordFilter filter);

    /// <summary>
    /// Gets the word or lemma counts for the subset of documents having
    /// the specified attribute name and any of its values. The counts are
    /// grouped by value or value bin when numeric.
    /// </summary>
    /// <param name="lemma">if set to <c>true</c> get lemma counts, else
    /// get word counts.</param>
    /// <param name="id">The identifier.</param>
    /// <param name="attrName">Name of the attribute.</param>
    /// <returns>Token counts.</returns>
    IList<TokenCount> GetTokenCounts(bool lemma, int id, string attrName);

    /// <summary>
    /// Gets the specified page of lemmata.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <returns>The results page.</returns>
    DataPage<Lemma> GetLemmata(LemmaFilter filter);

    /// <summary>
    /// Gets statistics about the index.
    /// </summary>
    /// <returns>Dictionary with statistics.</returns>
    IDictionary<string, double> GetStatistics();

    /// <summary>
    /// Builds the words index basing on tokens.
    /// </summary>
    /// <param name="binCounts">The desired bins counts. For each attribute
    /// (either privileged or not) which must be handled as a number,
    /// this dictionary includes its name as the key, and the desired count
    /// of bins as the value. For instance, an attribute named <c>year</c>
    /// whose value is a year number would have an entry with key=<c>year</c>
    /// and value=<c>3</c>, meaning that we want to distribute its values in
    /// 3 bins.</param>
    /// <param name="excludedAttrNames">The names of the non-privileged
    /// attributes to be excluded from the pairs. All the names of non categorical
    /// attributes should be excluded.</param>
    /// <param name="token">The cancellation token.</param>
    /// <param name="progress">The progress.</param>
    Task BuildWordIndexAsync(IDictionary<string, int> binCounts,
        HashSet<string> excludedAttrNames,
        CancellationToken token,
        IProgress<ProgressReport>? progress = null);

    /// <summary>
    /// Finalizes the index by eventually adding calculated data into it.
    /// </summary>
    void FinalizeIndex();
}
