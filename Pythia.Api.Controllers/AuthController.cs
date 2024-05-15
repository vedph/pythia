using Fusi.Api.Auth.Controllers;
using Fusi.Api.Auth.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Pythia.Api.Controllers;

/// <summary>
/// Authentication and authorization.
/// </summary>
[ApiController]
public sealed class AuthController :
    AuthControllerBase<NamedUser, IdentityRole>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthController"/> class.
    /// </summary>
    /// <param name="userManager">The user manager.</param>
    /// <param name="roleManager">The role manager.</param>
    /// <param name="signInManager">The sign in manager.</param>
    /// <param name="configuration">The configuration.</param>
    public AuthController(UserManager<NamedUser> userManager,
        RoleManager<IdentityRole> roleManager,
        SignInManager<NamedUser> signInManager,
        IConfiguration configuration)
        : base(userManager, roleManager, signInManager, configuration)
    {
    }
}
