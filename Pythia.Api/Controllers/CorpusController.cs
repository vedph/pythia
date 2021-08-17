﻿using Corpus.Api.Controllers;
using Corpus.Api.Models;
using Corpus.Core;
using Fusi.Tools.Data;
using Microsoft.AspNetCore.Mvc;

namespace Pythia.Api.Controllers
{
    /// <summary>
    /// Corpora.
    /// </summary>
    /// <seealso cref="CorpusControllerBase" />
    [ApiController]
    public sealed class CorpusController : CorpusControllerBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CorpusController"/> class.
        /// </summary>
        /// <param name="repository">The repository.</param>
        public CorpusController(ICorpusRepository repository)
            : base(repository)
        {
        }

        /// <summary>
        /// Gets the specified page of corpora.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns>Page.</returns>
        [HttpGet("api/corpora")]
        [ProducesResponseType(200)]
        public ActionResult<DataPage<Corpus.Core.Corpus>> GetCorpora(
            [FromQuery] CorpusFilterModel model)
        {
            return DoGetCorpora(model);
        }

        /// <summary>
        /// Gets the corpus with the specified ID.
        /// </summary>
        /// <param name="id">The corpus ID.</param>
        /// <param name="noDocumentIds">If set to <c>true</c>, do not get the
        /// documents IDs for each corpus.</param>
        /// <returns>Corpus or 404.</returns>
        [HttpGet("api/corpora/{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public ActionResult<Corpus.Core.Corpus> GetCorpus([FromRoute] string id,
            [FromQuery] bool noDocumentIds)
        {
            return DoGetCorpus(id, noDocumentIds);
        }

        /// <summary>
        /// Adds or updates a corpus.
        /// </summary>
        /// <param name="model">The corpus model.</param>
        [HttpPost("api/corpora")]
        [ProducesResponseType(201)]
        [ProducesResponseType(400)]
        public ActionResult AddCorpus([FromBody] CorpusBindingModel model)
        {
            return AddCorpus(model);
        }

        /// <summary>
        /// Adds to the specified corpus all the documents matching the
        /// specified filter.
        /// </summary>
        /// <param name="id">The corpus identifier.</param>
        /// <param name="model">The document filter model.</param>
        /// <returns>201</returns>
        [HttpPut("api/corpora/{id}/add")]
        [ProducesResponseType(201)]
        [ProducesResponseType(400)]
        public ActionResult AddDocumentsByFilter([FromRoute] string id,
            [FromBody] DocumentFilterModel model)
        {
            return DoAddDocumentsByFilter(id, model);
        }

        /// <summary>
        /// Removes from the specified corpus all the documents matching the
        /// specified filter.
        /// </summary>
        /// <param name="id">The corpus identifier.</param>
        /// <param name="model">The document filter model.</param>
        /// <returns>201</returns>
        [HttpPut("api/corpora/{id}/del")]
        [ProducesResponseType(201)]
        [ProducesResponseType(400)]
        public IActionResult RemoveDocumentsByFilter([FromRoute] string id,
            [FromBody] DocumentFilterModel model)
        {
            return DoRemoveDocumentsByFilter(id, model);
        }

        /// <summary>
        /// Deletes the specified corpus.
        /// </summary>
        /// <param name="id">The corpus identifier.</param>
        [HttpDelete("api/corpora/{id}")]
        public void DeleteCorpus([FromRoute] string id)
        {
            DoDeleteCorpus(id);
        }
    }
}
