using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Security.Clients;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Diagnostics;
using SenseNet.Tools;
using SenseNet.Packaging;
using SenseNet.Search;
using SenseNet.Storage.Diagnostics;
using SenseNet.ContentRepository.Security.ApiKeys;
using Exception = System.Exception;

namespace SenseNet.ContentRepository
{
    public static class Repository
    {
        /// <summary>
        /// Executes the boot sequence of the Repository by the passed <see cref="RepositoryStartSettings"/>.
        /// </summary>
        /// <example>
        /// Use the following code in your tool or other outer application:
        /// <code>
        /// var startSettings = new RepositoryStartSettings
        /// {
        ///     PluginsPath = pluginsPath, // Local directory path of plugins if it is different from your tool's path.
        ///     Console = Console.Out      // Startup sequence will be traced to given writer.
        /// };
        /// using (SenseNet.ContentRepository.Repository.Start(startSettings))
        /// {
        ///     // your code
        /// }
        /// </code>
        /// </example>
        /// <remarks>
        /// Repository will be stopped if the returned <see cref="RepositoryStartSettings"/> instance is disposed.
        /// </remarks>
        /// <returns>A new IDisposable <see cref="RepositoryInstance"/> instance.</returns>
        /// <returns></returns>
        public static RepositoryInstance Start(RepositoryStartSettings settings)
        {
            if (!settings.ExecutingPatches)
            {
                // Switch ON this flag so that inner repository start operations
                // do not try to execute patches again recursively.
                settings.ExecutingPatches = true;

                //TODO: [auto-patch] this feature is not released yet
                //PackageManager.ExecuteAssemblyPatches(settings);
            }

            var instance = RepositoryInstance.Start(settings);
            SystemAccount.Execute(() => Root);
            return instance;
        }
        public static RepositoryInstance Start(RepositoryBuilder builder)
        {
            var repositoryStatus = Providers.Instance.RepositoryStatus;
            var connectionStrings = builder.Services?.GetRequiredService<IOptions<ConnectionStringOptions>>();

            repositoryStatus?.SetStatus("Starting BlobProviders");
            Providers.Instance.InitializeBlobProviders(connectionStrings?.Value ?? new ConnectionStringOptions());

            EnsureDatabase(builder);
            
            var initialData = builder.InitialData;
            if (initialData != null)
            {
                repositoryStatus?.SetStatus("Installing initial data");
                Providers.Instance.DataStore.InstallInitialDataAsync(initialData, CancellationToken.None)
                    .GetAwaiter().GetResult();
            }

            RepositoryInstance repositoryInstance = null;
            var exclusiveLockOptions = builder.Services?.GetService<IOptions<ExclusiveLockOptions>>()?.Value;

            ExclusiveBlock.RunAsync("SenseNet.PatchManager", Guid.NewGuid().ToString(),
                ExclusiveBlockType.WaitAndAcquire, exclusiveLockOptions, CancellationToken.None, () =>
            {
                var logger = Providers.Instance.GetProvider<ILogger<SnILogger>>();
                var patchManager = new PatchManager(builder, logRecord => { logRecord.WriteTo(logger); });
                repositoryStatus?.SetStatus("Executing patches before start");
                patchManager.ExecutePatchesOnBeforeStart();

                repositoryStatus?.SetStatus("Calling Repository.Start");
                repositoryInstance = Start((RepositoryStartSettings)builder);

                var permissions = initialData?.Permissions;
                if (permissions != null && permissions.Count > 0)
                    new SecurityInstaller(Providers.Instance.SecurityHandler, Providers.Instance.StorageSchema,
                        Providers.Instance.DataStore).InstallDefaultSecurityStructure(initialData);

                var indexingEngine = Providers.Instance.SearchEngine.IndexingEngine;
                if (indexingEngine.Running)
                {
                    if (initialData?.IndexDocuments != null)
                    {
                        // Build the index from an in-memory structure. This is a developer use case.
                        indexingEngine.ClearIndexAsync(CancellationToken.None)
                            .ConfigureAwait(false).GetAwaiter().GetResult();
                        indexingEngine.WriteIndexAsync(null, null,
                                initialData.IndexDocuments, CancellationToken.None)
                            .ConfigureAwait(false).GetAwaiter().GetResult();
                    }
                    else
                    {
                        // make sure the index exists and contains documents
                        EnsureIndex(builder);
                    }
                }

                repositoryStatus?.SetStatus("Executing patches after start");
                patchManager.ExecutePatchesOnAfterStart();
                RepositoryVersionInfo.Reset();

                return System.Threading.Tasks.Task.CompletedTask;
            }).GetAwaiter().GetResult();

            // generate default clients and secrets
            repositoryStatus?.SetStatus("Checking default items in ClientStore");
            var clientStore = builder.Services?.GetService<ClientStore>();
            var clientOptions = builder.Services?.GetService<IOptions<ClientStoreOptions>>()?.Value;
            var logger = builder.Services?.GetService<ILogger<RepositoryInstance>>();

            logger?.LogInformation("Ensuring default clients and secrets...");

            clientStore?.EnsureClientsAsync(clientOptions?.Authority, clientOptions?.RepositoryUrl?.RemoveUrlSchema())
                .GetAwaiter().GetResult();

            EnsureApiKeyForAdmin(builder.Services, logger);

            if(repositoryStatus != null)
                repositoryStatus.IsRunning = true;

            return repositoryInstance;
        }

        private static void EnsureApiKeyForAdmin(IServiceProvider services, ILogger logger)
        {
            using (new SystemAccount())
            {
                try
                {
                    logger?.LogInformation("Check apikey for admin...");
                    var akm = services.GetRequiredService<IApiKeyManager>();
                    var apiKey = akm.GetApiKeysByUserAsync(Identifiers.AdministratorUserId, CancellationToken.None)
                        .GetAwaiter().GetResult()
                        .Where(a => a.ExpirationDate > DateTime.UtcNow)
                        .OrderByDescending(a => a.ExpirationDate)
                        .FirstOrDefault();
                    if (apiKey == null)
                    {
                        apiKey = akm.CreateApiKeyAsync(Identifiers.AdministratorUserId, DateTime.UtcNow.AddYears(1),
                                CancellationToken.None)
                            .GetAwaiter().GetResult();
                        logger?.LogInformation("Apikey generated for admin");
                    }
                }
                catch (Exception e)
                {
                    logger?.LogError(e, "Error during checking / generating apikey for admin.");
                }
            }
        }

        internal static RepositoryInstance Start(IRepositoryBuilder builder)
        {
            // the RepositoryBuilder class should be the only implementation of this interface
            return Start((RepositoryBuilder)builder);
        }

        /// <summary>
        /// Returns the running state of the Repository.
        /// </summary>
        /// <returns>True if the Repository has started yet otherwise false.</returns>
        public static bool Started()
        {
            return RepositoryInstance.Started();
        }
        /// <summary>
        /// Stops all internal services of the Repository.
        /// </summary>
        public static void Shutdown()
        {
            RepositoryInstance.Shutdown();
        }

        private static void EnsureDatabase(RepositoryBuilder builder)
        {
            if (builder.Services == null)
                return;

            var ds = builder.Services.GetService<IDataStore>();
            var dbExists = ds.IsDatabaseReadyAsync(CancellationToken.None).GetAwaiter().GetResult();
            if (dbExists) 
                return;

            Providers.Instance.RepositoryStatus?.SetStatus("Database is not ready");

            var logger = builder.Services.GetService<ILogger<RepositoryInstance>>();
            var packageDescriptor = builder.Services.GetService<IInstallPackageDescriptor>();
            if (packageDescriptor == null)
                return;

            // this will install the database and the initial package
            new Installer(builder, null, logger)
                .InstallSenseNet(packageDescriptor.GetPackageAssembly(), packageDescriptor.GetPackageName());
        }

        private static void EnsureIndex(RepositoryBuilder builder)
        {
            Providers.Instance.RepositoryStatus?.SetStatus("Checking index existence");

            var logger = builder.Services?.GetService<ILogger<RepositoryInstance>>();
            logger?.LogInformation("Checking the index...");

            // execute a query that should return multiple items if the index is not empty
            var indexDocExist = SystemAccount.Execute(() => ContentQuery.QueryAsync(SafeQueries.ContentTypes,
                QuerySettings.AdminSettings, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult().Count > 10);

            if (indexDocExist)
                return;

            // This scenario auto-generates the whole index from the database. The most common case is
            // when a new web app domain (usually a container) is started in a load balanced environment.
            Providers.Instance.RepositoryStatus?.SetStatus("Executing ClearAndPopulateAll");

            var populator = Providers.Instance.SearchManager.GetIndexPopulator();
            var indexCount = 0;

            populator.IndexingError += (sender, eventArgs) =>
            {
                logger?.LogWarning($"Error during building app start index for {eventArgs.Path}. " +
                                         $"(id: {eventArgs.NodeId}). {eventArgs.Exception?.Message}");
            };
            populator.NodeIndexed += (sender, eventArgs) =>
            {
                Interlocked.Increment(ref indexCount);
            };

            logger?.LogInformation("Rebuilding the index...");

            populator.ClearAndPopulateAllAsync(CancellationToken.None, builder.Console ?? new LoggerConsole(logger)).GetAwaiter().GetResult();

            logger?.LogInformation($"Indexing of {indexCount} nodes finished.");
        }

        //TODO: move this helper logger class to a more appropriate place
        private class LoggerConsole : TextWriter
        {
            private readonly ILogger _logger;

            public LoggerConsole(ILogger logger)
            {
                _logger = logger;
            }

            public override void Write(string value)
            {
                _logger.LogInformation(value);
            }

            public override Encoding Encoding { get; } = Encoding.Unicode;
        }

        // ========================================================================= Constants

        public static readonly string RootName = "Root";
        public static readonly string SystemFolderName = "System";
        public static readonly string AspectsFolderName = "Aspects";
        public static readonly string SchemaFolderName = "Schema";
        public static readonly string ContentTypesFolderName = "ContentTypes";
        public static readonly string ContentTemplatesFolderName = "ContentTemplates";
        public static readonly string SettingsFolderName = "Settings";

        public static readonly string RootPath = String.Concat("/", RootName);
        public static readonly string SystemFolderPath = RepositoryPath.Combine(RootPath, SystemFolderName);
        public static readonly string SchemaFolderPath = RepositoryPath.Combine(SystemFolderPath, SchemaFolderName);
        public static readonly string AspectsFolderPath = RepositoryPath.Combine(SchemaFolderPath, AspectsFolderName);
        public static readonly string ContentTypesFolderPath = RepositoryPath.Combine(SchemaFolderPath, ContentTypesFolderName);
        public static readonly string SettingsFolderPath = RepositoryPath.Combine(SystemFolderPath, SettingsFolderName);

        [Obsolete("After V6.5 PATCH 9: Use RepositoryStructure.FieldControlTemplatesPath instead.")]
        public static string FieldControlTemplatesPath => RepositoryStructure.FieldControlTemplatesPath;
        [Obsolete("After V6.5 PATCH 9: Use RepositoryStructure.CellTemplatesPath instead.")]
        public static string CellTemplatesPath => RepositoryStructure.CellTemplatesPath;
        [Obsolete("After V6.5 PATCH 9: Use RepositoryStructure.ContentViewGlobalFolderPath instead.")]
        public static string ContentViewGlobalFolderPath => RepositoryStructure.ContentViewGlobalFolderPath;
        [Obsolete("After V6.5 PATCH 9: Use RepositoryStructure.ContentViewFolderName instead.")]
        public static string ContentViewFolderName => RepositoryStructure.ContentViewFolderName;
        [Obsolete("After V6.5 PATCH 9: Use RepositoryStructure.ContentTemplateFolderPath instead.")]
        public static string ContentTemplateFolderPath => RepositoryStructure.ContentTemplateFolderPath;
        [Obsolete("After V6.5 PATCH 9: Use RepositoryStructure.ImsFolderPath instead.")]
        public static string ImsFolderPath => RepositoryStructure.ImsFolderPath;
        [Obsolete("After V6.5 PATCH 9: Use RepositoryStructure.PageTemplateFolderPath instead.")]
        public static string PageTemplatesFolderPath => RepositoryStructure.PageTemplateFolderPath;
        [Obsolete("After V6.5 PATCH 9: Use RepositoryStructure.ResourceFolderPath instead.")]
        public static string ResourceFolderPath => RepositoryStructure.ResourceFolderPath;
        [Obsolete("After V6.5 PATCH 9: Use RepositoryStructure.SkinRootFolderPath instead.")]
        public static string SkinRootFolderPath => RepositoryStructure.SkinRootFolderPath;
        [Obsolete("After V6.5 PATCH 9: Use RepositoryStructure.SkinGlobalFolderPath instead.")]
        public static string SkinGlobalFolderPath => RepositoryStructure.SkinGlobalFolderPath;

        public static string WorkflowDefinitionPath { get; internal set; } = "/Root/System/Workflows/";
        public static string UserProfilePath { get; internal set; } = "/Root/Profiles";
        public static string LocalGroupsFolderName { get; internal set; } = "Groups";

        // ========================================================================= Properties

        public static Folder SkinRootFolder => (Folder)Node.LoadNode(RepositoryStructure.SkinRootFolderPath);
        public static Folder SkinGlobalFolder => (Folder)Node.LoadNode(RepositoryStructure.SkinGlobalFolderPath);

        /// <summary>
        /// Gets the root Node.
        /// </summary>
        /// <value>The root Node.</value>
        public static PortalRoot Root => (PortalRoot)Node.LoadNode(Identifiers.PortalRootId);

        public static Folder AspectsFolder // creates if doesn't exist
        {
            get
            {
                var folder = Node.Load<Folder>(AspectsFolderPath);
                if (folder == null)
                {
                    var parent = Node.LoadNode(RepositoryPath.GetParentPath(AspectsFolderPath));
                    folder = new Folder(parent) { Name = AspectsFolderName };
                    folder.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
                }
                return folder;
            }
        }
        public static Folder SystemFolder => (Folder)Node.LoadNode(SystemFolderPath);
        public static Folder SchemaFolder => (Folder)Node.LoadNode(SchemaFolderPath);
        public static Folder ContentTypesFolder => (Folder)Node.LoadNode(ContentTypesFolderPath);
        public static Folder ImsFolder => (Folder)Node.LoadNode(RepositoryStructure.ImsFolderPath);
        public static Folder PageTemplatesFolder => (Folder)Node.LoadNode(RepositoryStructure.PageTemplateFolderPath);

        [Obsolete("After V6.5 PATCH 9: Use IdentityManagement.UserProfilesEnabled instead.")]
        public static bool UserProfilesEnabled => IdentityManagement.UserProfilesEnabled;
        [Obsolete("After V6.5 PATCH 9: Use Logging.DownloadCounterEnabled instead.")]
        public static bool DownloadCounterEnabled => Logging.DownloadCounterEnabled;
        [Obsolete("After V6.5 PATCH 9: Use Versioning.CheckInComments instead.")]
        public static CheckInCommentsMode CheckInCommentsMode => Configuration.Versioning.CheckInCommentsMode;
        [Obsolete("After V6.5 PATCH 9: Use Providers.RepositoryPathProviderEnabled instead.")]
        public static bool RepositoryPathProviderEnabled => Providers.RepositoryPathProviderEnabled;
        [Obsolete("After V6.5 PATCH 9: Use WebApplication.GlobaFieldControlTemplateEnabled instead.")]
        public static bool IsGlobalTemplateEnabled => true;
        [Obsolete("After V6.5 PATCH 9: Use RepositoryEnvironment.SkipBinaryImportIfFileDoesNotExist instead.")]
        public static bool SkipBinaryImport => RepositoryEnvironment.SkipBinaryImportIfFileDoesNotExist;
        [Obsolete("After V6.5 PATCH 9: Use RepositoryEnvironment.SkipImportingMissingReferences instead.")]
        public static bool SkipImportingMissingReferences => RepositoryEnvironment.SkipImportingMissingReferences;
        [Obsolete("After V6.5 PATCH 9: Use RepositoryEnvironment.SkipReferenceNames instead.")]
        public static string[] SkipReferenceNames => RepositoryEnvironment.SkipReferenceNames;

        [Obsolete("After V6.5 PATCH 9: Use Notification.DefaultEmailSender instead.")]
        public static string EmailSenderAddress => Notification.DefaultEmailSender;

        public static string[] ExecutableExtensions { get; internal set; } = { "aspx", "ashx", "asmx", "axd", "cshtml", "vbhtml" };

        public static readonly string[] ExecutableFileTypeNames = { "ExecutableFile", "WebServiceApplication" };
        public static readonly string DefaultExecutableFileTypeName = ExecutableFileTypeNames[0];
    }
}
