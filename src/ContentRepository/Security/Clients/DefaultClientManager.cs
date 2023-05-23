using Microsoft.Extensions.Options;
using System;
using STT=System.Threading.Tasks;
using System.Threading;
using SenseNet.ContentRepository.Storage.Security;
using Microsoft.Extensions.Logging;
using SenseNet.ApplicationModel;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage;
using System.Linq;

namespace SenseNet.ContentRepository.Security.Clients
{
    /// <inheritdoc/>
    public class DefaultClientManager : IClientManager
    {
        private readonly ClientStore _clientStore;
        private readonly ILogger<DefaultClientManager> _logger;
        private readonly ClientStoreOptions _clientStoreOptions;

        public DefaultClientManager(ClientStore clientStore, IOptions<ClientStoreOptions> clientStoreOptions,
            ILogger<DefaultClientManager> logger)
        {
            _clientStore = clientStore;
            _logger = logger;
            _clientStoreOptions = clientStoreOptions.Value;
        }

        public async STT.Task<Client[]> GetClientsAsync(CancellationToken cancel)
        {
            var clients = await _clientStore
                .GetClientsByRepositoryAsync(_clientStoreOptions.RepositoryUrl.RemoveUrlSchema(), ClientType.All,
                    cancel).ConfigureAwait(false);

            // filter clients not accessible by the current user
            if (!IsBuiltInAdmin())
                clients = clients.Where(IsUserAccessible).ToArray();

            return clients;
        }

        public async STT.Task<Client> CreateClientAsync(string name, ClientType type, string userName, CancellationToken cancel)
        {
            if (string.IsNullOrEmpty(_clientStoreOptions.Authority))
                throw new InvalidOperationException("There is no authority defined.");

            // only builtin admins can create internal clients
            if (!IsBuiltInAdmin() && ClientType.AllInternal.HasFlag(type))
                throw new InvalidOperationException("The current user cannot create internal clients.");

            // if the caller did not provide a user, set it automatically
            if (string.IsNullOrEmpty(userName))
            {
                userName = type switch
                {
                    ClientType.InternalClient => _clientStoreOptions.DefaultClientUserInternal,
                    ClientType.ExternalClient =>
                        // public admins default is the public admin user
                        !IsPublicAdmin()
                            ? AccessProvider.Current.GetOriginalUser().Username
                            : _clientStoreOptions.DefaultClientUserExternal,
                    _ => userName
                };
            }
            else
            {
                // only tool clients require a username
                if (!ClientType.AllClient.HasFlag(type))
                    userName = null;

                // Make sure that the provided user exists and the current user
                // has permissions for it.
                if (!IsUserAccessible(userName))
                {
                    if (!string.IsNullOrEmpty(userName))
                        _logger.LogTrace("User {userName} does not exist or is not accessible for {currentUser}", userName,
                            User.Current.Username);

                    throw new InvalidOperationException("User does not exist or is not accessible.");
                }
            }

            var client = new Client
            {
                Authority = _clientStoreOptions.Authority.RemoveUrlSchema(),
                Name = name,
                Repository = _clientStoreOptions.RepositoryUrl.RemoveUrlSchema(),
                Type = type,
                UserName = userName
            };

            await _clientStore.SaveClientAsync(client, cancel).ConfigureAwait(false);

            return client;
        }

        public async STT.Task DeleteClientAsync(string clientId, CancellationToken cancel)
        {
            if (string.IsNullOrEmpty(clientId))
                throw new ArgumentNullException(nameof(clientId));

            var client = await GetClientAsync(clientId, cancel).ConfigureAwait(false);

            AssertClient(client, clientId);

            await _clientStore.DeleteClientAsync(clientId, cancel).ConfigureAwait(false);
        }
        public async STT.Task<ClientSecret> CreateSecretAsync(string clientId, DateTime? validTill, CancellationToken cancel)
        {
            if (string.IsNullOrEmpty(clientId))
                throw new ArgumentNullException(nameof(clientId));

            var client = await GetClientAsync(clientId, cancel).ConfigureAwait(false);

            AssertClient(client, clientId);

            if (!ClientType.AllClient.HasFlag(client.Type))
                throw new InvalidOperationException($"Invalid client type: {client.Type}");

            var secret = await _clientStore.GenerateSecretAsync(client, validTill, cancel).ConfigureAwait(false);

            return secret;
        }
        public async STT.Task DeleteSecretAsync(string clientId, string secretId, CancellationToken cancel)
        {
            if (string.IsNullOrEmpty(clientId))
                throw new ArgumentNullException(nameof(clientId));
            if (string.IsNullOrEmpty(secretId))
                throw new ArgumentNullException(nameof(secretId));

            var client = await GetClientAsync(clientId, cancel).ConfigureAwait(false);

            AssertClient(client, clientId);

            await _clientStore.DeleteSecretAsync(clientId, secretId, cancel).ConfigureAwait(false);
        }

        public async STT.Task<ClientSecret> RegenerateSecretAsync(string clientId, string secretId, DateTime? validTill, CancellationToken cancel)
        {
            var client = await GetClientAsync(clientId, cancel).ConfigureAwait(false);

            AssertClient(client, clientId);

            return await _clientStore.RegenerateSecretAsync(client, secretId, validTill, cancel).ConfigureAwait(false);
        }

        // ====================================================================================== Helper methods

        protected bool IsBuiltInAdmin() => SystemAccount.Execute(() =>
            AccessProvider.Current.GetOriginalUser().IsInGroup(Group.Administrators));
        protected bool IsPublicAdmin()
        {
            var publicAdminsGroupHead = NodeHead.Get(N.R.PublicAdministrators);
            var originalUser = AccessProvider.Current.GetOriginalUser();

            return Providers.Instance.SecurityHandler.IsInGroup(originalUser.Id, publicAdminsGroupHead?.Id ?? 0);
        }
        protected bool IsUserAccessible(Client client) => IsUserAccessible(client?.UserName);
        protected bool IsUserAccessible(string userName)
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

        private STT.Task<Client> GetClientAsync(string clientId, CancellationToken cancel)
        {
            // make sure this clientId belongs to the current repo
            return _clientStore.GetClientAsync(_clientStoreOptions.RepositoryUrl.RemoveUrlSchema(), clientId, cancel);
        }

        protected void AssertClient(Client client, string clientId = null)
        {
            if (client == null)
                throw new InvalidOperationException($"Unknown client id: {clientId}");

            if (IsBuiltInAdmin())
                return;

            // do not allow internal clients or ones that belong to inaccessible users
            if (ClientType.AllInternal.HasFlag(client.Type) || !IsUserAccessible(client.UserName))
            {
                _logger.LogTrace($"{User.Current.Username} tried to access client {clientId ?? client.ClientId} without permission.");
                throw new SenseNetSecurityException("Client is not accessible.");
            }
        }
    }
}
