using System.Collections.Generic;

namespace Pythia.Core.Plugin.Analysis
{
    /// <summary>
    /// POS tagger for tokens.
    /// </summary>
    public interface ITokenPosTagger
    {
        /// <summary>
        /// Tags the specified tokens by adding them attributes with
        /// <paramref name="tagName"/> and value equal to their tag.
        /// </summary>
        /// <param name="tokens">The tokens.</param>
        /// <param name="tagName">The name of the POS tag attribute
        /// to be added to the tokens.</param>
        void Tag(IList<Token> tokens, string tagName);
    }
}
