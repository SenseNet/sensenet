using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SenseNet.Communication.Messaging;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Storage.Security;

namespace SenseNet.ContentRepository.Security.ApiKeys
{
    /// <summary>
    /// Default implementation of <see cref="IApiKeyManager"/> that uses the <see cref="AccessTokenVault"/>
    /// api to manage api keys.
    /// </summary>
    internal class ApiKeyManager : IApiKeyManager
    {
        private const string FeatureName = "apikey";
        private readonly ILogger _logger;
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly ApiKeysOptions _apiKeys;

        public ApiKeyManager(ILogger<ApiKeyManager> logger, IOptions<ApiKeysOptions> apiKeys)
        {
            _logger = logger;
            _apiKeys = apiKeys?.Value ?? new ApiKeysOptions();

            var now = DateTime.UtcNow;
            var apiKey = _apiKeys.HealthCheckerUser;
            if (!string.IsNullOrEmpty(apiKey))
            {
                _apiKeyCache.Set(apiKey, new AccessToken
                {
                    Id = -1,
                    UserId = -3,
                    ExpirationDate = now.AddYears(1),
                    CreationDate = now,
                    Feature = "/health"
                }, new MemoryCacheEntryOptions
                {
                    AbsoluteExpiration = new DateTimeOffset(now.AddYears(1)),
                    Size = 1
                });
            }
        }

        public async Task<IUser> GetUserByApiKeyAsync(string apiKey, CancellationToken cancel)
        {
            if (string.IsNullOrEmpty(apiKey))
                return null;

            MemoryCache cache;
            lock (_cacheLock)
                cache = _apiKeyCache;

            if (!cache.TryGetValue<AccessToken>(apiKey, out var token))
            {
                token = await AccessTokenVault.GetTokenAsync(apiKey, 0, FeatureName, cancel).ConfigureAwait(false);
                
                // check if expired
                if (token != null && token.ExpirationDate < DateTime.UtcNow)
                    token = null;

                // cache for 2 minutes
                if (token != null)
                    cache.Set(apiKey, token, new MemoryCacheEntryOptions
                    {
                        AbsoluteExpiration = new DateTimeOffset(DateTime.UtcNow.AddMinutes(2)),
                        Size = 1
                    });
            }


            if (token == null || token.UserId == 0 || token.ExpirationDate <= DateTime.UtcNow)
                return null;
            if(token.Id == -1 && token.UserId == -3 && token.Feature == "/health")
                return HealthCheckerUser.Instance;

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
            ResetCache();
        }

        public async System.Threading.Tasks.Task DeleteApiKeysByUserAsync(int userId, CancellationToken cancel)
        {
            if (userId < 1)
                return;

            AssertPermissions(userId);
            
            await AccessTokenVault.DeleteTokensAsync(userId, 0, FeatureName, cancel);
            ResetCache();
        }

        public async System.Threading.Tasks.Task DeleteApiKeysAsync(CancellationToken cancel)
        {
            // user id: -1 or 1
            if (Math.Abs(User.Current.Id) != 1)
                throw new SenseNetSecurityException("Only administrators may delete all api keys.");

            await AccessTokenVault.DeleteTokensByFeatureAsync(FeatureName, cancel);
            ResetCache();
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

        /* ==================================================================================== CACHE */

        private readonly object _cacheLock = new();
        private MemoryCache _apiKeyCache = CreateCache();

        private static MemoryCache CreateCache() => new(new MemoryCacheOptions { SizeLimit = 1024 });

        private void ResetCache()
        {
            new ApiKeyManagerCacheResetDistributedAction().ExecuteAsync(CancellationToken.None).GetAwaiter().GetResult();
        }
        private void ResetCachePrivate()
        {
            MemoryCache oldCache;
            lock (_cacheLock)
            {
                oldCache = _apiKeyCache;
                _apiKeyCache = CreateCache();
            }
            oldCache.Dispose();
        }

        [Serializable]
        internal sealed class ApiKeyManagerCacheResetDistributedAction : DistributedAction
        {
            public override string TraceMessage => null;

            public override System.Threading.Tasks.Task DoActionAsync(bool onRemote, bool isFromMe, CancellationToken cancellationToken)
            {
                // Local echo of my action: Return without doing anything
                if (onRemote && isFromMe)
                    return System.Threading.Tasks.Task.CompletedTask;
                var instance = Providers.Instance.Services.GetService<IApiKeyManager>() as ApiKeyManager;
                instance?.ResetCachePrivate();
                return System.Threading.Tasks.Task.CompletedTask;
            }
        }

    }
}
