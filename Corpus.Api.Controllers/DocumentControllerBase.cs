using Corpus.Api.Models;
using Corpus.Core;
using Fusi.Tools.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Corpus.Api.Controllers;

/// <summary>
/// Base class for a document controller. This provides the essential CRUD
/// operations for documents. Implement your controller by deriving it from
/// this one, and adding the required attributes (including auth when
/// required).
/// </summary>
// [ApiController]
// [Authorize]
public abstract class DocumentControllerBase : ControllerBase
{
    private readonly ICorpusRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentControllerBase"/>
    /// class.
    /// </summary>
    /// <param name="repository">The repository.</param>
    /// <exception cref="ArgumentNullException">repository</exception>
    protected DocumentControllerBase(ICorpusRepository repository)
    {
        _repository = repository ??
            throw new ArgumentNullException(nameof(repository));
    }

    /// <summary>
    /// Check if editing the document with the specified ID is allowed.
    /// The default implementation just returns true, and is called by
    /// any endpoint of this controller involving changes in a document.
    /// </summary>
    /// <param name="id"></param>
    /// <returns>True if changes allowed; else false.</returns>
    protected virtual Task<bool> CanEditDocumentAsync(int id)
        => Task.FromResult(true);

    /// <summary>
    /// Called when a set of documents has been fetched by
    /// <see cref="DoGetDocuments()"/>. The default implementation
    /// does nothing, but you can override it to provide more data to the
    /// documents.
    /// </summary>
    /// <param name="documents">The documents.</param>
    /// <returns>Documents.</returns>
    protected virtual IList<IDocument> OnDocumentsFetched(
        IList<IDocument> documents)
    {
        return documents;
    }

    /// <summary>
    /// Gets a page of matching documents.
    /// </summary>
    /// <param name="model">The documents filter model.</param>
    /// <returns>Page of documents.</returns>
    //[HttpGet("api/documents")]
    //[ProducesResponseType(200)]
    protected ActionResult<DataPage<IDocument>> DoGetDocuments(
        DocumentsFilterBindingModel model)
    {
        DocumentFilter filter = model.ToFilter();

        DataPage<IDocument> page = _repository.GetDocuments(filter);
        if (page.Total == 0) return Ok(page);

        List<IDocument> documents = new(OnDocumentsFetched(page.Items));
        page.Items.Clear();
        foreach (var document in documents) page.Items.Add(document);
        return Ok(page);
    }

    /// <summary>
    /// Called when a single document has been fetched by
    /// <see cref="DoGetDocument(int, bool)"/>. The default implementation
    /// does nothing, but you can override it to provide more data to the
    /// document.
    /// </summary>
    /// <param name="document">The document.</param>
    /// <returns>Document.</returns>
    protected virtual IDocument OnDocumentFetched(IDocument document)
    {
        return document;
    }

    /// <summary>
    /// Gets the specified document.
    /// </summary>
    /// <param name="id">The document's identifier.</param>
    /// <param name="content">True to include document's content.</param>
    /// <returns>document metadata</returns>
    //[HttpGet("api/documents/{id}")]
    //[ProducesResponseType(200)]
    //[ProducesResponseType(404)]
    protected ActionResult<IDocument> DoGetDocument(int id, bool content)
    {
        IDocument? document = _repository.GetDocument(id, content);
        if (document == null) return NotFound();

        return Ok(OnDocumentFetched(document));
    }

    /// <summary>
    /// Eventually override some properties of a document being added or
    /// updated. This can be used for properties like sort key and date value,
    /// which usually are calculated whithin the server.
    /// </summary>
    /// <remarks>The default implementation does nothing.</remarks>
    /// <param name="document">The document.</param>
    /// <param name="model">The model received from the request.</param>
    protected virtual void OverrideDocumentProps(Document document,
        DocumentBindingModel model)
    {
    }

    /// <summary>
    /// Called whenever a document gets added via
    /// <see cref="DoAddDocument(DocumentBindingModel)"/>. The default
    /// implementation does nothing.
    /// </summary>
    /// <param name="document">The document.</param>
    protected virtual void OnDocumentAdded(Document document)
    {
    }

    /// <summary>
    /// Adds or updates the specified document.
    /// </summary>
    /// <param name="model">The model.</param>
    /// <param name="hasContent">True if the binding model has content.
    /// In this case <paramref name="model"/> should derive from
    /// <see cref="DocumentBindingModel"/>, adding a property with its
    /// own requirements for the document's content. You can then
    /// override <see cref="OverrideDocumentProps(Document, DocumentBindingModel)"/>
    /// to inject the content into the newly created document before
    /// storing it.</param>
    //[HttpPost("api/documents")]
    //[ProducesResponseType(200)]
    //[ProducesResponseType(400)]
    protected async Task<IActionResult> DoAddDocumentAsync(
        DocumentBindingModel model,
        bool hasContent = false)
    {
        if (!ModelState.IsValid) return BadRequest(model);
        if (!await CanEditDocumentAsync(model.Id)) return Unauthorized();

        Document document = model.GetDocument(User.Identity!.Name!);

        // eventually override some properties
        OverrideDocumentProps(document, model);

        _repository.AddDocument(document, hasContent, true);
        OnDocumentAdded(document);

        return CreatedAtAction("GetDocument", new
        {
            document.Id
        }, document);
    }

    /// <summary>
    /// Called after the document's content has been set. The default
    /// implementation does nothing.
    /// </summary>
    /// <param name="document"></param>
    protected virtual void OnDocumentContentSet(IDocument document)
    {
    }

    /// <summary>
    /// Sets the content of the document.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="file">The file.</param>
    /// <exception cref="NotImplementedException"></exception>
    // [HttpPost("api/documents/{id}/text")]
    // [ProducesResponseType(200)]
    // [ProducesResponseType(404)]
    protected async Task<IActionResult> DoSetDocumentContentAsync(
        [FromRoute] int id, IFormFile file)
    {
        if (!await CanEditDocumentAsync(id)) return Unauthorized();

        // https://stackoverflow.com/questions/39226697/how-to-upload-file-using-angular-2
        // get the target document
        IDocument? document = _repository.GetDocument(id, false);
        if (document == null) return NotFound($"Document {id} not found");

        using (StreamReader reader = new(
            file.OpenReadStream(), Encoding.UTF8))
        {
            document.Content = reader.ReadToEnd();
            _repository.AddDocument(document, true, false);
        }

        OnDocumentContentSet(document);

        return Ok();
    }

    /// <summary>
    /// Deletes the document with the specified ID.
    /// </summary>
    /// <param name="id">The identifier.</param>
    //[HttpDelete("api/documents/{id}")]
    //[ProducesResponseType(200)]
    protected async Task<IActionResult> DoDeleteDocumentAsync(int id)
    {
        if (!await CanEditDocumentAsync(id)) return Unauthorized();
        _repository.DeleteDocument(id);
        return Ok();
    }
}
