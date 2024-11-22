using Corpus.Api.Models;
using Corpus.Core;
using Fusi.Tools.Data;
using Microsoft.AspNetCore.Mvc;
using System;

namespace Corpus.Api.Controllers;

public abstract class AttributeControllerBase : ControllerBase
{
    private readonly ICorpusRepository _repository;

    /// <summary>
    /// Create a new instance of the <see cref="AttributeControllerBase"/>
    /// class.
    /// </summary>
    /// <param name="repository">The corpus repository.</param>
    /// <exception cref="ArgumentNullException">repository</exception>
    protected AttributeControllerBase(ICorpusRepository repository)
    {
        _repository = repository ??
            throw new ArgumentNullException(nameof(repository));
    }

    /// <summary>
    /// Gets a page of/all attributes defined in the specified database.
    /// </summary>
    /// <param name="model">The attributes filter model.</param>
    /// <returns>page</returns>
    // [HttpGet("api/attributes")]
    // [ProducesResponseType(200)]
    // [ProducesResponseType(400)]
    protected IActionResult DoGetNames(
        [FromQuery] AttributeFilterBindingModel model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        DataPage<string> page = _repository.GetAttributeNames(
            model.ToFilter());

        return Ok(page);
    }
}
