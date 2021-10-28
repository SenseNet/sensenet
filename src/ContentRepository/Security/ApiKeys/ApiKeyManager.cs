using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
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

        public ApiKeyManager(ILogger<ApiKeyManager> logger)
        {
            _logger = logger;
        }

        public async Task<User> GetUserByApiKeyAsync(string apiKey, CancellationToken cancel)
        {
            //UNDONE: load token from cache
            var token = await AccessTokenVault.GetTokenAsync(apiKey, cancel).ConfigureAwait(false);
            if (token == null || token.ExpirationDate <= DateTime.UtcNow)
                return null;

            AssertPermissions(token.UserId);

            return await Node.LoadAsync<User>(token.UserId, cancel);
        }

        public async Task<ApiKey[]> GetApiKeysByUserAsync(int userId, CancellationToken cancel)
        {
            AssertPermissions(userId);

            // do not load tokens from cache, this is for editing tokens
            var allTokens = await AccessTokenVault.GetAllTokensAsync(userId, cancel).ConfigureAwait(false);

            return allTokens
                .Where(t => t.Feature == FeatureName)
                .Select(t => t.ToApiKey())
                .ToArray();
        }

        public async Task<ApiKey> CreateApiKeyAsync(int userId, DateTime expiration, CancellationToken cancel)
        {
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
            var token = await AccessTokenVault.GetTokenAsync(apiKey, cancel).ConfigureAwait(false);
            if (token == null)
                return;

            AssertPermissions(token.UserId);

            await AccessTokenVault.DeleteTokenAsync(apiKey, cancel).ConfigureAwait(false);
        }

        public System.Threading.Tasks.Task DeleteApiKeysAsync(bool expiredOnly, CancellationToken cancel)
        {
            //UNDONE: implement cleanup task that periodically deletes expired api keys
            throw new NotImplementedException();
        }

        private void AssertPermissions(int userId)
        {
            if (!SecurityHandler.HasPermission(userId, PermissionType.Save))
            {
                _logger.LogWarning($"ApiKeyManager: {User.Current.Name} tried to manage api keys of user {userId} without Save permissions.");
                throw new SenseNetSecurityException(userId, $"Current user ({User.Current.Name}) does not have enough permissions " +
                    $"to manage api keys. Save permission is required.");
            }
        }
    }
}
