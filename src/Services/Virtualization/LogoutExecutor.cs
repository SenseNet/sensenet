using System;
using SenseNet.ContentRepository.Security;
using SenseNet.Portal.Virtualization;

namespace SenseNet.Services.Virtualization
{
    /// <inheritdoc />
    internal class LogoutExecutor: ILogoutExecutor
    {
        /// <inheritdoc />
        public Func<string, PortalPrincipal> LoadPortalPrincipalForLogout { get; set; } = userName => AuthenticationHelper.LoadPortalPrincipal(userName);

        /// <inheritdoc />
        public void Logout(bool ultimateLogout)
        {
            AuthenticationHelper.Logout(ultimateLogout);
        }
    }
}