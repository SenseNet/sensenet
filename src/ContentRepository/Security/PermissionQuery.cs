using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.ApplicationModel;
using SenseNet.Security;

namespace SenseNet.ContentRepository.Security
{
    public static class PermissionQuery
    {
        public static IEnumerable<Content> GetRelatedIdentities(Content content, PermissionLevel level, IdentityKind identityKind)
        {
            content.ContentHandler.Security.AssertSubtree(PermissionType.SeePermissions);
            return SecurityHandler.PermissionQuery.GetRelatedIdentities(content.Id, level, identityKind).Select(n => Content.Create(n));
        }
        public static IDictionary<PermissionType, int> GetRelatedPermissions(Content content, PermissionLevel level, bool explicitOnly, ISecurityMember member, IEnumerable<string> includedTypes)
        {
            content.ContentHandler.Security.AssertSubtree(PermissionType.SeePermissions);
            return SecurityHandler.PermissionQuery.GetRelatedPermissions(content.Id, level, explicitOnly, member.Id, includedTypes);
        }
        public static IDictionary<PermissionType, int> GetExplicitPermissionsInSubtree(Content content, int[] identities, bool includeRoot)
        {
            content.ContentHandler.Security.AssertSubtree(PermissionType.SeePermissions);
            return SecurityHandler.PermissionQuery.GetExplicitPermissionsInSubtree(content.Id, identities, includeRoot);
        }
        public static IEnumerable<Content> GetRelatedItems(Content content, PermissionLevel level, bool explicitOnly, ISecurityMember member, IEnumerable<PermissionType> permissions)
        {
            content.ContentHandler.Security.AssertSubtree(PermissionType.SeePermissions);
            return SecurityHandler.PermissionQuery.GetRelatedNodes(content.Id, level, explicitOnly, member.Id, permissions).Select(n => Content.Create(n));
        }

        public static IEnumerable<Content> GetRelatedIdentities(Content content, PermissionLevel level, IdentityKind identityKind, IEnumerable<PermissionType> permissions)
        {
            content.ContentHandler.Security.AssertSubtree(PermissionType.SeePermissions);
            return SecurityHandler.PermissionQuery.GetRelatedIdentities(content.Id, level, identityKind, permissions).Select(n => Content.Create(n));
        }
        public static IEnumerable<Content> GetRelatedItemsOneLevel(Content content, PermissionLevel level, ISecurityMember member, IEnumerable<PermissionType> permissions)
        {
            content.ContentHandler.Security.AssertSubtree(PermissionType.SeePermissions);
            return SecurityHandler.PermissionQuery.GetRelatedNodesOneLevel(content.Id, level, member.Id, permissions).Select(n => Content.Create(n));

        }

        public static IEnumerable<Content> GetAllowedUsers(Content content, IEnumerable<PermissionType> permissions)
        {
            content.ContentHandler.Security.Assert(PermissionType.SeePermissions);
            return SecurityHandler.PermissionQuery
                .GetAllowedUsers(content.Id, permissions)
                .Where(n => n is User)
                .Select(n => Content.Create(n));
        }

        public static IEnumerable<Content> GetParentGroups(Content content, bool directOnly)
        {
            return SecurityHandler.PermissionQuery.GetParentGroups(content.Id, directOnly).Select(n => Content.Create(n));
        }
    }
    public static class PermissionQueryForRest
    {
        // ============================================================================= Classes for serialization

        private class IdentityInfo
        {
            [JsonProperty("path")]
            public string Path { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("displayName")]
            public string DisplayName { get; set; }

            [JsonProperty("groups")]
            public GroupInfo[] Groups { get; set; }
        }
        private class GroupInfo
        {
            [JsonProperty("path")]
            public string Path { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("displayName")]
            public string DisplayName { get; set; }
        }
        private class ChildPermissionInfo
        {
            [JsonProperty("path")]
            public string Path { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("displayName")]
            public string DisplayName { get; set; }

            [JsonProperty("isFolder")]
            public bool IsFolder { get; set; }

            [JsonProperty("break")]
            public bool Break { get; set; }

            [JsonProperty("permissions")]
            public PermissionInfo[] Permissions { get; set; }

            [JsonProperty("subPermissions")]
            public PermissionInfo[] SubPermissions { get; set; }
        }
        private class PermissionInfo
        {
            [JsonProperty("name")]
            public string Name { get; set; }
            [JsonProperty("index")]
            public int Index { get; set; }
            [JsonProperty("type")]
            public string Type { get; set; }
            [JsonProperty("localOnly")]
            public bool LocalOnly { get; set; }
        }

        /* ============================================================================= OData operations */

        /// <summary>
        /// Returns users and groups that have any explicit permissions on the given content or its subtree.
        /// </summary>
        /// <snCategory>Permissions</snCategory>
        /// <param name="content"></param>
        /// <param name="permissionLevel">Filtering by permission level. It can be Allowed, Denied, AllowedOrDenied.</param>
        /// <param name="identityKind">Filtering by identity kind. Valid values are: All, Users, Groups, OrganizationalUnits, UsersAndGroups, UsersAndOrganizationalUnits, GroupsAndOrganizationalUnits</param>
        /// <returns><see cref="Content"/> list containing related users and groups according to the <paramref name="identityKind"/> filter.</returns>
        [ODataFunction]
        [ContentTypes(N.CT.GenericContent, N.CT.ContentType)]
        [AllowedRoles(N.R.Everyone)]
        public static IEnumerable<Content> GetRelatedIdentities(Content content, string permissionLevel, string identityKind)
        {
            var level = GetPermissionLevel(permissionLevel);
            var kind = GetIdentityKind(identityKind);
            return PermissionQuery.GetRelatedIdentities(content, level, kind);
        }

        /// <summary>
        /// Collects all permission settings on the given content and its subtree related to the specified user or group.
        /// Output is grouped by permission types and can be filtered by the permission value or content type.
        /// </summary>
        /// <snCategory>Permissions</snCategory>
        /// <param name="content"></param>
        /// <param name="permissionLevel">Filtering by the permission value. It can be Allowed, Denied, AllowedOrDenied.</param>
        /// <param name="explicitOnly">Filter parameter for future use only. Allowed value is true.</param>
        /// <param name="memberPath">Path of the group or user.</param>
        /// <param name="includedTypes">Optional filter containing zero, one or more content type names.</param>
        /// <returns>An associative array containing count of permission settings grouped by permissions. For example:
        /// { "See": 14, "Open": 5, "Save": 10, ...}</returns>
        [ODataFunction]
        [ContentTypes(N.CT.GenericContent, N.CT.ContentType)]
        [AllowedRoles(N.R.Everyone)]
        public static IDictionary<PermissionType, int> GetRelatedPermissions(Content content, string permissionLevel, bool explicitOnly, string memberPath, IEnumerable<string> includedTypes)
        {
            var level = GetPermissionLevel(permissionLevel);
            var member = GetMember(memberPath);
            return PermissionQuery.GetRelatedPermissions(content, level, explicitOnly, member, includedTypes);
        }

        /// <summary>
        /// Returns all content in the requested content's subtree that have any permission setting
        /// filtered by permission value, user or group, and a permission mask.
        /// </summary>
        /// <snCategory>Permissions</snCategory>
        /// <param name="content"></param>
        /// <param name="permissionLevel">Filtering by the permission value. It can be Allowed, Denied, AllowedOrDenied.</param>
        /// <param name="explicitOnly">Filter parameter for future use only. The currently allowed value is true.</param>
        /// <param name="memberPath">Path of the group or user.</param>
        /// <param name="permissions">Permission filter. Only those content will appear in the output that have permission settings that are listed in this permissions list.</param>
        /// <returns><see cref="Content"/> list.</returns>
        [ODataFunction]
        [ContentTypes(N.CT.GenericContent, N.CT.ContentType)]
        [AllowedRoles(N.R.Everyone)]
        public static IEnumerable<Content> GetRelatedItems(Content content, string permissionLevel, bool explicitOnly, string memberPath, string[] permissions)
        {
            var level = GetPermissionLevel(permissionLevel);
            var member = GetMember(memberPath);
            var perms = GetPermissionTypes(permissions);
            return PermissionQuery.GetRelatedItems(content, level, explicitOnly, member, perms);
        }

        /// <summary>
        /// Returns users and groups that have any explicit permissions on the given content or its subtree.
        /// </summary>
        /// <param name="permissionLevel">Filtering by permission level. It can be Allowed, Denied, AllowedOrDenied.</param>
        /// <param name="identityKind">Filtering by identity kind. Valid values are: All, Users, Groups, OrganizationalUnits, UsersAndGroups, UsersAndOrganizationalUnits, GroupsAndOrganizationalUnits</param>
        /// <param name="permissions">Filtering by permission type.</param>
        /// <returns>Filtered <see cref="Content"/> list that have queried permissions.</returns>
        [ODataFunction("GetRelatedIdentitiesByPermissions")]
        [ContentTypes(N.CT.GenericContent, N.CT.ContentType)]
        [AllowedRoles(N.R.Everyone)]
        public static IEnumerable<Content> GetRelatedIdentities(Content content, string permissionLevel, string identityKind, string[] permissions)
        {
            var level = GetPermissionLevel(permissionLevel);
            var perms = GetPermissionTypes(permissions);
            var kind = GetIdentityKind(identityKind);
            return PermissionQuery.GetRelatedIdentities(content, level, kind, perms);
        }

        /// <summary>
        /// Returns all content in the requested content's direct child collection that have any permission setting
        /// filtered by permission value, user or group, and a permission mask.
        /// </summary>
        /// <param name="content"></param>
        /// <param name="permissionLevel">Filtering by the permission value. It can be Allowed, Denied, AllowedOrDenied.</param>
        /// <param name="memberPath">Path of the group or user.</param>
        /// <param name="permissions">Only those content will appear in the output that have permission settings that are listed in this permissions list.</param>
        /// <returns>Filtered <see cref="Content"/> list that have queried permissions.</returns>
        [ODataFunction]
        [ContentTypes(N.CT.GenericContent, N.CT.ContentType)]
        [AllowedRoles(N.R.Everyone)]
        public static IEnumerable<Content> GetRelatedItemsOneLevel(Content content, string permissionLevel, string memberPath, string[] permissions)
        {
            var level = GetPermissionLevel(permissionLevel);
            var member = GetMember(memberPath);
            var perms = GetPermissionTypes(permissions);
            return PermissionQuery.GetRelatedItemsOneLevel(content, level, member, perms);
        }

        /// <summary>
        /// Returns all users that have all given permission on the entity.
        /// User will be resulted even if the permissions are granted on a group where she is member directly or indirectly.
        /// </summary>
        /// <param name="content"></param>
        /// <param name="permissions">Only those users appear in the output that have permission settings in connection with the given permissions.</param>
        /// <returns>Filtered <see cref="Content"/> list of the users that have queried permissions.</returns>
        [ODataFunction]
        [ContentTypes(N.CT.GenericContent, N.CT.ContentType)]
        [AllowedRoles(N.R.Everyone)]
        public static IEnumerable<Content> GetAllowedUsers(Content content, string[] permissions)
        {
            var perms = GetPermissionTypes(permissions);
            return PermissionQuery.GetAllowedUsers(content, perms);
        }

        /// <summary>
        /// Returns all groups where the given user or group is member directly or indirectly.
        /// </summary>
        /// <param name="content"></param>
        /// <param name="directOnly">Switch of the direct or indirect membership.</param>
        /// <returns><see cref="Content"/> list of the groups.</returns>
        [ODataFunction]
        [ContentTypes(N.CT.Group, N.CT.User)]
        [AllowedRoles(N.R.Everyone)]
        public static IEnumerable<Content> GetParentGroups(Content content, bool directOnly)
        {
            return PermissionQuery.GetParentGroups(content, directOnly);
        }

        /// <summary>
        /// Assembles an object containing identity information (basic fields and all groups), inherited and subtree permissions.
        /// The result object will contain permission infos only for the provided content.
        /// </summary>
        /// <snCategory>Permissions</snCategory>
        /// <param name="content"></param>
        /// <param name="identity">Path of the related user.</param>
        /// <returns>A PermissionInfo object.</returns>
        [ODataFunction]
        [ContentTypes(N.CT.GenericContent, N.CT.ContentType)]
        [AllowedRoles(N.R.Everyone)]
        public static object GetPermissionInfo(Content content, string identity)
        {
            return GetPermissionInfo(content, identity, true);
        }

        /// <summary>
        /// Assembles an object containing identity information (basic fields and all groups), inherited and subtree permissions.
        /// The result object will contain permission infos for the children of the requested content and not the root.
        /// </summary>
        /// <snCategory>Permissions</snCategory>
        /// <param name="content"></param>
        /// <param name="identity">Path of the related user.</param>
        /// <returns>A PermissionInfo object.</returns>
        [ODataFunction]
        [ContentTypes(N.CT.GenericContent, N.CT.ContentType)]
        [AllowedRoles(N.R.Everyone)]
        public static object GetChildrenPermissionInfo(Content content, string identity)
        {
            return GetPermissionInfo(content, identity, false);
        }

        private static object GetPermissionInfo(Content content, string identity, bool singleContent)
        {
            // This method assembles an object containing identity information (basic fields and all groups), 
            // inherited and subtree permissions that can be serialized and sent to the client.
            // If the singleContent parameter is true, permissions will be collected and returned
            // only for the provided content. Otherwise the result object will contain permission infos for
            // the children of the provided content and not the root.

            if (string.IsNullOrEmpty(identity))
                throw new ArgumentException("Please provide an identity path");

            var user = Node.Load<User>(identity);
            if (user == null)
                throw new ArgumentException("Identity must be an existing user.");

            // collect all groups for the user
            var groups = Node.LoadNodes(SecurityHandler.GetGroupsWithOwnership(content.Id, user)).Select(n => Content.Create(n)).ToArray();
            var identityInfo = new IdentityInfo
            {
                Path = user.Path,
                Name = user.Name,
                DisplayName = user.DisplayName,
                Groups = groups.Select(g => new GroupInfo { DisplayName = g.DisplayName, Name = g.Name, Path = g.Path }).ToArray()
            };

            // identities include all groups and the user itself
            var identities = groups.Select(g => g.Id).Concat(new[] { user.Id }).ToArray();

            // If we have to collect permissions for the provided content, we will need its parent
            // to check inherited permissions. If children are in the focus than the parent is the
            // provided content itself.
            var parent = singleContent ? content.ContentHandler.Parent : content.ContentHandler;
            var effectiveParentEntries = parent == null || !parent.Security.HasPermission(PermissionType.SeePermissions)
                ? new AceInfo[0]
                : parent.Security.GetEffectiveEntries(EntryType.Normal).Where(e => identities.Contains(e.IdentityId) && !e.LocalOnly).ToArray();

            var permissionsTypes = PermissionType.BuiltInPermissionTypes.Select(p => new PermissionInfo { Index = p.Index, Name = p.Name }).ToArray();

            // Collect all entries on the parent that are not local-only therefore
            // have effect on children.
            foreach (var entry in permissionsTypes)
            {
                if (effectiveParentEntries.Any(e => e.GetPermissionValues()[entry.Index] == PermissionValue.Denied))
                    entry.Type = "effectivedeny";
                else if (effectiveParentEntries.Any(e => e.GetPermissionValues()[entry.Index] == PermissionValue.Allowed))
                    entry.Type = "effectiveallow";
                else
                    entry.Type = "off";
            }

            if (singleContent)
            {
                // collect permissions for the provided single content
                return new
                {
                    d = new
                    {
                        identity = identityInfo,
                        permissionInfo = GetPermissionInfo(content, identities, permissionsTypes)
                    }
                };
            }

            // alternatively, collect permissions for child elements
            return new
            {
                d = new
                {
                    identity = identityInfo,
                    results = content.Children.DisableAutofilters().AsEnumerable()
                        .Select(child => GetPermissionInfo(child, identities, permissionsTypes)).ToArray()
                }
            };
        }

        private static ChildPermissionInfo GetPermissionInfo(Content content, int[] identities, PermissionInfo[] inheritedPermissions)
        {
            // This method assembles a permission info object describing the provided content
            // and all its inherited and subtree permissions as separate arrays.

            var canSeePermissions = content.Security.HasPermission(PermissionType.SeePermissions);

            // Load explicit entries on the content that belong to any of the relevant identities.
            // If the current user (who browses the permission overview page) has no SeePermission
            // permission for the content, simply use an empty array.
            var explicitPermissions = canSeePermissions
                ? content.Security.GetExplicitEntries(EntryType.Normal).Where(e => identities.Contains(e.IdentityId)).ToArray()
                : new AceInfo[0];
            var explicitLocalOnlyPermissions = explicitPermissions.Where(ace => ace.LocalOnly).ToArray();

            // prepare slots for all permission types
            var permissionInfos = canSeePermissions
                ? PermissionType.BuiltInPermissionTypes.Select(p => new PermissionInfo {Index = p.Index, Name = p.Name}).ToArray()
                : new PermissionInfo[0];

            // Check if there are explicit permissions defined for any of the permission types.
            // If not, use the inherited value.
            foreach (var permissionInfo in permissionInfos)
            {
                if (explicitPermissions.Any(e => e.GetPermissionValues()[permissionInfo.Index] == PermissionValue.Denied))
                    permissionInfo.Type = "explicitdeny";
                else if (explicitPermissions.Any(e => e.GetPermissionValues()[permissionInfo.Index] == PermissionValue.Allowed))
                    permissionInfo.Type = "explicitallow";
                else
                    permissionInfo.Type = content.Security.IsInherited
                        ? inheritedPermissions[permissionInfo.Index].Type
                        : "off";

                // This flag means that among others there is at least one local-only entry 
                // containing this permission type. It is possible that there are inheritable
                // permissions for the same type also, this is just a flag that indicates
                // that there is something local-only here.
                permissionInfo.LocalOnly = explicitLocalOnlyPermissions.Any(e => e.GetPermissionValues()[permissionInfo.Index] != PermissionValue.Undefined);
            }

            // Check subtree permission entries only if the user has SeePermission permission
            // in the whole subtree, otherwise we would show partial data to the user.
            var subtreePermissions = content.Security.HasSubTreePermission(PermissionType.SeePermissions)
                ? PermissionQuery.GetExplicitPermissionsInSubtree(content, identities, false)
                : new Dictionary<PermissionType, int>();

            return new ChildPermissionInfo
            {
                Path = content.Path,
                Name = content.Name,
                DisplayName = content.DisplayName,
                IsFolder = content.IsFolder,
                Break = !content.Security.IsInherited,
                Permissions = permissionInfos,
                SubPermissions = subtreePermissions.Select((p, index) =>
                new PermissionInfo
                {
                    Name = p.Key.ToString(),
                    Index = index,
                    Type = p.Value > 0 ? "on" : "off"
                }).Take(PermissionType.BuiltInPermissionTypes.Length).ToArray() //TODO: filter unused permissions correctly
            };
        }

        private static PermissionLevel GetPermissionLevel(string permissionLevel)
        {
            PermissionLevel level;
            if (!Enum.TryParse<PermissionLevel>(permissionLevel, true, out level))
                throw new ArgumentException(String.Format("Invalid permissionLevel argument: {0}, expected one of the following: {1}", permissionLevel,
                    String.Join(", ", Enum.GetNames(typeof(PermissionLevel)))));
            return level;
        }
        private static IdentityKind GetIdentityKind(string identityKind)
        {
            IdentityKind result;
            if (!Enum.TryParse<IdentityKind>(identityKind, true, out result))
                throw new ArgumentException(String.Format("Invalid identityKind argument: {0}, expected one of the following: {1}", identityKind,
                    String.Join(", ", Enum.GetNames(typeof(IdentityKind)))));
            return result;
        }
        private static ISecurityMember GetMember(string path)
        {
            var member = Node.LoadNode(path) as ISecurityMember;
            if (member == null)
                throw new ArgumentException("Invalid memberPath argument. Result content is not an ISecurityMember: " + path);
            return member;
        }
        private static IEnumerable<PermissionType> GetPermissionTypes(string[] names)
        {
            var types = new PermissionType[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                var pt = PermissionType.GetByName(names[i]);
                if (pt == null)
                    throw new ArgumentException("Unknown permission: " + names[i]);
                types[i] = pt;
            }
            return types;
        }
    }
}
