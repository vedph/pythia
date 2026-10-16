using Corpus.Api.Controllers;
using Corpus.Api.Models;
using Corpus.Core;
using Fusi.Tools.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Pythia.Api.Services;
using System;
using System.Threading.Tasks;

namespace Pythia.Api.Controllers;

/// <summary>
/// Documents.
/// </summary>
/// <seealso cref="DocumentControllerBase" />
/// <remarks>
/// Initializes a new instance of the <see cref="DocumentController"/>
/// class.
/// </remarks>
/// <param name="repository">The repository.</param>
/// <param name="options">The reader options. When full text is disabled,
/// documents are always returned without their content, except to admins.
/// </param>
[ApiController]
[Route("api/documents")]
public sealed class DocumentController(ICorpusRepository repository,
    IOptions<ReaderOptions> options) :
    DocumentControllerBase(repository)
{
    private readonly ReaderOptions _options = options?.Value
        ?? throw new ArgumentNullException(nameof(options));

    /// <summary>
    /// Gets a page of matching documents.
    /// </summary>
    /// <param name="model">The documents filter model.</param>
    /// <returns>Page of documents.</returns>
    [HttpGet()]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DataPage<IDocument>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<DataPage<IDocument>> GetDocuments(
        [FromQuery] DocumentsFilterBindingModel model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        return DoGetDocuments(model);
    }

    /// <summary>
    /// Gets the specified document.
    /// </summary>
    /// <param name="id">The document's identifier.</param>
    /// <param name="content">Whether to include the document content. This
    /// is ignored when reading the full text of documents is disabled
    /// (unless the user is an admin).</param>
    /// <returns>Document metadata.</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IDocument))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<IDocument> GetDocument([FromRoute] int id,
        [FromQuery] bool content)
    {
        if (_options.IsFullTextDisabled && !User.IsInRole("admin"))
            content = false;
        return DoGetDocument(id, content);
    }

    /// <summary>
    /// Adds or updates the specified document. Admins only.
    /// </summary>
    /// <param name="model">The model.</param>
    /// <param name="content">Whether the document content is included in the
    /// input model.</param>
    [HttpPost()]
    [Authorize(Roles = "admin")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AddDocument(
        [FromBody] DocumentBindingModel model,
        [FromQuery] bool content)
    {
        return await DoAddDocumentAsync(model, content);
    }

    /// <summary>
    /// Deletes the document with the specified ID. Admins only.
    /// </summary>
    /// <param name="id">The identifier.</param>
    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteDocument([FromRoute] int id)
    {
        return await DoDeleteDocumentAsync(id);
    }
}
