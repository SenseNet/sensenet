using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.Diagnostics;
using SenseNet.Storage.DataModel.Usage;

namespace SenseNet.Services.Core.Operations
{
    public static class DiagnosticOperations
    {
        [ODataFunction]
        [ContentTypes(N.CT.PortalRoot)]
        [AllowedRoles(N.R.Administrators, N.R.Developers)]
        public static async Task<DatabaseUsage> GetDatabaseUsage(Content content, HttpContext httpContext, bool force = false)
        {
            var logger = (ILogger<DatabaseUsageHandler>)httpContext
                .RequestServices.GetService(typeof(ILogger<DatabaseUsageHandler>));
            var handler = new DatabaseUsageHandler(logger); //UNDONE:<?usage: GetService
            return await handler.GetDatabaseUsageAsync(force, httpContext.RequestAborted).ConfigureAwait(false);
        }
    }
}