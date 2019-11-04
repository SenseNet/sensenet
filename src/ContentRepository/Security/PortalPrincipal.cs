using System;
using System.Security.Claims;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.ContentRepository.Security
{
    /// <summary>
    /// An <see cref="System.Security.Principal.IPrincipal" /> implementation that supports multiple claims-based identities.
    /// </summary>
    /// <seealso cref="System.Security.Claims.ClaimsPrincipal" />
    public class PortalPrincipal : ClaimsPrincipal
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PortalPrincipal" /> class.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <exception cref="ArgumentNullException">user - The user parameter cannot be null. Use 'User.Visitor' instead.</exception>
        public PortalPrincipal(IUser user)
            : base(user ?? throw new ArgumentNullException(nameof(user), "User cannot be null. Use 'User.Visitor' instead."))
        {
        }

        /// <summary>
        /// Returns a value that indicates whether the entity (user) represented by this claims principal is in the specified role.
        /// </summary>
        /// <param name="role">The role for which to check.</param>
        /// <returns>
        /// true if claims principal is in the specified role; otherwise, false.
        /// </returns>
        /// <exception cref="NotSupportedException">Role management is not supported.</exception>
        public override bool IsInRole(string role)
            => throw new NotSupportedException("Role management is not supported.");
    }
}