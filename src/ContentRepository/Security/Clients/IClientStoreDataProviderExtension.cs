using System.Threading;
using Tasks=System.Threading.Tasks;
using SenseNet.Data;

namespace SenseNet.ContentRepository.Security.Clients
{
    /// <summary>
    /// Defines methods for handling IdentityServer clients and their secrets.
    /// </summary>
    public interface IClientStoreDataProviderExtension : IDataProviderExtension
    {
        /// <summary>
        /// Loads all clients by <paramref name="repositoryHost"/>.
        /// </summary>
        /// <param name="repositoryHost">Host name of the repository without schema (e.g. "example.com").</param>
        /// <param name="cancellation">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the loaded <see cref="Client"/>
        /// instances.</returns>
        Tasks.Task<Client[]> LoadClientsByRepositoryAsync(string repositoryHost, CancellationToken cancellation);
        /// <summary>
        /// Loads all clients by <paramref name="authority"/>.
        /// </summary>
        /// <param name="authority">The authority address without schema (e.g. "is.example.com").</param>
        /// <param name="cancellation">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the loaded <see cref="Client"/>
        /// instances.</returns>
        Tasks.Task<Client[]> LoadClientsByAuthorityAsync(string authority, CancellationToken cancellation);
        /// <summary> 
        /// Saves a new or updates an existing client and synchronizes its secrets.
        /// If the client id is not filled, it will generate a new one
        /// and fill the property of the provided client.
        /// </summary>
        /// <param name="client">A client to save.</param>
        /// <param name="cancellation">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Tasks.Task SaveClientAsync(Client client, CancellationToken cancellation);
        /// <summary>
        /// Saves or updates a <see cref="ClientSecret"/> instance of an existing client
        /// identified by the <paramref name="clientId"/>.
        /// </summary>
        /// <param name="clientId">Id of the client.</param>
        /// <param name="secret">The secret data.</param>
        /// <param name="cancellation">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Tasks.Task SaveSecretAsync(string clientId, ClientSecret secret, CancellationToken cancellation);
        /// <summary>
        /// Deletes the <see cref="Client"/> identified by the given <paramref name="clientId"/>.
        /// The client's secrets will also be removed.
        /// </summary>
        /// <param name="clientId">Id of the client.</param>
        /// <param name="cancellation">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Tasks.Task DeleteClientAsync(string clientId, CancellationToken cancellation);
        /// <summary>
        /// Deletes all <see cref="Client"/> items of a repository identified by the given <paramref name="repositoryHost"/>.
        /// The clients' secrets will also be removed.
        /// </summary>
        /// <param name="repositoryHost">Host of the repository (e.g. "example.com").</param>
        /// <param name="cancellation">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Tasks.Task DeleteClientByRepositoryHostAsync(string repositoryHost, CancellationToken cancellation);

        /// <summary>
        /// Deletes the <see cref="ClientSecret"/> identified by the given <paramref name="secretId"/>.
        /// </summary>
        /// <param name="clientId">Id of the client that the secret belongs to.</param>
        /// <param name="secretId">The secret to delete.</param>
        /// <param name="cancellation">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Tasks.Task DeleteSecretAsync(string clientId, string secretId, CancellationToken cancellation);
    }
}
