using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Security.Clients;
using SenseNet.ContentRepository.Storage.Security;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.Services.Core.Operations
{
    public static class ClientStoreOperations
    {
        /// <summary>
        /// Returns clients related to the current repository.
        /// </summary>
        /// <snCategory>Authentication</snCategory>
        /// <param name="content">The root content.</param>
        /// <param name="context">The current HttpContext.</param>
        /// <returns>A result object containing an array of clients.</returns>
        [ODataFunction]
        [ContentTypes(N.CT.PortalRoot)]
        [AllowedRoles(N.R.Administrators, N.R.PublicAdministrators)]
        public static async Task<object> GetClients(Content content, HttpContext context)
        {
            var clientManager = context.RequestServices.GetRequiredService<IClientManager>();
            var clients = await clientManager.GetClientsAsync(context.RequestAborted)
                .ConfigureAwait(false);

            return new
            {
                clients
            };
        }

        /// <summary>
        /// Creates a client.
        /// </summary>
        /// <snCategory>Authentication</snCategory>
        /// <param name="content"></param>
        /// <param name="context"></param>
        /// <param name="name">Name of the client.</param>
        /// <param name="type">Client type. Common types are ExternalClient, ExternalSpa, InternalClient</param>
        /// <param name="userName">Optional domain and username to register the client to.</param>
        /// <returns>The newly created client.</returns>
        /// <exception cref="InvalidOperationException"></exception>
        [ODataAction]
        [ContentTypes(N.CT.PortalRoot)]
        [AllowedRoles(N.R.Administrators, N.R.PublicAdministrators)]
        public static async Task<Client> CreateClient(Content content, HttpContext context,
            string name, string type, string userName = null)
        {
            if (!Enum.TryParse<ClientType>(type, true, out var clientType))
                throw new InvalidOperationException($"Unknown client type: {type}.");

            var clientManager = context.RequestServices.GetRequiredService<IClientManager>();
            var client = await clientManager.CreateClientAsync(name, clientType, userName, context.RequestAborted)
                .ConfigureAwait(false);

            return client;
        }

        /// <summary>
        /// Deletes a client.
        /// </summary>
        /// <snCategory>Authentication</snCategory>
        /// <param name="content"></param>
        /// <param name="context"></param>
        /// <param name="clientId">Client identifier.</param>
        /// <returns>An empty result.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="SenseNetSecurityException"></exception>
        [ODataAction]
        [ContentTypes(N.CT.PortalRoot)]
        [AllowedRoles(N.R.Administrators, N.R.PublicAdministrators)]
        public static Task DeleteClient(Content content, HttpContext context, string clientId)
        {
            if (string.IsNullOrEmpty(clientId))
                throw new ArgumentNullException(nameof(clientId));

            var clientManager = context.RequestServices.GetRequiredService<IClientManager>();

            return clientManager.DeleteClientAsync(clientId, context.RequestAborted);
        }

        /// <summary>
        /// Creates a secret for the specified client.
        /// </summary>
        /// <snCategory>Authentication</snCategory>
        /// <param name="content"></param>
        /// <param name="context"></param>
        /// <param name="clientId">Client identifier.</param>
        /// <param name="validTill">Expiration date. Default: maximum date value.</param>
        /// <returns>The newly created client.</returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="SenseNetSecurityException"></exception>
        [ODataAction]
        [ContentTypes(N.CT.PortalRoot)]
        [AllowedRoles(N.R.Administrators, N.R.PublicAdministrators)]
        public static Task<ClientSecret> CreateSecret(Content content, HttpContext context,
            string clientId, DateTime? validTill = null)
        {
            if (string.IsNullOrEmpty(clientId))
                throw new ArgumentNullException(nameof(clientId));

            var clientManager = context.RequestServices.GetRequiredService<IClientManager>();

            return clientManager.CreateSecretAsync(clientId, validTill, context.RequestAborted);
        }

        /// <summary>
        /// Deletes a secret.
        /// </summary>
        /// <snCategory>Authentication</snCategory>
        /// <remarks>It is necessary to provide both the client and secret identifiers for security reasons.</remarks>
        /// <param name="content"></param>
        /// <param name="context"></param>
        /// <param name="clientId">Client identifier.</param>
        /// <param name="secretId">Secret identifier.</param>
        /// <returns>An empty result.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="SenseNetSecurityException"></exception>
        [ODataAction]
        [ContentTypes(N.CT.PortalRoot)]
        [AllowedRoles(N.R.Administrators, N.R.PublicAdministrators)]
        public static Task DeleteSecret(Content content, HttpContext context, string clientId, string secretId)
        {
            if (string.IsNullOrEmpty(clientId))
                throw new ArgumentNullException(nameof(clientId));
            if (string.IsNullOrEmpty(secretId))
                throw new ArgumentNullException(nameof(secretId));

            var clientManager = context.RequestServices.GetRequiredService<IClientManager>();

            return clientManager.DeleteSecretAsync(clientId, secretId, context.RequestAborted);
        }

        /// <summary>
        /// Regenerates a secret.
        /// </summary>
        /// <snCategory>Authentication</snCategory>
        /// <param name="content"></param>
        /// <param name="context"></param>
        /// <param name="clientId">Client identifier.</param>
        /// <param name="secretId">Secret identifier.</param>
        /// <param name="validTill">Expiration date. Default: maximum date value.</param>
        /// <returns>The newly generated secret.</returns>
        [ODataAction]
        [ContentTypes(N.CT.PortalRoot)]
        [AllowedRoles(N.R.Administrators, N.R.PublicAdministrators)]
        public static async Task<ClientSecret> RegenerateSecretForRepository(Content content, HttpContext context,
            string clientId, string secretId, DateTime? validTill = null)
        {
            if (string.IsNullOrEmpty(clientId))
                throw new ArgumentNullException(nameof(clientId));
            if (string.IsNullOrEmpty(secretId))
                throw new ArgumentNullException(nameof(secretId));
            
            var clientManager = context.RequestServices.GetRequiredService<IClientManager>();
            var secret = await clientManager
                .RegenerateSecretAsync(clientId, secretId, validTill, context.RequestAborted)
                .ConfigureAwait(false);

            return secret;
        }
    }
}
