using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace SenseNet.Services.Core.Authentication.IdentityServer4
{
    public interface ISnClientRequestParametersProvider
    {
        IDictionary<string, string> GetClientParameters(HttpContext context, string clientType);
    }
}