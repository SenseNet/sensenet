using System;
using SenseNet.ContentRepository.Security;

namespace SenseNet.Services.Virtualization
{
    /// <summary>
    /// An ultimate logout feature, that helps users to log out from their sessions on all workstations they were logged in.
    /// </summary>
    internal interface ILogoutExecutor
    {
        /// <summary>
        /// Executes the logout process.
        /// </summary>
        /// <param name="ultimateLogout">Whether this should be an ultimate logout. If set to True, the user will be logged out from all clients.</param>
        void Logout(bool ultimateLogout);

        /// <summary>
        /// Loads the portal principal from the repository by the given user name.
        /// </summary>
        Func<string, PortalPrincipal> LoadPortalPrincipalForLogout { get; set; }
    }
}