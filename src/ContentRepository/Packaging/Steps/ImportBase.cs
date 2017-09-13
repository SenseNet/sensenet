using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.Storage;
using SNC = SenseNet.ContentRepository;
using IO = System.IO;
using System.Diagnostics;
using System.Xml;
using System.Xml.Xsl;
using SenseNet.ContentRepository;
using SenseNet.Portal.Handlers;
using SenseNet.Search;
using SenseNet.ContentRepository.Schema;
using System.Reflection;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.ContentRepository.Storage.Data;
using System.Timers;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Security;

namespace SenseNet.Packaging.Steps
{
    public enum ImportLogLevel { Info, Progress, Verbose }

    public abstract class ImportBase : Step
    {
        [DefaultProperty]
        public string Source { get; set; }
        public ImportLogLevel LogLevel { get; set; }

        private bool _abortOnError = true;
        public bool AbortOnError 
        {
            get { return _abortOnError; }
            set { _abortOnError = value; } 
        }

        public bool ResetSecurity { get; set; }

        protected void DoImport(string schemaPath, string fsPath, string repositoryPath)
        {
            var savedMode = RepositoryEnvironment.WorkingMode.Importing;
            RepositoryEnvironment.WorkingMode.SetImporting(true);
            try
            {
                var importer = new ImporterClass();
                importer.Run(schemaPath, fsPath, repositoryPath, LogLevel, ResetSecurity);

                if (importer.ErrorOccured && this.AbortOnError)
                    throw new ApplicationException("Error occured during importing, please review the log.");
            }
            finally
            {
                RepositoryEnvironment.WorkingMode.SetImporting(savedMode);
            }
        }

        [DebuggerDisplay("ContentInfo: Name={Name}; ContentType={ContentTypeName}; IsFolder={IsFolder} ({Attachments.Count} Attachments)")]
        private class ContentInfo
        {
            private string _metaDataPath;
            private int _contentId;
            private bool _isFolder;
            private string _name;
            private List<string> _attachments;
            private string _contentTypeName;
            private XmlDocument _xmlDoc;
            private string _childrenFolder;
            private ImportContext _transferringContext;

            public string MetaDataPath
            {
                get { return _metaDataPath; }
            }
            public int ContentId
            {
                get { return _contentId; }
            }
            public bool IsFolder
            {
                get { return _isFolder; }
            }
            public string Name
            {
                get { return _name; }
            }
            public List<string> Attachments
            {
                get { return _attachments; }
            }
            public string ContentTypeName
            {
                get { return _contentTypeName; }
            }
            public string ChildrenFolder
            {
                get { return _childrenFolder; }
                // Written by ImporterClass in case of existing .Children folder.
                set { _childrenFolder = value; }
            }
            public bool HasReference
            {
                get
                {
                    if (_transferringContext == null)
                        return false;
                    return _transferringContext.HasReference;
                }
            }
            public bool HasPermissions { get; private set; }
            public bool HasBreakPermissions { get; private set; }
            public bool HasAspect { get; private set; }
            public bool ClearPermissions { get; private set; }
            public bool FileIsHidden { get; private set; }
            public bool ContentTypeIsInferredFolder { get; private set; }
            public bool ContentTypeIsInferredFile { get; private set; }
            public bool Delete { get; private set; }

            public ContentInfo(string path, Node parent)
            {
                try
                {
                    _metaDataPath = path;
                    _attachments = new List<string>();

                    string directoryName = IO.Path.GetDirectoryName(path);
                    _name = IO.Path.GetFileName(path);
                    string extension = IO.Path.GetExtension(_name);
                    if (extension.ToLower() == ".content")
                    {
                        var fileInfo = new IO.FileInfo(path);
                        FileIsHidden = (fileInfo.Attributes & IO.FileAttributes.Hidden) == IO.FileAttributes.Hidden;

                        _xmlDoc = new XmlDocument();
                        _xmlDoc.Load(path);

                        XmlNode nameNode = _xmlDoc.SelectSingleNode("/ContentMetaData/ContentName");
                        _name = nameNode == null ? IO.Path.GetFileNameWithoutExtension(_name) : nameNode.InnerText;

                        var deleteAttr = _xmlDoc.DocumentElement.Attributes["delete"];
                        if (deleteAttr != null && deleteAttr.Value == "true")
                        {
                            this.Delete = true;
                        }
                        else
                        {
                            this.Delete = false;

                            _contentTypeName = _xmlDoc.SelectSingleNode("/ContentMetaData/ContentType").InnerText;

                            ClearPermissions = _xmlDoc.SelectSingleNode("/ContentMetaData/Permissions/Clear") != null;
                            HasBreakPermissions = _xmlDoc.SelectSingleNode("/ContentMetaData/Permissions/Break") != null;
                            HasPermissions = _xmlDoc.SelectNodes("/ContentMetaData/Permissions/Identity").Count > 0;
                            HasAspect = _xmlDoc.SelectNodes("ContentMetaData/Fields/Aspects").Count > 0;

                            // /ContentMetaData/Properties/*/@attachment
                            foreach (XmlAttribute attachmentAttr in _xmlDoc.SelectNodes("/ContentMetaData/Fields/*/@attachment"))
                            {
                                string attachment = attachmentAttr.Value;
                                _attachments.Add(attachment);
                                bool isFolder = IO.Directory.Exists(IO.Path.Combine(directoryName, attachment));
                                if (isFolder)
                                {
                                    if (_isFolder)
                                        throw new ApplicationException(String.Concat("Two or more attachment folder is not enabled. ContentName: ", _name));
                                    _isFolder = true;
                                    _childrenFolder = IO.Path.Combine(directoryName, attachment);
                                }
                            }
                            // default attachment
                            var defaultAttachmentPath = IO.Path.Combine(directoryName, _name);
                            if (!_attachments.Contains(_name))
                            {
                                string[] paths;
                                if (IO.Directory.Exists(defaultAttachmentPath))
                                    paths = new string[] { defaultAttachmentPath };
                                else
                                    paths = new string[0];

                                if (paths.Length == 1)
                                {
                                    if (_isFolder)
                                        throw new ApplicationException(String.Concat("Two or more attachment folder is not enabled. ContentName: ", _name));
                                    _isFolder = true;
                                    _childrenFolder = defaultAttachmentPath;
                                    _attachments.Add(_name);
                                }
                                else
                                {
                                    if (System.IO.File.Exists(defaultAttachmentPath))
                                        _attachments.Add(_name);
                                }
                            }
                        }
                    }
                    else
                    {
                        _isFolder = IO.Directory.Exists(path);
                        if (_isFolder)
                        {
                            var dirInfo = new IO.DirectoryInfo(path);
                            FileIsHidden = (dirInfo.Attributes & IO.FileAttributes.Hidden) == IO.FileAttributes.Hidden;
                            ContentTypeIsInferredFolder = true;

                            _contentTypeName = GetParentAllowedContentTypeName(path, parent, "Folder");
                            _childrenFolder = path;
                        }
                        else
                        {
                            var fileInfo = new IO.FileInfo(path);
                            FileIsHidden = (fileInfo.Attributes & IO.FileAttributes.Hidden) == IO.FileAttributes.Hidden;
                            ContentTypeIsInferredFile = true;

                            _xmlDoc = new XmlDocument();
                            _contentTypeName = UploadHelper.GetContentType(path, parent.Path) ?? GetParentAllowedContentTypeName(path, parent, "File");

                            // modified for possible contentname conversion
                            var contentMetaData = String.Concat("<ContentMetaData><ContentType>{0}</ContentType><ContentName>{1}</ContentName><Fields><Binary attachment='", _name.Replace("'", "&apos;"), "' /></Fields></ContentMetaData>");
                            _xmlDoc.LoadXml(String.Format(contentMetaData, _contentTypeName, _name));

                            _attachments.Add(_name);
                        }
                    }

                }
                catch (Exception e)
                {
                    throw new ApplicationException("Cannot create a ContentInfo. Path: " + path, e);
                }
            }

            public bool SetMetadata(SNC.Content content, string currentDirectory, bool isNewContent, bool updateReferences)
            {
                if (_xmlDoc == null)
                    return true;
                _transferringContext = new ImportContext(
                    _xmlDoc.SelectNodes("/ContentMetaData/Fields/*"), currentDirectory, isNewContent, true, updateReferences);
                bool result = content.ImportFieldData(_transferringContext);
                _contentId = content.ContentHandler.Id;
                return result;
            }

            internal bool UpdateReferences(SNC.Content content)
            {
                if (_transferringContext == null)
                    _transferringContext = new ImportContext(_xmlDoc.SelectNodes("/ContentMetaData/Fields/*"), null, false, true, true);
                else
                    _transferringContext.UpdateReferences = true;

                var node = content.ContentHandler;
                node.ModificationDate = node.ModificationDate;
                node.VersionModificationDate = node.VersionModificationDate;
                node.ModifiedBy = node.ModifiedBy;
                node.VersionModifiedBy = node.VersionModifiedBy;

                if (!content.ImportFieldData(_transferringContext))
                    return false;
                if (!HasPermissions && !HasBreakPermissions)
                    return true;
                var permissionsNode = _xmlDoc.SelectSingleNode("/ContentMetaData/Permissions");
                content.ContentHandler.Security.ImportPermissions(permissionsNode, this._metaDataPath);

                return true;
            }

            private static string GetParentAllowedContentTypeName(string fileName, Node parent, string defaultFileTypeName)
            {
                var node = (parent as GenericContent);
                if (node == null)
                    return defaultFileTypeName;

                var allowedChildTypes = node.GetAllowedChildTypes().ToList();
                string typeName = null;
                foreach (var item in allowedChildTypes)
                {
                    // skip any SystemFolder if it is not the only allowed type
                    if (item.IsInstaceOfOrDerivedFrom("SystemFolder")
                        && allowedChildTypes.Count > 1)
                        continue;

                    // choose the allowed type if this is the only suitable allowed type (eg the only type inheriting from File)
                    // otherwise if more allowed types are suitable, choose the default type
                    if (item.IsInstaceOfOrDerivedFrom(defaultFileTypeName))
                    {
                        if (typeName != null)
                            typeName = defaultFileTypeName;
                        else
                            typeName = item.Name;
                    }
                }

                return typeName ?? defaultFileTypeName;
            }
        }

        private class ImporterClass
        {
            private static string CR = Environment.NewLine;

            public string FSPath { get; set; }
            public string RepositoryPath { get; set; }
            public bool HasReference { get; set; }

            private string _logFolder = null;
            public string LogFolder
            {
                get
                {
                    if (_logFolder == null)
                        _logFolder = IO.Path.GetDirectoryName(Logger.GetLogFileName());
                    return _logFolder;
                }
                set
                {
                    if (!IO.Directory.Exists(value))
                        IO.Directory.CreateDirectory(value);
                    _logFolder = value;
                }
            }

            public ImportLogLevel LogLevel { get; private set; }

            public string RefLogFilePath { get; set; }
            public string ErrorLogFilePath { get; set; }

            /// <summary>
            /// Returns true if any error occured during the import process.
            /// </summary>
            public bool ErrorOccured { get; protected set; }

            internal void Run(string schemaPath, string fsPath, string repositoryPath, ImportLogLevel logLevel, bool resetSecurity)
            {
                // LogFolder = IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                this.LogLevel = logLevel;
                Log(ImportLogLevel.Info, "LogLevel: {0}", logLevel);

                if (schemaPath == null)
                {
                    FSPath = fsPath;
                    RepositoryPath = repositoryPath;
                }
                else
                {
                    FSPath = schemaPath;
                    RepositoryPath = ContentRepository.Repository.SchemaFolderPath;
                }

                string ctdPath = null;
                string aspectsPath = null;
                if (schemaPath != null)
                {
                    ctdPath = IO.Directory.GetDirectories(schemaPath, "ContentTypes").FirstOrDefault();
                    aspectsPath = IO.Directory.GetDirectories(schemaPath, "Aspects").FirstOrDefault();
                }

                if (ctdPath == null && aspectsPath == null && String.IsNullOrEmpty(fsPath))
                {
                    Log(ImportLogLevel.Info, "No changes");
                    return;
                }

                if (resetSecurity)
                {
                    Log(ImportLogLevel.Info, "Installing default security structure");
                    SecurityHandler.SecurityInstaller.InstallDefaultSecurityStructure();
                }

                // Elevation: there can be folders where even admins
                // do not have any permissions. This is why we need to
                // use system account for the whole import process.
                using (new SystemAccount())
                {
                    // Import ContentTypes (in ImportSchema step)
                    if (ctdPath != null || aspectsPath != null)
                    {
                        ImportContentTypeDefinitionsAndAspects(ctdPath, aspectsPath);
                        return;
                    }

                    // Import ContentTypes (in regular Import step)
                    if (!string.IsNullOrEmpty(fsPath))
                    {
                        ImportSchema(fsPath);
                    }

                    var firstImport = SaveInitialIndexDocuments();
                    if (firstImport)
                    {
                        var admin = Node.Load<User>(User.Administrator.Id);
                        var admins = Node.Load<Group>(Group.Administrators.Id);
                        var operators = Node.Load<Group>(Identifiers.OperatorsGroupPath);

                        admins.AddMember(admin);
                        admins.Save();

                        operators.AddMember(admins);
                        operators.Save();
                    }

                    // Import Contents
                    if (!string.IsNullOrEmpty(fsPath))
                    {
                        ImportSettings(fsPath);
                        ImportContents(fsPath, repositoryPath, false, false);
                    }
                    else
                    {
                        Log(ImportLogLevel.Info, "There is no content to import.");
                    }

                    if (firstImport)
                    {
                        SetInitialPermissions();
                    }
                }
            }

            private void ImportSchema(string fsPath)
            {
                // Check if the import structure contains the schema folder. If yes, import CTDs before other content.

                string ctdPath = null;
                string aspectPath = null;
                fsPath = IO.Path.GetFullPath(fsPath);

                // 1. if the import target is /Root, import from               \\source\System\Schema
                // 2. if the import target is /Root/System, import from        \\source\Schema
                // 3. if the import target is /Root/System/Schema, import from \\source
                // 4. if the import target is /Root/System/Schema/ContentTypes or Aspects, import from there
                // x. in any other case the source structure does not contain schema items

                if (RepositoryPathEquals(Repository.RootPath))
                {
                    ctdPath = IO.Path.Combine(fsPath, Repository.SystemFolderName, Repository.SchemaFolderName, Repository.ContentTypesFolderName);
                    aspectPath = IO.Path.Combine(fsPath, Repository.SystemFolderName, Repository.SchemaFolderName, Repository.AspectsFolderName);
                }
                else if (RepositoryPathEquals(Repository.SystemFolderPath))
                {
                    ctdPath = IO.Path.Combine(fsPath, Repository.SchemaFolderName, Repository.ContentTypesFolderName);
                    aspectPath = IO.Path.Combine(fsPath, Repository.SchemaFolderName, Repository.AspectsFolderName);
                }
                else if (RepositoryPathEquals(Repository.SchemaFolderPath))
                {
                    ctdPath = IO.Path.Combine(fsPath, Repository.ContentTypesFolderName);
                    aspectPath = IO.Path.Combine(fsPath, Repository.AspectsFolderName);
                }
                else if (RepositoryPathEquals(Repository.AspectsFolderPath))
                {
                    aspectPath = fsPath;
                }
                else if (RepositoryPathEquals(Repository.ContentTypesFolderPath))
                {
                    ctdPath = fsPath;
                }

                if (ctdPath != null || aspectPath != null)
                    ImportContentTypeDefinitionsAndAspects(ctdPath, aspectPath);
            }

            private void ImportSettings(string fsPath)
            {
                // Check if the import structure contains the global settings folder. If yes,
                // import global settings before other content.

                string srcPath = null;
                fsPath = IO.Path.GetFullPath(fsPath);

                // 1. if the import target is /Root, check if        \\source\System\Settings exists
                // 2. if the import target is /Root/System, check if \\source\Settings exists
                // 3. if the import target is /Root/System/Settings, import settings from \\source
                // 4. in any other case the source structure does not contain global settings

                if (RepositoryPathEquals(Repository.RootPath))
                    srcPath = IO.Path.Combine(fsPath, Repository.SystemFolderName, Repository.SettingsFolderName);
                else if (RepositoryPathEquals(Repository.SystemFolderPath))
                    srcPath = IO.Path.Combine(fsPath, Repository.SettingsFolderName);
                else if (RepositoryPathEquals(Repository.SettingsFolderPath))
                    srcPath = fsPath;

                if (srcPath == null || !IO.Directory.Exists(srcPath))
                    return;

                Log(ImportLogLevel.Info, "Installing global settings.");
                ImportContents(srcPath, Repository.SettingsFolderPath, false, true);
                Log(ImportLogLevel.Info, "Ok");
            }

            private bool SaveInitialIndexDocuments()
            {
                var idSet = DataProvider.LoadIdsOfNodesThatDoNotHaveIndexDocument(0, 1100);
                var nodes = Node.LoadNodes(idSet);
                var count = 0;

                if (nodes.Count == 0)
                    return false;

                Log(ImportLogLevel.Progress, "Create initial index documents.");

                foreach (var node in nodes)
                {
                    bool hasBinary;
                    DataBackingStore.SaveIndexDocument(node, false, false, out hasBinary);
                    Log(ImportLogLevel.Verbose, "  " + node.Path);
                    count++;
                }
                Log(ImportLogLevel.Verbose, "Ok.");
                return count > 0;
            }

            private void SetInitialPermissions()
            {
                Log(ImportLogLevel.Info, "Set initial permissions...");

                // ContentType ids
                var RootContentId = Identifiers.PortalRootId;
                var SystemFolderContentTypeId = ContentType.GetByName("SystemFolder").Id;
                var SurveyItemContentTypeId = ContentType.GetByName("SurveyItem")?.Id ?? 0;
                var VotingItemContentTypeId = ContentType.GetByName("VotingItem")?.Id ?? 0;
                var FormItemContentTypeId = ContentType.GetByName("FormItem")?.Id ?? 0;
                var RegistrationWorkflowContentTypeId = ContentType.GetByName("RegistrationWorkflow")?.Id ?? 0;
                var WorkspaceContentTypeId = ContentType.GetByName("Workspace").Id;
                var ContentListContentTypeId = ContentType.GetByName("ContentList").Id;
                var FileContentTypeId = ContentType.GetByName("File").Id;
                var ExecutableFileContentTypeId = ContentType.GetByName("ExecutableFile").Id;
                var ListItemContentTypeId = ContentType.GetByName("ListItem").Id;

                // Identity ids
                var AdministratorNodeId = Identifiers.AdministratorUserId;
                var AdministratorGroupNodeId = Identifiers.AdministratorsGroupId;
                var VisitorNodeId = Identifiers.VisitorUserId;
                var EveryoneGroupId = Identifiers.EveryoneGroupId;

                var DevelopersGroupId = Node.LoadNode("/Root/IMS/BuiltIn/Portal/Developers")?.Id ?? 0;
                var IdentifiedUsersGroupId = Node.LoadNode("/Root/IMS/BuiltIn/Portal/IdentifiedUsers")?.Id ?? 0;
                var contentExplorersGroup = Node.LoadNode("/Root/IMS/BuiltIn/Portal/ContentExplorers");

                // Set initial memberships: these relations must be set here manually because
                // these content were created by the install SQL scripts without the security 
                // component involved.
                var orgUnits = new List<OrganizationalUnit> { OrganizationalUnit.Portal }; // built-in orgunit for system groups and users
                orgUnits.AddRange(NodeQuery.QueryNodesByTypeAndPath(ActiveSchema.NodeTypes["OrganizationalUnit"], false, OrganizationalUnit.Portal.Path, true).Nodes.Cast<OrganizationalUnit>());

                foreach (var orgUnit in orgUnits)
                {
                    var users = orgUnit.Children.Where(c => c is User).Select(u => u.Id).ToArray();
                    var groups = orgUnit.Children.Where(c => c is Group || c is OrganizationalUnit).Select(g => g.Id).ToArray();

                    SecurityHandler.AddMembers(orgUnit.Id, users, groups);
                }

                // Created for several operations
                var aclEd = SecurityHandler.CreateAclEditor();

                // Break the permission inheritance on several content
                aclEd.BreakInheritance(SystemFolderContentTypeId)
                    .BreakInheritance(ExecutableFileContentTypeId)

                     // Allow all currently used permissions (all permissions except unused and custom ones) on SystemFolder for Administrators
                    .Allow(SystemFolderContentTypeId, AdministratorGroupNodeId, false, PermissionType.BuiltInPermissionTypes)

                    // Allow all currently used permissions (all permissions except unused and custom ones) on SystemFolder for Admin          
                    .Allow(SystemFolderContentTypeId, AdministratorNodeId, false, PermissionType.BuiltInPermissionTypes)

                    // Allow all currently used permissions (all permissions except unused and custom ones) on ExecutableFile for Administrators
                    .Allow(ExecutableFileContentTypeId, AdministratorGroupNodeId, false, PermissionType.BuiltInPermissionTypes);

                    // Allow See on public content types
                if (SurveyItemContentTypeId > 0)
                {
                    aclEd.Allow(SurveyItemContentTypeId, VisitorNodeId, false, PermissionType.See)
                        .Allow(SurveyItemContentTypeId, EveryoneGroupId, false, PermissionType.See);
                }
                if (VotingItemContentTypeId > 0)
                {
                    aclEd.Allow(VotingItemContentTypeId, VisitorNodeId, false, PermissionType.See)
                        .Allow(VotingItemContentTypeId, EveryoneGroupId, false, PermissionType.See);
                }

                if (FormItemContentTypeId > 0)
                {
                    aclEd.Allow(FormItemContentTypeId, VisitorNodeId, false, PermissionType.See)
                        .Allow(FormItemContentTypeId, EveryoneGroupId, false, PermissionType.See);
                }
                if (RegistrationWorkflowContentTypeId > 0)
                {
                    aclEd.Allow(RegistrationWorkflowContentTypeId, VisitorNodeId, false, PermissionType.See);
                }

                // Allow LOCAL See permissions on Root to let users invoke certain global actions
                aclEd.Allow(RootContentId, VisitorNodeId, true, PermissionType.See)
                    .Allow(RootContentId, EveryoneGroupId, true, PermissionType.See);

                if (DevelopersGroupId != 0)
                {
                    // Allow LOCAL ONLY See, Preview, Open on Root for Developers (to be able to open Content Explorer root)
                    aclEd.Allow(RootContentId, DevelopersGroupId, true, PermissionType.See, PermissionType.Preview, PermissionType.PreviewWithoutRedaction, PermissionType.PreviewWithoutWatermark, PermissionType.Open)
                        // Allow See, Open, RunApplication on SystemFolder for Developers
                        .Allow(SystemFolderContentTypeId, DevelopersGroupId, false, PermissionType.See, PermissionType.Open, PermissionType.RunApplication);
                }

                if (IdentifiedUsersGroupId != 0)
                {
                    // Allow See on common content types for Identified users (workspace, list, file, listitem)
                    aclEd.Allow(WorkspaceContentTypeId, IdentifiedUsersGroupId, false, PermissionType.See)
                        .Allow(ContentListContentTypeId, IdentifiedUsersGroupId, false, PermissionType.See)
                        .Allow(FileContentTypeId, IdentifiedUsersGroupId, false, PermissionType.See)
                        .Allow(ListItemContentTypeId, IdentifiedUsersGroupId, false, PermissionType.See);
                }
                if (contentExplorersGroup != null)
                {
                    aclEd.Allow(RootContentId, contentExplorersGroup.Id, true, PermissionType.Open);
                }

                // Apply all changes
                aclEd.Apply();
            }

            public void ImportContentTypeDefinitionsAndAspects(string ctdPath, string aspectsPath)
            {
                if (ctdPath != null && IO.Directory.Exists(ctdPath))
                {
                    Log(ImportLogLevel.Info, "Importing content types: " + ctdPath);

                    ContentTypeInstaller importer = ContentTypeInstaller.CreateBatchContentTypeInstaller();
                    var ctdFiles = IO.Directory.GetFiles(ctdPath, "*.xml");
                    foreach (var ctdFilePath in ctdFiles)
                    {
                        using (var stream = new IO.FileStream(ctdFilePath, IO.FileMode.Open, IO.FileAccess.Read))
                        {
                            try
                            {
                                Log(ImportLogLevel.Verbose, "  " + System.IO.Path.GetFileName(ctdFilePath));
                                importer.AddContentType(stream);
                            }
                            catch (ApplicationException e)
                            {
                                Logger.Errors++;
                                Log(ImportLogLevel.Info, "  SKIPPED: " + e.Message);
                            }
                        }
                    }
                    Log(ImportLogLevel.Progress, "  " + ctdFiles.Length + " file loaded...");

                    using (CreateProgressBar())
                        importer.ExecuteBatch();

                    Log(ImportLogLevel.Info, "  " + ctdFiles.Length + " CTD imported.");
                    Log(ImportLogLevel.Progress, "Ok");
                }
                else
                {
                    Log(ImportLogLevel.Info, "CTDs not changed");
                }

                // ==============================================================

                if (aspectsPath != null && IO.Directory.Exists(aspectsPath))
                {
                    if (!Node.Exists(Repository.AspectsFolderPath))
                    {
                        Log(ImportLogLevel.Info, "Creating aspect container (" + Repository.AspectsFolderPath + ")...");
                        Content.CreateNew(typeof(SystemFolder).Name, Repository.SchemaFolder, "Aspects").Save();
                        Log(ImportLogLevel.Info, "  Ok");
                    }

                    var aspectFiles = System.IO.Directory.GetFiles(aspectsPath, "*.content");
                    Log(ImportLogLevel.Info, "Importing aspects:");

                    ImportContents(aspectsPath, Repository.AspectsFolderPath, true, false);

                    Log(ImportLogLevel.Info, "  " + aspectFiles.Length + " aspect" + (aspectFiles.Length > 1 ? "s" : "") + " imported.");
                    Log(ImportLogLevel.Progress, "Ok");
                }
                else
                {
                    Log(ImportLogLevel.Info, "Aspects not changed.");
                }
            }

            private int _contentCount;

            // ImportContents
            public void ImportContents(string srcPath, string targetPath, bool aspects, bool settings)
            {
                bool pathIsFile = false;
                if (IO.File.Exists(srcPath))
                {
                    pathIsFile = true;
                }
                else if (!IO.Directory.Exists(srcPath))
                {
                    Log(ImportLogLevel.Info, "Source directory or file was not found: " + srcPath);
                    return;
                }

                var importTarget = Repository.Root as Node;
                if (!aspects && !settings)
                {
                    if (!string.IsNullOrEmpty(srcPath))
                        Log(ImportLogLevel.Info, "From: " + srcPath);
                    Log(ImportLogLevel.Info, "To:   " + targetPath);
                    Log(ImportLogLevel.Progress, "-------------------------------------------------------------");
                }

                if (targetPath != null)
                {
                    importTarget = Node.LoadNode(targetPath);
                    if (importTarget == null)
                    {
                        Log(ImportLogLevel.Info, "Target container was not found: " + targetPath);
                        return;
                    }
                }

                try
                {
                    HasReference = false;

                    _contentCount = 0;
                    using (CreateProgressBar())
                        TreeWalker(srcPath, pathIsFile, importTarget, "  ", aspects, settings);
                    Log(ImportLogLevel.Info, "{0} contents imported.", _contentCount);

                    if (HasReference)
                        UpdateReferences();
                }
                catch (Exception e)
                {                    
                    PrintException(e, null);
                }
            }

            private void TreeWalker(string path, bool pathIsFile, Node folder, string indent, bool aspects, bool settings)
            {
                // get entries
                // get contents
                // foreach contents
                //   create contentinfo
                //   entries.remove(content)
                //   entries.remove(contentinfo.attachments)
                // foreach entries
                //   create contentinfo
                if (!aspects)
                {
                    if (folder != null && (
                        String.Compare(folder.Path, Repository.AspectsFolderPath, StringComparison.InvariantCultureIgnoreCase) == 0 ||
                        String.Compare(folder.Path, Repository.ContentTypesFolderPath, StringComparison.InvariantCultureIgnoreCase) == 0))
                    {
                        if (LogLevel == ImportLogLevel.Progress)
                            Console.WriteLine();
                        Log(ImportLogLevel.Progress, "Skipped path: " + path);
                        return;
                    }
                }
                if (!settings)
                {
                    if (folder != null && (string.Compare(folder.Path, Repository.SettingsFolderPath, StringComparison.InvariantCultureIgnoreCase) == 0))
                    {
                        if (LogLevel == ImportLogLevel.Progress)
                            Console.WriteLine();
                        Log(ImportLogLevel.Progress, "Skipped path: " + path);
                        return;
                    }
                }

                string currentDir = pathIsFile ? IO.Path.GetDirectoryName(path) : path;
                List<ContentInfo> contentInfos = new List<ContentInfo>();
                List<string> paths;
                List<string> contentPaths;
                if (pathIsFile)
                {
                    paths = new List<string>(new string[] { path });
                    contentPaths = new List<string>();
                    if (path.ToLower().EndsWith(".content"))
                        contentPaths.Add(path);
                }
                else
                {
                    paths = new List<string>(IO.Directory.GetFileSystemEntries(path));
                    contentPaths = new List<string>(IO.Directory.GetFiles(path, "*.content"));
                }

                foreach (string contentPath in contentPaths)
                {
                    paths.Remove(contentPath);

                    try
                    {
                        var contentInfo = new ContentInfo(contentPath, folder);
                        contentInfos.Add(contentInfo);
                        foreach (string attachmentName in contentInfo.Attachments)
                        {
                            var attachmentPath = IO.Path.Combine(path, attachmentName);
                            RemovePath(paths, attachmentPath);

                            if (attachmentName == contentInfo.Name)
                            {
                                // Escaped children folder
                                var childrenPath = attachmentPath + ".Children";
                                if (IO.Directory.Exists(childrenPath))
                                {
                                    contentInfo.ChildrenFolder = childrenPath;
                                    RemovePath(paths, childrenPath);
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        PrintException(e, contentPath);
                    }
                }
                while (paths.Count > 0)
                {
                    try
                    {
                        var contentInfo = new ContentInfo(paths[0], folder);
                        contentInfos.Add(contentInfo);
                    }
                    catch (Exception e)
                    {
                        PrintException(e, paths[0]);
                    }

                    paths.RemoveAt(0);
                }

                // import local settings first
                contentInfos.Sort((a, b) => (a.Name == Repository.SettingsFolderName) ? -1 : ((b.Name == Repository.SettingsFolderName) ? 1 : 0));

                foreach (ContentInfo contentInfo in contentInfos)
                {
                    var stepDown = true;

                    var isNewContent = true;
                    Content content = null;

                    try
                    {
                        string mdp = contentInfo.MetaDataPath.Replace(FSPath, RepositoryPath).Replace('\\', '/');
                        string parentPath = SenseNet.ContentRepository.Storage.RepositoryPath.GetParentPath(mdp);
                        if (contentInfo.Delete)
                        {
                            var rpath = SenseNet.ContentRepository.Storage.RepositoryPath.Combine(parentPath, contentInfo.Name);
                            if (Node.Exists(rpath))
                            {
                                Log(ImportLogLevel.Verbose, indent + contentInfo.Name + " : [DELETE]");
                                Content.DeletePhysical(rpath);
                            }
                            else
                            {
                                Log(ImportLogLevel.Verbose, indent + contentInfo.Name + " : [already deleted]");
                            }
                        }
                        else
                        {
                            if (folder == null)
                            {
                                folder = Node.LoadNode(parentPath);
                            }
                            content = CreateOrLoadContent(contentInfo, folder, out isNewContent);
                        }
                    }
                    catch (Exception ex)
                    {
                        PrintException(ex, contentInfo.MetaDataPath);
                    }

                    if (content != null)
                    {
                        Log(ImportLogLevel.Verbose, "{0}{1} : {2} {3}", indent, contentInfo.Name, contentInfo.ContentTypeName, (isNewContent ? " [new]" : " [update]"));
                        _contentCount++;

                        // SetMetadata without references. Continue if the setting is false or exception was thrown.
                        try
                        {
                            if (!contentInfo.SetMetadata(content, currentDir, isNewContent, false))
                                PrintFieldErrors(content, contentInfo.MetaDataPath);
                            if (content.ContentHandler.Id == 0)
                                content.ContentHandler.Save();
                        }
                        catch (Exception e)
                        {
                            PrintException(e, contentInfo.MetaDataPath);
                            continue;
                        }

                        if (contentInfo.ClearPermissions)
                        {
                            content.ContentHandler.Security.RemoveExplicitEntries();
                            if (!(contentInfo.HasReference || contentInfo.HasPermissions || contentInfo.HasBreakPermissions))
                            {
                                content.ContentHandler.Security.RemoveBreakInheritance();
                            }
                        }
                        if (contentInfo.HasReference || contentInfo.HasPermissions || contentInfo.HasBreakPermissions || contentInfo.HasAspect)
                        {
                            LogWriteReference(contentInfo);
                            HasReference = true;
                        }
                    }

                    // recursion
                    if (stepDown && content != null)
                    {
                        Node node = null;
                        if (content != null)
                            node = content.ContentHandler;
                        if (node != null && (contentInfo.IsFolder || contentInfo.ChildrenFolder != null))
                        {
                            TreeWalker(contentInfo.ChildrenFolder, false, node, indent + "  ", aspects, settings);
                        }
                    }
                }
            }

            private static void RemovePath(List<string> paths, string attachmentPath)
            {
                if (!paths.Remove(attachmentPath))
                {
                    for (int i = 0; i < paths.Count; i++)
                    {
                        if (paths[i].Equals(attachmentPath, StringComparison.OrdinalIgnoreCase))
                        {
                            paths.RemoveAt(i);
                            break;
                        }
                    }
                }
            }

            private void UpdateReferences()
            {
                var count = 0;
                Log(ImportLogLevel.Progress, "---------------------------------- Permissions and references");
                using (CreateProgressBar())
                {
                    using (var reader = new IO.StreamReader(RefLogFilePath))
                    {
                        while (!reader.EndOfStream)
                        {
                            var s = reader.ReadLine();
                            var sa = s.Split('\t');
                            var id = int.Parse(sa[0]);
                            var path = sa[1];
                            UpdateReference(id, path);
                            count++;
                        }
                    }
                }
                Log(ImportLogLevel.Info, "{0} references and permissions updated.", count);
            }
            private void UpdateReference(int contentId, string metadataPath)
            {
                var contentInfo = new ContentInfo(metadataPath, null);

                Log(ImportLogLevel.Verbose, "  " + contentId + "\t" + contentInfo.Name);
                SNC.Content content = SNC.Content.Load(contentId);
                if (content != null)
                {
                    try
                    {
                        if (!contentInfo.UpdateReferences(content))
                            PrintFieldErrors(content, contentInfo.MetaDataPath);
                    }
                    catch (Exception e)
                    {
                        PrintException(e, contentInfo.MetaDataPath);
                    }
                }
                else
                {
                    Log(ImportLogLevel.Info, "---------- Content does not exist. MetaDataPath: {0}, ContentId: {1}, ContentTypeName: {2}",
                        contentInfo.MetaDataPath, contentInfo.ContentId, contentInfo.ContentTypeName);
                }
            }
            private Content CreateOrLoadContent(ContentInfo contentInfo, Node folder, out bool isNewContent)
            {
                var path = SenseNet.ContentRepository.Storage.RepositoryPath.Combine(folder.Path, contentInfo.Name);
                var content = Content.Load(path);

                if (content != null && !contentInfo.ContentTypeIsInferredFolder && content.ContentType.Name != contentInfo.ContentTypeName)
                {
                    throw new Exception(string.Format("Content {0} already exists but with a different type. Expected type: {1}, actual type: {2}.", content.Name, contentInfo.ContentTypeName, content.ContentType.Name));
                }

                if (content == null)
                {
                    content = Content.CreateNew(contentInfo.ContentTypeName, folder, contentInfo.Name);
                    isNewContent = true;
                }
                else
                {
                    isNewContent = false;
                }

                return content;
            }

            public void PrintException(Exception e, string path)
            {
                this.ErrorOccured = true;

                Logger.Errors++;
                Logger.LogMessage("========== Exception:");
                if (!String.IsNullOrEmpty(path))
                    Logger.LogMessage("Path: " + path);
                Logger.LogMessage("{0}: {1}", e.GetType().Name, e.Message);
                PrintTypeLoadError(e as ReflectionTypeLoadException);
                Logger.LogMessage(e.StackTrace);
                while ((e = e.InnerException) != null)
                {
                    Logger.LogMessage("{0}: {1}", e.GetType().Name, e.Message);
                    PrintTypeLoadError(e as ReflectionTypeLoadException);
                    Logger.LogMessage(e.StackTrace);
                }
                Logger.LogMessage("=====================");
            }
            private void PrintTypeLoadError(ReflectionTypeLoadException exc)
            {
                if (exc == null)
                    return;
                Logger.LogMessage("LoaderExceptions:");
                foreach (var e in exc.LoaderExceptions)
                {
                    Logger.LogMessage("-- {0}: {1}", e.GetType().FullName, e.Message);
                    var fileNotFoundException = e as IO.FileNotFoundException;
                    if (fileNotFoundException != null)
                    {
                        Logger.LogMessage("FUSION LOG:");
                        Logger.LogMessage(fileNotFoundException.FusionLog);
                    }
                }
            }
            private void PrintFieldErrors(Content content, string path)
            {
                this.ErrorOccured = true;

                Logger.Errors++;
                Logger.LogMessage("---------- Field Errors (path: {0}):", path);
                foreach (string fieldName in content.Fields.Keys)
                {
                    Field field = content.Fields[fieldName];
                    if (!field.IsValid)
                    {
                        Logger.LogMessage(field.Name + ": " + field.GetValidationMessage());
                    }
                }
                Logger.LogMessage("------------------------");
            }

            private bool RepositoryPathEquals(string path)
            {
                return RepositoryPath.Equals(path, StringComparison.InvariantCultureIgnoreCase);
            }

            // ================================================================================================================= Log

            private void Log(ImportLogLevel level, string format, params object[] parameters)
            {
                if (level > this.LogLevel)
                    return;
                Logger.LogMessage(format, parameters);
            }
            private Progress CreateProgressBar()
            {
                if (this.LogLevel != ImportLogLevel.Progress)
                    return new Progress();
                return new RealProgress();
            }

            // ================================================================================================================= ReferenceLog

            internal void LogWriteReference(ContentInfo contentInfo)
            {
                if (RefLogFilePath == null)
                    CreateRefLog(true);

                using (IO.StreamWriter writer = OpenLog(RefLogFilePath))
                    WriteToLog(writer, contentInfo.ContentId, '\t', contentInfo.MetaDataPath);
            }

            public void CreateRefLog(bool createNew)
            {
                RefLogFilePath = IO.Path.Combine(LogFolder, "importlog_" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + ".reflog");

                if (!IO.File.Exists(RefLogFilePath) || createNew)
                {
                    using (IO.FileStream fs = new IO.FileStream(RefLogFilePath, IO.FileMode.Create))
                    {
                        using (IO.StreamWriter wr = new IO.StreamWriter(fs))
                        {
                        }
                    }
                }
            }

            private static IO.StreamWriter OpenLog(string logFilePath)
            {
                return new IO.StreamWriter(logFilePath, true);
            }

            private void WriteToLog(IO.StreamWriter writer, params object[] values)
            {
                foreach (object value in values)
                {
                    writer.Write(value);
                }
                writer.WriteLine();
            }

            private class Progress : IDisposable
            {
                public virtual void Dispose()
                {
                    Console.WriteLine(">|");
                }
            }
            private class RealProgress : Progress
            {
                public override void Dispose()
                {
                    _timer.Stop();
                    _timer.Elapsed -= Timer_Elapsed;
                    _timer = null;
                    base.Dispose();
                }

                private Timer _timer = new Timer(5000);
                public RealProgress()
                {
                    Console.Write("|<");
                    _timer.Elapsed += Timer_Elapsed;
                    _timer.Start();
                }

                private void Timer_Elapsed(object sender, ElapsedEventArgs e)
                {
                    Console.Write("#");
                }
            }
        }

    }
}
