using Fusi.Api.Auth.Controllers;
using Fusi.Api.Auth.Models;
using Fusi.Api.Auth.Services;
using System;
using System.Collections.Generic;

namespace Pythia.Api.Models
{
    public sealed class ApplicationUserMapper
        : IUserMapper<ApplicationUser, NamedUserBindingModel, NamedUserModel>
    {
        public ApplicationUser GetModel(NamedUserBindingModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            return new ApplicationUser
            {
                UserName = model.UserName,
                Email = model.Email,
                EmailConfirmed = model.EmailConfirmed,
                FirstName = model.FirstName,
                LastName = model.LastName,
                LockoutEnabled = model.LockoutEnabled,
                LockoutEnd = model.LockoutEnd
            };
        }

        public NamedUserModel GetView(object user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            UserWithRoles<ApplicationUser>? ur = user as UserWithRoles<ApplicationUser>;
            if (ur == null) throw new ArgumentNullException(nameof(user));

            return new NamedUserModel
            {
                UserName = ur.User!.UserName,
                Email = ur.User.Email,
                FirstName = ur.User.FirstName,
                LastName = ur.User.LastName,
                Roles = ur.Roles,
                EmailConfirmed = ur.User.EmailConfirmed,
                LockoutEnabled = ur.User.LockoutEnabled,
                LockoutEnd = ur.User.LockoutEnd?.UtcDateTime
            };
        }

        public Dictionary<string, string> GetMessageDictionary(ApplicationUser user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            return new Dictionary<string, string>
            {
                ["FirstName"] = user.FirstName!,
                ["LastName"] = user.LastName!,
                ["UserName"] = user.UserName
            };
        }
    }
}
