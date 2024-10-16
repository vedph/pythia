using Fusi.Tools.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pythia.Api.Models;
using Pythia.Core;
using System;

namespace Pythia.Api.Controllers;

/// <summary>
/// Attributes.
/// </summary>
/// <seealso cref="ControllerBase" />
/// <remarks>
/// Initializes a new instance of the <see cref="AttributeController"/> class.
/// </remarks>
/// <param name="repository">The repository.</param>
[ApiController]
[Route("api/attributes")]
public sealed class AttributeController(IIndexRepository repository) : ControllerBase
{
    private readonly IIndexRepository _repository = repository
            ?? throw new ArgumentNullException(nameof(repository));

    /// <summary>
    /// Gets a page of/all attributes defined in the index.
    /// </summary>
    /// <param name="model">The attributes filter model. To get all the
    /// attributes in a single page, set
    /// <see cref="AttributeFilterBindingModel.PageSize"/> to 0.</param>
    /// <returns>Page of attributes.</returns>
    [HttpGet()]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DataPage<string>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<DataPage<string>> Get
        ([FromQuery] AttributeFilterBindingModel model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        DataPage<string> page = _repository.GetAttributeNames(model.ToFilter());

        return Ok(page);
    }
}
