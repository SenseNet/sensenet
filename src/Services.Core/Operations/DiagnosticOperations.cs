using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Packaging;
using SenseNet.Storage.DataModel.Usage;

namespace SenseNet.Services.Core.Operations
{
    public static class DiagnosticOperations
    {
        /// <summary>
        /// Provides version information about all components / packages / assemblies of the running sensenet system.
        /// </summary>
        /// <snCategory>Other</snCategory>
        /// <remarks>
        /// For example:
        /// <code>
        /// {
        ///   "Components": [
        ///     {
        ///       "ComponentId": "SenseNet.Services",
        ///       "Version": "7.7.13.4",
        ///       "LatestVersion": "7.8",
        ///       "Description": "sensenet Services"
        ///     }
        ///   ],
        ///   "Assemblies": {
        ///     "SenseNet": [
        ///       {
        ///         "Name": "SenseNet.BlobStorage, Version=7.5.0.0, Culture=neutral, PublicKeyToken=null",
        ///         "IsDynamic": false,
        ///         "Version": "7.5.0.0 Debug"
        ///       },
        ///       {
        ///         "Name": "SenseNet.Security, Version=4.1.0.0, Culture=neutral, PublicKeyToken=null",
        ///         "IsDynamic": false,
        ///         "Version": "4.1.0.0 Release"
        ///       },
        ///       ...
        ///     ],
        ///     "Plugins": [ ... ],
        ///     "GAC": [...],
        ///     "Other": [...],
        ///     "Dynamic": [...]
        ///   },
        ///   "InstalledPackages": [
        ///     {
        ///       "Id": 1,
        ///       "Description": "sensenet Services",
        ///       "ComponentId": "SenseNet.Services",
        ///       "PackageType": 2,
        ///       "ReleaseDate": "2020-08-30T08:38:38.0209081Z",
        ///       "ExecutionDate": "2020-08-30T08:38:38.021009Z",
        ///       "ExecutionResult": 0,
        ///       "ComponentVersion": "7.7.13.4",
        ///       "ExecutionError": null,
        ///       "Manifest": null
        ///     }
        ///   ],
        ///   "DatabaseAvailable": true
        /// }
        /// </code>
        /// </remarks>
        /// <param name="content"></param>
        /// <param name="httpContext"></param>
        /// <returns>A <see cref="RepositoryVersionInfo"/> instance containing package, component, assembly
        /// versions.</returns>
        [ODataFunction]
        [ContentTypes(N.CT.PortalRoot)]
        [AllowedRoles(N.R.Administrators, N.R.Developers)]
        public static RepositoryVersionView GetVersionInfo(Content content, HttpContext httpContext)
        {
            var sharedVersionInfo = RepositoryVersionInfo.Instance;
            var components = sharedVersionInfo.Components.ToArray();
            var versionInfoView = new RepositoryVersionView
            {
                Components = components
                    .Select(component => new SnComponentView
                    {
                        ComponentId = component.ComponentId,
                        Version = component.Version,
                        Description = component.Description,
                        Dependencies = component.Dependencies,
                    })
                    .ToArray(),
                Assemblies = sharedVersionInfo.Assemblies,
                InstalledPackages = sharedVersionInfo.InstalledPackages,
                DatabaseAvailable = sharedVersionInfo.DatabaseAvailable
            };

            var componentStore = httpContext.RequestServices.GetService<ILatestComponentStore>()
                                 ?? new DefaultLatestComponentStore();

            foreach (var componentView in versionInfoView.Components)
                componentView.LatestVersion = componentStore.GetLatestVersion(componentView.ComponentId);

            return versionInfoView;
        }

        /// <summary>
        /// Gets database usage information about the repository.
        /// </summary>
        /// <param name="content"></param>
        /// <param name="httpContext"></param>
        /// <param name="force">True if the data should be refreshed from the database. Default: false</param>
        /// <returns>A <see cref="DatabaseUsage"/> object containing content, preview, binary
        /// and version count information.</returns>
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