using Fusi.Tools.Data;
using System.Collections.Generic;

namespace Pythia.Tagger.Lookup;

/// <summary>
/// Lookup index.
/// </summary>
public interface ILookupIndex
{
    /// <summary>
    /// Clears the index, removing all entries.
    /// </summary>
    public void Clear();

    /// <summary>
    /// Add the specified entry to the index.
    /// </summary>
    /// <param name="entry">The entry to add.</param>
    public void Add(LookupEntry entry);

    /// <summary>
    /// Add a batch of entries to the index.
    /// </summary>
    /// <param name="entries">Entries to add.</param>
    public void AddBatch(IEnumerable<LookupEntry> entries);

    /// <summary>
    /// Gets the entry with the specified identifier.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <returns>Entry or null if not found</returns>
    LookupEntry? Get(int id);

    /// <summary>
    /// Lookup the index for all the entries exactly matching the specified
    /// value and optional part of speech.
    /// </summary>
    /// <param name="value">The entry value.</param>
    /// <param name="pos">The optional part of speech.</param>
    /// <returns>Zero or more matching entries.</returns>
    IList<LookupEntry> Lookup(string value, string? pos = null);

    /// <summary>
    /// Find the entries matching the specified filter.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <returns>The requested page of matching entries, including all the
    /// matching entries when page size is 0.</returns>
    DataPage<LookupEntry> Find(LookupFilter filter);
}
