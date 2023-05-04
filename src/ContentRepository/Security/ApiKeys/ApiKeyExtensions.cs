using SenseNet.ContentRepository.Storage.Security;
using System.Threading.Tasks;
using System.Threading;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.ContentRepository.Security.ApiKeys
{
    public static class ApiKeyExtensions
    {
        internal static ApiKey ToApiKey(this AccessToken token)
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

        public static async Task<User> GetUserByApiKeyAsync(this IApiKeyManager apiKeyManager, string apiKey, CancellationToken cancel)
        {
            var userId = await apiKeyManager.GetUserIdByApiKeyAsync(apiKey, cancel).ConfigureAwait(false);
            if (!userId.HasValue)
                return null;

            return await Node.LoadAsync<User>(userId.Value, cancel);
        }
    }
}
