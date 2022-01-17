using Corpus.Core;
using Fusi.Tools.Data;
using Microsoft.AspNetCore.Mvc;
using Pythia.Api.Models;
using Pythia.Core;
using System;

namespace Pythia.Api.Controllers
{
    /// <summary>
    /// Indexing profiles.
    /// </summary>
    /// <seealso cref="ControllerBase" />
    [ApiController]
    public sealed class ProfileController : ControllerBase
    {
        private readonly IIndexRepository _repository;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProfileController"/>
        /// class.
        /// </summary>
        /// <param name="repository">The repository.</param>
        /// <exception cref="ArgumentNullException">repository</exception>
        public ProfileController(IIndexRepository repository)
        {
            _repository = repository
                ?? throw new ArgumentNullException(nameof(repository));
        }

        /// <summary>
        /// Gets the specified page of profiles (without their content).
        /// </summary>
        /// <param name="model">The model. If you set the page size to 0,
        /// all the profiles will be retrieved in a unique page.</param>
        /// <returns>Profiles page.</returns>
        [HttpGet("api/profiles")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
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
        [HttpGet("api/profiles/{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public ActionResult<IProfile> GetProfile([FromRoute] string id)
        {
            IProfile profile = _repository.GetProfile(id);
            if (profile == null) return NotFound();
            return Ok(profile);
        }
    }
}
