using Microsoft.AspNetCore.Http;
using SenseNet.ContentRepository;
using SenseNet.Services.Core.Operations;
using System.Threading;
using System.Threading.Tasks;

namespace SenseNet.Services.Core.Authentication
{
    public interface IRegistrationProvider
    {
        Task<User> CreateProviderUserAsync(Content content, HttpContext context, string provider, string userId, ClaimInfo[] claims, CancellationToken cancellationToken);
        Task<User> CreateLocalUserAsync(Content content, HttpContext context, string userName, string password, CancellationToken cancellationToken);
    }
}
