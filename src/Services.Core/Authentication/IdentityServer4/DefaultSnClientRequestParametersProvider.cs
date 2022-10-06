using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SenseNet.ContentRepository.Security.Clients;
using SenseNet.Diagnostics;

namespace SenseNet.Services.Core.Authentication.IdentityServer4
{
    internal class DefaultSnClientRequestParametersProvider : ISnClientRequestParametersProvider
    {
        private readonly IDictionary<string, IDictionary<string, string>> _parameters =
            new Dictionary<string, IDictionary<string, string>>();

        private const string ClientAdminUi = "adminui";
        private const string ClientTool = "client";

        public DefaultSnClientRequestParametersProvider(ClientStore clientStore,
            IOptions<ClientRequestOptions> clientOptions, 
            IOptions<AuthenticationOptions> authOptions,
            ILogger<DefaultSnClientRequestParametersProvider> logger)
        {
            var crOptions = clientOptions?.Value ?? new ClientRequestOptions();
            var authority = authOptions?.Value?.Authority ?? string.Empty;

            // load admin ui clients from db using ClientStore
            List<SnIdentityServerClient> clients;
            try
            {
                // only admin ui clients are loaded, other types are not needed by this feature
                clients = clientStore.GetClientsByAuthorityAsync(authority).GetAwaiter().GetResult()
                    .Where(c => c.Type.HasFlag(ClientType.AdminUi))
                    .Select(c => new SnIdentityServerClient
                    {
                        ClientType = ClientAdminUi,
                        ClientId = c.ClientId
                    }).ToList();

                logger.LogTrace("Clients loaded from storage: " +
                                $"{string.Join(", ", clients.Select(c => c.ClientId + $" ({c.ClientType})"))}");
            }
            catch (Exception e)
            {
                logger.LogError(e, "Cannot load clients from clientStore.");
                clients = new List<SnIdentityServerClient>();
            }

            // add clients from config
            clients.AddRange(crOptions.Clients);

            // add default clients only if their type is missing
            if (clients.All(c => c.ClientType != ClientAdminUi))
            {
                logger.LogTrace($"AdminUI client type not found in the storage, adding the default client.");

                clients.Add(new SnIdentityServerClient
                {
                    ClientType = ClientAdminUi,
                    ClientId = ClientAdminUi
                });
            }
            if (clients.All(c => c.ClientType != ClientTool))
            {
                // Add the default tool client because the .Net client library tries to
                // download the authority info using this client type.
                clients.Add(new SnIdentityServerClient
                {
                    ClientType = ClientTool,
                    ClientId = ClientTool
                });
            }

            foreach (var client in clients)
            {
                // duplicate client types will overwrite older values silently
                if (_parameters.ContainsKey(client.ClientType))
                    logger.LogTrace($"DefaultSnClientRequestParametersProvider: client type {client.ClientType} is " +
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
