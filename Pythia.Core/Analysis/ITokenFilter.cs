namespace Pythia.Core.Analysis
{
    /// <summary>
    /// Interface for token filters.
    /// </summary>
    public interface ITokenFilter
    {
        /// <summary>
        /// Apply the filter to the specified token.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <param name="position">The position which will be assigned to
        /// the resulting token, provided that it's not empty. Some filters
        /// may use this value, e.g. to identify tokens like in deferred
        /// POS tagging.</param>
        void Apply(Token token, int position);
    }
}
