using Corpus.Api.Models;
using Corpus.Core;
using Fusi.Tools.Data;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Corpus.Api.Controllers;

/// <summary>
/// Base class for profile controllers.
/// </summary>
/// <seealso cref="ControllerBase" />
public abstract class ProfileControllerBase : ControllerBase
{
    private readonly ICorpusRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileControllerBase"/>
    /// class.
    /// </summary>
    /// <param name="repository">The repository.</param>
    /// <exception cref="ArgumentNullException">repository</exception>
    protected ProfileControllerBase(ICorpusRepository repository)
    {
        _repository = repository ??
            throw new ArgumentNullException(nameof(repository));
    }

    /// <summary>
    /// Check if editing the profile with the specified ID is allowed.
    /// The default implementation just returns true, and is called by
    /// any endpoint of this controller involving changes in a profile.
    /// </summary>
    /// <param name="id"></param>
    /// <returns>True if changes allowed; else false.</returns>
    protected virtual Task<bool> CanEditProfileAsync(string id)
        => Task.FromResult(true);

    /// <summary>
    /// Get the profile with the specified ID.
    /// You should set your controller's method HttpGetAttribute's name
    /// to <c>GetProfileById</c>.
    /// </summary>
    /// <param name="id">The profile ID.</param>
    /// <returns>The profile.</returns>
    //[HttpGet("api/profiles/{id}", Name = "GetProfileById")]
    //[ProducesResponseType(200)]
    //[ProducesResponseType(404)]
    protected ActionResult<IProfile> DoGetProfile(string id,
        bool noContent = false)
    {
        IProfile? profile = _repository.GetProfile(id, noContent);
        if (profile == null) return NotFound();
        return Ok(profile);
    }

    /// <summary>
    /// Gets the requested page of profiles. Set the page size to 0
    /// to return all the matching profiles at once.
    /// </summary>
    /// <param name="filter">The profiles filter.</param>
    /// <param name="noContent">True to avoid retrieving the profile's
    /// content.</param>
    /// <returns>Page.</returns>
    //[HttpGet("api/profiles")]
    //[ProducesResponseType(200)]
    //[ProducesResponseType(400)]
    protected ActionResult<DataPage<IProfile>> DoGetProfiles(
        ProfileFilterBindingModel filter, bool noContent = false)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        DataPage<IProfile> page = _repository.GetProfiles(
            filter.ToProfileFilter(), noContent);
        return Ok(page);
    }

    /// <summary>
    /// Adds or updates the specified profile.
    /// </summary>
    /// <param name="model">The model.</param>
    /// <param name="userId">The user ID to assign to the corpus, or null
    /// to use the currently logged in user.</param>
    //[HttpPost("api/profiles")]
    //[ProducesResponseType(200)]
    //[ProducesResponseType(400)]
    protected async Task<IActionResult> DoAddProfileAsync(
        ProfileBindingModel model, string? userId = null)
    {
        if (!ModelState.IsValid) return BadRequest(model);
        if (!await CanEditProfileAsync(model.Id!)) return Unauthorized();

        Profile profile = new()
        {
            Id = model.Id,
            Type = model.Type,
            Content = model.Content,
            UserId = userId ?? User.Identity!.Name
        };
        _repository.AddProfile(profile);
        return CreatedAtRoute("GetProfileById", new
        {
            profile.Id
        }, profile);
    }

    /// <summary>
    /// Deletes the profile with the specified ID.
    /// </summary>
    /// <param name="id">The profile identifier.</param>
    //[HttpDelete("api/profiles/{id}")]
    //[ProducesResponseType(200)]
    protected async Task<IActionResult> DoDeleteProfileAsync(string id)
    {
        if (!await CanEditProfileAsync(id)) return Unauthorized();
        _repository.DeleteProfile(id);
        return Ok();
    }
}
