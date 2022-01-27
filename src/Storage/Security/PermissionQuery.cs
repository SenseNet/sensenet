using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.Configuration;
using SenseNet.Security;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Security
{
    public static class PermissionQuery
    {
        /// <summary>
        /// Returns users and groups that have any explicit permissions on the given content or its subtree.
        /// </summary>
        /// <param name="contentPath">Path of the content.</param>
        /// <param name="level">Filtering by permission level. It can be Allowed, Denied, AllowedOrDenied.</param>
        /// <param name="identityKind">Filtering by identity kind.</param>
        public static IEnumerable<Node> GetRelatedIdentities(string contentPath, PermissionLevel level, IdentityKind identityKind)
        {
            var head = NodeHead.Get(contentPath);
            if (head == null)
                throw new ContentNotFoundException(contentPath);
            return GetRelatedIdentities(head.Id, level, identityKind);
        }
        /// <summary>
        /// Returns users and groups that have any explicit permissions on the given content or its subtree.
        /// </summary>
        /// <param name="contentId">Id of the content.</param>
        /// <param name="level">Filtering by permission level. It can be Allowed, Denied, AllowedOrDenied.</param>
        /// <param name="identityKind">Filtering by identity kind.</param>
        public static IEnumerable<Node> GetRelatedIdentities(int contentId, PermissionLevel level, IdentityKind identityKind)
        {
            var identityIds = Providers.Instance.SecurityHandler.SecurityContext.GetRelatedIdentities(contentId, level);
            return Filter(identityIds, identityKind);
        }

        /// <summary>
        /// Collects all permission settings on the given content and its subtree related to the specified user or group.
        /// Output is grouped by permission types and can be filtered by the permission value or content type.
        /// </summary>
        /// <param name="contentPath">Path of the content.</param>
        /// <param name="level">Filtering by the permission value. It can be Allowed, Denied, AllowedOrDenied.</param>
        /// <param name="explicitOnly">Filter parameter for future use only. Allowed value is true.</param>
        /// <param name="identityId">Id of the group or user.</param>
        /// <param name="includedTypes">Filter by content type names.</param>
        public static IDictionary<PermissionType, int> GetRelatedPermissions(string contentPath, PermissionLevel level, bool explicitOnly, int identityId, IEnumerable<string> includedTypes)
        {
            var head = NodeHead.Get(contentPath);
            if (head == null)
                throw new ContentNotFoundException(contentPath);
            return GetRelatedPermissions(head.Id, level, explicitOnly, identityId, includedTypes);
        }

        /// <summary>
        /// Collects all permission settings on the given content and its subtree related to the specified user or group.
        /// Output is grouped by permission types and can be filtered by the permission value or content type.
        /// </summary>
        /// <param name="contentId">Id of the content.</param>
        /// <param name="level">Filtering by the permission value. It can be Allowed, Denied, AllowedOrDenied.</param>
        /// <param name="explicitOnly">Filter parameter for future use only. Allowed value is true.</param>
        /// <param name="identityId">Id of the group or user.</param>
        /// <param name="includedTypes">Filter by content type names.</param>
        public static IDictionary<PermissionType, int> GetRelatedPermissions(int contentId, PermissionLevel level, bool explicitOnly, int identityId, IEnumerable<string> includedTypes)
        {
            var filter = new ContentTypeFilterForGettingRelatedPermissions(includedTypes);
            var counters = Providers.Instance.SecurityHandler.SecurityContext.GetRelatedPermissions(contentId, level, explicitOnly, identityId, filter.IsEnabled);
            var result = new Dictionary<PermissionType, int>(PermissionType.PermissionCount);
            foreach (var item in counters)
                result.Add(PermissionType.GetByIndex(item.Key.Index), item.Value);
            return result;
        }
        public static IDictionary<PermissionType, int> GetExplicitPermissionsInSubtree(int contentId, int[] identities, bool includeRoot)
        {
            var counters = Providers.Instance.SecurityHandler.SecurityContext.GetExplicitPermissionsInSubtree(contentId, identities, includeRoot);
            var result = new Dictionary<PermissionType, int>(PermissionType.PermissionCount);
            foreach (var item in counters)
                result.Add(PermissionType.GetByIndex(item.Key.Index), item.Value);
            return result;
        }

        /// <summary>
        /// Returns all content in the requested content's subtree that have any permission setting
        /// filtered by permission value, user or group, and a permission list.
        /// </summary>
        /// <param name="contentPath">Path of the content.</param>
        /// <param name="level">Filtering by the permission value. It can be Allowed, Denied, AllowedOrDenied.</param>
        /// <param name="explicitOnly">Filter parameter for future use only. The currently allowed value is true.</param>
        /// <param name="identityId">Id of the group or user.</param>
        /// <param name="permissions">Only those content will appear in the output that have permission settings that are listed in this permissions list.</param>
        public static IEnumerable<Node> GetRelatedNodes(string contentPath, PermissionLevel level, bool explicitOnly, int identityId, IEnumerable<PermissionType> permissions)
        {
            var head = NodeHead.Get(contentPath);
            if (head == null)
                throw new ContentNotFoundException(contentPath);
            return GetRelatedNodes(head.Id, level, explicitOnly, identityId, permissions);
        }
        /// <summary>
        /// Returns all content in the requested content's subtree that have any permission setting
        /// filtered by permission value, user or group, and a permission mask.
        /// </summary>
        /// <param name="contentId">Id of the content.</param>
        /// <param name="level">Filtering by the permission value. It can be Allowed, Denied, AllowedOrDenied.</param>
        /// <param name="explicitOnly">Filter parameter for future use only. The currently allowed value is true.</param>
        /// <param name="identityId">Id of the group or user.</param>
        /// <param name="permissions">Only those content will appear in the output that have permission settings that are listed in this permissions list.</param>
        public static IEnumerable<Node> GetRelatedNodes(int contentId, PermissionLevel level, bool explicitOnly, int identityId, IEnumerable<PermissionType> permissions)
        {
            var contentIds = Providers.Instance.SecurityHandler.SecurityContext.GetRelatedEntities(contentId, level, explicitOnly, identityId, permissions);
            return new NodeList<Node>(contentIds);
        }

        /// <summary>
        /// Returns users and groups that have any explicit permissions on the given content or its subtree.
        /// </summary>
        /// <param name="contentPath">Path of the content.</param>
        /// <param name="level">Filtering by permission level. It can be Allowed, Denied, AllowedOrDenied.</param>
        /// <param name="identityKind">Filtering by identity kind.</param>
        /// <param name="permissions">Filtering by permission type.</param>
        public static IEnumerable<Node> GetRelatedIdentities(string contentPath, PermissionLevel level, IdentityKind identityKind, IEnumerable<PermissionTypeBase> permissions)
        {
            var head = NodeHead.Get(contentPath);
            if (head == null)
                throw new ContentNotFoundException(contentPath);
            return GetRelatedIdentities(head.Id, level, identityKind, permissions);
        }
        /// <summary>
        /// Returns users and groups that have any explicit permissions on the given content or its subtree.
        /// </summary>
        /// <param name="contentId">Id of the content.</param>
        /// <param name="level">Filtering by permission level. It can be Allowed, Denied, AllowedOrDenied.</param>
        /// <param name="identityKind">Filtering by identity kind.</param>
        /// <param name="permissions">Filtering by permission type.</param>
        public static IEnumerable<Node> GetRelatedIdentities(int contentId, PermissionLevel level, IdentityKind identityKind, IEnumerable<PermissionTypeBase> permissions)
        {
            var identityIds = Providers.Instance.SecurityHandler.SecurityContext.GetRelatedIdentities(contentId, level, permissions);
            return Filter(identityIds, identityKind);
        }

        /// <summary>
        /// Returns all content in the requested content's direct child collection that have any permission setting
        /// filtered by permission value, user or group, and a permission mask.
        /// </summary>
        /// <param name="contentPath">Path of the content.</param>
        /// <param name="level">Filtering by the permission value. It can be Allowed, Denied, AllowedOrDenied.</param>
        /// <param name="identityId">Id of the group or user.</param>
        /// <param name="permissions">Only those content will appear in the output that have permission settings that are listed in this permissions list.</param>
        public static IEnumerable<Node> GetRelatedNodesOneLevel(string contentPath, PermissionLevel level, int identityId, IEnumerable<PermissionTypeBase> permissions)
        {
            var head = NodeHead.Get(contentPath);
            if (head == null)
                throw new ContentNotFoundException(contentPath);
            return GetRelatedNodesOneLevel(head.Id, level, identityId, permissions);
        }
        /// <summary>
        /// Returns all content in the requested content's direct child collection that have any permission setting
        /// filtered by permission value, user or group, and a permission mask.
        /// </summary>
        /// <param name="contentId">Id of the content.</param>
        /// <param name="level">Filtering by the permission value. It can be Allowed, Denied, AllowedOrDenied.</param>
        /// <param name="identityId">Id of the group or user.</param>
        /// <param name="permissions">Only those content will appear in the output that have permission settings that are listed in this permissions list.</param>
        public static IEnumerable<Node> GetRelatedNodesOneLevel(int contentId, PermissionLevel level, int identityId, IEnumerable<PermissionTypeBase> permissions)
        {
            var folder = Node.LoadNode(contentId) as IFolder;
            if (folder == null)
                return new Node[0];

            var entityIds = Providers.Instance.SecurityHandler.SecurityContext.GetRelatedEntitiesOneLevel(contentId, level, identityId, permissions);
            return new NodeList<Node>(entityIds);
        }


        /// <summary>
        /// Returns Ids of all users that have all given permission on the entity.
        /// User will be resulted even if the permissions are granted on a group where she is member directly or indirectly.
        /// </summary>
        /// <param name="contentPath">Path of the content.</param>
        /// <param name="permissions">Only those users appear in the output that have permission settings in connection with the given permissions.</param>
        public static IEnumerable<Node> GetAllowedUsers(string contentPath, IEnumerable<PermissionType> permissions)
        {
            var head = NodeHead.Get(contentPath);
            if (head == null)
                throw new ContentNotFoundException(contentPath);
            return GetAllowedUsers(head.Id, permissions);
        }
        /// <summary>
        /// Returns Ids of all users that have all given permission on the entity.
        /// User will be resulted even if the permissions are granted on a group where she is member directly or indirectly.
        /// </summary>
        /// <param name="contentId">Id of the content.</param>
        /// <param name="permissions">Only those users appear in the output that have permission settings in connection with the given permissions.</param>
        public static IEnumerable<Node> GetAllowedUsers(int contentId, IEnumerable<PermissionType> permissions)
        {
            var contentIds = Providers.Instance.SecurityHandler.SecurityContext.GetAllowedUsers(contentId, permissions);
            return new NodeList<Node>(contentIds);
        }

        /// <summary>
        /// Returns Ids of all groups where the given user or group is member directly or indirectly.
        /// </summary>
        /// <param name="contentPath">Path of the group or user.</param>
        /// <param name="directOnly">Switch of the direct or indirect membership.</param>
        public static IEnumerable<Node> GetParentGroups(string contentPath, bool directOnly)
        {
            var head = NodeHead.Get(contentPath);
            if (head == null)
                throw new ContentNotFoundException(contentPath);
            return GetParentGroups(head.Id, directOnly);
        }
        /// <summary>
        /// Returns Ids of all groups where the given user or group is member directly or indirectly.
        /// </summary>
        /// <param name="contentId">Id of the group or user.</param>
        /// <param name="directOnly">Switch of the direct or indirect membership.</param>
        public static IEnumerable<Node> GetParentGroups(int contentId, bool directOnly)
        {
            var contentIds = Providers.Instance.SecurityHandler.SecurityContext.GetParentGroups(contentId, directOnly);
            return new NodeList<Node>(contentIds);
        }

        private static IEnumerable<Node> Filter(IEnumerable<int> identityIds, IdentityKind identityKind)
        {
            var identities = new NodeList<Node>(identityIds);
            switch (identityKind)
            {
                case IdentityKind.All: return identities;
                case IdentityKind.Users: return identities.Where(n => n is IUser);
                case IdentityKind.Groups: return identities.Where(n => n is IGroup);
                case IdentityKind.OrganizationalUnits: return identities.Where(n => n is IOrganizationalUnit);
                case IdentityKind.UsersAndGroups: return identities.Where(n => n is IUser || n is IGroup);
                case IdentityKind.UsersAndOrganizationalUnits: return identities.Where(n => n is IUser || n is IOrganizationalUnit);
                case IdentityKind.GroupsAndOrganizationalUnits: return identities.Where(n => n is ISecurityContainer);
                default: throw new SnNotSupportedException("Unknown IdentityKind: " + identityKind);
            }
        }
        private class ContentTypeFilterForGettingRelatedPermissions
        {
            private string[] _enabledTypes;
            public ContentTypeFilterForGettingRelatedPermissions(IEnumerable<string> enabledTypes)
            {
                if (enabledTypes != null)
                {
                    var types = enabledTypes.ToArray();
                    if (types.Length != 0)
                        _enabledTypes = types;
                }
            }
            public bool IsEnabled(int contentId)
            {
                var head = NodeHead.Get(contentId);
                if (head == null)
                    return false;

                if (_enabledTypes == null)
                    return true;

                var nodeType = Providers.Instance.StorageSchema.NodeTypes.GetItemById(head.NodeTypeId);
                if (nodeType == null)
                    return false;

                return _enabledTypes.Contains(nodeType.Name);
            }
        }
    }
}
