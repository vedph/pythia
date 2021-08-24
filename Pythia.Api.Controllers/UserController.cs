using Fusi.Api.Auth.Controllers;
using Fusi.Api.Auth.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Pythia.Api.Models;

namespace Pythia.Api.Controllers
{
    /// <summary>
    /// Users browsing and updates.
    /// </summary>
    [ApiController]
    public sealed class UserController :
        UserControllerBase<ApplicationUser, NamedUserBindingModel, NamedUserModel>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserController"/> class.
        /// </summary>
        /// <param name="repository">The repository.</param>
        /// <param name="logger">The logger.</param>
        public UserController(IUserRepository<ApplicationUser> repository,
            ILogger<UserControllerBase<ApplicationUser, NamedUserBindingModel,
                NamedUserModel>> logger)
            : base(repository, logger, new ApplicationUserMapper())
        {
        }
    }
}
