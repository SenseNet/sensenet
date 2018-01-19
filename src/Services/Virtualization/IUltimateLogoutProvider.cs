using System;
using SenseNet.ContentRepository.Security;

namespace SenseNet.Services.Virtualization
{
    public interface IUltimateLogoutProvider
    {
        void UltimateLogout(bool ultimateLogout);
        Func<string, PortalPrincipal> LoadPortalPrincipalForLogout { get; set; }
    }
}