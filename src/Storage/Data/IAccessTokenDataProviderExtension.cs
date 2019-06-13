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
            if (DataStore.Enabled)
                DataStore.DataProvider.SetExtension(typeof(IAccessTokenDataProviderExtension), provider);
            else
                DataProvider.Instance.SetExtension(typeof(IAccessTokenDataProviderExtension), provider); //DB:ok
            return builder;
        }
    }

    public interface IAccessTokenDataProviderExtension : IDataProviderExtension
    {
        Task DeleteAllAccessTokensAsync(CancellationToken cancellationToken = default(CancellationToken));
        Task SaveAccessTokenAsync(AccessToken token, CancellationToken cancellationToken = default(CancellationToken));
        Task<AccessToken> LoadAccessTokenByIdAsync(int accessTokenId, CancellationToken cancellationToken = default(CancellationToken));
        Task<AccessToken> LoadAccessTokenAsync(string tokenValue, int contentId, string feature, CancellationToken cancellationToken = default(CancellationToken));
        Task<AccessToken[]> LoadAccessTokensAsync(int userId, CancellationToken cancellationToken = default(CancellationToken));
        Task UpdateAccessTokenAsync(string tokenValue, DateTime newExpirationDate, CancellationToken cancellationToken = default(CancellationToken));
        Task DeleteAccessTokenAsync(string tokenValue, CancellationToken cancellationToken = default(CancellationToken));
        Task DeleteAccessTokensByUserAsync(int userId, CancellationToken cancellationToken = default(CancellationToken));
        Task DeleteAccessTokensByContentAsync(int contentId, CancellationToken cancellationToken = default(CancellationToken));
        Task CleanupAccessTokensAsync(CancellationToken cancellationToken = default(CancellationToken));
    }
}
