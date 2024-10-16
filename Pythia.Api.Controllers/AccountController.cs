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
