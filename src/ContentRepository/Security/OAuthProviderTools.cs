using System;

namespace SenseNet.ContentRepository.Security
{
    [Obsolete("This feature is obsolete. Use newer authentication methods.", true)]
    public static class OAuthProviderTools
    {
        private const string ProviderNamePrefix = "oauth-";

        /// <summary>
        /// Central method for generating a feature-specific provider name 
        /// (e.g. 'oauth-google') for identifying provider instances.
        /// </summary>
        public static string GetProviderRegistrationName(string providerName)
        {
            return ProviderNamePrefix + providerName;
        }
    }
}
