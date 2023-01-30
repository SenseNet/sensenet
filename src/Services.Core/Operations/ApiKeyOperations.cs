using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Security.ApiKeys;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.Services.Core.Operations
{
    public static class ApiKeyOperations
    {
        /// <summary>
        /// Gets API keys related to the target user.
        /// </summary>
        /// <snCategory>Authentication</snCategory>
        /// <param name="content"></param>
        /// <param name="context"></param>
        /// <returns>An object containing an array of API keys related to the target user.</returns>
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

        /// <summary>
        /// Creates an api key for the target user.
        /// </summary>
        /// <snCategory>Authentication</snCategory>
        /// <exception cref="SenseNetSecurityException">Thrown when the caller does not have enough permissions
        /// to manage the API keys of the target user.</exception>
        /// <param name="content"></param>
        /// <param name="context"></param>
        /// <returns>The newly created API key.</returns>
        [ODataAction]
        [ContentTypes(N.CT.User)]
        [AllowedRoles(N.R.All)]
        public static async Task<ApiKey> CreateApiKey(Content content, HttpContext context)
        {
            var akm = context.RequestServices.GetRequiredService<IApiKeyManager>();
            var apiKey = await akm.CreateApiKeyAsync(content.Id, DateTime.UtcNow.AddYears(1), context.RequestAborted).ConfigureAwait(false);

            return apiKey;
        }

        /// <summary>
        /// Deletes an API key.
        /// </summary>
        /// <snCategory>Authentication</snCategory>
        /// <exception cref="SenseNetSecurityException">Thrown when the caller does not have enough permissions
        /// to manage the API keys of the target user.</exception>
        /// <param name="content"></param>
        /// <param name="context"></param>
        /// <param name="apiKey">API key identifier.</param>
        /// <returns>An empty result.</returns>
        /// <exception cref="ArgumentNullException"></exception>
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

        /// <summary>
        /// Deletes all api keys.
        /// </summary>
        /// <snCategory>Authentication</snCategory>
        /// <exception cref="SenseNetSecurityException">Thrown when the caller does not have enough permissions
        /// to manage the API keys of the target user.</exception>
        /// <param name="content"></param>
        /// <param name="context"></param>
        /// <returns>An empty result.</returns>
        [ODataAction]
        [ContentTypes(N.CT.PortalRoot)]
        [AllowedRoles(N.R.Administrators, N.R.PublicAdministrators)]
        public static System.Threading.Tasks.Task DeleteApiKeys(Content content, HttpContext context)
        {
            var akm = context.RequestServices.GetRequiredService<IApiKeyManager>();
            return akm.DeleteApiKeysAsync(context.RequestAborted);
        }
        /// <summary>
        /// Deletes api keys of the target user.
        /// </summary>
        /// <snCategory>Authentication</snCategory>
        /// <exception cref="SenseNetSecurityException">Thrown when the caller does not have enough permissions
        /// to manage the API keys of the target user.</exception>
        /// <param name="content"></param>
        /// <param name="context"></param>
        /// <returns>An empty result.</returns>
        [ODataAction(OperationName = "DeleteApiKeys")]
        [ContentTypes(N.CT.User)]
        [AllowedRoles(N.R.Administrators, N.R.PublicAdministrators)]
        public static System.Threading.Tasks.Task DeleteApiKeysByUser(Content content, HttpContext context)
        {
            var akm = context.RequestServices.GetRequiredService<IApiKeyManager>();
            return akm.DeleteApiKeysByUserAsync(content.Id, context.RequestAborted);
        }
    }
}
