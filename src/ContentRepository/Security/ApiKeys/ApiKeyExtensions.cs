using SenseNet.ContentRepository.Storage.Security;
using System;
using System.Collections.Generic;
using System.Text;

namespace SenseNet.ContentRepository.Security.ApiKeys
{
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
}
