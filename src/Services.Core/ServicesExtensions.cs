using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SenseNet.BackgroundOperations;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.Services.Core;
using SenseNet.Services.Core.Authentication;
using SenseNet.Services.Core.Authentication.IdentityServer4;
using SenseNet.Services.Core.Configuration;
using SenseNet.Storage;
using SenseNet.Storage.Security;
using SenseNet.TaskManagement.Core;
using SenseNet.Tools;
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
            services.Configure<DataOptions>(configuration.GetSection("sensenet:Data"));
            services.Configure<BlobStorageOptions>(configuration.GetSection("sensenet:BlobStorage"));
            services.Configure<TaskManagementOptions>(configuration.GetSection("sensenet:TaskManagement"));
            services.Configure<EmailOptions>(configuration.GetSection("sensenet:Email"));
            services.Configure<RegistrationOptions>(configuration.GetSection("sensenet:Registration"));
            services.Configure<AuthenticationOptions>(configuration.GetSection("sensenet:Authentication"));
            services.Configure<ClientRequestOptions>(configuration.GetSection("sensenet:ClientRequest"));
            services.Configure<HttpRequestOptions>(configuration.GetSection("sensenet:HttpRequest"));
            services.Configure<ExclusiveLockOptions>(configuration.GetSection("sensenet:ExclusiveLock"));

            //TODO: remove workaround for legacy connection string configuration
            // and replace it with real configuration load like above.
            services.Configure<ConnectionStringOptions>(options =>
            {
                options.ConnectionString = ConnectionStrings.ConnectionString;
                options.SecurityDatabase = ConnectionStrings.SecurityDatabaseConnectionString;
                options.SignalRDatabase = ConnectionStrings.SignalRDatabaseConnectionString;
            });
            
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
                .AddSenseNetTaskManager()
                .AddSenseNetDocumentPreviewProvider()
                .AddLatestComponentStore()
                .AddSenseNetCors()
                .AddSenseNetIdentityServerClients()
                .AddSenseNetRegistration();

            // add maintenance tasks
            services
                .AddSingleton<IMaintenanceTask, CleanupFilesTask>()
                .AddSingleton<IMaintenanceTask, StartActiveDirectorySynchronizationTask>()
                .AddSingleton<IMaintenanceTask, AccessTokenCleanupTask>()
                .AddSingleton<IMaintenanceTask, SharedLockCleanupTask>()
                //.AddSingleton<IMaintenanceTask, ReindexBinariesTask>()

                .AddHostedService(provider => new RepositoryHostedService(provider, buildRepository, onRepositoryStartedAsync))
                .AddHostedService<SnMaintenance>();

            // add sn components defined in the content repository layer
            services.AddRepositoryComponents();

            return services;
        }
        
        /// <summary>
        /// Sets well-known singleton provider instances that are used by legacy code.
        /// </summary>
        internal static IServiceProvider AddSenseNetProviderInstances(this IServiceProvider provider)
        {
            Providers.Instance.BlobProviders = provider.GetRequiredService<IBlobProviderStore>();
            Providers.Instance.BlobStorage = provider.GetRequiredService<IBlobStorage>();
            Providers.Instance.BlobMetaDataProvider = provider.GetRequiredService<IBlobStorageMetaDataProvider>();
            Providers.Instance.BlobProviderSelector = provider.GetRequiredService<IBlobProviderSelector>();

#pragma warning disable 618

            var previewProvider = provider.GetService<IPreviewProvider>();
            if (previewProvider != null)
                Providers.Instance.PreviewProvider = previewProvider;

            var taskManager = provider.GetService<ITaskManager>();
            if (taskManager != null)
                SnTaskManager.Instance = taskManager;

            Providers.Instance.PropertyCollector = new EventPropertyCollector();

#pragma warning restore 618

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
    }
}
