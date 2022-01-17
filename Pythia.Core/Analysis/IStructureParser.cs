using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Corpus.Core;
using Fusi.Tools;

namespace Pythia.Core.Analysis
{
    /// <summary>
    /// Parser for document's structures. Note that any structure parser
    /// requires the tokenization of the corresponding document.
    /// </summary>
    public interface IStructureParser
    {
        /// <summary>
        /// Gets the optional filters to be applied to the structure's values.
        /// </summary>
        IList<IStructureValueFilter> Filters { get; }

        /// <summary>
        /// Parses the structures in the specified document content.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="reader">The document's text reader.</param>
        /// <param name="calculator">The document's char index calculator.</param>
        /// <param name="repository">The repository.</param>
        /// <param name="progress">The optional progress reporter.</param>
        /// <param name="cancel">The optional cancellation token.</param>
        /// <exception cref="ArgumentNullException">null reader or
        /// calculator</exception>
        void Parse(IDocument document, TextReader reader,
            CharIndexCalculator calculator,
            IIndexRepository repository,
            IProgress<ProgressReport> progress = null,
            CancellationToken? cancel = null);
    }
}