using Fusi.Tools.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pythia.Api.Models;
using Pythia.Core;
using System;
using System.Collections.Generic;

namespace Pythia.Api.Controllers;

/// <summary>
/// Words index.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="WordController"/> class.
/// </remarks>
/// <param name="repository">The repository.</param>
/// <exception cref="ArgumentNullException">repository</exception>
[ApiController]
[Route("api/words")]
public class WordController(IIndexRepository repository) : ControllerBase
{
    private readonly IIndexRepository _repository = repository
        ?? throw new ArgumentNullException(nameof(repository));

    /// <summary>
    /// Gets a page of words.
    /// </summary>
    /// <param name="filter">The words filter model.</param>
    /// <returns>page</returns>
    [HttpGet()]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DataPage<Word>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<DataPage<Word>> Get(
        [FromQuery] WordFilterBindingModel filter)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        return _repository.GetWords(filter.ToFilter());
    }

    /// <summary>
    /// Gets information about document attribute types.
    /// </summary>
    /// <param name="privileged">if set to <c>true</c> include privileged
    /// document attribute names in the list.</param>
    /// <returns>List of names and types.</returns>
    [HttpGet("doc-attr-info")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IList<AttributeInfo> GetDocAttributeInfo(
        [FromQuery] bool privileged)
    {
        return _repository.GetDocAttributeInfo(privileged);
    }

    /// <summary>
    /// Gets the words counts for the specified attribute name(s).
    /// </summary>
    /// <param name="id">The word identifier.</param>
    /// <param name="attributes">The attribute names.</param>
    /// <returns>Word counts for each attribute name and value, in a dictionary
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
                false, id, attrName);
            if (attrCounts.Count > 0) counts[attrName] = attrCounts;
        }
        return counts;
    }
}
