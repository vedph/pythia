using Fusi.Tools.Config;
using Pythia.Core.Analysis;
using System;
using System.Collections.Generic;

namespace Pythia.Core.Plugin.Analysis
{
    /// <summary>
    /// Attributes supplier token filter, drawing selected attributes from
    /// the tokens stored in a file-system based cache.
    /// Tag: <c>token-filter.cache-supplier.fs</c>.
    /// </summary>
    /// <remarks>This filter is used in deferred POS tagging, to supply POS
    /// tags from a tokens cache, which is assumed to have been processed
    /// by a 3rd-party POS tagger. Typically, this adds a <c>pos</c> attribute
    /// to each tagged token, which is later consumed by this filter during
    /// indexing.</remarks>
    /// <seealso cref="Pythia.Core.Analysis.ITokenFilter" />
    [Tag("token-filter.cache-supplier.fs")]
    public sealed class FsCacheSupplierTokenFilter : ITokenFilter,
        IConfigurable<FsCacheSupplierTokenFilterOptions>
    {
        private readonly HashSet<string> _attrNames;
        private string _cacheDir;
        private ITokenCache _cache;

        /// <summary>
        /// Initializes a new instance of the <see cref="FsCacheSupplierTokenFilter"/>
        /// class.
        /// </summary>
        public FsCacheSupplierTokenFilter()
        {
            _attrNames = new HashSet<string>();
        }

        /// <summary>
        /// Configures the object with the specified options.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException">options</exception>
        public void Configure(FsCacheSupplierTokenFilterOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            _cacheDir = options.CacheDirectory;
            _attrNames.Clear();
            _cache = new FsForwardTokenCache();
            _cache.Open(_cacheDir);
            foreach (string a in options.SuppliedAttributes)
                _attrNames.Add(a);
        }

        /// <summary>
        /// Apply the filter to the specified token.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <param name="position">The position which will be assigned to
        /// the resulting token, provided that it's not empty. This value
        /// is used (together with the current document ID) to identify tokens
        /// in the cache.</param>
        /// <exception cref="ArgumentNullException">token</exception>
        public void Apply(Token token, int position)
        {
            if (token == null)
                throw new ArgumentNullException(nameof(token));

            if (_cache == null || _attrNames.Count == 0) return;

            Token cached = _cache.GetToken(token.DocumentId, position);
            if (cached != null)
            {
                foreach (var attribute in cached.Attributes)
                {
                    if (_attrNames.Contains(attribute.Name))
                        token.Attributes.Add(attribute);
                }
            }
        }
    }

    /// <summary>
    /// Options for <see cref="FsCacheSupplierTokenFilter"/>.
    /// </summary>
    public sealed class FsCacheSupplierTokenFilterOptions
    {
        /// <summary>
        /// Gets or sets the tokens cache directory.
        /// </summary>
        public string CacheDirectory { get; set; }

        /// <summary>
        /// Gets or sets the names of the attributes to be supplied from
        /// the cached tokens. All the other attributes of cached tokens
        /// are ignored.
        /// </summary>
        public string[] SuppliedAttributes { get; set; }
    }
}
