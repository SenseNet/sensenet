using Microsoft.AspNetCore.Http;
using SenseNet.ContentRepository;
using SenseNet.Services.Core.Operations;
using System.Threading;
using System.Threading.Tasks;

namespace SenseNet.Services.Core.Authentication
{
    /// <summary>
    /// Defines methods for registering users in the repository either by
    /// username/password, or by an external provider. The latter is 
    /// usually one of the main services out there - e.g. Google,
    /// Facebook, MS Azure or GitHub. 
    /// </summary>
    /// <remarks>
    /// This interface is not about authentication: it defines methods
    /// only for creating users.
    /// Developers may implement this if they want a custom mechanism 
    /// for creating users automatically during the registration process 
    /// - for example setting additional fields, creating users with 
    /// a different type or in a different container.
    /// </remarks>
    public interface IRegistrationProvider
    {
        /// <summary>
        /// Create users based on the auth provider that they used for signing in
        /// - e.g. Google or GitHub.
        /// </summary>
        Task<User> CreateProviderUserAsync(Content content, HttpContext context, string provider, string userId, ClaimInfo[] claims, CancellationToken cancellationToken);
        /// <summary>
        /// Create local users using username and a password.
        /// </summary>
        Task<User> CreateLocalUserAsync(Content content, HttpContext context, string userName, string password, CancellationToken cancellationToken);
    }
}
