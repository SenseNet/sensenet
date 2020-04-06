using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.Diagnostics;

namespace SenseNet.Services.Core.Authentication.IdentityServer4
{
    public static class IdentityServerOperations
    {
        [ODataFunction]
        [ContentTypes(N.CT.PortalRoot)]
        [AllowedRoles(N.R.All)]
        public static object GetClientRequestParameters(Content content, HttpContext context, string clientType)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            try
            {
                if (!(context.RequestServices.GetService(typeof(ISnClientRequestParametersProvider)) is ISnClientRequestParametersProvider provider))
                    return new Dictionary<string, string>();

                return provider.GetClientParameters(context, clientType);
            }
            catch (Exception ex)
            {
                SnTrace.System.WriteError($"Error loading ISnClientRequestParametersProvider. {ex.Message}");
            }

            return new Dictionary<string, string>();
        }
    }
}
