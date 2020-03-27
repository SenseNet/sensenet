using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Security;
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
    }
}
