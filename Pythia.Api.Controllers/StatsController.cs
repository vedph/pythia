using Microsoft.AspNetCore.Mvc;
using Pythia.Core;
using System;
using System.Collections.Generic;

namespace Pythia.Api.Controllers;

/// <summary>
/// Index statistics.
/// </summary>
/// <seealso cref="ControllerBase" />
[ApiController]
public class StatsController : ControllerBase
{
    private readonly IIndexRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="StatsController"/> class.
    /// </summary>
    /// <param name="repository">The repository.</param>
    public StatsController(IIndexRepository repository)
    {
        _repository = repository
            ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <summary>
    /// Gets statistics about the index.
    /// </summary>
    /// <returns>Dictionary with name=value pairs.</returns>
    [HttpGet("api/stats")]
    [ProducesResponseType(200)]
    public ActionResult<IDictionary<string,double>> GetStatistics()
    {
        return Ok(_repository.GetStatistics());
    }
}
