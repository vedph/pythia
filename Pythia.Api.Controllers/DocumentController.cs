using Corpus.Api.Controllers;
using Corpus.Api.Models;
using Corpus.Core;
using Fusi.Tools.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
[ApiController]
[Route("api/documents")]
public sealed class DocumentController(ICorpusRepository repository) :
    DocumentControllerBase(repository)
{
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
    /// <returns>Document metadata.</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IDocument))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<IDocument> GetDocument([FromRoute] int id,
        [FromQuery] bool content)
    {
        return DoGetDocument(id, content);
    }

    /// <summary>
    /// Adds or updates the specified document.
    /// </summary>
    /// <param name="model">The model.</param>
    [HttpPost()]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddDocument(
        [FromBody] DocumentBindingModel model,
        [FromQuery] bool content)
    {
        return await DoAddDocumentAsync(model, content);
    }

    /// <summary>
    /// Deletes the document with the specified ID.
    /// </summary>
    /// <param name="id">The identifier.</param>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task DeleteDocument([FromRoute] int id)
    {
        await DoDeleteDocumentAsync(id);
    }
}
