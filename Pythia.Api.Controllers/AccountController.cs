using Fusi.Api.Auth.Controllers;
using Fusi.Api.Auth.Models;
using MessagingApi;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Pythia.Api.Controllers;

/// <summary>
/// User accounts.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AccountController"/> class.
/// </remarks>
/// <param name="userManager">The user manager.</param>
/// <param name="messageBuilder">The message builder.</param>
/// <param name="mailer">The mailer.</param>
/// <param name="options">The options.</param>
/// <param name="logger">The logger.</param>
[ApiController]
public sealed class AccountController(UserManager<NamedUser> userManager,
    IMessageBuilderService messageBuilder,
    IMailerService mailer,
    IOptions<MessagingOptions> options,
    ILogger<AccountController> logger) :
    AccountControllerBase<NamedUser, NamedRegisterBindingModel>(
        userManager, messageBuilder, mailer, options, logger)
{
    /// <summary>
    /// Creates a new instance of the NamedUser class using the information
    /// provided in the registration binding model.
    /// </summary>
    /// <remarks>This method is typically used during user registration to
    /// convert the binding model into a user entity suitable for persistence 
    /// or further processing.</remarks>
    /// <param name="model">The registration binding model containing user 
    /// details such as email address, user name, first name, and last
    /// name.</param>
    /// <returns>A NamedUser object populated with the corresponding values
    /// from the specified binding model.</returns>
    protected override NamedUser GetUserFromBindingModel(
        NamedRegisterBindingModel model)
    {
        return new NamedUser
        {
            Email = model.Email,
            UserName = model.Name,
            FirstName = model.FirstName,
            LastName = model.LastName,
        };
    }
}
