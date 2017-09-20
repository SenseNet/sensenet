using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using SenseNet.Configuration;
using SenseNet.Portal.Virtualization;

namespace SenseNet.Services.Virtualization
{
    public class OAuthManager
    {
        //UNDONE: get it from the Providers class when available
        public static OAuthManager Instance = new OAuthManager();

        protected virtual OAuthProvider GetProvider(string name)
        {
            //UNDONE: where do we set these providers?
            return Providers.Instance.GetProvider<OAuthProvider>("oauth-" + name);
        }
    
        private const string OAuthPath = "/sn-oauth";

        public bool Authenticate(HttpApplication application)
        {
            var request = AuthenticationHelper.GetRequest(application);
            if (!IsOAuthRequest(request))
                return false;

            var providerName = GetProviderName(request);
            if (string.IsNullOrEmpty(providerName))
                throw new InvalidOperationException("Provider parameter is missing.");

            var provider = this.GetProvider(providerName);

            //UNDONE: get the token from the request 
            // Load and pass the whole body stream, extracting the token is the 
            // reposonsibility of the provider.

            string body;
            using (var reader = new StreamReader(request.InputStream))
            {
                body = reader.ReadToEnd();
            }
            
            var principal = provider.VerifyToken(body);

            //UNDONE: implement user loading or creation
            // Who's responsibility is creating the user? The provider knows the user field 
            // where it stores its id, it knows about other fields (e.g. email) to fill.
            
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
