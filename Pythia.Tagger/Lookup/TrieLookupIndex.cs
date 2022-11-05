using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Fusi.Tools;
using Fusi.Tools.Data;

namespace Pythia.Tagger.Lookup
{
    /// <summary>
    /// Trie-based lookup index.
    /// </summary>
    /// <remarks>A trie-based index stores entries in a trie structure,
    /// where each key is the lemma key and data is a list of entries (as more
    /// than 1 entry might match the same key).
    /// As for the storage format, this is defined by the
    /// <see cref="ILookupEntrySerializer"/> used.
    /// </remarks>
    public sealed class TrieLookupIndex : ILookupIndex
    {
        private readonly ILookupEntrySerializer _serializer;
        private readonly Trie _trie;

        /// <summary>
        /// Initializes a new instance of the <see cref="TrieLookupIndex"/> class.
        /// </summary>
        /// <param name="serializer">The serializer.</param>
        /// <exception cref="ArgumentNullException">serializer</exception>
        public TrieLookupIndex(ILookupEntrySerializer serializer)
        {
            _serializer = serializer
                ?? throw new ArgumentNullException(nameof(serializer));
            _trie = new Trie();
        }

        /// <summary>
        /// Loads the trie from the specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="progress">The optional progress reporter.</param>
        /// <param name="cancel">The optional cancel token.</param>
        /// <exception cref="ArgumentNullException">null stream</exception>
        public void Load(Stream stream,
            IProgress<ProgressReport>? progress = null,
            CancellationToken? cancel = null)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            _trie.Clear();
            LookupEntry? lemma;
            ProgressReport report = new();

            while ((lemma = _serializer.Deserialize(stream)) != null)
            {
                LookupEntry lemmaCopy = lemma;
                _trie.Insert(lemma.Value!,
                    node =>
                    {
                        if (node.Data == null)
                        {
                            node.Data = new List<LookupEntry>();
                        }
                        else
                        {
                            ((IList<LookupEntry>)node.Data).Add(lemmaCopy);
                        }
                    });

                if (cancel.HasValue && cancel.Value.IsCancellationRequested)
                    break;

                if (progress != null)
                {
                    report.Count++;
                    report.Message = lemma.Id + ": " + lemma.Value;
                    progress.Report(report);
                }
            }
        }

        private IList<LookupEntry> FindExact(LookupFilter query)
        {
            TrieNode? node = _trie.Get(query.Value!);
            if (node == null) return Array.Empty<LookupEntry>();

            int skip = (query.PageNumber - 1) * query.PageSize;
            var entries = (IEnumerable<LookupEntry>)node.Data!;

            if (query.Filter == null)
                return entries.Skip(skip).Take(query.PageSize).ToList();

            return entries.Where(l => query.Filter(l))
                .Skip(skip).Take(query.PageSize).ToList();
        }

        /// <summary>
        /// Finds the lemmata matching the specified key.
        /// </summary>
        /// <param name="filter">The filter to match.</param>
        /// <returns>lemmata</returns>
        /// <exception cref="ArgumentNullException">null filter</exception>
        public IList<LookupEntry> Find(LookupFilter filter)
        {
            if (filter == null) throw new ArgumentNullException(nameof(filter));

            IEnumerable<TrieNode> nodes;

            if (!string.IsNullOrEmpty(filter.Value))
            {
                if (!filter.IsValuePrefix) return FindExact(filter);
                nodes = _trie.Find(filter.Value).Skip((filter.PageNumber - 1) *
                    filter.PageSize).Take(filter.PageSize);
            }
            else
            {
                nodes = _trie.GetAll();
            }

            List<LookupEntry> entries = new();
            int skip = (filter.PageNumber - 1) * filter.PageSize;

            foreach (TrieNode node in nodes)
            {
                IEnumerable<LookupEntry> lemmata = (IEnumerable<LookupEntry>)
                    node.Data!;
                foreach (LookupEntry lemma in lemmata)
                {
                    if ((filter.Filter == null) || filter.Filter(lemma))
                    {
                        if (skip == 0)
                        {
                            entries.Add(lemma);
                            if (entries.Count == filter.PageSize) break;
                        }
                        else
                        {
                            skip--;
                        }
                    }
                }
            }

            return entries;
        }

        /// <summary>
        /// Gets the lemma with the specified identifier.
        /// </summary>
        /// <param name="id">The lemma identifier.</param>
        /// <returns>lemma or null if not found</returns>
        public LookupEntry? Get(int id)
        {
            foreach (TrieNode node in _trie.GetAll())
            {
                IEnumerable<LookupEntry> lemmata = (IEnumerable<LookupEntry>)
                    node.Data!;
                LookupEntry? entry = lemmata.FirstOrDefault(l => l.Id == id);
                if (entry != null) return entry;
            }

            return null;
        }
    }
}
