using System;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Tools;

namespace SenseNet.ContentRepository.Storage.Data
{
    public static class AccessTokenDataProviderExtensions
    {
        public static IRepositoryBuilder UseAccessTokenDataProviderExtension(this IRepositoryBuilder builder, IAccessTokenDataProviderExtension provider)
        {
            DataStore.DataProvider.SetExtension(typeof(IAccessTokenDataProviderExtension), provider);
            return builder;
        }
    }

    public interface IAccessTokenDataProviderExtension : IDataProviderExtension
    {
        Task DeleteAllAccessTokensAsync(CancellationToken cancellationToken);
        Task SaveAccessTokenAsync(AccessToken token, CancellationToken cancellationToken);
        Task<AccessToken> LoadAccessTokenByIdAsync(int accessTokenId, CancellationToken cancellationToken);
        Task<AccessToken> LoadAccessTokenAsync(string tokenValue, int contentId, string feature, CancellationToken cancellationToken);
        Task<AccessToken[]> LoadAccessTokensAsync(int userId, CancellationToken cancellationToken);
        Task UpdateAccessTokenAsync(string tokenValue, DateTime newExpirationDate, CancellationToken cancellationToken);
        Task DeleteAccessTokenAsync(string tokenValue, CancellationToken cancellationToken);
        Task DeleteAccessTokensByUserAsync(int userId, CancellationToken cancellationToken);
        Task DeleteAccessTokensByContentAsync(int contentId, CancellationToken cancellationToken);
        Task CleanupAccessTokensAsync(CancellationToken cancellationToken);
    }
}
