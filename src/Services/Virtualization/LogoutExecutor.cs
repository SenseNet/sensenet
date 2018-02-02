using System;
using SenseNet.ContentRepository.Security;
using SenseNet.Portal.Virtualization;

namespace SenseNet.Services.Virtualization
{
    /// <summary>
    /// Executes logout process.
    /// </summary>
    internal class LogoutExecutor: ILogoutExecutor
    {
        public Func<string, PortalPrincipal> LoadPortalPrincipalForLogout { get; set; } = userName => AuthenticationHelper.LoadPortalPrincipal(userName);

        public void Logout(bool ultimateLogout)
        {
            AuthenticationHelper.Logout(ultimateLogout);
        }

    }
}