using Fusi.Api.Auth.Controllers;
using Fusi.Api.Auth.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Pythia.Api.Controllers;

/// <summary>
/// Authentication and authorization.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AuthController"/> class.
/// </remarks>
/// <param name="userManager">The user manager.</param>
/// <param name="roleManager">The role manager.</param>
/// <param name="signInManager">The sign in manager.</param>
/// <param name="configuration">The configuration.</param>
[ApiController]
public sealed class AuthController(UserManager<NamedUser> userManager,
    RoleManager<IdentityRole> roleManager,
    SignInManager<NamedUser> signInManager,
    IConfiguration configuration) :
    AuthControllerBase<NamedUser, IdentityRole>(
        userManager, roleManager, signInManager, configuration)
{
    protected override async Task<IList<Claim>> GetUserClaimsAsync(NamedUser user)
    {
        IList<Claim> claims = await base.GetUserClaimsAsync(user);

        // claims from additional user properties
        // http://docs.oasis-open.org/imi/identity/v1.0/os/identity-1.0-spec-os.html#_Toc229451870

        if (!string.IsNullOrEmpty(user.FirstName))
        {
            claims.Add(new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname",
                user.FirstName));
        }
        if (!string.IsNullOrEmpty(user.LastName))
        {
            claims.Add(new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname",
                user.LastName));
        }

        return claims;
    }
}
