using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.Services.Core.Operations
{
    /// <summary>
    /// Defines methods for providing dashboard data to admin clients.
    /// </summary>
    public interface IDashboardDataProvider
    {
        /// <summary>
        /// Gets dashboard data.
        /// </summary>
        /// <returns>An object containing dashboard data properties. This will be serialized
        /// as the response of the GetDashboardData action.</returns>
        Task<object> GetDashboardDataAsync();
    }

    public static class DashboardOperations
    {
        /// <summary>
        /// Gets dashboard data about this repository.
        /// </summary>
        /// <param name="content"></param>
        /// <param name="httpContext"></param>
        /// <returns>Dashboard data or an empty object.</returns>
        [ODataFunction(Category = "Other")]
        [ContentTypes(N.CT.PortalRoot)]
        [AllowedRoles(N.R.Everyone)]
        public static Task<object> GetDashboardData(Content content, HttpContext httpContext)
        {
            var ddp = httpContext.RequestServices.GetService<IDashboardDataProvider>();
            
            return ddp == null ? Task.FromResult(default(object)) : ddp.GetDashboardDataAsync();
        }
    }
}
