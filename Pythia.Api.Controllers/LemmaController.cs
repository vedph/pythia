using Fusi.Tools.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pythia.Api.Models;
using Pythia.Core;
using System;
using System.Collections.Generic;

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
    /// <param name="filter">The lemmata filter model.</param>
    /// <returns>page</returns>
    [HttpGet()]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DataPage<Lemma>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<DataPage<Lemma>> Get([FromQuery]
        LemmaFilterBindingModel filter)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        return _repository.GetLemmata(filter.ToFilter());
    }

    /// <summary>
    /// Gets the lemmata counts for the specified attribute name(s).
    /// </summary>
    /// <param name="id">The lemma identifier.</param>
    /// <param name="attributes">The attribute names.</param>
    /// <returns>Lemma counts for each attribute name and value, in a dictionary
    /// where key=attribute name and value=counts, sorted in descending order.
    /// </returns>
    [HttpGet("{id}/counts")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Dictionary<string, IList<TokenCount>> GetTokenCounts(
        [FromRoute] int id,
        [FromQuery] IList<string> attributes)
    {
        Dictionary<string, IList<TokenCount>> counts = [];

        foreach (string attrName in attributes)
        {
            IList<TokenCount> attrCounts = _repository.GetTokenCounts(
                true, id, attrName);
            if (attrCounts.Count > 0) counts[attrName] = attrCounts;
        }
        return counts;
    }
}
