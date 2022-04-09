using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Security;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage
{
    /// <summary>
    /// Defines methods to prevent the executability of the  Content Delete, ForceDelete, Move operations.
    /// Supports an extendable whitelist of the not-deletable Content paths.
    /// </summary>
    public interface IContentProtector
    {
        /// <summary>
        /// Returns the whitelist of all protected paths.
        /// WARNING: The protected paths are sensitive information.
        /// </summary>
        string[] GetProtectedPaths();

        /// <summary>
        /// Gets the list of all protected groups.
        /// WARNING: The protected paths are sensitive information.
        /// </summary>
        string[] GetProtectedGroups();

        /// <summary>
        /// Gets the list of all protected group ids.
        /// WARNING: The protected ids are sensitive information.
        /// </summary>
        int[] GetProtectedGroupIds();

        /// <summary>
        /// If the whitelist contains the passed path, an <see cref="ApplicationException"/> will be thrown.
        /// WARNING: The protected paths are sensitive information.
        /// </summary>
        /// <param name="path">The examined path.</param>
        void AssertIsDeletable(string path);

        /// <summary>
        /// Adds the all passed paths and their complete ancestor axis to the whitelist of the not-deletable Contents.
        /// </summary>
        /// <param name="paths">One or pore paths that will be added to.</param>
        void AddPaths(params string[] paths);

        /// <summary>
        /// Adds the provided paths to the list of groups to protect.
        /// </summary>
        /// <param name="paths">Group paths.</param>
        void AddGroupPaths(params string[] paths);
    }

    /// <summary>
    /// This class can prevent the executability of the  Content Delete, ForceDelete, Move operations.
    /// Supports an extendable whitelist of the not-deletable Content paths.
    /// </summary>
    public class ContentProtector : IContentProtector
    {
        private readonly List<string> _protectedPaths = new List<string>
        {
            "/Root",
            "/Root/IMS",
            "/Root/IMS/BuiltIn",
            "/Root/IMS/BuiltIn/Portal",
            "/Root/IMS/BuiltIn/Portal/Admin",
            "/Root/IMS/BuiltIn/Portal/Administrators",
            "/Root/IMS/BuiltIn/Portal/AdminUIViewers",
            "/Root/IMS/BuiltIn/Portal/PublicAdmin",
            "/Root/IMS/BuiltIn/Portal/Visitor",
            "/Root/IMS/BuiltIn/Portal/Everyone",
            "/Root/IMS/Public",
            "/Root/IMS/Public/Administrators",
            "/Root/System",
            "/Root/System/Schema",
            "/Root/System/Schema/ContentTypes",
            "/Root/System/Schema/ContentTypes/ContentType",
            "/Root/System/Schema/ContentTypes/GenericContent",
            "/Root/System/Schema/ContentTypes/GenericContent/Application",
            "/Root/System/Schema/ContentTypes/GenericContent/Application/ApplicationOverride",
            "/Root/System/Schema/ContentTypes/GenericContent/Application/ClientApplication",
            "/Root/System/Schema/ContentTypes/GenericContent/Application/GenericODataApplication",
            "/Root/System/Schema/ContentTypes/GenericContent/Application/WebServiceApplication",
            "/Root/System/Schema/ContentTypes/GenericContent/ContentLink",
            "/Root/System/Schema/ContentTypes/GenericContent/EmailTemplate",
            "/Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent",
            "/Root/System/Schema/ContentTypes/GenericContent/File",
            "/Root/System/Schema/ContentTypes/GenericContent/File/ExecutableFile",
            "/Root/System/Schema/ContentTypes/GenericContent/File/Image",
            "/Root/System/Schema/ContentTypes/GenericContent/File/Image/PreviewImage",
            "/Root/System/Schema/ContentTypes/GenericContent/File/Settings",
            "/Root/System/Schema/ContentTypes/GenericContent/File/Settings/IndexingSettings",
            "/Root/System/Schema/ContentTypes/GenericContent/File/Settings/LoggingSettings",
            "/Root/System/Schema/ContentTypes/GenericContent/File/SystemFile",
            "/Root/System/Schema/ContentTypes/GenericContent/File/SystemFile/Resource",
            "/Root/System/Schema/ContentTypes/GenericContent/Folder",
            "/Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList",
            "/Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/Aspect",
            "/Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/ItemList",
            "/Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/ItemList/CustomList",
            "/Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/ItemList/EventList",
            "/Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/ItemList/LinkList",
            "/Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/ItemList/MemoList",
            "/Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/ItemList/TaskList",
            "/Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/Library",
            "/Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/Library/DocumentLibrary",
            "/Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/Library/ImageLibrary",
            "/Root/System/Schema/ContentTypes/GenericContent/Folder/Device",
            "/Root/System/Schema/ContentTypes/GenericContent/Folder/Domain",
            "/Root/System/Schema/ContentTypes/GenericContent/Folder/Domains",
            "/Root/System/Schema/ContentTypes/GenericContent/Folder/Email",
            "/Root/System/Schema/ContentTypes/GenericContent/Folder/OrganizationalUnit",
            "/Root/System/Schema/ContentTypes/GenericContent/Folder/PortalRoot",
            "/Root/System/Schema/ContentTypes/GenericContent/Folder/ProfileDomain",
            "/Root/System/Schema/ContentTypes/GenericContent/Folder/Profiles",
            "/Root/System/Schema/ContentTypes/GenericContent/Folder/RuntimeContentContainer",
            "/Root/System/Schema/ContentTypes/GenericContent/Folder/Sites",
            "/Root/System/Schema/ContentTypes/GenericContent/Folder/SmartFolder",
            "/Root/System/Schema/ContentTypes/GenericContent/Folder/SystemFolder",
            "/Root/System/Schema/ContentTypes/GenericContent/Folder/SystemFolder/Resources",
            "/Root/System/Schema/ContentTypes/GenericContent/Folder/TrashBag",
            "/Root/System/Schema/ContentTypes/GenericContent/Folder/Workspace",
            "/Root/System/Schema/ContentTypes/GenericContent/Folder/Workspace/TrashBin",
            "/Root/System/Schema/ContentTypes/GenericContent/Folder/Workspace/UserProfile",
            "/Root/System/Schema/ContentTypes/GenericContent/Group",
            "/Root/System/Schema/ContentTypes/GenericContent/Group/SharingGroup",
            "/Root/System/Schema/ContentTypes/GenericContent/ListItem",
            "/Root/System/Schema/ContentTypes/GenericContent/ListItem/CalendarEvent",
            "/Root/System/Schema/ContentTypes/GenericContent/ListItem/CustomListItem",
            "/Root/System/Schema/ContentTypes/GenericContent/ListItem/Link",
            "/Root/System/Schema/ContentTypes/GenericContent/ListItem/Memo",
            "/Root/System/Schema/ContentTypes/GenericContent/ListItem/Task",
            "/Root/System/Schema/ContentTypes/GenericContent/Query",
            "/Root/System/Schema/ContentTypes/GenericContent/User",
            "/Root/System/Schema/ContentTypes/GenericContent/WebHookSubscription"
        };
        private readonly List<string> _protectedGroups = new List<string>
        {
            "/Root/IMS/BuiltIn/Portal/Administrators",
            "/Root/IMS/Public/Administrators"
        };
        private Lazy<int[]> _protectedGroupIds;

        public ContentProtector()
        {
            ResetGroupIds();
        }

        private void ResetGroupIds()
        {
            _protectedGroupIds = new Lazy<int[]>(() => _protectedGroups
                .Select(gp => NodeHead.Get(gp)?.Id)
                .Where(gid => gid.HasValue)
                .Select(gid => gid.Value)
                .ToArray());
        }

        public string[] GetProtectedPaths()
        {
            return _protectedPaths.ToArray();
        }
        public string[] GetProtectedGroups()
        {
            return _protectedGroups.ToArray();
        }
        public int[] GetProtectedGroupIds()
        {
            return _protectedGroupIds.Value;
        }

        public void AssertIsDeletable(string path)
        {
            if (_protectedPaths.Contains(path, StringComparer.OrdinalIgnoreCase))
                throw new ApplicationException("Protected content cannot be deleted or moved.");

            if (AccessProvider.Current.GetOriginalUser() is Node user)
            {
                if (RepositoryPath.IsInTree(user.Path, path))
                    throw new ApplicationException("Users cannot delete or move themselves.");
            }
        }

        public void AddPaths(params string[] paths)
        {
            IEnumerable<string> GetAncestorAxis(string path)
            {
                var p = path.Length;
                while (true)
                {
                    path = path.Substring(0, p);
                    yield return path;
                    p = path.LastIndexOf("/", StringComparison.Ordinal);
                    if (path.Length <= Identifiers.RootPath.Length)
                        break;
                }
            }

            var allPaths = paths
                .SelectMany(GetAncestorAxis)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToArray();

            var protectedPaths = _protectedPaths;
            protectedPaths.AddRange(allPaths.Except(protectedPaths, StringComparer.OrdinalIgnoreCase));
        }

        public void AddGroupPaths(params string[] paths)
        {
            var newPaths = paths.Except(_protectedGroups).ToArray();
            if (newPaths.Length == 0)
                return;

            _protectedGroups.AddRange(newPaths);
            ResetGroupIds();
        }
    }
}
