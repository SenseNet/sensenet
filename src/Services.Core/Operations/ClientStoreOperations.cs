using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Security.Clients;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.Services.Core.Operations
{
    public static class ClientStoreOperations
    {
        /// <summary>
        /// Returns external clients related to this repository.
        /// </summary>
        /// <snCategory>Authentication</snCategory>
        /// <param name="content">The repository content.</param>
        /// <param name="context">The current HttpContext.</param>
        /// <returns>A result object containing an array of clients.</returns>
        [ODataFunction]
        [ContentTypes(N.CT.PortalRoot)]
        [AllowedRoles(N.R.Administrators, N.R.PublicAdministrators)]
        public static async Task<object> GetClientsForRepository(Content content, HttpContext context)
        {
            var clientStore = context.RequestServices.GetRequiredService<IClientManager>();
            var clients = await clientStore.GetClientsAsync(ClientType.AllExternal, context.RequestAborted)
                .ConfigureAwait(false);

            return new
            {
                clients
            };
        }

        /// <summary>
        /// Returns clients related to the current repository. Only administrators will get
        /// all types. Regular users will only get external clients.
        /// </summary>
        /// <snCategory>Authentication</snCategory>
        /// <param name="content">The root content.</param>
        /// <param name="context">The current HttpContext.</param>
        /// <remarks>This method is intended for internal server-to-server communication.</remarks>
        /// <returns>A result object containing an array of clients.</returns>
        [ODataFunction("GetClients")]
        [ContentTypes(N.CT.PortalRoot)]
        [AllowedRoles(N.R.Administrators, N.R.PublicAdministrators)]
        public static async Task<object> GetClients(Content content, HttpContext context)
        {
            // Only administrators are allowed to get all types of clients. Regular
            // users can receive only external tools and spa clients/secrets.
            var clientType = IsAdmin() ? ClientType.All : ClientType.AllExternal;
            var clientStore = context.GetClientStore();
            var options = context.GetOptions();
            var clients = (await clientStore.GetClientsByRepositoryAsync(options.RepositoryUrl.RemoveUrlSchema(), clientType)).ToArray();

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
        [AllowedRoles(N.R.Administrators)]
        public static async Task<Client> CreateClient(Content content, HttpContext context,
            string name, string type, string userName = null)
        {
            // Currently only Administrators can create a new client. 

            //TODO: When this method is opened to everyone, make sure that the provided
            // user is correct and it has only the necessary permissions, nothing more!

            if (!Enum.TryParse<ClientType>(type, true, out var clientType))
                throw new InvalidOperationException($"Unknown client type: {type}.");

            var clientStore = context.GetClientStore();
            var options = context.GetOptions();
            if (string.IsNullOrEmpty(options.Authority))
                throw new InvalidOperationException("There is no authority defined.");

            // if the caller did not provide a user, set it automatically
            if (string.IsNullOrEmpty(userName))
            {
                switch (clientType)
                {
                    case ClientType.InternalClient:
                        userName = options.DefaultClientUserInternal;
                        break;
                    case ClientType.ExternalClient:
                        userName = options.DefaultClientUserExternal;
                        break;
                }
            }

            var client = new Client
            {
                Authority = options.Authority.RemoveUrlSchema(),
                Name = name,
                Repository = options.RepositoryUrl.RemoveUrlSchema(),
                Type = clientType,
                UserName = userName
            };

            await clientStore.SaveClientAsync(client).ConfigureAwait(false);

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
        [AllowedRoles(N.R.Administrators)]
        public static async System.Threading.Tasks.Task DeleteClient(Content content, HttpContext context, string clientId)
        {
            if (string.IsNullOrEmpty(clientId))
                throw new ArgumentNullException(nameof(clientId));

            var clientStore = context.GetClientStore();
            var options = context.GetOptions();

            // check if this clientId belongs to the current repo
            var client = await clientStore.GetClientAsync(options.RepositoryUrl.RemoveUrlSchema(), clientId, context.RequestAborted)
                .ConfigureAwait(false);

            // We throw an exception here because this is a security-related feature.
            // If the caller provided a nonexistent id, we should not return silently.
            if (client == null)
                throw new InvalidOperationException("Unknown client id.");

            if (ClientType.AllInternal.HasFlag(client.Type) && !IsAdmin())
                throw new SenseNetSecurityException("Client is not accessible.");

            await clientStore.DeleteClientAsync(clientId, context.RequestAborted).ConfigureAwait(false);
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
        [AllowedRoles(N.R.Administrators)]
        public static async Task<ClientSecret> CreateSecret(Content content, HttpContext context,
            string clientId, DateTime? validTill = null)
        {
            var clientStore = context.GetClientStore();
            var options = context.GetOptions();
            var client = await clientStore.GetClientAsync(options.RepositoryUrl.RemoveUrlSchema(), clientId)
                .ConfigureAwait(false);

            if (client == null)
                throw new InvalidOperationException("Unknown client id.");
            if (!ClientType.AllClient.HasFlag(client.Type))
                throw new InvalidOperationException("Invalid client type.");
            if (ClientType.AllInternal.HasFlag(client.Type) && !IsAdmin())
                throw new SenseNetSecurityException("Client is not accessible.");

            var secret = await clientStore.GenerateSecretAsync(client, validTill).ConfigureAwait(false);

            return secret;
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
        [AllowedRoles(N.R.Administrators)]
        public static async System.Threading.Tasks.Task DeleteSecret(Content content, HttpContext context, string clientId, string secretId)
        {
            if (string.IsNullOrEmpty(clientId))
                throw new ArgumentNullException(nameof(clientId));
            if (string.IsNullOrEmpty(secretId))
                throw new ArgumentNullException(nameof(secretId));

            var clientStore = context.GetClientStore();
            var options = context.GetOptions();

            // check if this clientId belongs to the current repo
            var client = await clientStore.GetClientAsync(options.RepositoryUrl.RemoveUrlSchema(), clientId, context.RequestAborted)
                .ConfigureAwait(false);

            // We throw an exception here because this is a security-related feature.
            // If the caller provided a nonexistent id, we should not return silently.
            if (client == null)
                throw new InvalidOperationException("Unknown client id.");

            if (ClientType.AllInternal.HasFlag(client.Type) && !IsAdmin())
                throw new SenseNetSecurityException("Client is not accessible.");

            await clientStore.DeleteSecretAsync(clientId, secretId, context.RequestAborted).ConfigureAwait(false);
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
        [AllowedRoles(N.R.Administrators)]
        public static async Task<ClientSecret> RegenerateSecretForRepository(Content content, HttpContext context,
            string clientId, string secretId, DateTime? validTill = null)
        {
            var clientStore = context.RequestServices.GetRequiredService<IClientManager>();
            var secret = await clientStore
                .RegenerateSecretAsync(clientId, secretId, validTill ?? DateTime.MaxValue, context.RequestAborted)
                .ConfigureAwait(false);

            return secret;
        }

        private static bool IsAdmin()
        {
            return SystemAccount.Execute(() =>
                AccessProvider.Current.GetOriginalUser().IsInGroup(Group.Administrators));
        }
        private static ClientStore GetClientStore(this HttpContext context)
        {
            return context.RequestServices.GetService<ClientStore>();
        }
        private static ClientStoreOptions GetOptions(this HttpContext context)
        {
            return context.RequestServices.GetService<IOptions<ClientStoreOptions>>().Value;
        }
    }
}
