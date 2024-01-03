using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.ContentRepository.Security.ApiKeys
{
    internal static class ApiKeyExtensions
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
    }
}
