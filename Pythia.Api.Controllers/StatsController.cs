using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pythia.Core;
using System;
using System.Collections.Generic;

namespace Pythia.Api.Controllers;

/// <summary>
/// Index statistics.
/// </summary>
/// <seealso cref="ControllerBase" />
/// <remarks>
/// Initializes a new instance of the <see cref="StatsController"/> class.
/// </remarks>
/// <param name="repository">The repository.</param>
[ApiController]
[Route("api/stats")]
public class StatsController(IIndexRepository repository) : ControllerBase
{
    private readonly IIndexRepository _repository = repository
        ?? throw new ArgumentNullException(nameof(repository));

    /// <summary>
    /// Gets statistics about the index.
    /// </summary>
    /// <returns>Dictionary with name=value pairs.</returns>
    [HttpGet()]
    [ProducesResponseType(StatusCodes.Status200OK,
        Type = typeof(IDictionary<string,double>))]
    public ActionResult<IDictionary<string,double>> GetStatistics()
    {
        return Ok(_repository.GetStatistics());
    }
}
