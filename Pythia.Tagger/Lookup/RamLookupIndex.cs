using System;
using System.Collections.Generic;
using System.Linq;

namespace Pythia.Tagger.Lookup
{
    /// <summary>
    /// RAM-based lookup index, used for testing.
    /// </summary>
    /// <seealso cref="ILookupIndex" />
    public sealed class RamLookupIndex : ILookupIndex
    {
        /// <summary>
        /// Gets the entries.
        /// </summary>
        public List<LookupEntry> Entries { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RamLookupIndex"/> class.
        /// </summary>
        public RamLookupIndex()
        {
            Entries = new List<LookupEntry>();
        }

        /// <summary>
        /// Finds the entries matching the specified filter.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <returns>
        /// entries
        /// </returns>
        public IList<LookupEntry> Find(LookupFilter filter)
        {
            IQueryable<LookupEntry> entries = Entries.AsQueryable();

            if (!string.IsNullOrEmpty(filter.Value))
            {
                entries = filter.IsValuePrefix
                    ? entries.Where(l => l.Value.StartsWith(filter.Value, StringComparison.Ordinal))
                    : entries.Where(l => l.Value == filter.Value);
            }

            if (filter.Filter != null)
                entries = entries.Where(e => filter.Filter(e));

            int skip = (filter.PageNumber - 1) * filter.PageSize;
            return entries.Skip(skip).Take(filter.PageSize).ToList();
        }

        /// <summary>
        /// Gets the entry with the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>
        /// entry or null if not found
        /// </returns>
        public LookupEntry Get(int id)
        {
            return Entries.Find(e => e.Id == id);
        }
    }
}
