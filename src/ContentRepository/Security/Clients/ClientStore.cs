using System;
using System.Linq;
using System.Threading;
using Tasks=System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SenseNet.ContentRepository.Security.Clients
{
    /// <summary>
    /// Main client store API. Manages IdentityServer clients and optionally their corresponding secrets.
    /// </summary>
    public class ClientStore
    {
        private readonly IClientStoreDataProviderExtension _storage;
        private readonly ClientStoreOptions _options;
        private readonly ILogger<ClientStore> _logger;
        public ClientStore(IClientStoreDataProviderExtension storage, IOptions<ClientStoreOptions> options, ILogger<ClientStore> logger)
        {
            _storage = storage;
            _options = options.Value;
            _logger = logger;
        }

        /// <summary>
        /// Loads clients with certain client types by <paramref name="repositoryHost"/>.
        /// </summary>
        /// <param name="repositoryHost">Host name of the repository without schema (e.g. "example.com").</param>
        /// <param name="type">One or more client type flags.</param>
        /// <param name="cancellation">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the loaded <see cref="Client"/>
        /// instances.</returns>
        public async Tasks.Task<Client[]> GetClientsByRepositoryAsync(string repositoryHost, ClientType type,
            CancellationToken cancellation = default)
        {
            var clients = await _storage.LoadClientsByRepositoryAsync(repositoryHost?.TrimSchema(), cancellation)
                .ConfigureAwait(false);

            // return clients that have any of the requested types
            return clients.Where(c => type.HasFlag(c.Type)).ToArray();
        }
        /// <summary>
        /// Loads clients by <paramref name="authority"/>.
        /// </summary>
        /// <param name="authority">The authority address without schema (e.g. "is.example.com").</param>
        /// <param name="cancellation">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the loaded <see cref="Client"/>
        /// instances.</returns>
        public Tasks.Task<Client[]> GetClientsByAuthorityAsync(string authority, CancellationToken cancellation = default)
        {
            return _storage.LoadClientsByAuthorityAsync(authority?.TrimSchema(), cancellation);
        }
        /// <summary>
        /// Loads a client of a repository by its client id.
        /// </summary>
        /// <param name="repositoryHost">Host name of the repository without schema (e.g. "example.com").</param>
        /// <param name="clientId">Client id.</param>
        /// <param name="cancellation">A Task that represents the asynchronous operation and wraps the client instance.</param>
        /// <returns></returns>
        public async Tasks.Task<Client> GetClientAsync(string repositoryHost, string clientId, CancellationToken cancellation = default)
        {
            var clients = await _storage.LoadClientsByRepositoryAsync(repositoryHost, cancellation)
                .ConfigureAwait(false);

            return clients.FirstOrDefault(c => c.ClientId == clientId);
        }

        /// <summary> 
        /// Saves a new or updates an existing client and synchronizes its secrets.
        /// If the client id is not filled, it will generate a new one
        /// and fill the property of the provided client.
        /// </summary>
        /// <param name="client">A client to save.</param>
        /// <param name="cancellation">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the saved <see cref="Client"/>
        /// instance.</returns>
        public async Tasks.Task<Client> SaveClientAsync(Client client, CancellationToken cancellation = default)
        {
            if (string.IsNullOrEmpty(client.ClientId))
                client.ClientId = GenerateId();
            if (string.IsNullOrEmpty(client.Name))
                client.Name = client.ClientId;

            await _storage.SaveClientAsync(client, cancellation);

            _logger.LogInformation($"New {client.Type} client created for {client.Repository} with id {client.ClientId}");

            return client;
        }
        /// <summary>
        /// Generates and saves a new secret for the provided client. The new secret will be added
        /// to the Secrets collection of the client instance.
        /// </summary>
        /// <param name="client">A client to add a secret for.</param>
        /// <param name="validTill">Expiration date for the secret (optional).</param>
        /// <param name="cancellation"></param>
        /// <returns>A Task that represents the asynchronous operation and wraps the new secret instance.</returns>
        public async Tasks.Task<ClientSecret> GenerateSecretAsync(Client client, DateTime? validTill = null,
            CancellationToken cancellation = default)
        {
            var secret = new ClientSecret
            {
                CreationDate = DateTime.UtcNow,
                Id = GenerateId(),
                Value = GenerateSecret(),
                ValidTill = validTill ?? DateTime.MaxValue
            };

            await _storage.SaveSecretAsync(client.ClientId, secret, cancellation).ConfigureAwait(false);

            // add if the data layer did not add it
            if (client.Secrets.All(s => s.Id != secret.Id))
                client.Secrets.Add(secret);

            _logger.LogInformation($"Secret generated for {client.Type} client {client.ClientId}. " +
                                   $"Expires at {secret.ValidTill}");

            return secret;
        }

        /// <summary>
        /// Regenerates the provided secret for the provided client.
        /// </summary>
        /// <param name="client">A client to regenerate an existing secret for.</param>
        /// <param name="secretId">Id of the secret to regenerate.</param>
        /// <param name="validTill">Expiration date for the secret (optional).</param>
        /// <param name="cancellation"></param>
        /// <returns>A Task that represents the asynchronous operation and wraps the updated secret instance.</returns>
        public async Tasks.Task<ClientSecret> RegenerateSecretAsync(Client client, string secretId, DateTime? validTill = null,
            CancellationToken cancellation = default)
        {
            var secret = client.Secrets.FirstOrDefault(s => s.Id == secretId);
            if (secret == null)
                throw new InvalidOperationException("Secret not found.");

            secret.Value = GenerateSecret();

            if (validTill.HasValue)
                secret.ValidTill = validTill.Value;

            // update the secret in the db
            await _storage.SaveSecretAsync(client.ClientId, secret, cancellation).ConfigureAwait(false);

            return secret;
        }

        /// <summary>
        /// Deletes the <see cref="Client"/> identified by the given <paramref name="clientId"/>.
        /// The client's secrets will also be removed.
        /// </summary>
        /// <param name="clientId">Id of the client.</param>
        /// <param name="cancellation">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public Tasks.Task DeleteClientAsync(string clientId, CancellationToken cancellation = default)
        {
            return _storage.DeleteClientAsync(clientId, cancellation);
        }
        /// <summary>
        /// Deletes all <see cref="Client"/> items of a repository identified by the given <paramref name="repositoryHost"/>.
        /// The clients' secrets will also be removed.
        /// </summary>
        /// <param name="repositoryHost">Host of the repository (e.g. "example.com").</param>
        /// <param name="cancellation">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public Tasks.Task DeleteClientByRepositoryHostAsync(string repositoryHost, CancellationToken cancellation = default)
        {
            return _storage.DeleteClientByRepositoryHostAsync(repositoryHost, cancellation);
        }

        /// <summary>
        /// Deletes the <see cref="ClientSecret"/> identified by the given <paramref name="secretId"/>.
        /// </summary>
        /// <param name="clientId">Id of the client that the secret belongs to.</param>
        /// <param name="secretId">The secret to delete.</param>
        /// <param name="cancellation">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public Tasks.Task DeleteSecretAsync(string clientId, string secretId, CancellationToken cancellation = default)
        {
            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(secretId))
                throw new ArgumentNullException();

            return _storage.DeleteSecretAsync(clientId, secretId, cancellation);
        }

        public async Tasks.Task EnsureClientsAsync(string authority, string host)
        {
            // check if we have enough information
            if (string.IsNullOrEmpty(authority) || string.IsNullOrEmpty(host))
                return;

            async Tasks.Task EnsureClientAsync(ClientType type, string userName = null)
            {
                var clients = await GetClientsByRepositoryAsync(host, type)
                    .ConfigureAwait(false);

                if (clients.Any()) return;

                var client = new Client
                {
                    Authority = authority.TrimSchema(),
                    Repository = host.TrimSchema(),
                    Type = type,
                    UserName = userName
                };

                await SaveClientAsync(client).ConfigureAwait(false);

                // generate secrets only for tool types
                if (ClientType.AllClient.HasFlag(type))
                {
                    await GenerateSecretAsync(client).ConfigureAwait(false);
                }
            }

            try
            {
                // We do not generate a client for admin ui by default because it would
                // interfere with the default client.

                // internal tools
                await EnsureClientAsync(ClientType.InternalClient, _options.DefaultClientUserInternal)
                    .ConfigureAwait(false);
                
                // external tools
                await EnsureClientAsync(ClientType.ExternalClient, _options.DefaultClientUserExternal)
                    .ConfigureAwait(false);

                // external SPA
                await EnsureClientAsync(ClientType.ExternalSpa)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Could not ensure clients for repository {host}. {ex.Message}");
            }
        }

        private static string GenerateId()
        {
            // short random string
            return RandomStringGenerator.New(16);
        }
        private static string GenerateSecret()
        {
            // long random string
            return RandomStringGenerator.New(64);
        }
    }
}
