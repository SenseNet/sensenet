using System;
using System.Security.Claims;
using System.Threading.Tasks;
using SenseNet.ContentRepository;

namespace SenseNet.Services.Core.Authentication
{
    public class AuthenticationOptions
    {
        /// <summary>
        /// Url of the authentication authority - for example IdentityServer.
        /// </summary>
        public string Authority { get; set; }

        /// <summary>
        /// Add a cookie containing the JWT bearer token if it was sent in the
        /// request header. If this cookie is sent by the client later and
        /// there is no authorization header, the system will set the value
        /// in the header.
        /// Use this setting only if you need to authenticate requests (e.g file
        /// download) where it is not possible to send the JWT token in the header.
        /// Default is false.
        /// </summary>
        public bool AddJwtCookie { get; set; }

        /// <summary>
        /// Optional custom method for loading the appropriate user from the repository
        /// at the beginning of a request based on the claims received.
        /// </summary>
        public Func<ClaimsPrincipal, Task<User>> FindUserAsync { get; set; }
    }
}
