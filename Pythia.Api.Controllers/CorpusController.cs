using Corpus.Api.Controllers;
using Corpus.Api.Models;
using Corpus.Core;
using Fusi.Tools.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Pythia.Api.Controllers;

/// <summary>
/// Corpora. Anyone can read corpora, while changing them requires an
/// authenticated user: a corpus can be changed only by its owner (the user
/// who created it) or by an admin.
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
    private readonly ICorpusRepository _repository = repository
        ?? throw new ArgumentNullException(nameof(repository));

    /// <summary>
    /// Check if the current user can edit the corpus with the specified ID.
    /// Admins can edit any corpus; other users can edit only their own
    /// corpora, or create new ones.
    /// </summary>
    /// <param name="id">The corpus ID.</param>
    /// <returns>True if changes allowed; else false.</returns>
    protected override Task<bool> CanEditCorpusAsync(string id)
    {
        if (User.Identity?.IsAuthenticated != true)
            return Task.FromResult(false);
        if (User.IsInRole("admin")) return Task.FromResult(true);

        ICorpus? corpus = string.IsNullOrEmpty(id)
            ? null : _repository.GetCorpus(id);

        return Task.FromResult(corpus == null ||
            (corpus.UserId != null && corpus.UserId == User.Identity.Name));
    }

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
    /// Adds or updates a corpus. A new corpus belongs to the current user,
    /// while an updated corpus keeps its owner.
    /// </summary>
    /// <param name="model">The corpus model.</param>
    [HttpPost()]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AddCorpus([FromBody] CorpusBindingModel model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        // keep the owner of an existing corpus (e.g. when edited by an admin)
        string? userId = _repository.GetCorpus(model.Id!)?.UserId;
        return await DoAddCorpusAsync(model, userId);
    }

    /// <summary>
    /// Adds to the specified corpus all the documents matching the
    /// specified filter.
    /// </summary>
    /// <param name="id">The corpus identifier.</param>
    /// <param name="model">The document filter model.</param>
    /// <returns>201</returns>
    [HttpPut("{id}/add")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
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
    [Authorize]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
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
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteCorpus([FromRoute] string id)
    {
        return await DoDeleteCorpusAsync(id);
    }
}
