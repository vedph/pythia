using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Corpus.Core;
using Fusi.Tools;
using Fusi.Tools.Data;
using Pythia.Core.Analysis;
using Pythia.Core.Query;
using Attribute = Corpus.Core.Attribute;

namespace Pythia.Core.Plugin.Test;

/// <summary>
/// Mock repository used by tests in this assembly, and only minimally
/// implemented.
/// </summary>
public sealed class MockIndexRepository : RamCorpusRepository,
    IIndexRepository
{
    private readonly object _locker;

    private int _nextTokenId;

    public ConcurrentDictionary<int, TextSpan> Spans { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MockIndexRepository" />
    /// class.
    /// </summary>
    public MockIndexRepository()
    {
        _locker = new object();
        Spans = new ConcurrentDictionary<int, TextSpan>();
    }

    private int GetNextTokenId()
    {
        lock(_locker)
        {
            Interlocked.Increment(ref _nextTokenId);
        }
        return _nextTokenId;
    }

    /// <summary>
    /// Gets information about all the documents attribute types.
    /// </summary>
    /// <param name="privileged">True to include also the privileged attribute
    /// names in the list.</param>
    /// <returns>
    /// Sorted list of unique names and types.
    /// </returns>
    /// <exception cref="NotImplementedException"></exception>
    public IList<AttributeInfo> GetDocAttributeInfo(bool privileged)
    {
        throw new NotImplementedException();
    }


    /// <summary>
    /// Adds the specified attribute to all the tokens included in the
    /// specified range of the specified document. This is typically used
    /// when adding token attributes which come from structures
    /// encompassing them.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="start">The start position.</param>
    /// <param name="end">The end position (inclusive).</param>
    /// <param name="name">The attribute name.</param>
    /// <param name="value">The attribute value.</param>
    /// <param name="type">The attribute type.</param>
    /// <exception cref="NotImplementedException"></exception>
    public void AddSpanAttributes(int documentId, int start, int end,
        string name, string value, AttributeType type)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Deletes the span attributes.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="start">The start.</param>
    /// <param name="end">The end.</param>
    /// <param name="names">The names.</param>
    /// <exception cref="NotImplementedException"></exception>
    public void DeleteSpanAttributes(int documentId, int start, int end,
        IEnumerable<string> names)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Removes all the tokens of the document with the specified ID.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="type">Token type or null to delete any types.</param>
    /// <param name="negatedType">True to delete any type except the specified
    /// one.</param>
    public void DeleteDocumentSpans(int documentId, string? type = null,
        bool negatedType = false)
    {
        foreach (var p in Spans.Where(p => p.Value.DocumentId == documentId))
        {
            if (type == null
                || (type == p.Value.Type && !negatedType)
                || (type != p.Value.Type && negatedType))
            {
                Spans.TryRemove(p.Key, out TextSpan? t);
            }
        }
    }

    /// <summary>
    /// Adds the specified token. This blindly adds the token without
    /// checking whether it exists or not.
    /// </summary>
    /// <param name="span">The span.</param>
    /// <exception cref="ArgumentNullException">token</exception>
    public void AddSpan(TextSpan span)
    {
        ArgumentNullException.ThrowIfNull(span);
        int id = GetNextTokenId();
        Spans[id] = span;
    }

    /// <summary>
    /// Adds all the specified spans.
    /// </summary>
    /// <param name="spans">The spans.</param>
    /// <exception cref="ArgumentNullException">spans</exception>
    public void AddSpans(IEnumerable<TextSpan> spans)
    {
        ArgumentNullException.ThrowIfNull(spans);

        foreach (TextSpan token in spans) AddSpan(token);
    }

    /// <summary>
    /// Gets the range of token positions starting from the specified
    /// range of token character indexes. This is used by structure parsers,
    /// which often must determine positional ranges starting from
    /// character indexes.
    /// </summary>
    /// <param name="documentId">The document ID.</param>
    /// <param name="startIndex">The start index.</param>
    /// <param name="endIndex">The end index.</param>
    /// <returns>range or null</returns>
    public Tuple<int, int>? GetPositionRange(int documentId,
        int startIndex, int endIndex)
    {
        // start: nearest token with index >= start index
        TextSpan? startToken = (from t in Spans.Values
            where t.DocumentId == documentId
                && t.Type == TextSpan.TYPE_TOKEN
                && t.Index >= startIndex
            orderby t.Index
            select t).FirstOrDefault();
        if (startToken == null) return null;

        // end: nearest token with index <= end index
        TextSpan? endToken = (from t in Spans.Values
            where t.DocumentId == documentId
                && t.Type == TextSpan.TYPE_TOKEN
                && t.Index <= endIndex
            orderby t.Index descending
            select t).FirstOrDefault();
        if (endToken == null) return null;

        return Tuple.Create(startToken.P1, endToken.P1);
    }

    public DataPage<SearchResult> Search(SearchRequest request,
        IList<ILiteralFilter>? literalFilters = null)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Gets the context for the specified result(s).
    /// </summary>
    /// <param name="results">The results to get context for.</param>
    /// <param name="contextSize">Size of the context: e.g. if 5, you will get
    /// 5 tokens to the left and 5 to the right.</param>
    /// <returns>results with context</returns>
    /// <exception cref="NotImplementedException">not implemented</exception>
    public IList<KwicSearchResult> GetResultContext(
        IList<SearchResult> results, int contextSize)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Gets the specified page of words.
    /// </summary>
    /// <param name="filter">The words filter.</param>
    /// <returns>The results page.</returns>
    /// <exception cref="ArgumentNullException">filter</exception>
    public DataPage<Word> GetWords(WordFilter filter)
    {
        throw new NotImplementedException();
    }

    public DataPage<Lemma> GetLemmata(LemmaFilter filter)
    {
        throw new NotImplementedException();
    }

    public IList<TokenCount> GetTokenCounts(bool lemma, int id,
        string attrName)
    {
        throw new NotImplementedException();
    }

    public void AddStructure(TextSpan structure, bool hasAttributes)
    {
        throw new NotImplementedException();
    }

    public IDictionary<string, double> GetStatistics()
    {
        throw new NotImplementedException();
    }

    public void PruneTokens()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Gets all the document name=value pairs to be used when filling
    /// word and lemma document counts.
    /// </summary>
    /// <param name="binCounts">The desired bins counts. For each attribute
    /// (either privileged or not) which must be handled as a number,
    /// this dictionary includes its name as the key, and the desired count
    /// of bins as the value. For instance, an attribute named <c>year</c>
    /// whose value is a year number would have an entry with key=<c>year</c>
    /// and value=<c>3</c>, meaning that we want to distribute its values in
    /// 3 bins.</param>
    /// <param name="excludedAttrNames">The names of the non-privileged
    /// attributes to be excluded from the pairs. All the names of
    /// non-categorical attributes (like e.g. the file path of a document,
    /// which is unique for each document) should be excluded.</param>
    /// <returns>Built pairs.</returns>
    public Task<IList<DocumentPair>> GetDocumentPairsAsync(
        IDictionary<string, int> binCounts,
        HashSet<string> excludedAttrNames)
    {
        throw new NotImplementedException();
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public async Task BuildWordIndexAsync(
#pragma warning restore CS1998
        IDictionary<string, int> binCounts,
        HashSet<string> excludedAttrNames,
        HashSet<string> excludedSpanAttrNames,
        CancellationToken cancel,
        IProgress<ProgressReport>? progress = null)
    {
        throw new NotImplementedException();
    }

    public void FinalizeIndex()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Gets the spans starting at the specified position.
    /// </summary>
    /// <param name="documentId">The document ID.</param>
    /// <param name="p1">The start position (P1).</param>
    /// <param name="type">The optional type filter.</param>
    /// <param name="attributes">True to include span attributes.</param>
    /// <returns>Spans.</returns>
    public IList<TextSpan> GetSpansAt(int documentId, int p1,
        string? type = null, bool attributes = false)
    {
        return Spans.Values
            .Where(s => s.DocumentId == documentId &&
                        s.P1 == p1 &&
                        (type == null || s.Type == type))
            .ToList();
    }

    /// <summary>
    /// Enumerates the spans matching the specified filter.
    /// Not implemented.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="attributes">True to include span attributes.</param>
    /// <returns>Spans.</returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TextSpan> EnumerateSpans(TextSpanFilter filter,
        bool attributes = false)
    {
        throw new NotImplementedException();
    }
}

public sealed class RamOccurrence : IHasAttributes
{
    public int DocumentId { get; set; }
    public int Position { get; set; }
    public int Index { get; set; }
    public short Length { get; set; }
    public IList<Attribute>? Attributes { get; set; }

    public RamOccurrence()
    {
        Attributes = [];
    }
}
