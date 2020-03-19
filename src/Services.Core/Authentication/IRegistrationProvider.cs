using Microsoft.AspNetCore.Http;
using SenseNet.ContentRepository;
using SenseNet.Services.Core.Operations;

namespace SenseNet.Services.Core.Authentication
{
    public interface IRegistrationProvider
    {
        User CreateProviderUser(Content content, HttpContext context, string provider, string userId, ClaimInfo[] claims);
        User CreateLocalUser(Content content, HttpContext context, string userName, string password);
    }
}
