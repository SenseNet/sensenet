﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace SenseNet.Services.Core.Authentication.IdentityServer4
{
    internal class DefaultSnClientRequestParametersProvider : ISnClientRequestParametersProvider
    {
        private readonly IDictionary<string, IDictionary<string, string>> _parameters =
            new Dictionary<string, IDictionary<string, string>>();

        public DefaultSnClientRequestParametersProvider(IOptions<SnClientRequestOptions> options)
        {
            var crOptions = options?.Value ?? new SnClientRequestOptions();
            foreach (var client in crOptions.Clients)
            {
                _parameters.Add(client.ClientType, new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
                {
                    { "authority", crOptions.Authority },
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
