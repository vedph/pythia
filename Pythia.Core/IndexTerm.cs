namespace Pythia.Core
{
    /// <summary>
    /// A term got from browsing the tokens index.
    /// </summary>
    public class IndexTerm
    {
        /// <summary>
        /// Gets or sets the token identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets the term value.
        /// </summary>
        public string? Value { get; set; }

        /// <summary>
        /// Gets the frequencies total count.
        /// </summary>
        public int Count { get; set; }

        /// <summary>Returns a string that represents the current object.</summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return $"#{Id} {Value}={Count}";
        }
    }
}
