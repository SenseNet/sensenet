using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Security;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Portal.Virtualization;
using SenseNet.Search;

namespace SenseNet.Services.Virtualization
{
    internal class SafeQueries : ISafeQueryHolder
    {
        public static string UsersByOAuthId => "+TypeIs:User +@0:@1";
    }

    public class OAuthManager
    {
        private const string OAuthPathLogin = "/sn-oauth/login";
        private const string OAuthPathCallback = "/sn-oauth/callback";

        //UNDONE: get it from the Providers class when available
        public static OAuthManager Instance = new OAuthManager();

        protected virtual OAuthProvider GetProvider(string name)
        {
            //UNDONE: where do we set these providers?
            return Providers.Instance.GetProvider<OAuthProvider>("oauth-" + name);
        }
    
        public bool Authenticate(HttpApplication application)
        {
            var request = AuthenticationHelper.GetRequest(application);
            var isLoginRequest = IsLoginRequest(request);
            var isCallbackRequest = IsCallbackRequest(request);
            if (!isLoginRequest && !isCallbackRequest)
                return false;

            var providerName = GetProviderName(request);
            if (string.IsNullOrEmpty(providerName))
                throw new InvalidOperationException("Provider parameter is missing.");

            var provider = this.GetProvider(providerName);
            if (provider == null)
                throw new InvalidOperationException("OAuth provider not found: " + providerName);

            //UNDONE: get the token from the request 
            // Load and pass the whole body stream, extracting the token is the 
            // reposonsibility of the provider.

            //string body;
            //using (var reader = new StreamReader(request.InputStream))
            //{
            //    body = reader.ReadToEnd();
            //}

            object tokenData;
            var userId = provider.VerifyToken(request, out tokenData);
            if (string.IsNullOrEmpty(userId))
                return false;

            var fieldName = provider.IdentifierFieldName;
            User user;

            using (new SystemAccount())
            {
                user = ContentQuery.Query(SafeQueries.UsersByOAuthId, QuerySettings.AdminSettings, fieldName, userId)
                    .Nodes.FirstOrDefault() as User;

                if (user == null)
                {
                    // create user
                    var userData = provider.GetUserData(tokenData);

                    //UNDONE: configurable user parent (domain or org unit)
                    var parent = Node.LoadNode(RepositoryPath.Combine(RepositoryStructure.ImsFolderPath,
                        IdentityManagement.DefaultDomain));

                    //UNDONE: configurable user content type
                    var userContent = Content.CreateNew("User", parent, userData.Username);
                    userContent[fieldName] = userId;
                    userContent["Enabled"] = true;
                    if (!string.IsNullOrEmpty(userData.Email))
                        userContent["Email"] = userData.Email;

                    userContent.Save();

                    user = userContent.ContentHandler as User;
                }
            }

            application.Context.User = new PortalPrincipal(user);
            return true;
        }

        internal bool IsLoginRequest(HttpRequestBase request)
        {
            var uri = request?.Url?.AbsolutePath;
            return string.Equals(uri, OAuthPathLogin, StringComparison.InvariantCultureIgnoreCase);
        }
        internal bool IsCallbackRequest(HttpRequestBase request)
        {
            var uri = request?.Url?.AbsolutePath;
            return string.Equals(uri, OAuthPathCallback, StringComparison.InvariantCultureIgnoreCase);
        }

        internal string GetProviderName(HttpRequestBase request)
        {
            return request?["provider"] ?? string.Empty;
        }
    }
}
