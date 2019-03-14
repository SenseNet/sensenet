using System;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Tools;

namespace SenseNet.ContentRepository.Storage.Data
{
    public static class AccessTokenDataProviderExtensions
    {
        public static IRepositoryBuilder UseAccessTokenDataProviderExtension(this IRepositoryBuilder builder, IAccessTokenDataProviderExtension provider)
        {
            DataProvider.Instance.SetExtension(typeof(IAccessTokenDataProviderExtension), provider);
            return builder;
        }
    }

    public interface IAccessTokenDataProviderExtension : IDataProviderExtension
    {
        void DeleteAllAccessTokens();
        void SaveAccessToken(AccessToken token);
        AccessToken LoadAccessTokenById(int accessTokenId);
        AccessToken LoadAccessToken(string tokenValue, int contentId, string feature);
        AccessToken[] LoadAccessTokens(int userId);
        void UpdateAccessToken(string tokenValue, DateTime newExpirationDate);
        void DeleteAccessToken(string tokenValue);
        void DeleteAccessTokensByUser(int userId);
        void DeleteAccessTokensByContent(int contentId);
        void CleanupAccessTokens();
    }
}
