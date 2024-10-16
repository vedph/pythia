using Corpus.Api.Controllers;
using Corpus.Api.Models;
using Corpus.Core;
using Fusi.Tools.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Pythia.Api.Controllers;

/// <summary>
/// Corpora.
/// </summary>
/// <seealso cref="CorpusControllerBase" />
/// <remarks>
/// Initializes a new instance of the <see cref="CorpusController"/> class.
/// </remarks>
/// <param name="repository">The repository.</param>
[ApiController]
[Route("api/corpora")]
public sealed class CorpusController(ICorpusRepository repository)
    : CorpusControllerBase(repository)
{
    /// <summary>
    /// Gets the specified page of corpora.
    /// </summary>
    /// <param name="model">The model.</param>
    /// <returns>Page.</returns>
    [HttpGet()]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DataPage<ICorpus>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<DataPage<ICorpus>> GetCorpora(
        [FromQuery] CorpusFilterBindingModel model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        return DoGetCorpora(model);
    }

    /// <summary>
    /// Gets the corpus with the specified ID.
    /// </summary>
    /// <param name="id">The corpus ID.</param>
    /// <param name="noDocumentIds">If set to <c>true</c>, do not get the
    /// documents IDs for each corpus.</param>
    /// <returns>Corpus or 404.</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<ICorpus> GetCorpus([FromRoute] string id,
        [FromQuery] bool noDocumentIds)
    {
        return DoGetCorpus(id, noDocumentIds);
    }

    /// <summary>
    /// Adds or updates a corpus.
    /// </summary>
    /// <param name="model">The corpus model.</param>
    [HttpPost()]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddCorpus([FromBody] CorpusBindingModel model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        return await DoAddCorpusAsync(model);
    }

    /// <summary>
    /// Adds to the specified corpus all the documents matching the
    /// specified filter.
    /// </summary>
    /// <param name="id">The corpus identifier.</param>
    /// <param name="model">The document filter model.</param>
    /// <returns>201</returns>
    [HttpPut("{id}/add")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task <IActionResult> AddDocumentsByFilter(
        [FromRoute] string id,
        [FromBody] DocumentFilterBindingModel model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        return await DoAddDocumentsByFilterAsync(id, model);
    }

    /// <summary>
    /// Removes from the specified corpus all the documents matching the
    /// specified filter.
    /// </summary>
    /// <param name="id">The corpus identifier.</param>
    /// <param name="model">The document filter model.</param>
    /// <returns>201</returns>
    [HttpPut("{id}/del")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RemoveDocumentsByFilter(
        [FromRoute] string id,
        [FromBody] DocumentFilterBindingModel model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        return await DoRemoveDocumentsByFilterAsync(id, model);
    }

    /// <summary>
    /// Deletes the specified corpus.
    /// </summary>
    /// <param name="id">The corpus identifier.</param>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task DeleteCorpus([FromRoute] string id)
    {
        await DoDeleteCorpusAsync(id);
    }
}
