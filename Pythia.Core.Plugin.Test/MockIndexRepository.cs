using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Corpus.Core;
using Fusi.Tools.Data;
using Pythia.Core.Analysis;
using Attribute = Corpus.Core.Attribute;

namespace Pythia.Core.Plugin.Test
{
    /// <summary>
    /// Mock repository used by tests in this assembly, and only minimally
    /// implemented.
    /// </summary>
    public sealed class MockIndexRepository : RamCorpusRepository,
        IIndexRepository
    {
        private readonly object _locker;

        private int _nextTokenId;
        private int _nextStructId;

        public ConcurrentDictionary<int, Token> Tokens { get; }
        public ConcurrentDictionary<int, Structure> Structures { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MockIndexRepository" />
        /// class.
        /// </summary>
        public MockIndexRepository()
        {
            _locker = new object();
            Tokens = new ConcurrentDictionary<int, Token>();
            Structures = new ConcurrentDictionary<int, Structure>();
        }

        #region Helpers
        private int GetNextTokenId()
        {
            lock(_locker)
            {
                Interlocked.Increment(ref _nextTokenId);
            }
            return _nextTokenId;
        }

        private int GetNextStructId()
        {
            lock (_locker)
            {
                Interlocked.Increment(ref _nextStructId);
            }
            return _nextStructId;
        }
        #endregion

        /// <summary>
        /// Adds the specified attribute.
        /// </summary>
        /// <param name="attribute">The attribute.</param>
        /// <param name="targetType">The target type for this attribute.</param>
        /// <param name="unique">If set to <c>true</c>, replace any other
        /// attribute from the same document and with the same type with the
        /// new one.</param>
        /// <exception cref="ArgumentNullException">attribute</exception>
        public override void AddAttribute(Attribute attribute,
            string targetType, bool unique)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Removes all the structures of the document with the specified ID.
        /// </summary>
        /// <param name="documentId">The document identifier.</param>
        public void DeleteDocumentStructures(int documentId)
        {
            foreach (var p in Structures
                .Where(p => p.Value.DocumentId == documentId))
            {
                Structures.TryRemove(p.Key, out Structure? s);
            }
        }

        /// <summary>
        /// Adds or updates the specified structure. A structure with ID=0 is
        /// new, and will be assigned a unique ID.
        /// </summary>
        /// <param name="structure">The structure.</param>
        /// <exception cref="ArgumentNullException">structure</exception>
        public void AddStructure(Structure structure)
        {
            if (structure == null)
                throw new ArgumentNullException(nameof(structure));

            if (structure.Id == 0) structure.Id = GetNextStructId();
            Structures[structure.Id] = structure;
        }

        /// <summary>
        /// Adds all the specified structures.
        /// </summary>
        /// <param name="structures">The structures.</param>
        /// <exception cref="NotImplementedException"></exception>
        public void AddStructures(IEnumerable<Structure> structures)
        {
            foreach (Structure structure in structures)
                AddStructure(structure);
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
        public void AddTokenAttributes(int documentId, int start, int end,
            string name, string value, AttributeType type)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Removes all the tokens of the document with the specified ID.
        /// </summary>
        /// <param name="documentId">The document identifier.</param>
        public void DeleteDocumentTokens(int documentId)
        {
            foreach (var p in Tokens
                .Where(p => p.Value.DocumentId == documentId))
            {
                Tokens.TryRemove(p.Key, out Token? t);
            }
        }

        /// <summary>
        /// Adds the specified token. This blindly adds the token without
        /// checking whether it exists or not.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <exception cref="ArgumentNullException">token</exception>
        public void AddToken(Token token)
        {
            int id = GetNextTokenId();
            Tokens[id] = token ?? throw new ArgumentNullException(nameof(token));
        }

        /// <summary>
        /// Adds all the specified tokens.
        /// </summary>
        /// <param name="tokens">The tokens.</param>
        /// <exception cref="ArgumentNullException">tokens</exception>
        public void AddTokens(IEnumerable<Token> tokens)
        {
            if (tokens == null) throw new ArgumentNullException(nameof(tokens));

            foreach (Token token in tokens) AddToken(token);
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
        public Tuple<int, int>? GetTokenPositionRange(int documentId,
            int startIndex, int endIndex)
        {
            // start: nearest token with index >= start index
            Token? startToken = (from t in Tokens.Values
                where t.DocumentId == documentId && t.Index >= startIndex
                orderby t.Index
                select t).FirstOrDefault();
            if (startToken == null) return null;

            // end: nearest token with index <= end index
            Token? endToken = (from t in Tokens.Values
                                where t.DocumentId == documentId
                                      && t.Index <= endIndex
                              orderby t.Index descending
                                select t).FirstOrDefault();
            if (endToken == null) return null;

            return Tuple.Create(startToken.Position, endToken.Position);
        }

        /// <summary>
        /// Gets a page of index terms matching the specified filter.
        /// </summary>
        /// <param name="filter">filter</param>
        /// <returns>page</returns>
        public DataPage<IndexTerm> GetTerms(TermFilter filter)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the distribution of the specified term with reference with
        /// the specified document/occurrence attributes.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>The result.</returns>
        public TermDistributionSet GetTermDistributions(
            TermDistributionRequest request)
        {
            throw new NotImplementedException();
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

        public IDictionary<string, double> GetStatistics(string id)
        {
            throw new NotImplementedException();
        }

        public void AddStructure(Structure structure, bool hasAttributes)
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
            Attributes = new List<Attribute>();
        }
    }
}
