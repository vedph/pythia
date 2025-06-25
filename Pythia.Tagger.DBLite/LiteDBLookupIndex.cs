using Fusi.Tools.Data;
using LiteDB;
using Pythia.Tagger.Lookup;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Pythia.Tagger.LiteDB;

/// <summary>
/// LiteDB-based implementation of <see cref="ILookupIndex"/>.
/// This provides efficient lookups with low memory overhead by storing
/// entries in a LiteDB database.
/// </summary>
public sealed class LiteDBLookupIndex : ILookupIndex, IDisposable
{
    private readonly LiteDatabase _db;
    private readonly ILiteCollection<LookupEntry> _collection;
    private readonly DamerauLevenshteinSimilarityScorer _similarityScorer = new();
    private bool _pendingRebuild;
    private bool _disposed;
    private readonly bool _readOnly;

    /// <summary>
    /// Gets the path to the LiteDB database file.
    /// </summary>
    public string DatabasePath { get; }

    /// <summary>
    /// Creates a new instance of the <see cref="LiteDBLookupIndex"/> class
    /// with the specified database path.
    /// </summary>
    /// <param name="dbPath">Path to the LiteDB database file.</param>
    /// <param name="readOnly">True to open in read-only mode, false for
    /// read/write.</param>
    /// <exception cref="ArgumentNullException">dbPath is null</exception>
    public LiteDBLookupIndex(string dbPath, bool readOnly = false)
    {
        ArgumentNullException.ThrowIfNull(dbPath);
        DatabasePath = dbPath;
        _readOnly = readOnly;

        // create directory if it doesn't exist
        string? directory = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // open database with appropriate connection settings
        ConnectionString connectionString = new()
        {
            Filename = dbPath,
            ReadOnly = readOnly
        };

        _db = new LiteDatabase(connectionString);
        _collection = _db.GetCollection<LookupEntry>("entries");

        // create indexes if not read-only
        if (!readOnly)
        {
            EnsureIndexes();
        }
    }

    private void EnsureIndexes()
    {
        // create indexes for efficient lookups
        _collection.EnsureIndex(e => e.Id);
        _collection.EnsureIndex(e => e.Value);
        _collection.EnsureIndex(e => e.Lemma);
        _collection.EnsureIndex(e => e.Pos);
    }

    /// <summary>
    /// Clears the index, removing all entries.
    /// </summary>
    /// <exception cref="InvalidOperationException">When attempting to modify a
    /// read-only index</exception>
    public void Clear()
    {
        if (_readOnly)
            throw new InvalidOperationException("Cannot modify a read-only index");

        _collection.DeleteAll();
        _pendingRebuild = true;
    }

    /// <summary>
    /// Add the specified entry to the index.
    /// </summary>
    /// <param name="entry">The entry to add.</param>
    /// <exception cref="ArgumentNullException">entry is null</exception>
    /// <exception cref="InvalidOperationException">When attempting to modify
    /// a read-only index</exception>
    public void Add(LookupEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (_readOnly)
            throw new InvalidOperationException("Cannot modify a read-only index");

        _collection.Upsert(entry);
        _pendingRebuild = true;
    }

    /// <summary>
    /// Add a batch of entries to the index.
    /// </summary>
    /// <param name="entries">Entries to add.</param>
    /// <exception cref="ArgumentNullException">entries is null</exception>
    /// <exception cref="InvalidOperationException">When attempting to modify
    /// a read-only index</exception>
    public void AddBatch(IEnumerable<LookupEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        if (_readOnly)
            throw new InvalidOperationException("Cannot modify a read-only index");

        // begin an implicit transaction
        _pendingRebuild = true;
        _db.BeginTrans();

        try
        {
            // process entries in small batches to avoid excessive memory usage
            List<LookupEntry> batch = new(1000);
            int count = 0;
            int totalCount = 0;

            foreach (LookupEntry entry in entries)
            {
                batch.Add(entry);
                count++;

                // insert in batches of 1000
                if (count >= 1000)
                {
                    _collection.Upsert(batch);
                    totalCount += batch.Count;
                    batch.Clear();
                    count = 0;

                    // commit and start a new transaction every 50,000 entries
                    // to prevent the transaction log from growing too large
                    if (totalCount % 50000 == 0)
                    {
                        _db.Commit();
                        _db.BeginTrans();
                    }
                }
            }

            // insert any remaining entries
            if (batch.Count > 0) _collection.Upsert(batch);

            // commit the transaction
            _db.Commit();
        }
        catch (Exception ex)
        {
            _db.Rollback();
            throw new InvalidOperationException(
                "Failed to add batch of entries", ex);
        }
    }

    /// <summary>
    /// Gets the entry with the specified identifier.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <returns>Entry or null if not found</returns>
    public LookupEntry? Get(int id)
    {
        return _collection.FindById(id);
    }

    /// <summary>
    /// Lookup the index for all the entries exactly matching the specified
    /// value and optional part of speech.
    /// </summary>
    /// <param name="value">The entry value.</param>
    /// <param name="pos">The optional part of speech.</param>
    /// <returns>Zero or more matching entries.</returns>
    /// <exception cref="ArgumentNullException">value is null</exception>
    public IList<LookupEntry> Lookup(string value, string? pos = null)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (string.IsNullOrEmpty(value)) return [];

        if (pos == null)
        {
            return [.. _collection.Find(e => e.Value == value)];
        }
        else
        {
            return [.. _collection.Find(e => e.Value == value && e.Pos == pos)];
        }
    }

    /// <summary>
    /// Find the entries matching the specified filter.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <returns>The requested page of matching entries, including all the
    /// matching entries when page size is 0.</returns>
    /// <exception cref="ArgumentNullException">filter is null</exception>
    public DataPage<LookupEntry> Find(LookupFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter);

        // start with a query for all entries
        ILiteQueryable<LookupEntry> query = _collection.Query();

        // apply filters
        if (!string.IsNullOrEmpty(filter.Lemma))
        {
            query = query.Where(e => e.Lemma == filter.Lemma);
        }

        if (!string.IsNullOrEmpty(filter.Pos))
        {
            query = query.Where(e => e.Pos == filter.Pos);
        }

        if (!string.IsNullOrEmpty(filter.Value))
        {
            switch (filter.Comparison)
            {
                case LookupEntryComparison.Exact:
                    query = query.Where(e => e.Value == filter.Value);
                    break;
                case LookupEntryComparison.Prefix:
                    // LiteDB doesn't have direct prefix filtering,
                    // so we use LIKE with case-insensitivity
                    query = query.Where($"LOWER($.Value) LIKE " +
                        $"LOWER('{filter.Value}%')");
                    break;
                case LookupEntryComparison.Substring:
                    // use LIKE for substring matching
                    query = query.Where($"LOWER($.Value) LIKE " +
                        $"LOWER('%{filter.Value}%')");
                    break;
                case LookupEntryComparison.Suffix:
                    query = query.Where($"LOWER($.Value) LIKE " +
                        $"LOWER('%{filter.Value}')");
                    break;
                case LookupEntryComparison.Fuzzy:
                    // LiteDB doesn't support fuzzy matching directly,
                    // so we'll do an initial filter then refine
                    query = query.Where($"LOWER($.Value" +
                        $") LIKE LOWER('%{filter.Value}%')");
                    break;
            }
        }

        // get total count before paging
        int total = query.Count();

        // if no results, return empty page
        if (total == 0)
        {
            return new DataPage<LookupEntry>(filter.PageNumber, filter.PageSize,
                0, []);
        }

        // handle paging if page size > 0
        IEnumerable<LookupEntry> results;
        if (filter.PageSize <= 0)
        {
            results = query.ToEnumerable();
        }
        else
        {
            results = query
                .Offset(filter.GetSkipCount())
                .Limit(filter.PageSize)
                .ToEnumerable();
        }

        // for fuzzy matching, we need to filter the results by similarity score
        List<LookupEntry> finalResults;
        if (filter.Comparison == LookupEntryComparison.Fuzzy &&
            !string.IsNullOrEmpty(filter.Value))
        {
            finalResults = [.. results
                .Where(e => e.Value != null &&
                       _similarityScorer.Score(e.Value, filter.Value)
                       >= filter.Threshold)];

            // recalculate total for paging with fuzzy filtering
            if (filter.PageSize > 0)
            {
                total = finalResults.Count;
                finalResults = [.. finalResults
                    .Skip(filter.GetSkipCount())
                    .Take(filter.PageSize)];
            }
        }
        else
        {
            finalResults = [.. results];
        }

        return new DataPage<LookupEntry>(
            filter.PageNumber,
            filter.PageSize,
            total,
            finalResults);
    }

    /// <summary>
    /// Optimizes the database by reindexing and shrinking the file.
    /// This is recommended after adding large batches of entries.
    /// </summary>
    /// <exception cref="InvalidOperationException">When attempting to modify
    /// a read-only index</exception>
    public void Optimize()
    {
        if (_readOnly)
            throw new InvalidOperationException("Cannot modify a read-only index");

        // drop and recreate indexes
        _collection.DropIndex("$.Id");
        _collection.DropIndex("$.Value");
        _collection.DropIndex("$.Lemma");
        _collection.DropIndex("$.Pos");

        EnsureIndexes();

        // shrink the database file to reclaim space
        _db.Rebuild();
    }

    /// <summary>
    /// Gets the count of entries in the index.
    /// </summary>
    /// <returns>The number of entries in the index.</returns>
    public long GetEntryCount()
    {
        return _collection.Count();
    }

    /// <summary>
    /// Disposes the LiteDB database.
    /// </summary>
    /// <summary>
    /// Disposes the LiteDB database.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            try
            {
                // only perform optimization if we're not in read-only mode and 
                // modifications have been made
                if (!_readOnly && _pendingRebuild)
                {
                    // for large databases, just ensuring indexes is more
                    // efficient than a full optimize
                    if (GetEntryCount() > 100000) EnsureIndexes();
                    else Optimize();
                }
            }
            catch
            {
                // ignore exceptions during cleanup
            }
            finally
            {
                _db.Dispose();
                _disposed = true;
            }
        }
    }
}
