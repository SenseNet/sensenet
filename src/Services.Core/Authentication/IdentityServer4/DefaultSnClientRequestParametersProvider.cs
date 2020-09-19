using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SenseNet.Diagnostics;

namespace SenseNet.Services.Core.Authentication.IdentityServer4
{
    internal class DefaultSnClientRequestParametersProvider : ISnClientRequestParametersProvider
    {
        private readonly IDictionary<string, IDictionary<string, string>> _parameters =
            new Dictionary<string, IDictionary<string, string>>();

        public DefaultSnClientRequestParametersProvider(IOptions<ClientRequestOptions> clientOptions, 
            IOptions<AuthenticationOptions> authOptions)
        {
            var crOptions = clientOptions?.Value ?? new ClientRequestOptions();
            var authority = authOptions?.Value?.Authority ?? string.Empty;

            foreach (var client in crOptions.Clients)
            {
                // duplicate client types will overwrite older values silently
                if (_parameters.ContainsKey(client.ClientType))
                    SnTrace.System.Write($"DefaultSnClientRequestParametersProvider: client type {client.ClientType} is " +
                                         $"already registered, the new value ({client.ClientId} will overwrite it.)");

                _parameters[client.ClientType] = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
                {
                    {"authority", authority},
                    {"client_id", client.ClientId}
                });
            }
        }
        public IDictionary<string, string> GetClientParameters(HttpContext context, string clientId)
        {
            if (string.IsNullOrEmpty(clientId))
                return new Dictionary<string, string>();

            if (_parameters.TryGetValue(clientId, out var clientParameters))
                return clientParameters;

            throw new InvalidOperationException($"Unknown client: {clientId}");
        }
    }
}
