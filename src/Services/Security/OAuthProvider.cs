using System.Web;

namespace SenseNet.ContentRepository.Security
{
    /// <summary>
    /// Base class for implementing OAuth authentication provided by a 3rd party service.
    /// </summary>
    public abstract class OAuthProvider : IOAuthProvider
    {
        /// <summary>
        /// Name of the field that should be defined on the User content type for holding
        /// the unique user identifier provided by the 3rd party OAuth service.
        /// </summary>
        public abstract string IdentifierFieldName { get; }
        /// <summary>
        /// Short name of the provider implementation (e.g. 'google', 'facebook'). It will be used
        /// by the client to send OAuth requests to the server and by the server to find the
        /// appropriate OAuth provider for the request.
        /// </summary>
        public abstract string ProviderName { get; }

        /// <summary>
        /// Extracts token data from the request send by the client and tries to verify
        /// the validity of the token by the 3rd party service.
        /// </summary>
        /// <param name="request">Http request containing token data.</param>
        /// <param name="tokenData">Extracted and formatted token data that will be
        /// provided later for the GetUserData method.</param>
        /// <returns>Unique user identifier in the 3rd party service.</returns>
        public abstract string VerifyToken(HttpRequestBase request, out object tokenData);
        /// <summary>
        /// Assemble a user data that will be used to fill user fields 
        /// when a new user is created in the Content Repository.
        /// It is called only when a new user is created.
        /// </summary>
        /// <param name="tokenData">Token data object that was previously created by the VerifyToken method.</param>
        /// <returns>User identity information.</returns>
        public abstract IOAuthIdentity GetUserData(object tokenData);
    }
}
