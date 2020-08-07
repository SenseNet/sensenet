using Microsoft.AspNetCore.Http;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.Services.Core
{
    public interface IMembershipExtender
    {
        MembershipExtension GetExtension(IUser user, HttpContext httpContext);
    }
}
