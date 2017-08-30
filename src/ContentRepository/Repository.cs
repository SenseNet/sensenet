using System;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Configuration;

namespace SenseNet.ContentRepository
{
    public static class Repository
    {
        /// <summary>
        /// Executes the default boot sequence of the Repository.
        /// </summary>
        /// <example>
        /// Use the following code in your tool or other outer application:
        /// <code>
        /// using (Repository.Start())
        /// {
        ///     // your code
        /// }
        /// </code>
        /// </example>
        /// <remarks>
        /// Repository will be stopped if the returned <see cref="RepositoryStartSettings"/> instance is disposed.
        /// </remarks>
        /// <returns>A new IDisposable <see cref="RepositoryInstance"/> instance.</returns>
        public static RepositoryInstance Start()
        {
            return Start(RepositoryStartSettings.Default);
        }
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
            var instance = RepositoryInstance.Start(settings);
            AccessProvider.ChangeToSystemAccount();
            Root = (PortalRoot)Node.LoadNode(RootPath);
            AccessProvider.RestoreOriginalUser();
            return instance;
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

        [SenseNet.ApplicationModel.ODataFunction]
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
        public static PortalRoot Root { get; private set; }

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
