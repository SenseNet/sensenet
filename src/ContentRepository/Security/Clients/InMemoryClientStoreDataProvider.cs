using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Tasks=System.Threading.Tasks;

namespace SenseNet.ContentRepository.Security.Clients
{
    /// <summary>
    /// In memory implementation of the <see cref="IClientStoreDataProvider"/> interface.
    /// Do not use this in production code, the class is not thread safe.
    /// </summary>
    public class InMemoryClientStoreDataProvider : IClientStoreDataProvider
    {
        private readonly List<Client> _clients = new List<Client>();
        
        public Tasks.Task<Client[]> LoadClientsByAuthorityAsync(string authority, CancellationToken cancellation)
        {
            return Tasks.Task.FromResult(_clients
                .Where(c => c.Authority == authority)
                .Select(x=>x.Clone())
                .ToArray());
        }

        public Tasks.Task<Client[]> LoadClientsByRepositoryAsync(string repositoryHost, CancellationToken cancellation)
        {
            return Tasks.Task.FromResult(_clients
                .Where(c => c.Repository == repositoryHost)
                .Select(x => x.Clone())
                .ToArray());
        }

        public Tasks.Task SaveClientAsync(Client client, CancellationToken cancellation)
        {
            var existing = _clients.FirstOrDefault(c => c.ClientId == client.ClientId);
            if (existing != null)
                _clients.Remove(existing);

            _clients.Add(client.Clone());

            return Tasks.Task.CompletedTask;
        }

        public Tasks.Task SaveSecretAsync(string clientId, ClientSecret secret, CancellationToken cancellation)
        {
            var client = _clients.First(c => c.ClientId == clientId);

            // update: remove existing object and add the new one (NOT thread safe)
            var existingSecret = client.Secrets.FirstOrDefault(s => s.Id == secret.Id);
            if (existingSecret != null)
                client.Secrets.Remove(existingSecret);

            client.Secrets.Add(secret.Clone());

            return Tasks.Task.CompletedTask;
        }

        public Tasks.Task DeleteClientAsync(string clientId, CancellationToken cancellation)
        {
            _clients.RemoveAll(c => c.ClientId == clientId);
            return Tasks.Task.CompletedTask;
        }

        public Tasks.Task DeleteClientByRepositoryHostAsync(string repositoryHost, CancellationToken cancellation)
        {
            _clients.RemoveAll(c => c.Repository == repositoryHost);
            return Tasks.Task.CompletedTask;
        }

        public Tasks.Task DeleteSecretAsync(string clientId, string secretId, CancellationToken cancellation)
        {
            var client = _clients.FirstOrDefault(c => c.ClientId == clientId);
            var secret = client?.Secrets.FirstOrDefault(s => s.Id == secretId);
            if (secret != null)
                client.Secrets.Remove(secret);

            return Tasks.Task.CompletedTask;
        }
    }
}
