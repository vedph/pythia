using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Corpus.Core;
using Fusi.Tools;

namespace Pythia.Core.Analysis
{
    /// <summary>
    /// Base class for structure parsers.
    /// </summary>
    /// <seealso cref="IStructureParser" />
    public abstract class StructureParserBase : IStructureParser
    {
        private readonly Dictionary<string, string> _docFilters;

        /// <summary>
        /// Gets the optional filters to be applied to the structure's values.
        /// </summary>
        public IList<IStructureValueFilter> Filters { get; }

        /// <summary>
        /// Gets the character index calculator.
        /// </summary>
        protected CharIndexCalculator IndexCalculator { get; private set; }

        /// <summary>
        /// Gets the repository.
        /// </summary>
        protected IIndexRepository Repository { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StructureParserBase"/>
        /// class.
        /// </summary>
        protected StructureParserBase()
        {
            _docFilters = new Dictionary<string, string>();
            Filters = new List<IStructureValueFilter>();
        }

        /// <summary>
        /// Sets the document filters from the specified array of
        /// <c>name=value</c> pairs.
        /// </summary>
        /// <param name="filters">The filters pairs array.</param>
        protected void SetDocumentFilters(string[] filters)
        {
            _docFilters.Clear();
            if (filters == null || filters.Length == 0) return;

            Regex r = new("^([^=]+)=(.*)$");
            foreach (string filter in filters)
            {
                Match m = r.Match(filter);
                if (m.Success) _docFilters[m.Groups[1].Value] = m.Groups[2].Value;
            }
        }

        private bool IsApplicable(IDocument document)
        {
            if (_docFilters.Count == 0) return true;

            if (document.Attributes == null || document.Attributes.Count == 0)
                return false;

            foreach (var p in _docFilters)
            {
                if (document.Attributes.Any(a =>
                    string.Compare(a.Name, p.Key, true) == 0
                    && string.Compare(a.Value, p.Key, true) == 0))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Does the parsing.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="reader">The document's text reader.</param>
        /// <param name="progress">The optional progress reporter.</param>
        /// <param name="cancel">The optional cancellation token.</param>
        protected abstract void DoParse(IDocument document, TextReader reader,
            IProgress<ProgressReport> progress = null,
            CancellationToken? cancel = null);

        /// <summary>
        /// Parses the structures in the specified document content.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="reader">The document's text reader.</param>
        /// <param name="calculator">The document's char index calculator.</param>
        /// <param name="repository">The repository.</param>
        /// <param name="progress">The optional progress reporter.</param>
        /// <param name="cancel">The optional cancellation token.</param>
        public void Parse(IDocument document, TextReader reader,
            CharIndexCalculator calculator, IIndexRepository repository,
            IProgress<ProgressReport> progress = null,
            CancellationToken? cancel = null)
        {
            if (!IsApplicable(document)) return;

            try
            {
                IndexCalculator = calculator;
                Repository = repository;
                DoParse(document, reader, progress, cancel);
            }
            finally
            {
                IndexCalculator = null;
                Repository = null;
            }
        }
    }

    /// <summary>
    /// Base options for structure parsers. Whenever you need a structure
    /// parser with document filtering, you can just derive your structure
    /// parser options from this class, and your structure parser from
    /// <see cref="StructureParserBase"/>.
    /// </summary>
    public class StructureParserOptions
    {
        /// <summary>
        /// Gets or sets the document filters. Each string in this array is
        /// a <c>name=value</c> pair, representing a document's attribute name and
        /// value to be matched. Any of these pairs should be matched for
        /// the parser to be applied. If not specified, the parser will be
        /// applied to any document.
        /// </summary>
        public string[] DocumentFilters { get; set; }
    }
}
