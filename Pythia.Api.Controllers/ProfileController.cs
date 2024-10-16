using Corpus.Core;
using Fusi.Tools.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pythia.Api.Models;
using Pythia.Core;
using System;

namespace Pythia.Api.Controllers;

/// <summary>
/// Indexing profiles.
/// </summary>
/// <seealso cref="ControllerBase" />
/// <remarks>
/// Initializes a new instance of the <see cref="ProfileController"/>
/// class.
/// </remarks>
/// <param name="repository">The repository.</param>
/// <exception cref="ArgumentNullException">repository</exception>
[ApiController]
[Route("api/profiles")]
public sealed class ProfileController(IIndexRepository repository) : ControllerBase
{
    private readonly IIndexRepository _repository = repository
        ?? throw new ArgumentNullException(nameof(repository));

    /// <summary>
    /// Gets the specified page of profiles (without their content).
    /// </summary>
    /// <param name="model">The model. If you set the page size to 0,
    /// all the profiles will be retrieved in a unique page.</param>
    /// <returns>Profiles page.</returns>
    [HttpGet()]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DataPage<IProfile>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<DataPage<IProfile>> GetProfiles(
        [FromQuery] ProfileFilterBindingModel model)
    {
        return Ok(_repository.GetProfiles(model.ToFilter()));
    }

    /// <summary>
    /// Gets the profile with the specified ID.
    /// </summary>
    /// <param name="id">The profile identifier.</param>
    /// <returns>The profile.</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<IProfile> GetProfile([FromRoute] string id)
    {
        IProfile? profile = _repository.GetProfile(id);
        if (profile == null) return NotFound();
        return Ok(profile);
    }
}
