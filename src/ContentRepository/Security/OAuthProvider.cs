using System.Web;

namespace SenseNet.ContentRepository.Security
{
    public abstract class OAuthProvider
    {
        public abstract string IdentifierFieldName { get; }
        public abstract string ProviderName { get; }

        public abstract string VerifyToken(HttpRequestBase request, out object tokenData);
        public abstract IOAuthIdentity GetUserData(object tokenData);
    }
}
