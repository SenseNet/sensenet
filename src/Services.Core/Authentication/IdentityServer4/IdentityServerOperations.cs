using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.Diagnostics;

namespace SenseNet.Services.Core.Authentication.IdentityServer4
{
    public static class IdentityServerOperations
    {
        /// <summary>Gets authority information for a repository.
        /// This action is intended for internal use by the admin UI client.</summary>
        /// <snCategory>Authentication</snCategory>
        /// <param name="content"></param>
        /// <param name="context"></param>
        /// <param name="clientType">Client type (currently: adminui).</param>
        /// <returns>A custom object containing the url of the Identity Server used by the repository
        /// and the appropriate client id that should be used by the client.
        /// {
        ///     "authority": "https://example.is.sensenet.cloud",
        ///     "client_id": "abcdefg"
        /// }
        /// </returns>
        [ODataFunction]
        [ContentTypes(N.CT.PortalRoot)]
        [AllowedRoles(N.R.All)]
        public static object GetClientRequestParameters(Content content, HttpContext context, string clientType)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            try
            {
                var provider = context.RequestServices.GetRequiredService<ISnClientRequestParametersProvider>();

                return provider == null 
                    ? new Dictionary<string, string>() 
                    : provider.GetClientParameters(context, clientType);
            }
            catch (Exception ex)
            {
                SnTrace.System.WriteError($"Error loading ISnClientRequestParametersProvider. {ex.Message}");
            }

            return new Dictionary<string, string>();
        }
    }
}
