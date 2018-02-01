using System;
using SenseNet.ContentRepository.Security;

namespace SenseNet.Services.Virtualization
{
    public interface IUltimateLogoutSupplier
    {
        void Logout(bool ultimateLogout);
        Func<string, PortalPrincipal> LoadPortalPrincipalForLogout { get; set; }
    }
}