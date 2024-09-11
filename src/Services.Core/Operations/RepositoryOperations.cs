using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;

namespace SenseNet.Services.Core.Operations
{
    // SnDocs: legacy option class, not documented
    public class RepositoryTypeOptions
    {
        public string RepositoryType { get; set; } = "standalone";
    }

    public static class RepositoryOperations
    {
        /// <summary>
        /// Gets the type of the repository. This is 'standalone' by default.
        /// </summary>
        /// <param name="content"></param>
        /// <param name="context"></param>
        /// <returns>The type of the repository.</returns>
        [ODataFunction(Category = "Other")]
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
