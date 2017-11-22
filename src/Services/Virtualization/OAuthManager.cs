using System;
using System.Linq;
using System.Web;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Security;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.Portal.Virtualization;
using SenseNet.Search;

namespace SenseNet.Services.Virtualization
{
    internal class SafeQueries : ISafeQueryHolder
    {
        public static string UsersByOAuthId => "+TypeIs:User +@0:@1";
    }

    internal class OAuthManager
    {
        private const string OAuthPathLogin = "/sn-oauth/login";
        private const string OAuthPathCallback = "/sn-oauth/callback";
        private const string SettingsName = "OAuth";
        private const string UserTypeSettingName = "UserType";
        private const string DomainSettingName = "Domain";

        internal static OAuthManager Instance = new OAuthManager();

        /// <summary>
        /// Derived classes may override this method and serve providers from a 
        /// different location - e.g. for testing purposes.
        /// </summary>
        /// <param name="providerName"></param>
        /// <returns></returns>
        protected virtual OAuthProvider GetProvider(string providerName)
        {
            return Providers.Instance.GetProvider<OAuthProvider>(OAuthProvider.GetProviderRegistrationName(providerName));
        }

        internal bool Authenticate(HttpApplication application)
        {
            var request = AuthenticationHelper.GetRequest(application);
            var isLoginRequest = IsLoginRequest(request);
            var isCallbackRequest = IsCallbackRequest(request);

            // Currently only login requests are implemented. In the future 
            // we may implement/handle server-side callback requests too.

            if (!isLoginRequest && !isCallbackRequest)
                return false;

            var providerName = GetProviderName(request);
            if (string.IsNullOrEmpty(providerName))
                throw new InvalidOperationException("Provider parameter is missing from the request.");

            var provider = GetProvider(providerName);
            if (provider == null)
                throw new InvalidOperationException("OAuth provider not found: " + providerName);

            string userId;
            object tokenData;

            try
            {
                userId = provider.VerifyToken(request, out tokenData);
            }
            catch (Exception ex)
            {
                SnTrace.Security.Write($"Unsuccessful OAuth token verification. Provider: {providerName}. Error: {ex.Message}");
                return false;
            }

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
                    var parent = LoadOrCreateUserParent(providerName);

                    var userContentType = Settings.GetValue(SettingsName, UserTypeSettingName, null, "User");
                    var userContent = Content.CreateNew(userContentType, parent, userData.Username);

                    if (!userContent.Fields.ContainsKey(fieldName))
                    {
                        var message = $"The {userContent.ContentType.Name} content type does not contain a field named {fieldName}. " + 
                            $"Please register this field before using the {providerName} OAuth provider.";
                        throw new InvalidOperationException(message);
                    }

                    userContent["LoginName"] = userData.Username;
                    userContent[fieldName] = userId;
                    userContent["Enabled"] = true;
                    userContent["FullName"] = userData.FullName ?? userData.Username;

                    if (!string.IsNullOrEmpty(userData.Email))
                        userContent["Email"] = userData.Email;

                    // If a user with the same name already exists, this will throw an exception
                    // so that the caller knows that the registration could not be completed.
                    userContent.Save();

                    user = userContent.ContentHandler as User;
                }
            }

            application.Context.User = new PortalPrincipal(user);
            return true;
        }

        private static bool IsLoginRequest(HttpRequestBase request)
        {
            var uri = request?.Url?.AbsolutePath;
            return string.Equals(uri, OAuthPathLogin, StringComparison.InvariantCultureIgnoreCase);
        }
        private static bool IsCallbackRequest(HttpRequestBase request)
        {
            var uri = request?.Url?.AbsolutePath;
            return string.Equals(uri, OAuthPathCallback, StringComparison.InvariantCultureIgnoreCase);
        }

        private static string GetProviderName(HttpRequestBase request)
        {
            return request?["provider"] ?? string.Empty;
        }

        private static Node LoadOrCreateUserParent(string providerName)
        {
            // E.g. /Root/IMS/Public/facebook
            var userDomain = Settings.GetValue(SettingsName, DomainSettingName, null, "Public");
            var domainPath = RepositoryPath.Combine(RepositoryStructure.ImsFolderPath, userDomain);
            var dummy = Node.LoadNode(domainPath) ??
                        RepositoryTools.CreateStructure(domainPath, "Domain")?.ContentHandler;
            var orgUnitPath = RepositoryPath.Combine(domainPath, providerName);
            var orgUnit = Node.LoadNode(orgUnitPath) ??
                         RepositoryTools.CreateStructure(orgUnitPath, "OrganizationalUnit")?.ContentHandler;

            return orgUnit;
        }
    }
}
