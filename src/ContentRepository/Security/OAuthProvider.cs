using System.Web;

namespace SenseNet.ContentRepository.Security
{
    public abstract class OAuthProvider
    {
        private const string ProviderNamePrefix = "oauth-";

        public abstract string IdentifierFieldName { get; }
        public abstract string ProviderName { get; }

        public abstract string VerifyToken(HttpRequestBase request, out object tokenData);
        public abstract IOAuthIdentity GetUserData(object tokenData);

        //============================================================================ Helper methods

        internal string GetProviderRegistrationName()
        {
            return GetProviderRegistrationName(ProviderName);
        }
        public static string GetProviderRegistrationName(string providerName)
        {
            return ProviderNamePrefix + providerName;
        }
    }
}
