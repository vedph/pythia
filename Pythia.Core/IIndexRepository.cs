using System;
using System.Collections.Generic;
using Corpus.Core;
using Fusi.Tools.Data;
using Pythia.Core.Analysis;

namespace Pythia.Core
{
    /// <summary>
    /// Repository for tokens, structures, and documents, with their attributes.
    /// </summary>
    public interface IIndexRepository : ICorpusRepository
    {
        /// <summary>
        /// Adds the specified token. A token is identified by its document
        /// ID and position, but it is assumed that no token exists with the
        /// same ID when adding tokens.
        /// </summary>
        /// <param name="token">The token.</param>
        void AddToken(Token token);

        /// <summary>
        /// Adds all the specified tokens.
        /// </summary>
        /// <param name="tokens">The tokens.</param>
        void AddTokens(IEnumerable<Token> tokens);

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
        void AddTokenAttributes(int documentId, int start, int end,
            string name, string value, AttributeType type);

        /// <summary>
        /// Deletes all the tokens of the document with the specified ID.
        /// </summary>
        /// <param name="documentId">The document identifier.</param>
        void DeleteDocumentTokens(int documentId);

        /// <summary>
        /// Prunes the tokens by deleting all the tokens without any occurrence.
        /// </summary>
        void PruneTokens();

        /// <summary>
        /// Gets the range of token positions starting from the specified
        /// range of token character indexes. This is used by structure
        /// parsers, which often must determine positional ranges starting
        /// from character indexes.
        /// </summary>
        /// <param name="documentId">The document ID.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="endIndex">The end index.</param>
        /// <returns>range or null</returns>
        Tuple<int, int>? GetTokenPositionRange(int documentId,
            int startIndex, int endIndex);

        /// <summary>
        /// Deletes all the structures of the document with the specified ID.
        /// </summary>
        /// <param name="documentId">The document identifier.</param>
        void DeleteDocumentStructures(int documentId);

        /// <summary>
        /// Adds or updates the specified structure. A structure with ID=0
        /// is new, and will be assigned a unique ID.
        /// </summary>
        /// <param name="structure">The structure.</param>
        /// <param name="hasAttributes">If set to <c>true</c>, the attributes
        /// of an existing document should be updated.</param>
        void AddStructure(Structure structure, bool hasAttributes);

        /// <summary>
        /// Adds all the specified structures.
        /// </summary>
        /// <param name="structures">The structures.</param>
        void AddStructures(IEnumerable<Structure> structures);

        /// <summary>
        /// Gets a page of index terms matching the specified filter.
        /// </summary>
        /// <param name="filter">filter</param>
        /// <returns>page</returns>
        DataPage<IndexTerm> GetTerms(TermFilter filter);

        /// <summary>
        /// Searches the index using the specified query.
        /// </summary>
        /// <param name="request">The query request.</param>
        /// <param name="literalFilters">The optional filters to apply to literal
        /// values in the query text.</param>
        /// <returns>results page</returns>
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
        /// Gets statistics about the index.
        /// </summary>
        /// <returns>Dictionary with statistics.</returns>
        IDictionary<string, double> GetStatistics();
    }
}
