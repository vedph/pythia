using Fusi.Api.Auth.Controllers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Pythia.Api.Models;

namespace Pythia.Api.Controllers;

/// <summary>
/// Authentication and authorization.
/// </summary>
[ApiController]
public sealed class AuthController :
    AuthControllerBase<ApplicationUser, ApplicationRole>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthController"/> class.
    /// </summary>
    /// <param name="userManager">The user manager.</param>
    /// <param name="roleManager">The role manager.</param>
    /// <param name="signInManager">The sign in manager.</param>
    /// <param name="configuration">The configuration.</param>
    public AuthController(UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        SignInManager<ApplicationUser> signInManager,
        IConfiguration configuration)
        : base(userManager, roleManager, signInManager, configuration)
    {
    }
}
