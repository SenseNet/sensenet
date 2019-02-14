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
        /// <summary>
        /// Loads all non-expired tokens that fulfill all the provided criteria.
        /// This method cannot be used to load all tokens for the user: to achieve that,
        /// please use the LoadAccessTokens(int userId) overload.
        /// </summary>
        AccessToken[] LoadAccessTokens(int userId, int contentId, string feature);
        void UpdateAccessToken(string tokenValue, DateTime newExpirationDate);
        void DeleteAccessToken(string tokenValue);
        void DeleteAccessTokensByUser(int userId);
        void DeleteAccessTokensByContent(int contentId);
    }
}
