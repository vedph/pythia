using Fusi.Api.Auth.Controllers;
using Fusi.Api.Auth.Models;
using Fusi.Api.Auth.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Pythia.Api.Controllers;

/// <summary>
/// Users browsing and updates.
/// </summary>
[ApiController]
public sealed class UserController :
    UserControllerBase<NamedUser, NamedUserBindingModel>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserController"/> class.
    /// </summary>
    /// <param name="repository">The repository.</param>
    /// <param name="logger">The logger.</param>
    public UserController(IUserRepository<NamedUser> repository,
        ILogger<UserController> logger)
        : base(repository, logger)
    {
    }
}
