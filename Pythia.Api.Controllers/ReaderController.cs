﻿using Corpus.Core;
using Corpus.Core.Reading;
using Microsoft.AspNetCore.Http;
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

namespace Pythia.Api.Controllers;

/// <summary>
/// A generic text response returning a string.
/// </summary>
public class TextResponse
{
    public string? Text { get; set; }
}

/// <summary>
/// Document text reader.
/// </summary>
/// <seealso cref="Controller" />
/// <remarks>
/// Initializes a new instance of the <see cref="ReaderController"/> class.
/// </remarks>
/// <param name="cache">The cache.</param>
/// <param name="factoryProvider">The factory provider.</param>
/// <param name="repository">The repository.</param>
/// <exception cref="ArgumentNullException">repository</exception>
[ApiController]
[Route("api/documents")]
public class ReaderController(IMemoryCache cache,
    IPythiaFactoryProvider factoryProvider,
    IIndexRepository repository) : ControllerBase
{
    private readonly IMemoryCache _cache = cache
        ?? throw new ArgumentNullException(nameof(cache));
    private readonly IPythiaFactoryProvider _factoryProvider = factoryProvider
        ?? throw new ArgumentNullException(nameof(factoryProvider));
    private readonly IIndexRepository _repository = repository
        ?? throw new ArgumentNullException(nameof(repository));

    /// <summary>
    /// Gets the contents map of the specified document.
    /// </summary>
    /// <param name="id">The document's identifier.</param>
    /// <returns>the root node of the map</returns>
    [HttpGet("{id}/map", Name = "GetDocumentMap")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TextMapNodeModel))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TextMapNodeModel>>
        GetDocumentMap([FromRoute] int id)
    {
        // get the document
        IDocument? document = _repository.GetDocument(id, false);
        if (document == null) return NotFound();

        // load the profile
        IProfile? profile = _repository.GetProfile(document.ProfileId!);
        if (profile == null)
            return NotFound($"Profile {document.ProfileId} not found");

        // get the factory
        PythiaFactory factory = _factoryProvider.GetFactory(profile.Content!);

        TextMapNode root;
        if (_cache.TryGetValue($"map-{document.Id}", out var o))
        {
            root = (TextMapNode)o!;
        }
        else
        {
            // get the text retriever for that profile
            ITextRetriever? retriever = factory.GetTextRetriever();
            if (retriever == null)
            {
                return NotFound($"Retriever for profile {document.ProfileId} " +
                   "not found");
            }

            // get the text mapper for that profile
            ITextMapper? mapper = factory.GetTextMapper();
            if (mapper == null)
            {
                return NotFound($"Mapper for profile {document.ProfileId} " +
                   "not found");
            }

            // retrieve the text and map it
            string text = (await retriever.GetAsync(document))!;
            root = mapper.Map(text, document.Attributes!
                .ToImmutableDictionary(a => a.Name!, a => a.Value ?? ""))!;

            _cache.Set($"map-{document.Id}", root);
        }

        TextMapNodeModel result = new(root);
        return Ok(result);
    }

    /// <summary>
    /// Gets the text of the specified document.
    /// </summary>
    /// <param name="id">The document's identifier.</param>
    /// <returns>plain text</returns>
    [HttpGet("{id}/text", Name = "GetDocumentText")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileStreamResult))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("text/plain")]
    public async Task<IActionResult> GetDocumentText([FromRoute] int id)
    {
        // get the document
        IDocument? document = _repository.GetDocument(id, false);
        if (document == null) return NotFound();

        // load the profile
        IProfile? profile = _repository.GetProfile(document.ProfileId!);
        if (profile == null)
            return NotFound($"Profile {document.ProfileId} not found");

        // get the factory
        PythiaFactory factory = _factoryProvider.GetFactory(profile.Content!);

        // get the text retriever
        ITextRetriever? retriever = factory.GetTextRetriever();
        if (retriever == null)
        {
            return NotFound($"Retriever for profile {document.ProfileId} " +
               "not found");
        }

        // retrieve the text
        string text = (await retriever.GetAsync(document))!;
        return new FileStreamResult(
            new MemoryStream(Encoding.UTF8.GetBytes(text)), "text/plain");
    }

    /// <summary>
    /// Gets the specified document piece from its path.
    /// </summary>
    /// <param name="id">The document identifier.</param>
    /// <param name="path">The path (dots are represented by dashes).</param>
    /// <returns>model with property <c>text</c> = rendered piece of text
    /// </returns>
    [HttpGet("{id}/path/{path}", Name = "GetDocumentPieceFromPath")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TextResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TextResponse>> GetDocumentPieceFromPath(
        [FromRoute] int id, [FromRoute] string path)
    {
        path = path.Replace('-', '.');

        // get the document
        IDocument? document = _repository.GetDocument(id, false);
        if (document == null) return NotFound();

        // load the profile
        IProfile? profile = _repository.GetProfile(document.ProfileId!);
        if (profile == null)
            return NotFound($"Profile {document.ProfileId} not found");

        // get the document's profile
        PythiaFactory factory = _factoryProvider.GetFactory(profile.Content!);

        // get the text retriever for that profile
        ITextRetriever? retriever = factory.GetTextRetriever();
        if (retriever == null)
        {
            return NotFound($"Retriever for profile {document.ProfileId} " +
               "not found");
        }

        // get the text mapper for that profile
        ITextMapper? mapper = factory.GetTextMapper();
        if (mapper == null)
        {
            return NotFound($"Mapper for profile {document.ProfileId} " +
               "not found");
        }

        // get the text picker for that profile
        ITextPicker? picker = factory.GetTextPicker();
        if (picker == null)
        {
            return NotFound($"Picker for profile {document.ProfileId} " +
               "not found");
        }

        // read the requested piece
        DocumentReader reader = new(retriever, mapper, picker);
        TextPiece? piece = await reader.ReadAsync(document, path);
        if (piece == null)
            return NotFound($"Document {id} text at {path} not found");

        // render it
        ITextRenderer? renderer = factory.GetTextRenderer();
        if (renderer != null)
        {
            piece.Text = renderer.Render(document, piece.Text);
        }

        return Ok(new TextResponse { Text = piece.Text });
    }

    /// <summary>
    /// Gets the specified document piece from a range of locations.
    /// </summary>
    /// <param name="id">The document identifier.</param>
    /// <param name="start">The range start.</param>
    /// <param name="end">The range end (exclusive).</param>
    /// <returns>model with property <c>text</c> = rendered piece of text
    /// </returns>
    [HttpGet("{id}/range/{start}/{end}", Name = "GetDocumentPieceFromRange")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TextResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TextResponse>> GetDocumentPieceFromRange(
        [FromRoute] int id, [FromRoute] int start, [FromRoute] int end)
    {
        // get the document
        IDocument? document = _repository.GetDocument(id, false);
        if (document == null) return NotFound();

        // load the profile
        IProfile? profile = _repository.GetProfile(document.ProfileId!);
        if (profile == null)
            return NotFound($"Profile {document.ProfileId} not found");

        // get the factory
        PythiaFactory factory = _factoryProvider.GetFactory(profile.Content!);

        // get the text retriever for that profile
        ITextRetriever? retriever = factory.GetTextRetriever();
        if (retriever == null)
        {
            return NotFound($"Retriever for profile {document.ProfileId} " +
               "not found");
        }

        // get the text mapper for that profile
        ITextMapper? mapper = factory.GetTextMapper();
        if (mapper == null)
        {
            return NotFound($"Mapper for profile {document.ProfileId} " +
               "not found");
        }

        // get the text picker for that profile
        ITextPicker? picker = factory.GetTextPicker();
        if (picker == null)
        {
            return NotFound($"Picker for profile {document.ProfileId} " +
               "not found");
        }

        // read the requested piece
        DocumentReader reader = new(retriever, mapper, picker);
        TextPiece? piece = await reader.ReadAsync(document, start, end);
        if (piece == null)
            return NotFound($"Document {id} text at {start}-{end} not found");

        // render it
        ITextRenderer? renderer = factory.GetTextRenderer();
        if (renderer != null)
        {
            piece.Text = renderer.Render(document, piece.Text);
        }

        return Ok(new TextResponse { Text = piece.Text });
    }
}
