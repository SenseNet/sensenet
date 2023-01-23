using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SenseNet.ApplicationModel;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Security.Clients;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using Task = System.Threading.Tasks.Task;

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
            // load all EXTERNAL clients
            var clientManager = context.RequestServices.GetRequiredService<IClientManager>();
            var clients = await clientManager.GetClientsAsync(ClientType.AllExternal, context.RequestAborted)
                .ConfigureAwait(false);

            // filter clients not accessible by the current user
            if (!IsBuiltInAdmin())
                clients = clients.Where(IsUserAccessible).ToArray();

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
            var isBuiltInAdmin = IsBuiltInAdmin();
            var clientType = isBuiltInAdmin ? ClientType.All : ClientType.AllExternal;
            var clientStore = context.GetClientStore();
            var options = context.GetOptions();
            var clients = await clientStore.GetClientsByRepositoryAsync(options.RepositoryUrl.RemoveUrlSchema(),
                clientType, context.RequestAborted);

            // filter clients not accessible by the current user
            if (!isBuiltInAdmin)
                clients = clients.Where(IsUserAccessible).ToArray();

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

            var clientStore = context.GetClientStore();
            var options = context.GetOptions();
            if (string.IsNullOrEmpty(options.Authority))
                throw new InvalidOperationException("There is no authority defined.");

            // only builtin admins can create internal clients
            var isBuiltInAdmin = IsBuiltInAdmin();
            if (!isBuiltInAdmin && ClientType.AllInternal.HasFlag(clientType))
                throw new InvalidOperationException("The current user cannot create internal clients.");

            // if the caller did not provide a user, set it automatically
            if (string.IsNullOrEmpty(userName))
            {
                switch (clientType)
                {
                    case ClientType.InternalClient:
                        userName = options.DefaultClientUserInternal;
                        break;
                    case ClientType.ExternalClient:
                        // public admins default is the public admin user
                        userName = !IsPublicAdmin()
                            ? AccessProvider.Current.GetOriginalUser().Username
                            : options.DefaultClientUserExternal;
                        break;
                }
            }
            else
            {
                // only tool clients require a username
                if (!ClientType.AllClient.HasFlag(clientType))
                    userName = null;

                // Make sure that the provided user exists and the current user
                // has permissions for it.
                if (!IsUserAccessible(userName))
                    throw new InvalidOperationException("User does not exist or is not accessible.");
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
        [AllowedRoles(N.R.Administrators, N.R.PublicAdministrators)]
        public static async Task DeleteClient(Content content, HttpContext context, string clientId)
        {
            if (string.IsNullOrEmpty(clientId))
                throw new ArgumentNullException(nameof(clientId));

            var clientStore = context.GetClientStore();
            var client = await context.GetClientAsync(clientId, clientStore).ConfigureAwait(false);

            AssertClient(client, clientId);

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
        [AllowedRoles(N.R.Administrators, N.R.PublicAdministrators)]
        public static async Task<ClientSecret> CreateSecret(Content content, HttpContext context,
            string clientId, DateTime? validTill = null)
        {
            if (string.IsNullOrEmpty(clientId))
                throw new ArgumentNullException(nameof(clientId));

            var clientStore = context.GetClientStore();
            var client = await context.GetClientAsync(clientId, clientStore).ConfigureAwait(false);

            AssertClient(client, clientId);

            if (!ClientType.AllClient.HasFlag(client.Type))
                throw new InvalidOperationException($"Invalid client type: {client.Type}");
            
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
        [AllowedRoles(N.R.Administrators, N.R.PublicAdministrators)]
        public static async Task DeleteSecret(Content content, HttpContext context, string clientId, string secretId)
        {
            if (string.IsNullOrEmpty(clientId))
                throw new ArgumentNullException(nameof(clientId));
            if (string.IsNullOrEmpty(secretId))
                throw new ArgumentNullException(nameof(secretId));

            var clientStore = context.GetClientStore();
            var client = await context.GetClientAsync(clientId, clientStore).ConfigureAwait(false);

            AssertClient(client, clientId);

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
        [AllowedRoles(N.R.Administrators, N.R.PublicAdministrators)]
        public static async Task<ClientSecret> RegenerateSecretForRepository(Content content, HttpContext context,
            string clientId, string secretId, DateTime? validTill = null)
        {
            if (string.IsNullOrEmpty(clientId))
                throw new ArgumentNullException(nameof(clientId));
            if (string.IsNullOrEmpty(secretId))
                throw new ArgumentNullException(nameof(secretId));
            
            var client = await context.GetClientAsync(clientId).ConfigureAwait(false);
            AssertClient(client, clientId);

            var clientManager = context.RequestServices.GetRequiredService<IClientManager>();
            var secret = await clientManager
                .RegenerateSecretAsync(clientId, secretId, validTill ?? DateTime.MaxValue, context.RequestAborted)
                .ConfigureAwait(false);

            return secret;
        }

        private static bool IsBuiltInAdmin() => SystemAccount.Execute(() =>
            AccessProvider.Current.GetOriginalUser().IsInGroup(Group.Administrators));

        private static bool IsPublicAdmin()
        {
            var publicAdminsGroupHead = NodeHead.Get(N.R.PublicAdministrators);
            var originalUser = AccessProvider.Current.GetOriginalUser();

            return Providers.Instance.SecurityHandler.IsInGroup(originalUser.Id, publicAdminsGroupHead?.Id ?? 0);
        }
        private static bool IsUserAccessible(Client client) => IsUserAccessible(client?.UserName);
        private static bool IsUserAccessible(string userName)
        {
            if (string.IsNullOrEmpty(userName))
                return true;
            
            // Load the user in elevated mode to avoid access denied exceptions.
            // We'll check permissions below anyway.
            var user = SystemAccount.Execute((() => User.Load(userName)));
            if (user == null)
                return false;

            // Members of the Public Administrators GROUP are allowed to manage clients
            // of the public admin USER, because that user essentially represents that group.
            if (string.Equals(user.Path, Identifiers.PublicAdminPath, StringComparison.Ordinal))
            {
                if (IsPublicAdmin())
                    return true;
            }

            // admins need to have Save permission for a user if they want to manage their clients
            return Providers.Instance.SecurityHandler.HasPermission(user, PermissionType.Save);
        }
        
        private static Task<Client> GetClientAsync(this HttpContext context, string clientId)
        {
            var clientStore = context.GetClientStore();
            return GetClientAsync(context, clientId, clientStore);
        }
        private static async Task<Client> GetClientAsync(this HttpContext context, string clientId, ClientStore clientStore)
        {
            var options = context.GetOptions();

            // make sure this clientId belongs to the current repo
            var client = await clientStore
                .GetClientAsync(options.RepositoryUrl.RemoveUrlSchema(), clientId, context.RequestAborted)
                .ConfigureAwait(false);

            return client;
        }

        private static void AssertClient(Client client, string clientId = null)
        {
            if (client == null)
                throw new InvalidOperationException($"Unknown client id: {clientId}");

            if (IsBuiltInAdmin())
                return;

            // do not allow internal clients or ones that belong to inaccessible users
            if (ClientType.AllInternal.HasFlag(client.Type) || !IsUserAccessible(client.UserName))
                throw new SenseNetSecurityException($"Client {clientId} is not accessible.");
        }
        
        private static ClientStore GetClientStore(this HttpContext context) =>
            context.RequestServices.GetRequiredService<ClientStore>();
        private static ClientStoreOptions GetOptions(this HttpContext context) =>
            context.RequestServices.GetRequiredService<IOptions<ClientStoreOptions>>().Value;
    }
}
