﻿using Fusi.Tools.Data;
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
    /// <param name="model">The words filter model.</param>
    /// <returns>page</returns>
    [HttpGet()]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public DataPage<Word> Get([FromQuery] WordFilterBindingModel model)
    {
        return _repository.GetWords(model.ToFilter());
    }

    /// <summary>
    /// Gets the list of unique document attribute names.
    /// </summary>
    /// <param name="privileged">if set to <c>true</c> include privileged
    /// document attribute names in the list.</param>
    /// <returns>List of names.</returns>
    [HttpGet("doc-attr-names")]
    [ProducesResponseType(200)]
    public IList<string> GetDocAttributeNames(
        [FromQuery] bool privileged)
    {
        return _repository.GetDocAttributeNames(privileged);
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
