using System;

namespace Pythia.Core
{
    /// <summary>
    /// A search result with additional data defining the result's context.
    /// </summary>
    /// <seealso cref="T:Pythia.Core.SearchResult" />
    public class KwicSearchResult : SearchResult
    {
        /// <summary>
        /// Gets or sets the left context, including also empty values
        /// when the context length is less than the available surrounding
        /// tokens.
        /// </summary>
        public string[] LeftContext { get; set; }

        /// <summary>
        /// Gets or sets the right context, including also empty values
        /// when the context length is less than the available surrounding
        /// tokens.
        /// </summary>
        public string[] RightContext { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="KwicSearchResult"/>
        /// class.
        /// </summary>
        /// <param name="result">The source result to copy data from.</param>
        /// <exception cref="System.ArgumentNullException">result</exception>
        public KwicSearchResult(SearchResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));

            LeftContext = RightContext = Array.Empty<string>();
            DocumentId = result.DocumentId;
            Position = result.Position;
            Index = result.Index;
            Length = result.Length;
            EntityId = result.EntityId;
            EntityType = result.EntityType;
            Value = result.Value;
            Author = result.Author;
            Title = result.Title;
            SortKey = result.SortKey;
        }
    }
}
