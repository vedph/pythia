using Corpus.Core.Analysis;
using Pythia.Core.Config;
using System;
using System.Collections.Generic;
using Corpus.Core;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using Corpus.Core.Reading;
using Fusi.Tools;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Pythia.Core.Analysis
{
    /// <summary>
    /// Index builder.
    /// </summary>
    public sealed class IndexBuilder
    {
        private readonly PythiaFactory _factory;
        private readonly IIndexRepository _repository;
        private ITextFilter[]? _filters;
        private IAttributeParser[]? _attributeParsers;
        private IDocSortKeyBuilder? _docSortKeyBuilder;
        private IDocDateValueCalculator? _docDateValueCalculator;
        private ITokenizer? _tokenizer;
        private IStructureParser[]? _structureParsers;
        private ITextRetriever? _textRetriever;

        /// <summary>
        /// Gets or sets a value indicating whether this builder is working
        /// in dry mode, where no data is written to the repository.
        /// </summary>
        public bool IsDryMode { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the builder should store
        /// each document's content in the index.
        /// </summary>
        public bool IsContentStored { get; set; }

        /// <summary>
        /// Gets or sets the optional logger to use.
        /// </summary>
        public ILogger? Logger { get; set; }

        /// <summary>
        /// Gets or sets the index contents to be built by this builder.
        /// </summary>
        public IndexContents? Contents { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexBuilder" /> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="repository">The repository.</param>
        /// <exception cref="ArgumentNullException">factory or repository</exception>
        public IndexBuilder(PythiaFactory factory, IIndexRepository repository)
        {
            _factory = factory
                ?? throw new ArgumentNullException(nameof(factory));
            _repository = repository
                ?? throw new ArgumentNullException(nameof(repository));

            Contents = IndexContents.All;
        }

        private void CreateComponents()
        {
            // text filters
            Logger?.LogInformation("Getting text filters...");
            _filters = _factory.GetTextFilters().ToArray();

            // attribute parsers
            Logger?.LogInformation("Getting attribute parser...");
            _attributeParsers = _factory.GetAttributeParsers().ToArray();

            // doc sort key builder
            Logger?.LogInformation("Getting sort key builder...");
            _docSortKeyBuilder = _factory.GetDocSortKeyBuilder();

            // doc date value calculator
            Logger?.LogInformation("Getting date value calculator...");
            _docDateValueCalculator = _factory.GetDocDateValueCalculator();

            // tokenizer
            Logger?.LogInformation("Getting tokenizer...");
            _tokenizer = _factory.GetTokenizer();

            // structure parsers
            Logger?.LogInformation("Getting structure parsers...");
            _structureParsers = _factory.GetStructureParsers().ToArray();

            // text retriever
            Logger?.LogInformation("Getting text retriever...");
            _textRetriever = _factory.GetTextRetriever();
        }

        /// <summary>
        /// Adds to the repository the tokens from the specified document.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="document">The document.</param>
        /// <param name="repository">The repository.</param>
        /// <param name="updating">if set to <c>true</c> the document tokens are
        /// being updated.</param>
        /// <param name="context">The optional context.</param>
        private void AddTokens(string text, IDocument document,
            IIndexRepository repository, bool updating, IHasDataDictionary? context)
        {
            Logger?.LogInformation("Tokenizing {DocumentId}: {DocumentTitle}",
                document.Id, document.Title);
            if (updating && !IsDryMode)
                repository.DeleteDocumentTokens(document.Id);

            using TextReader reader = new StringReader(text);
            try
            {
                _tokenizer!.Start(reader, document.Id, context);

                List<Token> tokens = new();
                while (_tokenizer.Next())
                {
                    // ignore empty tokens
                    if (string.IsNullOrEmpty(_tokenizer.CurrentToken.Value)) continue;

                    tokens.Add(_tokenizer.CurrentToken.Clone());
                    if (tokens.Count >= 100)
                    {
                        if (!IsDryMode) repository.AddTokens(tokens);
                        tokens.Clear();
                    }
                }
                if (tokens.Count > 0 && !IsDryMode) repository.AddTokens(tokens);
                Logger?.LogInformation("Tokenization complete");
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error adding tokens from document #{DocumentId}",
                    document.Id);
                if (!IsDryMode) repository.DeleteDocumentTokens(document.Id);
                throw;
            }
        }

        private void ParseMetadata(string text, IDocument document)
        {
            if (_attributeParsers?.Length > 0)
            {
                Logger?.LogInformation("Parsing document metadata");

                foreach (IAttributeParser parser in _attributeParsers)
                {
                    foreach (Corpus.Core.Attribute attribute in parser.Parse(
                        new StringReader(text), document))
                    {
                        switch (attribute.Name)
                        {
                            case "author":
                                document.Author = attribute.Value;
                                break;
                            case "title":
                                document.Title = attribute.Value;
                                break;
                            default:
                                document.Attributes!.Add(attribute);
                                break;
                        }
                    }
                }
                Logger?.LogInformation("Parsing completed");
            }

            // title and author are required
            document.Author ??= "-";
            document.Title ??= $"#{document.Id:00000}";

            // calculated metadata
            Logger?.LogInformation("Calculating document metadata");
            document.DateValue = _docDateValueCalculator!.Calculate(
                document.Attributes!);
            document.SortKey = _docSortKeyBuilder!.Build(document);
        }

        private void AddStructures(string text, IDocument document,
            IIndexRepository repository, bool updating, IHasDataDictionary? context)
        {
            Logger?.LogInformation("Detecting structures");

            if (updating && !IsDryMode)
                repository.DeleteDocumentStructures(document.Id);

            if (_structureParsers == null || _structureParsers.Length == 0)
                return;

            foreach (IStructureParser parser in _structureParsers)
            {
                Logger?.LogInformation("Structure parser: {ParserName}",
                    parser.GetType().Name);
                parser.Parse(document,
                    new StringReader(text),
                    new CharIndexCalculator(new StringReader(text)),
                        IsDryMode ? null : repository,
                    context,
                    new Progress<ProgressReport>(r =>
                        Logger?.LogInformation("Structures: {Count}", r.Count)));
            }

            Logger?.LogInformation("Structure detection complete");
        }

        private async Task IndexDocument(string source, string profileId,
            IIndexRepository repository)
        {
            // document: retrieve an existing one or just create a new one.
            // Document's metadata are cleared before adding/updating.
            bool updating = false;
            IDocument? document = repository.GetDocumentBySource(source, false);
            if (document == null)
            {
                document = new Document
                {
                    Source = source,
                    ProfileId = profileId
                };
            }
            else
            {
                document.Attributes!.Clear();
                updating = true;
            }

            // parse and add document's metadata
            string? text = await _textRetriever!.GetAsync(document);
            if (text == null) return;
            document.Content = text;

            // get the text
            StringReader reader = new(text);

            // extract metadata from it (unfiltered)
            ParseMetadata(text, document);
            Logger?.LogInformation((updating ? "Updating" : "Adding") + " document");
            if (!IsDryMode)
                repository.AddDocument(document, IsContentStored, true);
            Logger?.LogInformation((updating ? "Updated" : "Added") +
                $" document #{document.Id}");

            // create a data context for filters
            DataDictionary context = new();

            // get a filtered version of the original text
            Logger?.LogInformation("Applying text filters");

            foreach (ITextFilter filter in _filters!)
            {
                reader = (StringReader)await filter.ApplyAsync(reader, context);
            }
            string filteredText = reader.ReadToEnd();

            // analyze tokens from filtered text (only if requested)
            if ((Contents & IndexContents.Tokens) != 0)
                AddTokens(filteredText, document, repository, updating, context);

            // analyze structures from unfiltered text (only if requested)
            if ((Contents & IndexContents.Structures) != 0)
            {
                // ensure that the length of the filtered text did not change
                if (text.Length != filteredText.Length)
                {
                    throw new ArgumentException(
                        LocalizedStrings.Format(
                            Properties.Resources.TextLengthMismatch,
                            filteredText.Length,
                            text.Length));
                }

                AddStructures(text, document, repository, updating, context);
            }
        }

        private async Task<string> GetFilteredText(string text)
        {
            StringReader reader = new(text);

            // get a filtered version of the original text
            foreach (ITextFilter filter in _filters!)
            {
                reader = (StringReader)await filter.ApplyAsync(reader);
            }
            return reader.ReadToEnd();
        }

        /// <summary>
        /// Caches the tokens or a subset of their data in the specified cache.
        /// The cache can serve for diagnostic purposes, or be used by 3rd-party
        /// tools like e.g. POS taggers in deferred POS tagging.
        /// </summary>
        /// <param name="profileId">The profile identifier.</param>
        /// <param name="source">The documents source.</param>
        /// <param name="cache">The tokens cache.</param>
        /// <param name="cancel">The cancel.</param>
        /// <param name="progress">The progress.</param>
        /// <exception cref="ArgumentNullException">cache</exception>
        public async Task CacheTokensAsync(string profileId, string source,
            ITokenCache cache, CancellationToken cancel,
            IProgress<ProgressReport> progress)
        {
            if (cache == null)
                throw new ArgumentNullException(nameof(cache));

            CreateComponents();

            // get the source collector to get text sources
            ISourceCollector? collector = _factory.GetSourceCollector();
            if (collector == null) return;

            // process each text from source
            int docCount = 0;
            ProgressReport? report = progress != null ? new ProgressReport() : null;

            foreach (string src in collector.Collect(source))
            {
                docCount++;

                if (progress != null)
                {
                    report!.Count = docCount;
                    report.Message = src;
                    progress.Report(report);
                }
                Logger?.LogInformation(src);

                // document: retrieve an existing one or just create new.
                // Document's metadata are cleared before adding/updating.
                IDocument? document = _repository.GetDocumentBySource(src, false);
                if (document == null)
                {
                    document = new Document
                    {
                        Source = src,
                        ProfileId = profileId
                    };
                }
                else
                {
                    document.Attributes!.Clear();
                }

                // parse and add document's metadata
                string? text = await _textRetriever!.GetAsync(document);
                if (text == null) continue;

                // extract metadata from it (unfiltered)
                ParseMetadata(text, document);

                // store document (with content if required)
                if (IsContentStored) document.Content = text;
                _repository.AddDocument(document, IsContentStored, true);

                // tokenize the filtered text
                string filteredText = await GetFilteredText(text);
                using (TextReader reader = new StringReader(filteredText))
                {
                    _tokenizer!.Start(reader, document.Id);

                    List<Token> tokens = new();
                    while (_tokenizer.Next())
                    {
                        // ignore empty tokens
                        if (string.IsNullOrEmpty(_tokenizer.CurrentToken.Value))
                            continue;

                        _tokenizer.CurrentToken.DocumentId = document.Id;
                        tokens.Add(_tokenizer.CurrentToken.Clone());
                        if (tokens.Count >= 100)
                        {
                            cache.AddTokens(tokens[0].DocumentId, tokens,
                                filteredText);
                            tokens.Clear();
                        }
                    }
                    if (tokens.Count > 0)
                    {
                        cache.AddTokens(tokens[0].DocumentId, tokens,
                            filteredText);
                    }
                }
                if (cancel.IsCancellationRequested) break;
            } // for
        }

        /// <summary>
        /// Builds the index.
        /// </summary>
        /// <param name="profileId">The profile identifier.</param>
        /// <param name="source">The documents source.</param>
        /// <param name="cancel">The cancellation token.</param>
        /// <param name="progress">The optional progress reporter.</param>
        /// <exception cref="ArgumentNullException">profileId or profile or
        /// source or name</exception>
        /// <exception cref="ApplicationException">Indexing issue.</exception>
        public async Task Build(string profileId, string source,
            CancellationToken cancel, IProgress<ProgressReport> progress)
        {
            if (profileId == null) throw new ArgumentNullException(nameof(profileId));
            if (source == null) throw new ArgumentNullException(nameof(source));

            CreateComponents();

            // get the source collector to get text sources
            ISourceCollector? collector = _factory.GetSourceCollector();
            if (collector == null) return;

            // process each document from source
            int docCount = 0;
            ProgressReport? report = progress != null? new ProgressReport() : null;

            foreach (string src in collector.Collect(source))
            {
                docCount++;

                if (progress != null)
                {
                    report!.Count = docCount;
                    report.Message = src;
                    progress.Report(report);
                }

                Logger?.LogInformation(src);
                await IndexDocument(src, profileId, _repository);
                if (cancel.IsCancellationRequested) break;
            }
        }
    }

    /// <summary>
    /// Index contents handled by <see cref="IndexBuilder"/>.
    /// </summary>
    [Flags]
    public enum IndexContents
    {
        /// <summary>
        /// No content.
        /// </summary>
        None = 0,

        /// <summary>
        /// Tokens.
        /// </summary>
        Tokens = 0x01,

        /// <summary>
        /// Structures.
        /// </summary>
        Structures = 0x02,

        /// <summary>
        /// All, i.e. both tokens and structures.
        /// </summary>
        All = Tokens | Structures
    }
}
