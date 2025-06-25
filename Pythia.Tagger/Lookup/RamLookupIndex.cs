using Fusi.Tools.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Pythia.Tagger.Lookup;

/// <summary>
/// RAM-based lookup index, used for testing or fast lookup.
/// </summary>
/// <seealso cref="ILookupIndex" />
public sealed class RamLookupIndex : ILookupIndex
{
    private readonly List<LookupEntry> _entries = [];
    private readonly DamerauLevenshteinSimilarityScorer _similarityScorer = new();

    public RamLookupIndex(IEnumerable<LookupEntry>? entries = null)
    {
        ArgumentNullException.ThrowIfNull(entries);

        _entries.Clear();
        _entries.AddRange(entries);
    }

    /// <summary>
    /// Clears the index, removing all entries.
    /// </summary>
    public void Clear()
    {
        _entries.Clear();
    }

    /// <summary>
    /// Add the specified entry to the index.
    /// </summary>
    /// <param name="entry">The entry to add.</param>
    /// <exception cref="ArgumentNullException">entry</exception>
    public void Add(LookupEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        _entries.Add(entry);
    }

    /// <summary>
    /// Add a batch of entries to the index.
    /// </summary>
    /// <param name="entries">Entries to add.</param>
    /// <exception cref="ArgumentNullException">entries</exception>
    public void AddBatch(IEnumerable<LookupEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        _entries.AddRange(entries);
    }

    /// <summary>
    /// Gets the entry with the specified identifier.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <returns>Entry or null if not found</returns>
    public LookupEntry? Get(int id)
    {
        return _entries.FirstOrDefault(w => w.Id == id);
    }

    /// <summary>
    /// Lookup the index for all the entries exactly matching the specified
    /// value and optional part of speech.
    /// </summary>
    /// <param name="value">The entry value.</param>
    /// <param name="pos">The optional part of speech.</param>
    /// <returns>Zero or more matching entries.</returns>
    /// <exception cref="ArgumentNullException">value</exception>
    public IList<LookupEntry> Lookup(string value, string? pos = null)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (string.IsNullOrEmpty(value)) return [];
        return [.. _entries
            .Where(w => w.Value == value && (pos == null || w.Pos == pos))];
    }

    private IQueryable<LookupEntry> ApplyFilter(IQueryable<LookupEntry> query,
        LookupFilter filter)
    {
        // lemma
        if (!string.IsNullOrEmpty(filter.Lemma))
        {
            query = query.Where(w => w.Lemma == filter.Lemma);
        }

        // pos
        if (!string.IsNullOrEmpty(filter.Pos))
        {
            query = query.Where(w => w.Pos == filter.Pos);
        }

        // value
        if (!string.IsNullOrEmpty(filter.Value))
        {
            switch (filter.Comparison)
            {
                case LookupEntryComparison.Exact:
                    query = query.Where(w => w.Value == filter.Value);
                    break;
                case LookupEntryComparison.Prefix:
                    query = query.Where(w => w.Value!.StartsWith(filter.Value, StringComparison.OrdinalIgnoreCase));
                    break;
                case LookupEntryComparison.Substring:
                    query = query.Where(w => w.Value!.Contains(filter.Value,
                        StringComparison.OrdinalIgnoreCase));
                    break;
                case LookupEntryComparison.Suffix:
                    query = query.Where(w => w.Value!.EndsWith(filter.Value,
                        StringComparison.OrdinalIgnoreCase));
                    break;
                case LookupEntryComparison.Fuzzy:
                    if (filter.Threshold < 0 || filter.Threshold > 1)
                    {
                        throw new ArgumentOutOfRangeException(nameof(
                            filter.Threshold),
                            "Threshold must be between 0 and 1.");
                    }
                    query = query.Where(w => w.Value != null &&
                        _similarityScorer.Score(w.Value, filter.Value)
                            >= filter.Threshold);
                    break;
            }
        }

        return query;
    }

    /// <summary>
    /// Find the entries matching the specified filter.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <returns>The requested page of matching entries, including all the
    /// matching entries when page size is 0.</returns>
    /// <exception cref="ArgumentNullException">filter</exception>
    public DataPage<LookupEntry> Find(LookupFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter);

        IQueryable<LookupEntry> query =
            ApplyFilter(_entries.AsQueryable(), filter);
        int total = query.Count();

        if (total == 0)
        {
            return new DataPage<LookupEntry>(
                filter.PageNumber, filter.PageSize, 0, []);
        }

        // apply paging if page size > 0
        if (filter.PageSize <= 0)
        {
            return new DataPage<LookupEntry>(filter.PageNumber, filter.PageSize,
                query.Count(), [.. query]);
        }
        else
        {
            query = query.Skip(filter.GetSkipCount())
                         .Take(filter.PageSize);
            return new DataPage<LookupEntry>(filter.PageNumber, filter.PageSize,
                total, [.. query]);
        }
    }
}
