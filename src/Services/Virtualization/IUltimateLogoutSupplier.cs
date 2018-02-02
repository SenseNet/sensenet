using System;
using SenseNet.ContentRepository.Security;

namespace SenseNet.Services.Virtualization
{
    /// <summary>
    /// An ultimate logout feature, that helps users to log out from their sessions on all workstations they were logged in.
    /// </summary>
    internal interface IUltimateLogoutSupplier
    {
        /// <summary>
        /// Executes the logout process
        /// </summary>
        /// <param name="ultimateLogout">Tells whether it is an ultimate logout.</param>
        void Logout(bool ultimateLogout);

        /// <summary>
        /// Loads the portal principal from the repository by the given user name.
        /// </summary>
        Func<string, PortalPrincipal> LoadPortalPrincipalForLogout { get; set; }
    }
}