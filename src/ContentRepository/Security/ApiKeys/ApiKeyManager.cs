using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.ContentRepository.Security.ApiKeys
{
    public class ApiKey
    {
        public string Value { get; internal set; }
        public DateTime CreationDate { get; internal set; }
        public DateTime ExpirationDate { get; internal set; }
    }

    /// <summary>
    /// Default implementation of <see cref="IApiKeyManager"/> that uses the <see cref="AccessTokenVault"/>
    /// api to manage api keys.
    /// </summary>
    internal class ApiKeyManager : IApiKeyManager
    {
        private const string FeatureName = "apikey";
        private readonly ILogger _logger;
        private readonly MemoryCache _apiKeyCache = new(new MemoryCacheOptions { SizeLimit = 1024 });

        public ApiKeyManager(ILogger<ApiKeyManager> logger)
        {
            _logger = logger;
        }

        public async Task<User> GetUserByApiKeyAsync(string apiKey, CancellationToken cancel)
        {
            if (string.IsNullOrEmpty(apiKey))
                return null;

            if (!_apiKeyCache.TryGetValue<AccessToken>(apiKey, out var token))
            {
                token = await AccessTokenVault.GetTokenAsync(apiKey, 0, FeatureName, cancel).ConfigureAwait(false);
                
                // check if expired
                if (token != null && token.ExpirationDate < DateTime.UtcNow)
                    token = null;

                // cache for 2 minutes
                if (token != null)
                    _apiKeyCache.Set(apiKey, token, new MemoryCacheEntryOptions
                    {
                        AbsoluteExpiration = new DateTimeOffset(DateTime.UtcNow.AddMinutes(2)),
                        Size = 1
                    });
            }

            if (token == null || token.ExpirationDate <= DateTime.UtcNow)
                return null;

            AssertPermissions(token.UserId);

            return await Node.LoadAsync<User>(token.UserId, cancel);
        }

        public async Task<ApiKey[]> GetApiKeysByUserAsync(int userId, CancellationToken cancel)
        {
            // in case of missing permissions: not an error, just return an empty list
            if (userId < 1 || !IsUserAllowed(userId))
                return Array.Empty<ApiKey>();
            
            // do not load tokens from cache, this is for editing tokens
            var allTokens = await AccessTokenVault.GetAllTokensAsync(userId, cancel).ConfigureAwait(false);

            return allTokens
                .Where(t => t.Feature == FeatureName)
                .Select(t => t.ToApiKey())
                .ToArray();
        }

        public async Task<ApiKey> CreateApiKeyAsync(int userId, DateTime expiration, CancellationToken cancel)
        {
            if (userId < 1)
                throw new InvalidOperationException($"ApiKeyManager: Invalid user id: {userId}");

            AssertPermissions(userId);

            var now = DateTime.UtcNow;
            var exp = expiration > now
                ? expiration - now
                : TimeSpan.FromDays(365);

            var token = await AccessTokenVault.CreateTokenAsync(userId, exp, 0, FeatureName, cancel).ConfigureAwait(false);

            return token.ToApiKey();
        }

        public async System.Threading.Tasks.Task DeleteApiKeyAsync(string apiKey, CancellationToken cancel)
        {
            if (string.IsNullOrEmpty(apiKey))
                return;

            // load the token to check permissions for the user it belongs to
            var token = await AccessTokenVault.GetTokenAsync(apiKey, 0, FeatureName, cancel).ConfigureAwait(false);
            if (token == null)
                return;

            AssertPermissions(token.UserId);

            await AccessTokenVault.DeleteTokenAsync(apiKey, cancel).ConfigureAwait(false);
        }

        public System.Threading.Tasks.Task DeleteApiKeysByUserAsync(int userId, CancellationToken cancel)
        {
            if (userId < 1)
                return System.Threading.Tasks.Task.CompletedTask;

            AssertPermissions(userId);
            
            return AccessTokenVault.DeleteTokensAsync(userId, 0, FeatureName, cancel);
        }

        public System.Threading.Tasks.Task DeleteApiKeysAsync(CancellationToken cancel)
        {
            // user id: -1 or 1
            if (Math.Abs(User.Current.Id) != 1)
                throw new SenseNetSecurityException("Only administrators may delete all api keys.");

            return AccessTokenVault.DeleteTokensByFeatureAsync(FeatureName, cancel);
        }

        private void AssertPermissions(int userId)
        {
            if (IsUserAllowed(userId)) 
                return;

            _logger.LogWarning($"ApiKeyManager: {User.Current.Name} tried to manage api keys of user {userId} without Save permissions.");
            throw new SenseNetSecurityException(userId, $"Current user ({User.Current.Name}) does not have enough permissions " +
                                                        $"to manage api keys. Save permission is required.");
        }
        private static bool IsUserAllowed(int userId)
        {
            var currentUser = AccessProvider.Current.GetOriginalUser();

            // Users can manage API keys of their own, or if they have Save permissions on the user.
            return currentUser.Id == userId ||
                   Providers.Instance.SecurityHandler.HasPermission(userId, PermissionType.Save);
        }
    }
}
