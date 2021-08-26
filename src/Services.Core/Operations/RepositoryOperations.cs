using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;

namespace SenseNet.Services.Core.Operations
{
    public class RepositoryTypeOptions
    {
        public string RepositoryType { get; set; } = "standalone";
    }

    public static class RepositoryOperations
    {
        [ODataFunction]
        [ContentTypes(N.CT.PortalRoot)]
        [AllowedRoles(N.R.All)]
        public static object GetRepositoryType(Content content, HttpContext context)
        {
            var rto = context.RequestServices.GetRequiredService<IOptions<RepositoryTypeOptions>>().Value;
            
            return new
            {
                repositoryType = rto.RepositoryType
            };
        }
    }
}
