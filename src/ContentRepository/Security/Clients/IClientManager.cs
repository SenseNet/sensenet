using System;
using System.Threading;
using STT=System.Threading.Tasks;

namespace SenseNet.ContentRepository.Security.Clients
{
    /// <summary>
    /// Defines methods for managing clients and secrets.
    /// </summary>
    public interface IClientManager
    {
        STT.Task<Client[]> GetClientsAsync(CancellationToken cancel);
        STT.Task<Client> CreateClientAsync(string name, ClientType type, string userName, CancellationToken cancel);
        STT.Task DeleteClientAsync(string clientId, CancellationToken cancel);
        STT.Task<ClientSecret> CreateSecretAsync(string clientId, DateTime? validTill, CancellationToken cancel);
        STT.Task DeleteSecretAsync(string clientId, string secretId, CancellationToken cancel);
        STT.Task<ClientSecret> RegenerateSecretAsync(string clientId, string secretId, DateTime? validTill, CancellationToken cancel);
    }    
}
