﻿using System;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Diagnostics;
using SenseNet.Tools;
using SenseNet.Packaging;

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
            // Required early configuration
            BlobStorageComponents.DataProvider = Providers.Instance.BlobMetaDataProvider;
            BlobStorageComponents.ProviderSelector = Providers.Instance.BlobProviderSelector;

            var initialData = builder.InitialData;
            if (initialData != null)
                DataStore.InstallInitialDataAsync(initialData, CancellationToken.None)
                    .GetAwaiter().GetResult();

            RepositoryInstance repositoryInstance = null;
            var exclusiveLockOptions = builder.Services?.GetService<IOptions<ExclusiveLockOptions>>()?.Value;

            ExclusiveBlock.RunAsync("SenseNet.PatchManager", Guid.NewGuid().ToString(),
                ExclusiveBlockType.WaitAndAcquire, exclusiveLockOptions, CancellationToken.None, () =>
            {
                var logger = Providers.Instance.GetProvider<ILogger<SnILogger>>();
                var patchManager = new PatchManager(builder, logRecord => { logRecord.WriteTo(logger); });
                patchManager.ExecutePatchesOnBeforeStart();

                repositoryInstance = Start((RepositoryStartSettings)builder);

                var permissions = initialData?.Permissions;
                if (permissions != null && permissions.Count > 0)
                    SecurityHandler.SecurityInstaller.InstallDefaultSecurityStructure(initialData);

                patchManager.ExecutePatchesOnAfterStart();

                return System.Threading.Tasks.Task.CompletedTask;
            }).GetAwaiter().GetResult();

            return repositoryInstance;
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
        /// <returns>A <see cref="RepositoryVersionInfo"/> instance containing package, component, assembly
        /// versions.</returns>
        [ODataFunction]
        [ContentTypes(N.CT.PortalRoot)]
        [AllowedRoles(N.R.Administrators, N.R.Developers)]
        public static RepositoryVersionInfo GetVersionInfo(Content content)
        {
            return RepositoryVersionInfo.Instance;
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

        [Obsolete("After V6.5 PATCH 9: Use Skin.DefaultSkinName instead.", true)]
        public static string DefaultSkinName => "sensenet";

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
                    folder.Save();
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
        [Obsolete("After V6.5 PATCH 9: Use SystemStart.WarmupEnabled instead.")]
        public static bool WarmupEnabled => SystemStart.WarmupEnabled;
        [Obsolete("After V6.5 PATCH 9: Use SystemStart.WarmupControlQueryFilter instead.")]
        public static string WarmupControlQueryFilter => SystemStart.WarmupControlQueryFilter;
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
        [Obsolete("After V6.5 PATCH 9: Use WebApplication.EditSourceExtensions instead.", true)]
        public static string[] EditSourceExtensions => new string[0];
        [Obsolete("After V6.5 PATCH 9: Use Webdav.WebdavEditExtensions instead.", true)]
        public static string[] WebdavEditExtensions => new string[0];
        [Obsolete("After V6.5 PATCH 9: Use WebApplication.DownloadExtensions instead.", true)]
        public static string[] DownloadExtensions => new string[0];

        [Obsolete("After V6.5 PATCH 9: Use Notification.DefaultEmailSender instead.")]
        public static string EmailSenderAddress => Notification.DefaultEmailSender;

        public static string[] ExecutableExtensions { get; internal set; } = { "aspx", "ashx", "asmx", "axd", "cshtml", "vbhtml" };

        public static readonly string[] ExecutableFileTypeNames = { "ExecutableFile", "WebServiceApplication" };
        public static readonly string DefaultExecutableFileTypeName = ExecutableFileTypeNames[0];
    }
}
