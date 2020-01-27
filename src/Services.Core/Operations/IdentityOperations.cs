using System;
using Microsoft.AspNetCore.Http;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.Services.Core.Operations
{
    public static class IdentityOperations
    {
        [ODataAction]
        [ContentTypes(N.CT.PortalRoot)]
        [AllowedRoles(N.R.All)]
        public static object ValidateCredentials(Content content, HttpContext context, string userName, string password)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (userName == null)
                throw new ArgumentNullException(nameof(userName));
            if (password == null)
                throw new ArgumentNullException(nameof(password));

            // check user in elevated mode, because this is a system operation
            using (new SystemAccount())
            {
                var user = User.Load(userName);

                if (user?.CheckPasswordMatch(password) ?? false)
                {
                    return new
                    {
                        id = user.Id,
                        email = user.Email,
                        username = user.Username,
                        name = user.Name,
                        loginName = user.LoginName
                    };
                }
            }

            throw new SenseNetSecurityException("Invalid username or password.");
        }
    }
}
