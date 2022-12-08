using Fusi.Tools;
using System.Collections.Generic;
using System.IO;

namespace Pythia.Core.Analysis
{
    /// <summary>
    /// Tokenizer interface.
    /// </summary>
    /// <remarks>A tokenizer chunks an input text into tokens, roughly
    /// corresponding to our generic notion of "words": it takes as input a
    /// text, and processes one token at a time, until its end (but you can
    /// force it to restart with a new text using <see cref="Start"/>).
    /// Instead of spitting out a new <see cref="Token"/> object at each step,
    /// it reuses its <see cref="CurrentToken"/> property. This is a common
    /// pattern found in text indexers like Lucene (since its version 3) to
    /// avoid filling the RAM with short-lived token objects. If you want to
    /// keep several tokens as they are spit out by a tokenizer,
    /// clone the <see cref="CurrentToken"/> after each advance.</remarks>
    public interface ITokenizer
    {
        /// <summary>
        /// Gets the token filters used by this tokenizer.
        /// </summary>
        IList<ITokenFilter> Filters { get; }

        /// <summary>
        /// Gets the current token.
        /// </summary>
        Token CurrentToken { get; }

        /// <summary>
        /// Start the tokenizer for the specified input text.
        /// </summary>
        /// <param name="reader">The reader to read the next token from.</param>
        /// <param name="documentId">The ID of the document to be tokenized.</param>
        /// <param name="context">The optional context.</param>
        void Start(TextReader reader, int documentId,
            IHasDataDictionary? context = null);

        /// <summary>
        /// Advance to the next available token if any.
        /// </summary>
        /// <returns>false if end of input reached</returns>
        bool Next();
    }
}
