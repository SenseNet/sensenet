using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace SenseNet.ContentRepository.Security.Clients
{
    public interface IClientManager
    {
        Task<Client[]> GetClientsAsync(ClientType type, CancellationToken cancellation);
        Task<ClientSecret> RegenerateSecretAsync(string clientId, string secretId, DateTime validTill, CancellationToken cancellation);
    }

    internal class DefaultClientManager : IClientManager
    {
        private readonly ClientStore _clientStore;
        private readonly ClientStoreOptions _clientStoreOptions;

        public DefaultClientManager(ClientStore clientStore, IOptions<ClientStoreOptions> clientStoreOptions)
        {
            _clientStore = clientStore;
            _clientStoreOptions = clientStoreOptions.Value;
        }

        public Task<Client[]> GetClientsAsync(ClientType type, CancellationToken cancellation)
        {
            return _clientStore.GetClientsByRepositoryAsync(_clientStoreOptions.RepositoryUrl.RemoveUrlSchema(), type, cancellation);
        }

        public async Task<ClientSecret> RegenerateSecretAsync(string clientId, string secretId, DateTime validTill, CancellationToken cancellation)
        {
            var client = await _clientStore.GetClientAsync(_clientStoreOptions.RepositoryUrl.RemoveUrlSchema(), 
                clientId, cancellation).ConfigureAwait(false);

            return await _clientStore.RegenerateSecretAsync(client, secretId, validTill, cancellation).ConfigureAwait(false);
        }
    }
}
