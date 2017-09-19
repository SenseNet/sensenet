using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using SenseNet.Portal.Virtualization;

namespace SenseNet.Services.Virtualization
{
    public class OAuthProviderFactory
    {
        //UNDONE: get it from the Providers class when available
        public static OAuthProviderFactory Instance = new OAuthProviderFactory();

        public OAuthProvider GetProvider(string name)
        {
            throw new NotImplementedException();
        }
    }

    public class OAuthProvider
    {
        private const string OAuthPath = "/sn-oauth";

        public bool Authenticate(HttpApplication application)
        {
            var request = AuthenticationHelper.GetRequest(application);
            if (!IsOAuthRequest(request))
                return false;

            var providerName = GetProviderName(request);
            if (string.IsNullOrEmpty(providerName))
                throw new InvalidOperationException("Provider parameter is missing.");

            //UNDONE:
            //var oap = OAuthProviderFactory.Instance.GetProvider(providerName);
            
            return false;
        }

        internal bool IsOAuthRequest(HttpRequestBase request)
        {
            var uri = request?.Url?.AbsolutePath;
            return string.Equals(uri, OAuthPath, StringComparison.InvariantCultureIgnoreCase);
        }

        internal string GetProviderName(HttpRequestBase request)
        {
            return request?["provider"] ?? string.Empty;
        }
    }
}
