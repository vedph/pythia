using Fusi.Tools.Data;

namespace Pythia.Core
{
    /// <summary>
    /// Search text filter.
    /// </summary>
    public class SearchFilter : PagingOptions
    {
        /// <summary>
        /// Gets or sets the search query.
        /// </summary>
        public string Query { get; set; }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return $"{PageNumber}×{PageSize}: {Query}";
        }
    }
}
