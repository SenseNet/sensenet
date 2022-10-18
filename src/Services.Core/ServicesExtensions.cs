using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SenseNet.BackgroundOperations;
using SenseNet.Communication.Messaging;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Diagnostics;
using SenseNet.ContentRepository.Search;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Security.Clients;
using SenseNet.ContentRepository.Security.Cryptography;
using SenseNet.ContentRepository.Sharing;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.AppModel;
using SenseNet.ContentRepository.Storage.Caching;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.Portal.Virtualization;
using SenseNet.Search;
using SenseNet.Search.Querying;
using SenseNet.Security.Configuration;
using SenseNet.Services.Core;
using SenseNet.Services.Core.Authentication;
using SenseNet.Services.Core.Authentication.IdentityServer4;
using SenseNet.Services.Core.Configuration;
using SenseNet.Services.Core.Diagnostics;
using SenseNet.Services.Core.Operations;
using SenseNet.Storage;
using SenseNet.Storage.DistributedApplication.Messaging;
using SenseNet.Storage.Security;
using SenseNet.TaskManagement.Core;
using SenseNet.Tools;
using SenseNet.Tools.Diagnostics;
using Task = System.Threading.Tasks.Task;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    public static class ServicesExtensions
    {
        /// <summary>
        /// Registers well-known sensenet-related configuration objects based on the app configuration.
        /// Call this before adding sensenet services.
        /// </summary>
        /// <param name="services">The IServiceCollection instance.</param>
        /// <param name="configuration">The main app configuration instance.</param>
        internal static IServiceCollection ConfigureSenseNet(this IServiceCollection services, IConfiguration configuration)
        {
            // set the current app configuration as the global configuration for the legacy SnConfig api
            services.SetSenseNetConfiguration(configuration);
            
            services.Configure<DataOptions>(configuration.GetSection("sensenet:Data"));
            services.Configure<BlobStorageOptions>(configuration.GetSection("sensenet:BlobStorage"));
            services.Configure<TaskManagementOptions>(configuration.GetSection("sensenet:TaskManagement"));
            services.Configure<RegistrationOptions>(configuration.GetSection("sensenet:Registration"));
            services.Configure<AuthenticationOptions>(configuration.GetSection("sensenet:Authentication"));
            services.Configure<ClientStoreOptions>(configuration.GetSection("sensenet:Authentication"));
            services.Configure<ClientRequestOptions>(configuration.GetSection("sensenet:ClientRequest"));
            services.Configure<HttpRequestOptions>(configuration.GetSection("sensenet:HttpRequest"));
            services.Configure<ExclusiveLockOptions>(configuration.GetSection("sensenet:ExclusiveLock"));
            services.Configure<MessagingOptions>(configuration.GetSection("sensenet:security:messaging"));
            services.Configure<CryptographyOptions>(configuration.GetSection("sensenet:cryptography"));
            services.Configure<RepositoryTypeOptions>(options => {});
            
            services.ConfigureConnectionStrings(configuration);

            return services;
        }

        /// <summary>
        /// Adds sensenet-related services to the service collection. Registers
        /// a background service that will start the content repository when
        /// the application starts.
        /// </summary>
        /// <param name="services">The IServiceCollection instance.</param>
        /// <param name="configuration">The main app configuration instance.</param>
        /// <param name="buildRepository">Optional builder method for adding repository-related providers.</param>
        /// <param name="onRepositoryStartedAsync">Optional steps to take after the repository has started.</param>
        public static IServiceCollection AddSenseNet(this IServiceCollection services, IConfiguration configuration, 
            Action<RepositoryBuilder, IServiceProvider> buildRepository,
            Func<RepositoryInstance, IServiceProvider, Task> onRepositoryStartedAsync = null)
        {
            services.ConfigureSenseNet(configuration)
                .AddSenseNetILogger()
                .AddSenseNetBlobStorage()
                .AddSenseNetPasswordHashProvider()
                .AddPasswordHashProviderForMigration<Sha256PasswordHashProviderWithoutSalt>()
                .AddSenseNetSecurity(config =>
                {
                    config.SystemUserId = Identifiers.SystemUserId;
                    config.VisitorUserId = Identifiers.VisitorUserId;
                    config.EveryoneGroupId = Identifiers.EveryoneGroupId;
                    config.OwnerGroupId = Identifiers.OwnersGroupId;
                })
                .AddPlatformIndependentServices()
                .AddSenseNetTaskManager()
                .AddContentNamingProvider<CharReplacementContentNamingProvider>()
                .AddSenseNetDocumentPreviewProvider()
                .AddLatestComponentStore()
                .AddSenseNetCors()
                .AddSenseNetIdentityServerClients()
                .AddSenseNetDefaultClientManager()
                .AddSenseNetApiKeys()
                .AddSenseNetEmailManager(options =>
                {
                    configuration.GetSection("sensenet:Email").Bind(options);
                })
                .AddSenseNetRegistration();

            services.AddStatistics();

            // add maintenance tasks
            services
                .AddSingleton<IMaintenanceTask, CleanupFilesTask>()
                .AddSingleton<IMaintenanceTask, StartActiveDirectorySynchronizationTask>()
                .AddSingleton<IMaintenanceTask, AccessTokenCleanupTask>()
                .AddSingleton<IMaintenanceTask, SharedLockCleanupTask>()
                //.AddSingleton<IMaintenanceTask, StatisticalDataAggregationMaintenanceTask>()
                //.AddSingleton<IMaintenanceTask, StatisticalDataCollectorMaintenanceTask>()
                //.AddSingleton<IMaintenanceTask, ReindexBinariesTask>()

                .AddHostedService(provider => new RepositoryHostedService(provider, buildRepository, onRepositoryStartedAsync))
                .AddHostedService<SnMaintenance>();

            // add sn components defined in the content repository layer
            services.AddRepositoryComponents();

            return services;
        }

        public static IServiceCollection AddPlatformIndependentServices(this IServiceCollection services)
        {
            return services
                .AddSenseNetDefaultRepositoryServices()
                .AddSingleton<StorageSchema>()
                .AddSingleton<ITreeLockController, TreeLockController>()

                .AddSingleton<SecurityHandler>()
                .AddSecurityMissingEntityHandler<SnMissingEntityHandler>()
                .AddSingleton<IPermissionFilterFactory, PermissionFilterFactory>()

                .AddSingleton<ISearchManager, SearchManager>()
                .AddSingleton<IIndexManager, IndexManager>()
                .AddSingleton<IIndexPopulator, DocumentPopulator>()
                .AddSingleton<IIndexingActivityFactory, IndexingActivityFactory>()

                .AddSingleton(ClusterMemberInfo.Current)
                .AddDefaultClusterMessageTypes()
                .AddSingleton<IClusterMessageFormatter, SnMessageFormatter>()
                .AddSingleton<IClusterChannel, VoidChannel>()

                .AddDefaultTextExtractors()

                .AddSingleton<ISnCache, SnMemoryCache>()
                .AddSingleton<IApplicationCache, ApplicationCache>() //not used?
                .AddSingleton<IIndexDocumentProvider, IndexDocumentProvider>()
                .AddSingleton<IEventPropertyCollector, EventPropertyCollector>()
                .AddSingleton<ICompatibilitySupport, EmptyCompatibilitySupport>()
                .AddSingleton<IContentProtector, ContentProtector>()
                .AddSingleton<DocumentBinaryProvider, DefaultDocumentBinaryProvider>()
                .AddSingleton<ISharingNotificationFormatter, DefaultSharingNotificationFormatter>()
            ;
        }

        /// <summary>
        /// Sets well-known singleton provider instances that are used by legacy code.
        /// </summary>
        internal static IServiceProvider AddSenseNetProviderInstances(this IServiceProvider provider)
        {
            Providers.Instance ??= new Providers(provider);

            Providers.Instance.BlobProviders = provider.GetRequiredService<IBlobProviderStore>();
            Providers.Instance.BlobStorage = provider.GetRequiredService<IBlobStorage>();
            Providers.Instance.BlobMetaDataProvider = provider.GetRequiredService<IBlobStorageMetaDataProvider>();
            Providers.Instance.BlobProviderSelector = provider.GetRequiredService<IBlobProviderSelector>();

            var searchEngine = provider.GetService<ISearchEngine>();
            if (searchEngine != null)
                Providers.Instance.SearchEngine = searchEngine;

            var statisticalDataProvider = provider.GetService<IStatisticalDataProvider>();
            if (statisticalDataProvider != null)
                Providers.Instance.SetProvider(typeof(IStatisticalDataProvider), statisticalDataProvider);

            var cmi = provider.GetService<IOptions<ClusterMemberInfo>>()?.Value;
            if (cmi != null)
                ClusterMemberInfo.Current = cmi;

            var csp = provider.GetService<ICryptoServiceProvider>();
            if (csp != null)
                Providers.Instance.SetProvider(typeof(ICryptoServiceProvider), csp);

            return provider;
        }
        
        /// <summary>
        /// Registers a membership extender type as a scoped service. To execute extenders at runtime,
        /// please call the <see cref="UseSenseNetMembershipExtenders"/> method in Startup.Configure.
        /// </summary>
        /// <typeparam name="T">An <see cref="IMembershipExtender"/> implementation.</typeparam>
        /// <param name="services">The IServiceCollection instance.</param>
        public static IServiceCollection AddSenseNetMembershipExtender<T>(this IServiceCollection services) where T : class, IMembershipExtender
        {
            services.AddScoped<IMembershipExtender, T>();
            RepositoryBuilder.WriteLog("MembershipExtender", typeof(T).FullName);

            return services;
        }

        /// <summary>
        /// Registers a middleware in the pipeline to execute previously configured membership extenders.
        /// To register an extender, please call <see cref="AddSenseNetMembershipExtender{T}"/> in the
        /// ConfigureServices method of your Startup class.
        /// </summary>
        /// <param name="builder">The IApplicationBuilder instance.</param>
        public static IApplicationBuilder UseSenseNetMembershipExtenders(this IApplicationBuilder builder)
        {
            builder.Use(async (context, next) =>
            {
                var user = User.Current;

                // get all extenders registered with the interface
                var extenders = context.RequestServices.GetServices<IMembershipExtender>();
                if (extenders != null)
                {
                    var extensions = extenders
                        .SelectMany(e =>
                        {
                            try
                            {
                                return e.GetExtension(user).ExtensionIds;
                            }
                            catch (Exception ex)
                            {
                                SnTrace.System.WriteError($"Error executing membership extender {e.GetType().FullName} " +
                                                          $"for user {User.Current.Username}. {ex.Message}");
                            }

                            return Array.Empty<int>();
                        })
                        .Distinct()
                        .ToArray();

                    user.AddMembershipIdentities(extensions);
                }

                /* -------------- */
                if (next != null)
                    await next();
            });

            return builder; 
        }

        /// <summary>
        /// Switches the ResponseLengthLimiter feature on.
        /// </summary>
        /// <param name="builder">The IRepositoryBuilder instance.</param>
        /// <param name="maxResponseLengthInBytes">Response length limit value in bytes (optional).</param>
        /// <param name="maxFileLengthInBytes">File length limit value in bytes (optional).</param>
        /// <returns>The IRepositoryBuilder instance.</returns>
        public static IRepositoryBuilder UseResponseLimiter(this IRepositoryBuilder builder,
            long maxResponseLengthInBytes = 0, long maxFileLengthInBytes = 0)
        {
            Providers.Instance.SetProvider(typeof(IResponseLimiter),
                new SnResponseLimiter(maxResponseLengthInBytes, maxFileLengthInBytes));

            return builder;
        }

        /// <summary>
        /// Sets the repository type that is returned by the GetRepositoryType action.
        /// </summary>
        public static IServiceCollection ConfigureRepositoryType(this IServiceCollection services, Action<RepositoryTypeOptions> configure)
        {
            if (configure != null)
                services.Configure(configure);

            return services;
        }

        /// <summary>
        /// Registers a dashboard data provider in the service container.
        /// </summary>
        /// <typeparam name="T">An <see cref="IDashboardDataProvider"/> implementation.</typeparam>
        /// <param name="services">The IServiceCollection instance.</param>
        public static IServiceCollection AddSenseNetDashboardDataProvider<T>(this IServiceCollection services) 
            where T: class, IDashboardDataProvider
        {
            return services.AddSingleton<IDashboardDataProvider, T>();
        }
    }
}
