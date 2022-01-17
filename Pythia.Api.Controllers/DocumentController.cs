using Corpus.Api.Controllers;
using Corpus.Api.Models;
using Corpus.Core;
using Fusi.Tools.Data;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Pythia.Api.Controllers
{
    /// <summary>
    /// Documents.
    /// </summary>
    /// <seealso cref="DocumentControllerBase" />
    [ApiController]
    public sealed class DocumentController : DocumentControllerBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentController"/>
        /// class.
        /// </summary>
        /// <param name="repository">The repository.</param>
        public DocumentController(ICorpusRepository repository)
            : base(repository)
        {
        }

        /// <summary>
        /// Gets a page of matching documents.
        /// </summary>
        /// <param name="model">The documents filter model.</param>
        /// <returns>Page of documents.</returns>
        [HttpGet("api/documents")]
        [ProducesResponseType(200)]
        public ActionResult<DataPage<IDocument>> GetDocuments(
            [FromQuery] DocumentsFilterBindingModel model)
        {
            return DoGetDocuments(model);
        }

        /// <summary>
        /// Gets the specified document.
        /// </summary>
        /// <param name="id">The document's identifier.</param>
        /// <returns>document metadata</returns>
        [HttpGet("api/documents/{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public ActionResult<IDocument> GetDocument([FromRoute] int id,
            [FromQuery] bool content)
        {
            return DoGetDocument(id, content);
        }

        /// <summary>
        /// Adds or updates the specified document.
        /// </summary>
        /// <param name="model">The model.</param>
        [HttpPost("api/documents")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
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
        [HttpDelete("api/documents/{id}")]
        [ProducesResponseType(200)]
        public async Task DeleteDocument([FromRoute] int id)
        {
            await DoDeleteDocumentAsync(id);
        }
    }
}
