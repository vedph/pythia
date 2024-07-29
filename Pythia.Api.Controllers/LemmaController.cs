using Fusi.Tools.Data;
using Microsoft.AspNetCore.Mvc;
using Pythia.Api.Models;
using Pythia.Core;
using System;

namespace Pythia.Api.Controllers;

/// <summary>
/// Lemmata index.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="LemmaController"/> class.
/// </remarks>
/// <param name="repository">The repository.</param>
/// <exception cref="ArgumentNullException">repository</exception>
[ApiController]
[Route("api/lemmata")]
public class LemmaController(IIndexRepository repository) : ControllerBase
{
    private readonly IIndexRepository _repository = repository
        ?? throw new ArgumentNullException(nameof(repository));

    /// <summary>
    /// Gets a page of lemmata.
    /// </summary>
    /// <param name="model">The lemmata filter model.</param>
    /// <returns>page</returns>
    [HttpGet()]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public DataPage<Lemma> Get([FromQuery] LemmaFilterBindingModel model)
    {
        return _repository.GetLemmata(model.ToFilter());
    }
    /*
        [HttpGet("api/terms/distributions")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public TermDistributionSet GetTermDistributions(
            [FromQuery] TermDistributionBindingModel model)
        {
            return _repository.GetTermDistributions(new TermDistributionRequest
            {
                TermId = model.TermId,
                Limit = model.Limit,
                Interval = model.Interval,
                DocAttributes = model.DocAttributes,
                OccAttributes = model.OccAttributes,
            });
        }
    */
}
