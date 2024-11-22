using Corpus.Api.Models;
using Corpus.Core;
using Fusi.Tools.Data;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Corpus.Api.Controllers;

/// <summary>
/// Base class for a corpus controller. This provides the essential CRUD
/// operations for corpora. Implement your controller by deriving it from
/// this one, and adding the required attributes (including auth when
/// required).
/// </summary>
/// <seealso cref="ControllerBase" />
// [ApiController]
// [Authorize]
public abstract class CorpusControllerBase : ControllerBase
{
    private readonly ICorpusRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="CorpusControllerBase"/>
    /// class.
    /// </summary>
    /// <param name="repository">The repository.</param>
    /// <exception cref="ArgumentNullException">repository</exception>
    protected CorpusControllerBase(ICorpusRepository repository)
    {
        _repository = repository ??
            throw new ArgumentNullException(nameof(repository));
    }

    /// <summary>
    /// Check if editing the corpus with the specified ID is allowed.
    /// The default implementation just returns true, and is called by
    /// any endpoint of this controller involving changes in a corpus.
    /// </summary>
    /// <param name="id"></param>
    /// <returns>True if changes allowed; else false.</returns>
    protected virtual Task<bool> CanEditCorpusAsync(string id)
        => Task.FromResult(true);

    /// <summary>
    /// Gets the specified page of corpora.
    /// </summary>
    /// <param name="model">The model.</param>
    /// <returns>Page.</returns>
    protected ActionResult<DataPage<ICorpus>> DoGetCorpora(
        CorpusFilterBindingModel model)
    {
        DataPage<ICorpus> page =
            _repository.GetCorpora(model.ToCorpusFilter(), model.Counts);
        return Ok(page);
    }

    /// <summary>
    /// Gets the corpus with the specified ID.
    /// </summary>
    /// <param name="id">The corpus ID.</param>
    /// <param name="noDocumentIds">If set to <c>true</c>, do not get the
    /// documents IDs for each corpus.</param>
    /// <returns>Corpus or 404.</returns>
    protected ActionResult<ICorpus> DoGetCorpus(string id,
        bool noDocumentIds)
    {
        ICorpus? corpus = _repository.GetCorpus(id);
        if (corpus == null) return NotFound();

        CorpusModel result = new(corpus);
        if (noDocumentIds) result.DocumentIds = null;
        return Ok(new CorpusModel(corpus));
    }

    /// <summary>
    /// Adds or updates a corpus.
    /// </summary>
    /// <param name="model">The corpus model.</param>
    /// <param name="userId">The user ID to assign to the corpus, or null
    /// to use the currently logged in user.</param>
    protected async Task<IActionResult> DoAddCorpusAsync(
        CorpusBindingModel model, string? userId = null)
    {
        if (!await CanEditCorpusAsync(model.Id!)) return Unauthorized();

        Core.Corpus corpus = new()
        {
            Id = model.Id,
            Title = model.Title,
            Description = model.Description ?? "",
            UserId = userId ?? User.Identity?.Name,
        };
        _repository.AddCorpus(corpus, model.SourceId);
        return CreatedAtAction("GetCorpus", new
        {
            model.Id
        }, corpus);
    }

    /// <summary>
    /// Adds to the specified corpus all the documents matching the
    /// specified filter.
    /// </summary>
    /// <param name="id">The corpus identifier.</param>
    /// <param name="model">The document filter model.</param>
    /// <returns>201</returns>
    protected async Task<ActionResult> DoAddDocumentsByFilterAsync(
        string id,
        DocumentFilterBindingModel model)
    {
        if (!await CanEditCorpusAsync(id)) return Unauthorized();

        DocumentFilter filter = model.ToFilter();
        if (filter.IsEmpty())
        {
            ModelState.AddModelError("", "No documents filtering condition set");
            return BadRequest(ModelState);
        }

        _repository.ChangeCorpusByFilter(id, null, filter, true);

        return CreatedAtAction("GetCorpus", new { id }, null);
    }

    /// <summary>
    /// Adds the specified documents to the specified corpus.
    /// </summary>
    /// <param name="id">The corpus ID.</param>
    /// <param name="documentIds">The documents IDs.</param>
    protected async Task<ActionResult> DoAddDocumentsAsync(
        string id, int[] documentIds)
    {
        if (string.IsNullOrEmpty(id)) return BadRequest(ModelState);
        if (!await CanEditCorpusAsync(id)) return Unauthorized();

        _repository.AddDocumentsToCorpus(id, null, documentIds);

        return CreatedAtAction("GetCorpus", new { id }, null);
    }

    /// <summary>
    /// Removes from the specified corpus all the documents matching the
    /// specified filter.
    /// </summary>
    /// <param name="id">The corpus identifier.</param>
    /// <param name="model">The document filter model.</param>
    /// <returns>201</returns>
    protected async Task<IActionResult> DoRemoveDocumentsByFilterAsync(
        string id, DocumentFilterBindingModel model)
    {
        if (string.IsNullOrEmpty(id)) return BadRequest(ModelState);

        if (!await CanEditCorpusAsync(id)) return Unauthorized();

        DocumentFilter filter = model.ToFilter();
        if (filter.IsEmpty())
        {
            ModelState.AddModelError("",
                "No documents filtering condition set");
            return BadRequest();
        }

        _repository.ChangeCorpusByFilter(id, null, filter, false);

        return CreatedAtAction("GetCorpus", new { id }, null);
    }

    /// <summary>
    /// Deletes the specified corpus.
    /// </summary>
    /// <param name="id">The corpus identifier.</param>
    protected async Task<IActionResult> DoDeleteCorpusAsync(string id)
    {
        if (!await CanEditCorpusAsync(id)) return Unauthorized();
        _repository.DeleteCorpus(id);
        return Ok();
    }
}
