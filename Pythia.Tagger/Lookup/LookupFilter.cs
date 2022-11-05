using Fusi.Tools.Data;
using System;

namespace Pythia.Tagger.Lookup
{
    /// <summary>
    /// Lookup filter.
    /// </summary>
    public class LookupFilter : PagingOptions
    {
        /// <summary>
        /// Gets or sets the value to look for.
        /// </summary>
        public string? Value { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether <see cref="Value" />
        /// represents a prefix to be matched rather than a complete value.
        /// Lookup is always optimized by prefix; for suffix or other types
        /// of matching, use the <see cref="Filter" /> property with or without
        /// the value.
        /// </summary>
        public bool IsValuePrefix { get; set; }

        /// <summary>
        /// Custom lemma filter.
        /// </summary>
        public Func<LookupEntry, bool>? Filter { get; set; }
    }
}
