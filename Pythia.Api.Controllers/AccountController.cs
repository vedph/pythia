using Fusi.Api.Auth.Controllers;
using MessagingApi;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pythia.Api.Models;

namespace Pythia.Api.Controllers
{
    /// <summary>
    /// User accounts.
    /// </summary>
    [ApiController]
    public sealed class AccountController :
        AccountControllerBase<ApplicationUser, NamedRegisterBindingModel,
            NamedUserModel>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AccountController"/> class.
        /// </summary>
        /// <param name="userManager">The user manager.</param>
        /// <param name="messageBuilder">The message builder.</param>
        /// <param name="mailer">The mailer.</param>
        /// <param name="options">The options.</param>
        /// <param name="logger">The logger.</param>
        public AccountController(UserManager<ApplicationUser> userManager,
            IMessageBuilderService messageBuilder,
            IMailerService mailer,
            IOptions<MessagingOptions> options,
            ILogger<AccountControllerBase<ApplicationUser,
                NamedRegisterBindingModel, NamedUserModel>> logger)
            : base(userManager, messageBuilder, mailer, options, logger,
                  new ApplicationRegUserMapper())
        {
        }
    }
}
