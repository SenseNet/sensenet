using System.Security.Principal;
using System.Web;

namespace SenseNet.Services.Virtualization
{
    public abstract class OAuthProvider
    {
        public abstract string IdentifierFieldName { get; }

        public abstract string VerifyToken(HttpRequestBase request, out object tokenData);
        public abstract IOAuthIdentity GetUserData(object tokenData);
    }
}
