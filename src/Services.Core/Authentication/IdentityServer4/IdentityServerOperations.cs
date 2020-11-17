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
        //UNDONE:Doc:
        /// <summary></summary>
        /// <snCategory>Authentication</snCategory>
        /// <param name="content"></param>
        /// <param name="context"></param>
        /// <param name="clientType"></param>
        /// <returns></returns>
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
