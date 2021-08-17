using Corpus.Core;
using Corpus.Core.Reading;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Pythia.Api.Models;
using Pythia.Api.Services;
using Pythia.Core;
using Pythia.Core.Config;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Pythia.Api.Controllers
{
    /// <summary>
    /// Document text reader.
    /// </summary>
    /// <seealso cref="Controller" />
    [ApiController]
    public class ReaderController : ControllerBase
    {
        private readonly IMemoryCache _cache;
        private readonly PythiaFactoryProvider _factoryProvider;
        private readonly IIndexRepository _repository;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReaderController"/> class.
        /// </summary>
        /// <param name="cache">The cache.</param>
        /// <param name="factoryProvider">The factory provider.</param>
        /// <param name="repository">The repository.</param>
        /// <exception cref="ArgumentNullException">repository</exception>
        public ReaderController(IMemoryCache cache,
            PythiaFactoryProvider factoryProvider,
            IIndexRepository repository)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _factoryProvider = factoryProvider
                ?? throw new ArgumentNullException(nameof(factoryProvider));
            _repository = repository
                ?? throw new ArgumentNullException(nameof(repository));
        }

        /// <summary>
        /// Gets the contents map of the specified document.
        /// </summary>
        /// <param name="id">The document's identifier.</param>
        /// <returns>the root node of the map</returns>
        [HttpGet("api/documents/{id}/map", Name = "GetDocumentMap")]
        public async Task<IActionResult> GetDocumentMap([FromRoute] int id)
        {
            // get the document
            Document document = _repository.GetDocument(id, false);
            if (document == null) return NotFound();

            // load the profile
            Profile profile = _repository.GetProfile(document.ProfileId);
            if (profile == null)
                return NotFound($"Profile {document.ProfileId} not found");

            // get the factory
            PythiaFactory factory = _factoryProvider.GetFactory(profile.Content);

            TextMapNode root;
            if (_cache.TryGetValue($"map-{document.Id}", out var o))
            {
                root = (TextMapNode)o;
            }
            else
            {
                // get the text retriever for that profile
                ITextRetriever retriever = factory.GetTextRetriever();
                if (retriever == null)
                {
                    return NotFound($"Retriever for profile {document.ProfileId} " +
                       "not found");
                }

                // get the text mapper for that profile
                ITextMapper mapper = factory.GetTextMapper();
                if (mapper == null)
                {
                    return NotFound($"Mapper for profile {document.ProfileId} " +
                       "not found");
                }

                // retrieve the text and map it
                string text = await retriever.GetAsync(document);
                root = mapper.Map(text, document.Attributes
                    .ToImmutableDictionary(a => a.Name, a => a.Value));

                _cache.Set($"map-{document.Id}", root);
            }

            TextMapNodeModel result = new TextMapNodeModel(root);
            return Ok(result);
        }

        /// <summary>
        /// Gets the text of the specified document.
        /// </summary>
        /// <param name="id">The document's identifier.</param>
        /// <returns>plain text</returns>
        [HttpGet("api/documents/{id}/text", Name = "GetDocumentText")]
        public async Task<IActionResult> GetDocumentText([FromRoute] int id)
        {
            // get the document
            Document document = _repository.GetDocument(id, false);
            if (document == null) return NotFound();

            // load the profile
            Profile profile = _repository.GetProfile(document.ProfileId);
            if (profile == null)
                return NotFound($"Profile {document.ProfileId} not found");

            // get the factory
            PythiaFactory factory = _factoryProvider.GetFactory(profile.Content);

            // get the text retriever
            ITextRetriever retriever = factory.GetTextRetriever();
            if (retriever == null)
            {
                return NotFound($"Retriever for profile {document.ProfileId} " +
                   "not found");
            }

            // retrieve the text
            string text = await retriever.GetAsync(document);
            return new FileStreamResult(
                new MemoryStream(Encoding.UTF8.GetBytes(text)), "text/plain");
        }

        /// <summary>
        /// Gets the specified document piece from its path.
        /// </summary>
        /// <param name="id">The document identifier.</param>
        /// <param name="path">The path (dots are represented by dashes).</param>
        /// <param name="parts">The output document's parts to be rendered:
        /// header=1, body=2, footer=4; any combination of these bit-values is
        /// allowed.</param>
        /// <returns>model with property <c>text</c> = rendered piece of text
        /// </returns>
        [HttpGet("api/documents/{id}/path/{path}",
            Name = "GetDocumentPieceFromPath")]
        public async Task<IActionResult> GetDocumentPieceFromPath(
            [FromRoute] int id,
            [FromRoute] string path, [FromQuery] int parts)
        {
            path = path.Replace('-', '.');

            // get the document
            Document document = _repository.GetDocument(id, false);
            if (document == null) return NotFound();

            // load the profile
            Profile profile = _repository.GetProfile(document.ProfileId);
            if (profile == null)
                return NotFound($"Profile {document.ProfileId} not found");

            // get the document's profile
            PythiaFactory factory = _factoryProvider.GetFactory(profile.Content);

            // get the text retriever for that profile
            ITextRetriever retriever = factory.GetTextRetriever();
            if (retriever == null)
            {
                return NotFound($"Retriever for profile {document.ProfileId} " +
                   "not found");
            }

            // get the text mapper for that profile
            ITextMapper mapper = factory.GetTextMapper();
            if (mapper == null)
            {
                return NotFound($"Mapper for profile {document.ProfileId} " +
                   "not found");
            }

            // get the text picker for that profile
            ITextPicker picker = factory.GetTextPicker();
            if (picker == null)
            {
                return NotFound($"Picker for profile {document.ProfileId} " +
                   "not found");
            }

            // read the requested piece
            DocumentReader reader =
                new DocumentReader(retriever, mapper, picker, _cache);
            TextPiece piece =
                await reader.ReadAsync(document, path, (RenderingParts)parts);
            if (piece == null)
                return NotFound($"Document {id} text at {path} not found");

            // render it
            ITextRenderer renderer = factory.GetTextRenderer();
            if (renderer != null)
            {
                piece.Text = renderer.Render(document, piece.Text,
                   (RenderingParts)parts);
            }

            return Ok(new { text = piece.Text });
        }

        // GET api/documents/{id}/range/{start}/{end}?parts=N

        /// <summary>
        /// Gets the specified document piece from a range of locations.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="id">The document identifier.</param>
        /// <param name="start">The range start.</param>
        /// <param name="end">The range end (exclusive).</param>
        /// <param name="parts">The output document's parts to be rendered:
        /// header=1, body=2, footer=4; any combination of these bit-values is
        /// allowed.</param>
        /// <returns>model with property <c>text</c> = rendered piece of text
        /// </returns>
        [HttpGet("api/documents/{id}/range/{start}/{end}",
            Name = "GetDocumentPieceFromRange")]
        public async Task<IActionResult> GetDocumentPieceFromRange(
            [FromRoute] string database, [FromRoute] int id,
            [FromRoute] int start, [FromRoute] int end, [FromQuery] int parts)
        {
            // get the document
            Document document = _repository.GetDocument(id, false);
            if (document == null) return NotFound();

            // load the profile
            Profile profile = _repository.GetProfile(document.ProfileId);
            if (profile == null)
                return NotFound($"Profile {document.ProfileId} not found");

            // get the factory
            PythiaFactory factory = _factoryProvider.GetFactory(profile.Content);

            // get the text retriever for that profile
            ITextRetriever retriever = factory.GetTextRetriever();
            if (retriever == null)
            {
                return NotFound($"Retriever for profile {document.ProfileId} " +
                   "not found");
            }

            // get the text mapper for that profile
            ITextMapper mapper = factory.GetTextMapper();
            if (mapper == null)
            {
                return NotFound($"Mapper for profile {document.ProfileId} " +
                   "not found");
            }

            // get the text picker for that profile
            ITextPicker picker = factory.GetTextPicker();
            if (picker == null)
            {
                return NotFound($"Picker for profile {document.ProfileId} " +
                   "not found");
            }

            // read the requested piece
            DocumentReader reader =
                new DocumentReader(retriever, mapper, picker, _cache);
            TextPiece piece = await reader.ReadAsync(document, start, end,
                (RenderingParts)parts);
            if (piece == null)
                return NotFound($"Document {id} text at {start}-{end} not found");

            // render it
            ITextRenderer renderer = factory.GetTextRenderer();
            if (renderer != null)
            {
                piece.Text = renderer.Render(document, piece.Text,
                   (RenderingParts)parts);
            }

            return Ok(new { text = piece.Text });
        }
    }
}
