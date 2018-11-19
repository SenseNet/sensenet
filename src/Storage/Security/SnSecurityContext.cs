using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.Security;
using SenseNet.Security.Messaging;
using SenseNet.Diagnostics;

namespace SenseNet.ContentRepository.Storage.Security
{
    /// <summary>
    /// Implementation of the SecurityContext class defined in the SenseNet.Security component. 
    /// It is responsible for initializing the security component and making 
    /// Sense/Net-specific operations (e.g. permission checks) before the base calls 
    /// to the security component.
    /// Sense/Net must always call the methods of this class when a security-related 
    /// change happens in the Content Repository (e.g. a content structure or 
    /// membership change) to make the same change in the security component.
    /// Call these methods directly only if there is no appropriate one on the main 
    /// Sense/Net entry point: the SecurityHandler class.
    /// </summary>
    public class SnSecurityContext : SecurityContext
    {
        /// <summary>
        /// Gets the associated user instance.
        /// </summary>
        public new ISecurityUser CurrentUser { get { return base.CurrentUser; } }
        /// <summary>
        /// Gets the configured ISecurityDataProvider instance
        /// </summary>
        public new ISecurityDataProvider DataProvider { get { return base.DataProvider; } }

        /// <summary>
        /// Creates a new instance of the SecurityContext from the provided user object
        /// and pointers to the ISecurityDataProvider, IMessageProvider and SecurityCache global objects.
        /// </summary>
        public SnSecurityContext(IUser user) : base(user) { }

        /// <summary>
        /// Collects security-related information about a content and returns true if the content with 
        /// the specified id exists in the content repository and also fills the parent and owner ids.
        /// </summary>
        protected override bool GetMissingEntity(int contentId, out int parentId, out int ownerId)
        {
            var nodeHead = NodeHead.Get(contentId);
            if (nodeHead == null)
            {
                parentId = 0;
                ownerId = 0;
                return false;
            }
            parentId = nodeHead.ParentId;
            ownerId = nodeHead.OwnerId;
            return true;
        }

        /// <summary>
        /// Starts the security subsystem using the passed configuration.
        /// The method prepares and memorizes the main components for 
        /// creating SecurityContext instances in a fastest possible way.
        /// The main components are global objects: 
        /// ISecurityDataProvider instance, IMessageProvider instance and SecurityCache instance.
        /// </summary>
        public static new void StartTheSystem(SecurityConfiguration configuration)
        {
            SecurityContext.StartTheSystem(configuration);
            _generalContext = new SnSecurityContext(new SystemUser(null));
        }

        /// <summary>
        /// Creates a new context for the logged in user.
        /// </summary>
        public static SnSecurityContext Create()
        {
            return new SnSecurityContext(AccessProvider.Current.GetCurrentUser());
        }

        /// <summary>
        /// Empties the security database and memory.
        /// WARNING! Do not use this method in your code except in installing or developing scenarios.
        /// </summary>
        public new void DeleteAllAndRestart()
        {
            base.DeleteAllAndRestart();
        }
        
        /*********************** ACL API **********************/

        /// <summary>
        /// Creates a new instance of the SnAclEditor class for modifying permissions. 
        /// </summary>
        public new SnAclEditor CreateAclEditor(EntryType entryType = EntryType.Normal)
        {
            return SnAclEditor.Create(this, entryType);
        }
        /// <summary>
        /// Returns the AccessControlList of the passed content to help building a rich GUI for modifications.
        /// The current user must have SeePermissions permission on the requested content.
        /// The content must exist.
        /// </summary>
        public new AccessControlList GetAcl(int contentId, EntryType entryType = EntryType.Normal)
        {
            this.AssertPermission(contentId, PermissionType.SeePermissions);
            return base.GetAcl(contentId, entryType);
        }

        /// <summary>
        /// Returns an aggregated effective entries of the requested content.
        /// Inheritance information is not included.
        /// The content must exist.
        /// </summary>
        /// <param name="contentId">Id of the content.</param>
        /// <param name="relatedIdentities">Optional, can be null.
        /// If it is provided, the output will be filtered for the related identities or entry type.
        /// Empty collection means: nobody so in case of passing empty,
        /// the method will return with an empty list.</param>
        /// <param name="entryType">Optional filter parameter.
        /// If it is provided, the output contains only the matched entries.</param>
        public new List<AceInfo> GetEffectiveEntries(int contentId, IEnumerable<int> relatedIdentities = null, EntryType? entryType = null)
        {
            return base.GetEffectiveEntries(contentId, relatedIdentities, entryType);
        }
        /// <summary>
        /// Returns an aggregated effective entries of the requested content.
        /// Inheritance information is not included.
        /// The content must exist.
        /// </summary>
        /// <param name="contentId">Id of the content.</param>
        /// <param name="relatedIdentities">Optional, can be null.
        /// If it is provided, the output will be filtered for the related identities.
        /// Empty collection means: nobody so in case of passing empty,
        /// the method will return with empty list.</param>
        /// <param name="entryType">Optional filter parameter.
        /// If it is provided, the output contains only the matched entries.</param>
        public new List<AceInfo> GetExplicitEntries(int contentId, IEnumerable<int> relatedIdentities = null, EntryType? entryType = null)
        {
            return base.GetExplicitEntries(contentId, relatedIdentities, entryType);
        }

        /*********************** Evaluator API **********************/

        /// <summary>
        /// If one or more passed permissions are not allowed (undefined or denied)
        /// on the passed content for the current user,
        /// SenseNetSecurityException will be thrown.
        /// </summary>
        /// <param name="contentId">Id of the content. Cannot be 0.</param>
        /// <param name="permissions">Set of related permissions. Cannot be null.
        /// Empty set means "allowed nothing" so SenseNetSecurityException will be thrown.</param>
        public new void AssertPermission(int contentId, params PermissionTypeBase[] permissions)
        {
            base.AssertPermission(contentId, permissions);
        }
        /// <summary>
        /// If one or more passed permissions are not allowed (undefined or denied)
        /// on every content in whole subtree of the passed content for the current user,
        /// SenseNetSecurityException will be thrown.
        /// </summary>
        /// <param name="contentId">Id of the content. Cannot be 0.</param>
        /// <param name="permissions">Set of related permissions. Cannot be null.
        /// Empty set means "allowed nothing" so SenseNetSecurityException will be thrown.</param>
        public new void AssertSubtreePermission(int contentId, params PermissionTypeBase[] permissions)
        {
            base.AssertSubtreePermission(contentId, permissions);
        }
        /// <summary>
        /// Returns true if all passed permissions are allowed on the passed content for the current user.
        /// </summary>
        /// <param name="contentId">Id of the content. Cannot be 0.</param>
        /// <param name="permissions">Set of related permissions. Cannot be null. Empty set means "allowed nothing".</param>
        public new bool HasPermission(int contentId, params PermissionTypeBase[] permissions)
        {
            return base.HasPermission(contentId, permissions);
        }
        /// <summary>
        /// Returns true if all passed permissions are allowed for the current user on every content in the whole subtree of the passed content.
        /// </summary>
        /// <param name="contentId">Id of the content. Cannot be 0.</param>
        /// <param name="permissions">Set of related permissions. Cannot be null. Empty set means "allowed nothing".</param>
        public new bool HasSubtreePermission(int contentId, params PermissionTypeBase[] permissions)
        {
            return base.HasSubtreePermission(contentId, permissions);
        }
        /// <summary>
        /// Returns an aggregated permission value by all passed permissions for the current user on the passed content.
        /// Value is Denied if there is at least one denied among the passed permissions,
        ///   Undefined if there is an undefined and there is no denied among the passed permissions,
        ///   Allowed if every passed permission is allowed.
        /// </summary>
        /// <param name="contentId">Id of the content. Cannot be 0.</param>
        /// <param name="permissions">Set of related permissions. Cannot be null. Empty set means "allowed nothing".</param>
        public new PermissionValue GetPermission(int contentId, params PermissionTypeBase[] permissions)
        {
            return base.GetPermission(contentId, permissions);
        }
        /// <summary>
        /// Returns an aggregated permission value by all passed permissions for the current user on every content in the whole subtree of the passed content.
        /// Value is Denied if there is at least one denied among the passed permissions,
        ///   Undefined if there is an undefined and there is no denied among the passed permissions,
        ///   Allowed if every passed permission is allowed in the whole subtree.
        /// </summary>
        /// <param name="contentId">Id of the content. Cannot be 0.</param>
        /// <param name="permissions">Set of related permissions. Cannot be null. Empty set means "allowed nothing".</param>
        public new PermissionValue GetSubtreePermission(int contentId, params PermissionTypeBase[] permissions)
        {
            return base.GetSubtreePermission(contentId, permissions);
        }

        /*********************** Structure API **********************/

        /// <summary>
        /// Creates a new entity in the security component that represents a content in the repository. 
        /// If the entity already exists in the security db, creation is skipped.
        /// Parent content must exist.
        /// </summary>
        /// <param name="contentId">Id of the created content. Cannot be 0.</param>
        /// <param name="parentId">Id of the parent content. Cannot be 0.</param>
        /// <param name="ownerId">Id of the content's owner identity.</param>
        public new void CreateSecurityEntity(int contentId, int parentId, int ownerId)
        {
            using (var op = SnTrace.Security.StartOperation("CreateSecurityEntity id:{0}, parent:{1}, owner:{2}", contentId, parentId, ownerId))
            {
                base.CreateSecurityEntity(contentId, parentId, ownerId);
                op.Successful = true;
            }
        }
        /// <summary>
        /// Rewrites the owner of the content.
        /// </summary>
        /// <param name="contentId">Id of the content. Cannot be 0.</param>
        /// <param name="ownerId">Id of the content's owner identity.</param>
        public new void ModifyEntityOwner(int contentId, int ownerId)
        {
            using (var op = SnTrace.Security.StartOperation("ModifyEntityOwner id:{0}, owner:{1}", contentId, ownerId))
            {
                base.ModifyEntityOwner(contentId, ownerId);
                op.Successful = true;
            }
        }
        /// <summary>
        /// Deletes the entity and it's whole subtree including the related ACLs in the 
        /// security component after a content was deleted in the repository.
        /// </summary>
        /// <param name="contentId">Id of the content. Cannot be 0.</param>
        public new void DeleteEntity(int contentId)
        {
            using (var op = SnTrace.Security.StartOperation("DeleteEntity id:{0}", contentId))
            {
                base.DeleteEntity(contentId);
                op.Successful = true;
            }
        }
        /// <summary>
        /// Moves the entity and its whole subtree including the related ACLs in the 
        /// security component after a content was moved in the repository.
        /// </summary>
        /// <param name="sourceId">Id of the source content. Cannot be 0.</param>
        /// <param name="targetId">Id of the target content that will contain the source. Cannot be 0.</param>
        public new void MoveEntity(int sourceId, int targetId)
        {
            using (var op = SnTrace.Security.StartOperation("MoveEntity sourceId:{0}, targetId:{1}", sourceId, targetId))
            {
                base.MoveEntity(sourceId, targetId);
                op.Successful = true;
            }
        }
        /// <summary>
        /// Returns false if the content inherits the permissions from it's parent.
        /// </summary>
        /// <param name="contentId">Id of the content. Cannot be 0.</param>
        public new bool IsEntityInherited(int contentId)
        {
            return base.IsEntityInherited(contentId);
        }
        /// <summary>
        /// Returns true if the content exists as an entity in the security system.
        /// This method assumes that the entity exists and if not, executes a compensation algorithm
        /// that can repair a data integrity error (which may occur in case of a distributed system).
        /// The compensation works on two levels:
        /// 1 - loads the entity from the security database to memory.
        /// 2 - executes a callback to the repository (<see cref="GetMissingEntity"/>) for the entity info and saves the entity if it is needed.
        /// </summary>
        /// <param name="contentId">Id of the content.</param>
        public new bool IsEntityExist(int contentId)
        {
            return base.IsEntityExist(contentId);
        }

        /*********************** Public permission query API **********************/

        /// <summary>
        /// Returns all user and group ids that have any explicit permissions on the given content or its subtree.
        /// </summary>
        /// <param name="contentId">Id of the content.</param>
        /// <param name="level">Filtering by permission level. It can be Allowed, Denied, AllowedOrDenied.</param>
        public new IEnumerable<int> GetRelatedIdentities(int contentId, PermissionLevel level)
        {
            return base.GetRelatedIdentities(contentId, level);
        }
        /// <summary>
        /// Collects all permission settings on the given content and its subtree related to the specified user or group.
        /// Output is grouped by permission types and can be filtered by the permission value.
        /// </summary>
        /// <param name="contentId">Id of the content.</param>
        /// <param name="level">Filtering by the permission value. It can be Allowed, Denied, AllowedOrDenied.</param>
        /// <param name="explicitOnly">Filter parameter for future use only. Allowed value is true.</param>
        /// <param name="identityId">Id of the group or user.</param>
        /// <param name="isEnabled">Filter method that can enable or disable any content.</param>
        public new Dictionary<PermissionTypeBase, int> GetRelatedPermissions(int contentId, PermissionLevel level, bool explicitOnly, int identityId, Func<int, bool> isEnabled)
        {
            return base.GetRelatedPermissions(contentId, level, explicitOnly, identityId, isEnabled);
        }
        public new Dictionary<PermissionTypeBase, int> GetExplicitPermissionsInSubtree(int contentId, int[] identities, bool includeRoot)
        {
            return base.GetExplicitPermissionsInSubtree(contentId, identities, includeRoot);
        }
        /// <summary>
        /// Returns all content ids in the requested content's subtree that have any permission setting
        /// filtered by permission value, user or group, and a permission mask.
        /// </summary>
        /// <param name="contentId">Id of the content.</param>
        /// <param name="level">Filtering by the permission value. It can be Allowed, Denied, AllowedOrDenied.</param>
        /// <param name="explicitOnly">Filter parameter for future use only. The currently allowed value is true.</param>
        /// <param name="identityId">Id of the group or user.</param>
        /// <param name="permissions">Only those content will appear in the output that have permission settings that are listed in this permissions list.</param>
        public new IEnumerable<int> GetRelatedEntities(int contentId, PermissionLevel level, bool explicitOnly, int identityId, IEnumerable<PermissionTypeBase> permissions)
        {
            return base.GetRelatedEntities(contentId, level, explicitOnly, identityId, permissions);
        }
        /// <summary>
        /// Returns all user and group ids that have any explicit permissions on the given content and its subtree.
        /// </summary>
        /// <param name="contentId">Id of the content.</param>
        /// <param name="level">Filtering by permission level. It can be Allowed, Denied, AllowedOrDenied</param>
        /// <param name="permissions">Only those content will appear in the output that have permission settings that are listed in this permissions list.</param>
        public new IEnumerable<int> GetRelatedIdentities(int contentId, PermissionLevel level, IEnumerable<PermissionTypeBase> permissions)
        {
            return base.GetRelatedIdentities(contentId, level, permissions);
        }
        /// <summary>
        /// Returns all content ids in the requested content's children that have any permission setting
        /// filtered by permission value, user or group, and a permission mask
        /// </summary>
        /// <param name="contentId">Id of the content.</param>
        /// <param name="level">Filtering by the permission value. It can be Allowed, Denied, AllowedOrDenied.</param>
        /// <param name="identityId">Id of the group or user.</param>
        /// <param name="permissions">Only those content will appear in the output that have permission settings that are listed in this permissions list.</param>
        public new IEnumerable<int> GetRelatedEntitiesOneLevel(int contentId, PermissionLevel level, int identityId, IEnumerable<PermissionTypeBase> permissions)
        {
            return base.GetRelatedEntitiesOneLevel(contentId, level, identityId, permissions);
        }

        /// <summary>
        /// Returns Ids of all users that have all given permission on the entity.
        /// User will be resulted even if the permissions are granted on a group where she is member directly or indirectly.
        /// </summary>
        /// <param name="contentId">Id of the content.</param>
        /// <param name="permissions">Only those users appear in the output that have permission settings in connection with the given permissions.</param>
        public new IEnumerable<int> GetAllowedUsers(int contentId, IEnumerable<PermissionTypeBase> permissions)
        {
            return base.GetAllowedUsers(contentId, permissions);
        }
        /// <summary>
        /// Returns Ids of all groups where the given user or group is member directly or indirectly.
        /// </summary>
        /// <param name="contentId">Id of the group or user.</param>
        /// <param name="directOnly">Switch of the direct or indirect membership.</param>
        public new IEnumerable<int> GetParentGroups(int contentId, bool directOnly)
        {
            return base.GetParentGroups(contentId, directOnly);
        }

        /*********************** Membership API **********************/

        /// <summary>
        /// Gets the ids of all the groups that contain the current user as a member, even through other groups.
        /// </summary>
        public new int[] GetFlattenedGroups()
        {
            return base.GetFlattenedGroups();
        }
        /// <summary>
        /// Gets the ids of all the groups that contain the current user as a member, even through other groups,
        /// plus Everyone (except in case of a visitor) and the optional dynamic groups provided by the 
        /// membership extender.
        /// </summary>
        public new List<int> GetGroups()
        {
            return base.GetGroups();
        }
        /// <summary>
        /// Gets the ids of all the groups that contain the current user as a member, even through other groups,
        /// plus Everyone (except in case of a visitor), plus Owners (if applicable) and the optional 
        /// dynamic groups provided by the membership extender.
        /// </summary>
        public new List<int> GetGroupsWithOwnership(int entityId)
        {
            return base.GetGroupsWithOwnership(entityId);
        }

        /// <summary>
        /// Determines if the provided member (user or group) is a member of a group. This method
        /// is transitive, meaning it will look for relations in the whole group graph, not 
        /// only direct memberships.
        /// </summary>
        public new bool IsInGroup(int memberId, int groupId)
        {
            return base.IsInGroup(memberId, groupId);
        }

        /// <summary>
        /// Adds different kinds of members to a group in one step.
        /// Non-existing groups or member groups will be created.
        /// If all the parameters are null or empty, nothing will happen.
        /// </summary>
        /// <param name="groupId">Identifier of the container group. Cannot be 0.</param>
        /// <param name="userMembers">Collection of user member identifiers. Can be null or empty.</param>
        /// <param name="groupMembers">Collection of group member identifiers. Can be null or empty.</param>
        /// <param name="parentGroups">Collection of parent group identifiers. Use this if the parent 
        /// group or groups are already known when this method is called. Can be null or empty.</param>
        public new void AddMembersToSecurityGroup(int groupId, IEnumerable<int> userMembers, IEnumerable<int> groupMembers, IEnumerable<int> parentGroups = null)
        {
            using (var op = SnTrace.Security.StartOperation("AddMembersToSecurityGroup: groupId:{0}, userMembers:[{1}], groupMembers:[{2}], parentGroups:[{3}]",
                groupId, string.Join(",", userMembers ?? new int[0]), string.Join(",", groupMembers ?? new int[0]), string.Join(",", parentGroups ?? new int[0])))
            {
                base.AddMembersToSecurityGroup(groupId, userMembers, groupMembers, parentGroups);
                op.Successful = true;
            }
        }
        /// <summary>
        /// Removes multiple kinds of members from a group in one step.
        /// Non-existing groups or member groups will be skipped.
        /// If all the parameters are null or empty, nothing will happen.
        /// </summary>
        /// <param name="groupId">Identifier of the container group. Cannot be 0.</param>
        /// <param name="userMembers">Collection of user member identifiers. Can be null or empty.</param>
        /// <param name="groupMembers">Collection of group member identifiers. Can be null or empty.</param>
        /// <param name="parentGroups">Collection of parent group identifiers. Can be null or empty.</param>
        public new void RemoveMembersFromSecurityGroup(int groupId, IEnumerable<int> userMembers, IEnumerable<int> groupMembers, IEnumerable<int> parentGroups = null)
        {
            using (var op = SnTrace.Security.StartOperation("RemoveMembersFromSecurityGroup: groupId:{0}, userMembers:[{1}], groupMembers:[{2}], parentGroups:[{3}]",
                groupId, string.Join(",", userMembers), string.Join(",", groupMembers ?? new int[0]), string.Join(",", parentGroups ?? new int[0])))
            {
                base.RemoveMembersFromSecurityGroup(groupId, userMembers, groupMembers, parentGroups);
                op.Successful = true;
            }
        }

        /// <summary>
        /// Add one or more group members to a group. If the main group or any member is unknown it will be created.
        /// This method is a shortcut for AddMembersToSecurityGroup(...).
        /// </summary>
        /// <param name="groupId">Identifier of the container group. Cannot be 0.</param>
        /// <param name="groupMembers">Collection of the group member identifiers. Can be null or empty.</param>
        public new void AddGroupsToSecurityGroup(int groupId, IEnumerable<int> groupMembers)
        {
            using (var op = SnTrace.Security.StartOperation("AddGroupsToSecurityGroup: groupId:{0}, groupMembers:[{1}]", groupId, string.Join(",", groupMembers ?? new int[0])))
            {
                base.AddGroupsToSecurityGroup(groupId, groupMembers);
                op.Successful = true;
            }
        }
        /// <summary>
        /// Add a group as a member of one or more parent groups. If the main group or any parent is unknown it will be created.
        /// This method is a shortcut for AddMembersToSecurityGroup(...).
        /// </summary>
        /// <param name="groupId">Identifier of the member group. Cannot be 0.</param>
        /// <param name="parentGroups">Collection of the parent group identifiers. Can be null or empty.</param>
        public new void AddGroupToSecurityGroups(int groupId, IEnumerable<int> parentGroups)
        {
            using (var op = SnTrace.Security.StartOperation("AddGroupToSecurityGroups: groupId:{0}, parentGroups:[{1}]", groupId, string.Join(",", string.Join(",", parentGroups ?? new int[0]))))
            {
                base.AddGroupToSecurityGroups(groupId, parentGroups);
                op.Successful = true;
            }
        }
        /// <summary>
        /// Removes one or more group members from a group in one step.
        /// Non-existing group or member groups will be skipped.
        /// This method is a shortcut for RemoveMembersFromSecurityGroup(...).
        /// </summary>
        /// <param name="groupId">Identifier of the container group. Cannot be 0.</param>
        /// <param name="groupMembers">Collection of the group member identifiers. Can be null or empty.</param>
        public new void RemoveGroupsFromSecurityGroup(int groupId, IEnumerable<int> groupMembers)
        {
            using (var op = SnTrace.Security.StartOperation("RemoveGroupsFromSecurityGroup: groupId:{0}, groupMembers:[{1}]", groupId, string.Join(",", groupMembers ?? new int[0])))
            {
                base.RemoveGroupsFromSecurityGroup(groupId, groupMembers);
                op.Successful = true;
            }
        }
        /// <summary>
        /// Removes a group from one or more parent groups
        /// Non-existing group or parent groups will be skipped.
        /// This method is a shortcut for RemoveMembersFromSecurityGroup(...).
        /// </summary>
        /// <param name="groupId">Identifier of the member group. Cannot be 0.</param>
        /// <param name="parentGroups">Collection of the parent group identifiers. Can be null or empty.</param>
        public new void RemoveGroupFromSecurityGroups(int groupId, IEnumerable<int> parentGroups)
        {
            using (var op = SnTrace.Security.StartOperation("RemoveGroupFromSecurityGroups: groupId:{0}, groupMembers:[{1}]", groupId, string.Join(",", parentGroups ?? new int[0])))
            {
                base.RemoveGroupFromSecurityGroups(groupId, parentGroups);
                op.Successful = true;
            }
        }
        /// <summary>
        /// Adds one or more users to a group in one step.
        /// Non-existing group will be created.
        /// This method is a shortcut for AddMembersToSecurityGroup(...).
        /// </summary>
        /// <param name="groupId">Identifier of the container group. Cannot be 0.</param>
        /// <param name="userMembers">Collection of the user member identifiers. Can be null or empty.</param>
        public new void AddUsersToSecurityGroup(int groupId, IEnumerable<int> userMembers)
        {
            using (var op = SnTrace.Security.StartOperation("AddUsersToSecurityGroup: groupId:{0}, userMembers:[{1}]", groupId, string.Join(",", userMembers ?? new int[0])))
            {
                base.AddUsersToSecurityGroup(groupId, userMembers);
                op.Successful = true;
            }
        }
        /// <summary>
        /// Add a user to one or more groups in one step.
        /// Non-existing groups will be created.
        /// </summary>
        /// <param name="userId">Identifier of the the user member that will be added. Cannot be 0.</param>
        /// <param name="parentGroups">Collection of the parent group identifiers. Can be null or empty.</param>
        public new void AddUserToSecurityGroups(int userId, IEnumerable<int> parentGroups)
        {
            using (var op = SnTrace.Security.StartOperation("AddUserToSecurityGroups: userId:{0}, parentGroups:[{1}]", userId, string.Join(",", parentGroups ?? new int[0])))
            {
                base.AddUserToSecurityGroups(userId, parentGroups);
                op.Successful = true;
            }
        }
        /// <summary>
        /// Removes a user from one or more groups in one step.
        /// Non-existing group or member will be skipped.
        /// </summary>
        /// <param name="userId">Identifier of the user the will be removed. Cannot be 0.</param>
        /// <param name="parentGroups">Collection of the parent group identifiers. Can be null or empty.</param>
        public new void RemoveUserFromSecurityGroups(int userId, IEnumerable<int> parentGroups)
        {
            using (var op = SnTrace.Security.StartOperation("RemoveUserFromSecurityGroups: userId:{0}, parentGroups:[{1}]", userId, string.Join(",", parentGroups ?? new int[0])))
            {
                base.RemoveUserFromSecurityGroups(userId, parentGroups);
                op.Successful = true;
            }
        }
        /// <summary>
        /// Removes one or more users from a group in one step.
        /// Non-existing group or member will be skipped.
        /// This method is a shortcut for RemoveMembersFromSecurityGroup(...).
        /// </summary>
        /// <param name="groupId">Identifier of the container group. Cannot be 0.</param>
        /// <param name="userMembers">Collection of the user member identifiers. Can be null or empty.</param>
        public new void RemoveUsersFromSecurityGroup(int groupId, IEnumerable<int> userMembers)
        {
            using (var op = SnTrace.Security.StartOperation("RemoveUsersFromSecurityGroup: groupId:{0}, userMembers:[{1}]", groupId, string.Join(",", userMembers ?? new int[0])))
            {
                base.RemoveUsersFromSecurityGroup(groupId, userMembers);
                op.Successful = true;
            }
        }

        /// <summary>
        /// Deletes the specified group and its relations including related security entries.
        /// </summary>
        public new void DeleteSecurityGroup(int groupId)
        {
            using (var op = SnTrace.Security.StartOperation("DeleteSecurityGroup: id:{0}", groupId))
            {
                base.DeleteSecurityGroup(groupId);
                op.Successful = true;
            }
        }
        /// <summary>
        /// Deletes the user from the system by removing all memberships and security entries related to this user.
        /// </summary>
        public new void DeleteUser(int userId)
        {
            using (var op = SnTrace.Security.StartOperation("DeleteUser: id:{0}", userId))
            {
                base.DeleteUser(userId);
                op.Successful = true;
            }
        }
        /// <summary>
        /// Deletes the specified group or user and its relations including related security entries.
        /// </summary>
        public new void DeleteIdentity(int id)
        {
            using (var op = SnTrace.Security.StartOperation("DeleteIdentity: id:{0}", id))
            {
                base.DeleteIdentity(id);
                op.Successful = true;
            }
        }
        /// <summary>
        /// Deletes the specified groups or users and their relations including related security entries.
        /// </summary>
        public new void DeleteIdentities(IEnumerable<int> ids)
        {
            using (var op = SnTrace.Security.StartOperation("DeleteIdentities: ids:[{0}]", string.Join(",", ids ?? new int[0])))
            {
                base.DeleteIdentities(ids);
                op.Successful = true;
            }
        }

        /***************** General context for built in system user ***************/

        private static SnSecurityContext _generalContext;
        internal static SnSecurityContext General
        {
            get { return _generalContext; }
        }

        /***************** Debug info ***************/

        /// <summary>
        /// Returns an object that conains information about the execution of the last few SecurityActivities.
        /// </summary>
        public new SecurityActivityHistory GetRecentActivities()
        {
            //TODO: secu: permission check for GetRecentActivities
            return base.GetRecentActivities();
        }
        /// <summary>WARNING! Do not use this method in your code. Used in consistency checker tool.</summary>
        public new IEnumerable<long> GetCachedMembershipForConsistencyCheck()
        {
            //TODO: secu: permission check for GetCachedMembershipForConsistencyCheck
            return base.GetCachedMembershipForConsistencyCheck();
        }
        /// <summary>WARNING! Do not use this method in your code. Used in consistency checker tool.</summary>
        public new void GetFlatteningForConsistencyCheck(out IEnumerable<long> missingInFlattening, out IEnumerable<long> unknownInFlattening)
        {
            //TODO: secu: permission check for GetFlatteningForConsistencyCheck
            base.GetFlatteningForConsistencyCheck(out missingInFlattening, out unknownInFlattening);
        }
        /// <summary>WARNING! Do not use this method in your code. Used in consistency checker tool.</summary>
        internal new IDictionary<int, SecurityEntity> GetCachedEntitiesForConsistencyCheck()
        {
            return base.GetCachedEntitiesForConsistencyCheck();
        }
    }
}
