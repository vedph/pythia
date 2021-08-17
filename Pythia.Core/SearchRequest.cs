using Fusi.Tools.Data;

namespace Pythia.Core
{
    /// <summary>
    /// A Pythia query request.
    /// </summary>
    public class SearchRequest : PagingOptions
    {
        /// <summary>
        /// The query.
        /// </summary>
        public string Query { get; set; }
    }
}
