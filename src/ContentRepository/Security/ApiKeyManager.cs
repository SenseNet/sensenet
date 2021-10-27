using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.ContentRepository.Security
{
    public class ApiKey
    {
        public string Value { get; internal set; }
        public DateTime CreationDate { get; internal set; } //UNDONE: do we need creation date here? This class is mainly for cache.
        public DateTime ExpirationDate { get; internal set; }
    }

    internal static class ApiKeyExtensions
    {
        public static ApiKey ToApiKey(this AccessToken token)
        {
            if (token == null)
                return null;

            return new ApiKey
            {
                Value = token.Value,
                CreationDate = token.CreationDate,
                ExpirationDate = token.ExpirationDate
            };
        }
    }

    public interface IApiKeyManager
    {
        Task<User> GetUserByApiKeyAsync(string apiKey, CancellationToken cancel);
        Task<ApiKey[]> GetApiKeysByUserAsync(int userId, CancellationToken cancel);
        Task<ApiKey> CreateApiKeyAsync(int userId, DateTime expiration, CancellationToken cancel);
        System.Threading.Tasks.Task DeleteApiKeyAsync(string apiKey, CancellationToken cancel);
    }
    
    //UNDONE: implement api key manager
    internal class ApiKeyManager : IApiKeyManager
    {
        private const string FeatureName = "apikey";

        public async Task<User> GetUserByApiKeyAsync(string apiKey, CancellationToken cancel)
        {
            //UNDONE: load token from cache
            var token = await AccessTokenVault.GetTokenAsync(apiKey, cancel).ConfigureAwait(false);
            if (token == null)
                return null;

            return await Node.LoadAsync<User>(token.UserId, cancel);
        }

        public async Task<ApiKey[]> GetApiKeysByUserAsync(int userId, CancellationToken cancel)
        {
            //UNDONE: security: check permissions for user!!!!! (Save?)

            // do not load tokens from cache, this is for editing tokens
            var allTokens = await AccessTokenVault.GetAllTokensAsync(userId, cancel).ConfigureAwait(false);

            return allTokens
                .Where(t => t.Feature == FeatureName)
                .Select(t => t.ToApiKey())
                .ToArray();
        }

        public async Task<ApiKey> CreateApiKeyAsync(int userId, DateTime expiration, CancellationToken cancel)
        {
            var now = DateTime.UtcNow;
            var exp = expiration > now
                ? expiration - now
                : TimeSpan.FromDays(365);

            var token = await AccessTokenVault.CreateTokenAsync(userId, exp, 0, FeatureName, cancel).ConfigureAwait(false);

            return token.ToApiKey();
        }

        public System.Threading.Tasks.Task DeleteApiKeyAsync(string apiKey, CancellationToken cancel)
        {
            return AccessTokenVault.DeleteTokenAsync(apiKey, cancel);
        }
    }
}
