using Corpus.Api.Controllers;
using Corpus.Api.Models;
using Corpus.Core;
using Fusi.Tools.Data;
using Microsoft.AspNetCore.Mvc;

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
        public ActionResult<DataPage<Document>> GetDocuments(
            [FromQuery] DocumentsFilterModel model)
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
        public ActionResult<Document> GetDocument([FromRoute] int id)
        {
            return DoGetDocument(id);
        }

        /// <summary>
        /// Adds or updates the specified document.
        /// </summary>
        /// <param name="model">The model.</param>
        [HttpPost("api/documents")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public IActionResult AddDocument([FromBody] DocumentBindingModel model)
        {
            return DoAddDocument(model);
        }

        /// <summary>
        /// Deletes the document with the specified ID.
        /// </summary>
        /// <param name="id">The identifier.</param>
        [HttpDelete("api/documents/{id}")]
        [ProducesResponseType(200)]
        public void DeleteDocument([FromRoute] int id)
        {
            DoDeleteDocument(id);
        }
    }
}
