using Fusi.Tools.Data;
using Pythia.Tagger.Lookup;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Pythia.Tagger.LiteDB.Test;

public sealed class LiteDBLookupIndexTest : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(),
        $"litedb-test-{Guid.NewGuid()}.db");

    public void Dispose()
    {
        try
        {
            // clean up any test database files
            if (File.Exists(_dbPath)) File.Delete(_dbPath);
        }
        catch
        {
            // ignore errors during cleanup
        }
    }

    private LiteDBLookupIndex CreateIndex(bool readOnly = false)
    {
        return new LiteDBLookupIndex(_dbPath, readOnly);
    }

    private static LookupEntry CreateEntry(int id, string value,
        string? lemma = null, string? pos = null)
    {
        return new LookupEntry
        {
            Id = id,
            Text = value,
            Value = value,
            Lemma = lemma,
            Pos = pos
        };
    }

    private void EnsureDatabaseCreated()
    {
        string dbPath = _dbPath;
        using (var writableIndex = new LiteDBLookupIndex(dbPath))
        {
            // add at least one entry so the database file is created
            writableIndex.Add(CreateEntry(0, "dummy"));
        }
    }

    [Fact]
    public void Constructor_CreatesDatabase()
    {
        using LiteDBLookupIndex index = CreateIndex();

        Assert.True(File.Exists(_dbPath));
    }

    [Fact]
    public void Constructor_WithNonexistentDirectory_CreatesDirectoryAndDatabase()
    {
        string directory = Path.Combine(Path.GetTempPath(),
            Guid.NewGuid().ToString());
        string path = Path.Combine(directory, "test.db");

        try
        {
            using LiteDBLookupIndex index = new(path);

            Assert.True(Directory.Exists(directory));
            Assert.True(File.Exists(path));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
            if (Directory.Exists(directory))
                Directory.Delete(directory);
        }
    }

    [Fact]
    public void Constructor_NullPath_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new LiteDBLookupIndex(null!));
    }

    [Fact]
    public void Clear_EmptyDatabase_RemovesNoEntries()
    {
        using LiteDBLookupIndex index = CreateIndex();

        index.Clear();

        Assert.Equal(0, index.GetEntryCount());
    }

    [Fact]
    public void Clear_WithEntries_RemovesAllEntries()
    {
        using LiteDBLookupIndex index = CreateIndex();
        index.Add(CreateEntry(1, "test"));

        index.Clear();

        Assert.Equal(0, index.GetEntryCount());
    }

    [Fact]
    public void Clear_ReadOnly_ThrowsInvalidOperationException()
    {
        EnsureDatabaseCreated();

        using LiteDBLookupIndex index = CreateIndex(readOnly: true);

        Assert.Throws<InvalidOperationException>(() => index.Clear());
    }

    [Fact]
    public void Add_ValidEntry_AddsToIndex()
    {
        using LiteDBLookupIndex index = CreateIndex();
        LookupEntry entry = CreateEntry(1, "test");

        index.Add(entry);

        Assert.Equal(1, index.GetEntryCount());
        LookupEntry? retrieved = index.Get(1);
        Assert.Equal(entry.Value, retrieved?.Value);
    }

    [Fact]
    public void Add_NullEntry_ThrowsArgumentNullException()
    {
        using LiteDBLookupIndex index = CreateIndex();

        Assert.Throws<ArgumentNullException>(() => index.Add(null!));
    }

    [Fact]
    public void Add_ReadOnly_ThrowsInvalidOperationException()
    {
        EnsureDatabaseCreated();

        using LiteDBLookupIndex index = CreateIndex(readOnly: true);
        LookupEntry entry = CreateEntry(1, "test");

        Assert.Throws<InvalidOperationException>(() => index.Add(entry));
    }

    [Fact]
    public void AddBatch_ValidEntries_AddsAllToIndex()
    {
        using LiteDBLookupIndex index = CreateIndex();
        List<LookupEntry> entries =
        [
            CreateEntry(1, "one"),
            CreateEntry(2, "two"),
            CreateEntry(3, "three")
        ];

        index.AddBatch(entries);

        Assert.Equal(3, index.GetEntryCount());
        Assert.Equal("one", index.Get(1)?.Value);
        Assert.Equal("two", index.Get(2)?.Value);
        Assert.Equal("three", index.Get(3)?.Value);
    }

    [Fact]
    public void AddBatch_LargeNumberOfEntries_AddsBatchesCorrectly()
    {
        using LiteDBLookupIndex index = CreateIndex();
        List<LookupEntry> entries = [];

        // create 2500 entries (to test transaction batching)
        for (int i = 1; i <= 2500; i++)
            entries.Add(CreateEntry(i, $"entry-{i}"));

        index.AddBatch(entries);

        Assert.Equal(2500, index.GetEntryCount());

        Assert.Equal("entry-1", index.Get(1)?.Value);
        Assert.Equal("entry-1000", index.Get(1000)?.Value);
        Assert.Equal("entry-2500", index.Get(2500)?.Value);
    }

    [Fact]
    public void AddBatch_NullEntries_ThrowsArgumentNullException()
    {
        using LiteDBLookupIndex index = CreateIndex();

        Assert.Throws<ArgumentNullException>(() => index.AddBatch(null!));
    }

    [Fact]
    public void AddBatch_ReadOnly_ThrowsInvalidOperationException()
    {
        EnsureDatabaseCreated();

        using LiteDBLookupIndex index = CreateIndex(readOnly: true);
        List<LookupEntry> entries = [CreateEntry(1, "test")];

        Assert.Throws<InvalidOperationException>(() => index.AddBatch(entries));
    }

    [Fact]
    public void Get_ExistingId_ReturnsEntry()
    {
        using LiteDBLookupIndex index = CreateIndex();
        LookupEntry entry = CreateEntry(42, "test");
        index.Add(entry);

        LookupEntry? result = index.Get(42);

        Assert.NotNull(result);
        Assert.Equal(42, result.Id);
        Assert.Equal("test", result.Value);
    }

    [Fact]
    public void Get_NonexistentId_ReturnsNull()
    {
        using LiteDBLookupIndex index = CreateIndex();

        LookupEntry? result = index.Get(999);

        Assert.Null(result);
    }

    [Fact]
    public void Lookup_ExactMatchNoPos_ReturnsEntries()
    {
        using LiteDBLookupIndex index = CreateIndex();
        index.Add(CreateEntry(1, "apple", "apple", "NOUN"));
        index.Add(CreateEntry(2, "apple", "apple", "VERB"));
        index.Add(CreateEntry(3, "banana", "banana", "NOUN"));

        IList<LookupEntry> results = index.Lookup("apple");

        Assert.Equal(2, results.Count);
        Assert.Contains(results, e => e.Id == 1);
        Assert.Contains(results, e => e.Id == 2);
    }

    [Fact]
    public void Lookup_ExactMatchWithPos_ReturnsMatchingEntries()
    {
        using LiteDBLookupIndex index = CreateIndex();
        index.Add(CreateEntry(1, "apple", "apple", "NOUN"));
        index.Add(CreateEntry(2, "apple", "apple", "VERB"));
        index.Add(CreateEntry(3, "banana", "banana", "NOUN"));

        IList<LookupEntry> results = index.Lookup("apple", "NOUN");

        Assert.Single(results);
        Assert.Equal(1, results[0].Id);
    }

    [Fact]
    public void Lookup_NoMatch_ReturnsEmptyList()
    {
        using LiteDBLookupIndex index = CreateIndex();
        index.Add(CreateEntry(1, "apple"));

        IList<LookupEntry> results = index.Lookup("orange");

        Assert.Empty(results);
    }

    [Fact]
    public void Lookup_EmptyValue_ReturnsEmptyList()
    {
        using LiteDBLookupIndex index = CreateIndex();
        index.Add(CreateEntry(1, "apple"));

        IList<LookupEntry> results = index.Lookup("");

        Assert.Empty(results);
    }

    [Fact]
    public void Lookup_NullValue_ThrowsArgumentNullException()
    {
        using LiteDBLookupIndex index = CreateIndex();

        Assert.Throws<ArgumentNullException>(() => index.Lookup(null!));
    }

    [Fact]
    public void Find_ExactMatch_ReturnsMatchingEntries()
    {
        using LiteDBLookupIndex index = CreateIndex();
        index.Add(CreateEntry(1, "apple", "apple", "NOUN"));
        index.Add(CreateEntry(2, "apple", "apple", "VERB"));
        index.Add(CreateEntry(3, "banana", "banana", "NOUN"));

        LookupFilter filter = new()
        {
            Value = "apple",
            Comparison = LookupEntryComparison.Exact
        };

        DataPage<LookupEntry> page = index.Find(filter);

        Assert.Equal(2, page.Total);
        Assert.Equal(2, page.Items.Count);
        Assert.Contains(page.Items, e => e.Id == 1);
        Assert.Contains(page.Items, e => e.Id == 2);
    }

    [Fact]
    public void Find_PrefixMatch_ReturnsMatchingEntries()
    {
        using LiteDBLookupIndex index = CreateIndex();
        index.Add(CreateEntry(1, "apple"));
        index.Add(CreateEntry(2, "application"));
        index.Add(CreateEntry(3, "banana"));

        LookupFilter filter = new()
        {
            Value = "app",
            Comparison = LookupEntryComparison.Prefix
        };

        DataPage<LookupEntry> page = index.Find(filter);

        Assert.Equal(2, page.Total);
        Assert.Equal(2, page.Items.Count);
        Assert.Contains(page.Items, e => e.Id == 1);
        Assert.Contains(page.Items, e => e.Id == 2);
    }

    [Fact]
    public void Find_SubstringMatch_ReturnsMatchingEntries()
    {
        using LiteDBLookupIndex index = CreateIndex();
        index.Add(CreateEntry(1, "apple"));
        index.Add(CreateEntry(2, "application"));
        index.Add(CreateEntry(3, "banana"));
        index.Add(CreateEntry(4, "snapple"));

        LookupFilter filter = new()
        {
            Value = "ppl",
            Comparison = LookupEntryComparison.Substring
        };

        DataPage<LookupEntry> page = index.Find(filter);

        Assert.Equal(3, page.Total);
        Assert.Equal(3, page.Items.Count);
        Assert.Contains(page.Items, e => e.Id == 1);
        Assert.Contains(page.Items, e => e.Id == 2);
        Assert.Contains(page.Items, e => e.Id == 4);
    }

    [Fact]
    public void Find_SuffixMatch_ReturnsMatchingEntries()
    {
        using LiteDBLookupIndex index = CreateIndex();
        index.Add(CreateEntry(1, "apple"));
        index.Add(CreateEntry(2, "snapple"));
        index.Add(CreateEntry(3, "banana"));

        LookupFilter filter = new()
        {
            Value = "ple",
            Comparison = LookupEntryComparison.Suffix
        };

        DataPage<LookupEntry> page = index.Find(filter);

        Assert.Equal(2, page.Total);
        Assert.Equal(2, page.Items.Count);
        Assert.Contains(page.Items, e => e.Id == 1);
        Assert.Contains(page.Items, e => e.Id == 2);
    }

    [Fact]
    public void Find_FuzzyMatch_ReturnsMatchingEntries()
    {
        using LiteDBLookupIndex index = CreateIndex();
        index.Add(CreateEntry(1, "apple"));
        index.Add(CreateEntry(2, "appl"));
        index.Add(CreateEntry(3, "aple"));
        index.Add(CreateEntry(4, "banana"));

        LookupFilter filter = new()
        {
            Value = "apple",
            Comparison = LookupEntryComparison.Fuzzy,
            Threshold = 0.7f // 70% similarity threshold
        };

        DataPage<LookupEntry> page = index.Find(filter);

        Assert.Equal(3, page.Total);
        Assert.Equal(3, page.Items.Count);
        Assert.Contains(page.Items, e => e.Id == 1);
        Assert.Contains(page.Items, e => e.Id == 2);
        Assert.Contains(page.Items, e => e.Id == 3);
    }

    [Fact]
    public void Find_WithLemmaFilter_ReturnsMatchingEntries()
    {
        using LiteDBLookupIndex index = CreateIndex();
        index.Add(CreateEntry(1, "running", "run", "VERB"));
        index.Add(CreateEntry(2, "runs", "run", "VERB"));
        index.Add(CreateEntry(3, "running", "running", "NOUN"));

        LookupFilter filter = new() { Lemma = "run" };

        DataPage<LookupEntry> page = index.Find(filter);

        Assert.Equal(2, page.Total);
        Assert.Equal(2, page.Items.Count);
        Assert.Contains(page.Items, e => e.Id == 1);
        Assert.Contains(page.Items, e => e.Id == 2);
    }

    [Fact]
    public void Find_WithPosFilter_ReturnsMatchingEntries()
    {
        using LiteDBLookupIndex index = CreateIndex();
        index.Add(CreateEntry(1, "run", "run", "VERB"));
        index.Add(CreateEntry(2, "run", "run", "NOUN"));

        LookupFilter filter = new() { Pos = "VERB" };

        DataPage<LookupEntry> page = index.Find(filter);

        Assert.Equal(1, page.Total);
        Assert.Single(page.Items);
        Assert.Equal(1, page.Items[0].Id);
    }

    [Fact]
    public void Find_WithPaging_ReturnsCorrectPage()
    {
        using LiteDBLookupIndex index = CreateIndex();
        for (int i = 1; i <= 10; i++)
        {
            index.Add(CreateEntry(i, $"entry-{i}"));
        }

        LookupFilter filter = new()
        {
            PageNumber = 2,
            PageSize = 3
        };

        DataPage<LookupEntry> page = index.Find(filter);

        Assert.Equal(10, page.Total); // total number of entries
        Assert.Equal(3, page.Items.Count); // page size
        Assert.Equal(2, page.PageNumber);
        Assert.Contains(page.Items, e => e.Id == 4);
        Assert.Contains(page.Items, e => e.Id == 5);
        Assert.Contains(page.Items, e => e.Id == 6);
    }

    [Fact]
    public void Find_WithZeroPageSize_ReturnsAllEntries()
    {
        using LiteDBLookupIndex index = CreateIndex();
        for (int i = 1; i <= 10; i++)
        {
            index.Add(CreateEntry(i, $"entry-{i}"));
        }

        LookupFilter filter = new()
        {
            PageSize = 0
        };

        DataPage<LookupEntry> page = index.Find(filter);

        Assert.Equal(10, page.Total);
        Assert.Equal(10, page.Items.Count);
    }

    [Fact]
    public void Find_NullFilter_ThrowsArgumentNullException()
    {
        using LiteDBLookupIndex index = CreateIndex();

        Assert.Throws<ArgumentNullException>(() => index.Find(null!));
    }

    [Fact]
    public void Find_NoMatches_ReturnsEmptyPage()
    {
        using LiteDBLookupIndex index = CreateIndex();
        index.Add(CreateEntry(1, "apple"));

        LookupFilter filter = new() { Value = "nonexistent" };

        DataPage<LookupEntry> page = index.Find(filter);

        Assert.Equal(0, page.Total);
        Assert.Empty(page.Items);
    }

    [Fact]
    public void GetEntryCount_EmptyIndex_ReturnsZero()
    {
        using LiteDBLookupIndex index = CreateIndex();

        long count = index.GetEntryCount();

        Assert.Equal(0, count);
    }

    [Fact]
    public void GetEntryCount_WithEntries_ReturnsCorrectCount()
    {
        using LiteDBLookupIndex index = CreateIndex();
        index.Add(CreateEntry(1, "one"));
        index.Add(CreateEntry(2, "two"));

        long count = index.GetEntryCount();

        Assert.Equal(2, count);
    }

    [Fact]
    public void Optimize_ModifiesDatabase()
    {
        using LiteDBLookupIndex index = CreateIndex();
        for (int i = 1; i <= 10; i++)
        {
            index.Add(CreateEntry(i, $"entry-{i}"));
        }

        // primarily testing that it doesn't throw
        index.Optimize();

        // additional verification that data is still accessible
        Assert.Equal(10, index.GetEntryCount());
        Assert.Equal("entry-5", index.Get(5)?.Value);
    }

    [Fact]
    public void Optimize_ReadOnly_ThrowsInvalidOperationException()
    {
        using LiteDBLookupIndex writableIndex = CreateIndex();
        writableIndex.Add(CreateEntry(1, "test"));

        // create a new read-only index on the same database
        using LiteDBLookupIndex readOnlyIndex = CreateIndex(readOnly: true);

        Assert.Throws<InvalidOperationException>(() => readOnlyIndex.Optimize());
    }

    [Fact]
    public void ReadOnlyMode_CanReadExistingData()
    {
        // create and populate a database
        {
            using LiteDBLookupIndex writableIndex = CreateIndex();
            writableIndex.Add(CreateEntry(1, "test"));
        }

        // open in read-only mode
        using LiteDBLookupIndex readOnlyIndex = CreateIndex(readOnly: true);
        LookupEntry? entry = readOnlyIndex.Get(1);

        Assert.NotNull(entry);
        Assert.Equal("test", entry.Value);
    }

    [Fact]
    public void Dispose_CallsDispose()
    {
        LiteDBLookupIndex index = CreateIndex();
        index.Add(CreateEntry(1, "test"));

        index.Dispose();

        // attempting to use after dispose should throw
        Assert.Throws<ObjectDisposedException>(() =>
        {
            index.Get(1);
        });
    }
}
