using Fusi.Tools.Data;
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
    /// <param name="model">The lemmata filter model.</param>
    /// <returns>page</returns>
    [HttpGet()]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public DataPage<Lemma> Get([FromQuery] LemmaFilterBindingModel model)
    {
        return _repository.GetLemmata(model.ToFilter());
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
    [ProducesResponseType(200)]
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
