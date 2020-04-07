using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Http;

namespace SenseNet.Services.Core.Authentication.IdentityServer4
{
    internal class DefaultSnClientRequestParametersProvider : ISnClientRequestParametersProvider
    {
        private readonly IDictionary<string, IDictionary<string, string>> _parameters =
            new Dictionary<string, IDictionary<string, string>>();

        public DefaultSnClientRequestParametersProvider(SnClientRequestOptions options)
        {
            foreach (var client in options.Clients)
            {
                _parameters.Add(client.ClientType, new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
                {
                    { "authority", options.Authority },
                    { "client_id", client.ClientId }
                }));
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
