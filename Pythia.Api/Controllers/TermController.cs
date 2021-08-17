using Fusi.Tools.Data;
using Microsoft.AspNetCore.Mvc;
using Pythia.Api.Models;
using Pythia.Core;
using System;

namespace Pythia.Api.Controllers
{
    /// <summary>
    /// Index terms.
    /// </summary>
    [ApiController]
    public class TermController : ControllerBase
    {
        private readonly IIndexRepository _repository;

        /// <summary>
        /// Initializes a new instance of the <see cref="TermController"/> class.
        /// </summary>
        /// <param name="repository">The repository.</param>
        /// <exception cref="ArgumentNullException">repository</exception>
        public TermController(IIndexRepository repository)
        {
            _repository = repository
                ?? throw new ArgumentNullException(nameof(repository));
        }

        /// <summary>
        /// Gets a page of index terms.
        /// </summary>
        /// <param name="model">The terms filter model.</param>
        /// <returns>page</returns>
        [HttpGet("api/terms")]
        public IActionResult Get([FromQuery] TermFilterModel model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            DataPage<IndexTerm> page = _repository.GetTerms(model.ToFilter());
            return Ok(page);
        }
    }
}
