using System.Collections.Generic;

namespace Pythia.Tagger.Lookup
{
    /// <summary>
    /// Lookup index.
    /// </summary>
    public interface ILookupIndex
    {
        /// <summary>
        /// Finds the entries matching the specified filter.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <returns>entries</returns>
        IList<LookupEntry> Find(LookupFilter filter);

        /// <summary>
        /// Gets the entry with the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>entry or null if not found</returns>
        LookupEntry? Get(int id);
    }
}
