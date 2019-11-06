// Copyright (c) SenseNet. All rights reserved.
// Licensed under the GNU GPL License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Security.Principal;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.ContentRepository.Security
{
    /// <summary>
    /// An <see cref="System.Security.Principal.IPrincipal" /> implementation that supports multiple claims-based identities.
    /// </summary>
    /// <seealso cref="System.Security.Claims.ClaimsPrincipal" />
    public class PortalPrincipal : ClaimsPrincipal
    {
        private readonly IUser _user;

        /// <summary>
        /// Initializes a new instance of the <see cref="PortalPrincipal" /> class.
        /// </summary>
        /// <param name="user">The primary user identity associated with this claims principal.</param>
        /// <exception cref="ArgumentNullException">user - The user parameter cannot be null. Use 'User.Visitor' instead.</exception>
        public PortalPrincipal(IUser user)
            : base(user)
        {
            _user = user ?? throw new ArgumentNullException(nameof(user), "User cannot be null. Use 'User.Visitor' instead.");
        }

        /// <summary>
        /// Gets the primary claims identity associated with this claims principal.
        /// </summary>
        public override IIdentity Identity => _user;

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