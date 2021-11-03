using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Security.ApiKeys;

namespace SenseNet.Services.Core.Operations
{
    public static class ApiKeyOperations
    {
        [ODataFunction]
        [ContentTypes(N.CT.User)]
        [AllowedRoles(N.R.All)]
        public static async Task<object> GetApiKeys(Content content, HttpContext context)
        {
            var akm = context.RequestServices.GetRequiredService<IApiKeyManager>();
            var apiKeys = await akm.GetApiKeysByUserAsync(content.Id, context.RequestAborted).ConfigureAwait(false);

            return new
            {
                apiKeys
            };
        }

        [ODataAction]
        [ContentTypes(N.CT.User)]
        [AllowedRoles(N.R.All)]
        public static async Task<ApiKey> CreateApiKey(Content content, HttpContext context)
        {
            var akm = context.RequestServices.GetRequiredService<IApiKeyManager>();
            var apiKey = await akm.CreateApiKeyAsync(content.Id, DateTime.UtcNow.AddYears(1), context.RequestAborted).ConfigureAwait(false);

            return apiKey;
        }

        [ODataAction]
        [ContentTypes(N.CT.User, N.CT.PortalRoot)]
        [AllowedRoles(N.R.All)]
        public static async System.Threading.Tasks.Task DeleteApiKey(Content content, HttpContext context, string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey))
                throw new ArgumentNullException(nameof(apiKey));

            var akm = context.RequestServices.GetRequiredService<IApiKeyManager>();
            await akm.DeleteApiKeyAsync(apiKey, context.RequestAborted).ConfigureAwait(false);
        }

        [ODataAction]
        [ContentTypes(N.CT.PortalRoot)]
        [AllowedRoles(N.R.Administrators)]
        public static System.Threading.Tasks.Task DeleteApiKeys(Content content, HttpContext context)
        {
            var akm = context.RequestServices.GetRequiredService<IApiKeyManager>();
            return akm.DeleteApiKeysAsync(context.RequestAborted);
        }
        [ODataAction(OperationName = "DeleteApiKeys")]
        [ContentTypes(N.CT.User)]
        [AllowedRoles(N.R.Administrators)]
        public static System.Threading.Tasks.Task DeleteApiKeysByUser(Content content, HttpContext context)
        {
            var akm = context.RequestServices.GetRequiredService<IApiKeyManager>();
            return akm.DeleteApiKeysByUserAsync(content.Id, context.RequestAborted);
        }
    }
}
