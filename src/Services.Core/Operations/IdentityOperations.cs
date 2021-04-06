using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.Services.Core.Authentication;

namespace SenseNet.Services.Core.Operations
{
    public class ClaimInfo
    {
        public string Type { get; set; }
        public string Value { get; set; }
    }

    public static class IdentityOperations
    {
        /// <summary>Validates the provided user credentials.</summary>
        /// <snCategory>Authentication</snCategory>
        /// <param name="content"></param>
        /// <param name="context"></param>
        /// <param name="userName">Username (domain name can be omitted if it is the default).</param>
        /// <param name="password">Password</param>
        /// <returns>A custom object containing basic user data. If the credentials are not valid,
        /// the request throws a <see cref="SenseNetSecurityException"/> and return a 404 response.
        /// For example:
        /// {
        ///     id: 1234,
        ///     email: "mail@example.com",
        ///     username: "example",
        ///     name: "example",
        ///     loginName: "example"
        /// }
        /// </returns>
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
                if (user == null)
                {
                    SnTrace.Security.Write($"Could not find a user with the name: {userName}");
                }
                else if (!user.Enabled)
                {
                    SnTrace.Security.Write($"User {userName} is disabled, not allowed to log in.");
                    user = null;
                }

                if (user != null)
                {
                    if (user.CheckPasswordMatch(password))
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

                    SnTrace.Security.Write($"Password match failed for user: {userName}");
                }
            }

            throw new SenseNetSecurityException("Invalid username or password.");
        }

        /// <summary>Creates an external user who registered using one of the available
        /// external providers.</summary>
        /// <snCategory>Users and Groups</snCategory>
        /// <param name="content"></param>
        /// <param name="context"></param>
        /// <param name="provider">Name of the provider (e.g. Google, GitHub).</param>
        /// <param name="userId">External user id given by the provider.</param>
        /// <param name="claims">List of claims given by the provider.</param>
        /// <returns>The newly created user content.</returns>
        [ODataAction]
        [ContentTypes(N.CT.PortalRoot)]
        [AllowedRoles(N.R.Administrators)]
        public static async Task<Content> CreateUserByProvider(Content content, HttpContext context, string provider, 
            string userId, string claims)
        {
            if (!(context.RequestServices.GetService(typeof(RegistrationProviderStore)) is RegistrationProviderStore providerStore))
                throw new InvalidOperationException("sensenet user registration service is not available.");

            var registrationProvider = providerStore.Get(provider);
            var claimsList = string.IsNullOrEmpty(claims)
                ? Array.Empty<ClaimInfo>()
                : JsonConvert.DeserializeObject(claims, typeof(ClaimInfo[])) as ClaimInfo[];

            var user = await registrationProvider.CreateProviderUserAsync(content, context, provider, 
                userId, claimsList, context.RequestAborted).ConfigureAwait(false);

            return Content.Create(user);
        }

        /// <summary>Creates a local user who registered using a username and password.</summary>
        /// <snCategory>Users and Groups</snCategory>
        /// <param name="content"></param>
        /// <param name="context"></param>
        /// <param name="loginName">Login name.</param>
        /// <param name="password">Password.</param>
        /// <param name="email">Email address.</param>
        /// <returns>The newly created user content.</returns>
        [ODataAction]
        [ContentTypes(N.CT.PortalRoot)]
        [AllowedRoles(N.R.Administrators)]
        public static async Task<Content> CreateLocalUser(Content content, HttpContext context, string loginName, 
            string password, string email)
        {
            if (!(context.RequestServices.GetService(typeof(RegistrationProviderStore)) is RegistrationProviderStore providerStore))
                throw new InvalidOperationException("sensenet user registration service is not available.");

            var registrationProvider = providerStore.Get("local");
            var user = await registrationProvider.CreateLocalUserAsync(content, context, loginName,
                password, email, context.RequestAborted).ConfigureAwait(false);

            return Content.Create(user);
        }
    }
}
