using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Security;
using SenseNet.Security.Messaging;
using SenseNet.Diagnostics;

//TODO: Cleanup documentation and unnecessary overrides.
/**/
// ReSharper disable once CheckNamespace
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
        /// Creates a new instance of the SecurityContext from the provided user object
        /// and pointers to the ISecurityDataProvider, IMessageProvider and SecurityCache global objects.
        /// </summary>
        public SnSecurityContext(IUser user, SecuritySystem securitySystem) : base(user, securitySystem) { }

        /*********************** ACL API **********************/

        /// <summary>
        /// Creates a new instance of the AclEditor class for modifying permissions. 
        /// </summary>
        public override AclEditor CreateAclEditor(EntryType entryType = EntryType.Normal)
        {
            return SnAclEditor.Create(this, entryType);
        }
        /// <summary>
        /// Creates a new instance of the SnAclEditor class for modifying permissions. 
        /// </summary>
        public SnAclEditor CreateSnAclEditor(EntryType entryType = EntryType.Normal)
        {
            return SnAclEditor.Create(this, entryType);
        }

        /// <summary>
        /// Returns the AccessControlList of the passed content to help building a rich GUI for modifications.
        /// The current user must have SeePermissions permission on the requested content.
        /// The content must exist.
        /// </summary>
        public override AccessControlList GetAcl(int contentId, EntryType entryType = EntryType.Normal)
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
        public override List<AceInfo> GetEffectiveEntries(int contentId, IEnumerable<int> relatedIdentities = null, EntryType? entryType = null)
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
        public override List<AceInfo> GetExplicitEntries(int contentId, IEnumerable<int> relatedIdentities = null, EntryType? entryType = null)
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
        public override void AssertPermission(int contentId, params PermissionTypeBase[] permissions)
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
        public override void AssertSubtreePermission(int contentId, params PermissionTypeBase[] permissions)
        {
            base.AssertSubtreePermission(contentId, permissions);
        }
        /// <summary>
        /// Returns true if all passed permissions are allowed on the passed content for the current user.
        /// </summary>
        /// <param name="contentId">Id of the content. Cannot be 0.</param>
        /// <param name="permissions">Set of related permissions. Cannot be null. Empty set means "allowed nothing".</param>
        public override bool HasPermission(int contentId, params PermissionTypeBase[] permissions)
        {
            return base.HasPermission(contentId, permissions);
        }
        /// <summary>
        /// Returns true if all passed permissions are allowed for the current user on every content in the whole subtree of the passed content.
        /// </summary>
        /// <param name="contentId">Id of the content. Cannot be 0.</param>
        /// <param name="permissions">Set of related permissions. Cannot be null. Empty set means "allowed nothing".</param>
        public override bool HasSubtreePermission(int contentId, params PermissionTypeBase[] permissions)
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
        public override PermissionValue GetPermission(int contentId, params PermissionTypeBase[] permissions)
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
        public override PermissionValue GetSubtreePermission(int contentId, params PermissionTypeBase[] permissions)
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
        [Obsolete("Use async version instead.", true)]//UNDONE:xxx0:AsyncSecu: change to true
        public override void CreateSecurityEntity(int contentId, int parentId, int ownerId)
        {
            using (var op = SnTrace.Security.StartOperation("CreateSecurityEntity id:{0}, parent:{1}, owner:{2}", contentId, parentId, ownerId))
            {
                base.CreateSecurityEntity(contentId, parentId, ownerId);
                op.Successful = true;
            }
        }
        /// <summary>
        /// Creates a new entity in the security component that represents a content in the repository. 
        /// If the entity already exists in the security db, creation is skipped.
        /// Parent content must exist.
        /// </summary>
        /// <param name="contentId">Id of the created content. Cannot be 0.</param>
        /// <param name="parentId">Id of the parent content. Cannot be 0.</param>
        /// <param name="ownerId">Id of the content's owner identity.</param>
        /// <param name="cancel">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public override async Task CreateSecurityEntityAsync(int contentId, int parentId, int ownerId, CancellationToken cancel)
        {
            using var op = SnTrace.Security.StartOperation(() =>
                $"CreateSecurityEntity id:{contentId}, parent:{parentId}, owner:{ownerId}");
            await base.CreateSecurityEntityAsync(contentId, parentId, ownerId, cancel).ConfigureAwait(false);
            op.Successful = true;
        }

        /// <summary>
        /// Rewrites the owner of the content.
        /// </summary>
        /// <param name="contentId">Id of the content. Cannot be 0.</param>
        /// <param name="ownerId">Id of the content's owner identity.</param>
        [Obsolete("Use async version instead.", true)]//UNDONE:xxx0:AsyncSecu: change to true
        public override void ModifyEntityOwner(int contentId, int ownerId)
        {
            using (var op = SnTrace.Security.StartOperation("ModifyEntityOwner id:{0}, owner:{1}", contentId, ownerId))
            {
                base.ModifyEntityOwner(contentId, ownerId);
                op.Successful = true;
            }
        }
        /// <summary>
        /// Rewrites the owner of the content.
        /// </summary>
        /// <param name="contentId">Id of the content. Cannot be 0.</param>
        /// <param name="ownerId">Id of the content's owner identity.</param>
        /// <param name="cancel">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public override async Task ModifyEntityOwnerAsync(int contentId, int ownerId, CancellationToken cancel)
        {
            using var op = SnTrace.Security.StartOperation(() =>
                $"ModifyEntityOwner id:{contentId}, owner:{ownerId}");
            await base.ModifyEntityOwnerAsync(contentId, ownerId, cancel).ConfigureAwait(false);
            op.Successful = true;
        }

        /// <summary>
        /// Deletes the entity and it's whole subtree including the related ACLs in the 
        /// security component after a content was deleted in the repository.
        /// </summary>
        /// <param name="contentId">Id of the content. Cannot be 0.</param>
        [Obsolete("Use async version instead.", true)]//UNDONE:xxx0:AsyncSecu: change to true
        public override void DeleteEntity(int contentId)
        {
            using (var op = SnTrace.Security.StartOperation("DeleteEntity id:{0}", contentId))
            {
                base.DeleteEntity(contentId);
                op.Successful = true;
            }
        }
        /// <summary>
        /// Deletes the entity and it's whole subtree including the related ACLs in the 
        /// security component after a content was deleted in the repository.
        /// </summary>
        /// <param name="contentId">Id of the content. Cannot be 0.</param>
        /// <param name="cancel">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public override async Task DeleteEntityAsync(int contentId, CancellationToken cancel)
        {
            using var op = SnTrace.Security.StartOperation(() => $"DeleteEntity id:{contentId}");
            await base.DeleteEntityAsync(contentId, cancel).ConfigureAwait(false);
            op.Successful = true;
        }

        /// <summary>
        /// Moves the entity and its whole subtree including the related ACLs in the 
        /// security component after a content was moved in the repository.
        /// </summary>
        /// <param name="sourceId">Id of the source content. Cannot be 0.</param>
        /// <param name="targetId">Id of the target content that will contain the source. Cannot be 0.</param>
        [Obsolete("Use async version instead.", true)]//UNDONE:xxx0:AsyncSecu: change to true
        public override void MoveEntity(int sourceId, int targetId)
        {
            using (var op = SnTrace.Security.StartOperation("MoveEntity sourceId:{0}, targetId:{1}", sourceId, targetId))
            {
                base.MoveEntity(sourceId, targetId);
                op.Successful = true;
            }
        }
        /// <summary>
        /// Moves the entity and its whole subtree including the related ACLs in the 
        /// security component after a content was moved in the repository.
        /// </summary>
        /// <param name="sourceId">Id of the source content. Cannot be 0.</param>
        /// <param name="targetId">Id of the target content that will contain the source. Cannot be 0.</param>
        /// <param name="cancel">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public override async Task MoveEntityAsync(int sourceId, int targetId, CancellationToken cancel)
        {
            using var op = SnTrace.Security.StartOperation(() =>
                $"MoveEntity sourceId:{sourceId}, targetId:{targetId}");
            await base.MoveEntityAsync(sourceId, targetId, cancel).ConfigureAwait(false);
            op.Successful = true;
        }

        /// <summary>
        /// Returns false if the content inherits the permissions from it's parent.
        /// </summary>
        /// <param name="contentId">Id of the content. Cannot be 0.</param>
        public override bool IsEntityInherited(int contentId)
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
        public override bool IsEntityExist(int contentId)
        {
            return base.IsEntityExist(contentId);
        }

        /*********************** Public permission query API **********************/

        /// <summary>
        /// Returns all user and group ids that have any explicit permissions on the given content or its subtree.
        /// </summary>
        /// <param name="contentId">Id of the content.</param>
        /// <param name="level">Filtering by permission level. It can be Allowed, Denied, AllowedOrDenied.</param>
        public override IEnumerable<int> GetRelatedIdentities(int contentId, PermissionLevel level)
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
        public override Dictionary<PermissionTypeBase, int> GetRelatedPermissions(int contentId, PermissionLevel level, bool explicitOnly, int identityId, Func<int, bool> isEnabled)
        {
            return base.GetRelatedPermissions(contentId, level, explicitOnly, identityId, isEnabled);
        }
        public override Dictionary<PermissionTypeBase, int> GetExplicitPermissionsInSubtree(int contentId, int[] identities, bool includeRoot)
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
        public override IEnumerable<int> GetRelatedEntities(int contentId, PermissionLevel level, bool explicitOnly, int identityId, IEnumerable<PermissionTypeBase> permissions)
        {
            return base.GetRelatedEntities(contentId, level, explicitOnly, identityId, permissions);
        }
        /// <summary>
        /// Returns all user and group ids that have any explicit permissions on the given content and its subtree.
        /// </summary>
        /// <param name="contentId">Id of the content.</param>
        /// <param name="level">Filtering by permission level. It can be Allowed, Denied, AllowedOrDenied</param>
        /// <param name="permissions">Only those content will appear in the output that have permission settings that are listed in this permissions list.</param>
        public override IEnumerable<int> GetRelatedIdentities(int contentId, PermissionLevel level, IEnumerable<PermissionTypeBase> permissions)
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
        public override IEnumerable<int> GetRelatedEntitiesOneLevel(int contentId, PermissionLevel level, int identityId, IEnumerable<PermissionTypeBase> permissions)
        {
            return base.GetRelatedEntitiesOneLevel(contentId, level, identityId, permissions);
        }

        /// <summary>
        /// Returns Ids of all users that have all given permission on the entity.
        /// User will be resulted even if the permissions are granted on a group where she is member directly or indirectly.
        /// </summary>
        /// <param name="contentId">Id of the content.</param>
        /// <param name="permissions">Only those users appear in the output that have permission settings in connection with the given permissions.</param>
        public override IEnumerable<int> GetAllowedUsers(int contentId, IEnumerable<PermissionTypeBase> permissions)
        {
            return base.GetAllowedUsers(contentId, permissions);
        }
        /// <summary>
        /// Returns Ids of all groups where the given user or group is member directly or indirectly.
        /// </summary>
        /// <param name="contentId">Id of the group or user.</param>
        /// <param name="directOnly">Switch of the direct or indirect membership.</param>
        public override IEnumerable<int> GetParentGroups(int contentId, bool directOnly)
        {
            return base.GetParentGroups(contentId, directOnly);
        }

        /*********************** Membership API **********************/

        /// <summary>
        /// Gets the ids of all the groups that contain the current user as a member, even through other groups.
        /// </summary>
        public override int[] GetFlattenedGroups()
        {
            return base.GetFlattenedGroups();
        }
        /// <summary>
        /// Gets the ids of all the groups that contain the current user as a member, even through other groups,
        /// plus Everyone (except in case of a visitor) and the optional dynamic groups provided by the 
        /// membership extender.
        /// </summary>
        public override List<int> GetGroups()
        {
            return base.GetGroups();
        }
        /// <summary>
        /// Gets the ids of all the groups that contain the current user as a member, even through other groups,
        /// plus Everyone (except in case of a visitor), plus Owners (if applicable) and the optional 
        /// dynamic groups provided by the membership extender.
        /// </summary>
        public override List<int> GetGroupsWithOwnership(int entityId)
        {
            return base.GetGroupsWithOwnership(entityId);
        }

        /// <summary>
        /// Determines if the provided member (user or group) is a member of a group. This method
        /// is transitive, meaning it will look for relations in the whole group graph, not 
        /// only direct memberships.
        /// </summary>
        public override bool IsInGroup(int memberId, int groupId)
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
        [Obsolete("Use async version instead.", true)]//UNDONE:xxx0:AsyncSecu: change to true
        public override void AddMembersToSecurityGroup(int groupId, IEnumerable<int> userMembers, IEnumerable<int> groupMembers, IEnumerable<int> parentGroups = null)
        {
            using (var op = SnTrace.Security.StartOperation("AddMembersToSecurityGroup: groupId:{0}, userMembers:[{1}], groupMembers:[{2}], parentGroups:[{3}]",
                       groupId, string.Join(",", userMembers ?? new int[0]), string.Join(",", groupMembers ?? new int[0]), string.Join(",", parentGroups ?? new int[0])))
            {
                base.AddMembersToSecurityGroup(groupId, userMembers, groupMembers, parentGroups);
                op.Successful = true;
            }
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
        /// <param name="cancel">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public override async Task AddMembersToSecurityGroupAsync(int groupId, IEnumerable<int> userMembers, IEnumerable<int> groupMembers, CancellationToken cancel, IEnumerable<int> parentGroups = null)
        {
            using var op = SnTrace.Security.StartOperation(() =>
                "AddMembersToSecurityGroup: " +
                $"groupId:{groupId}, " +
                $"userMembers:[{string.Join(",", userMembers ?? Array.Empty<int>())}], " +
                $"groupMembers:[{string.Join(",", groupMembers ?? Array.Empty<int>())}], " +
                $"parentGroups:[{string.Join(",", parentGroups ?? Array.Empty<int>())}]");
            await base.AddMembersToSecurityGroupAsync(groupId, userMembers, groupMembers, cancel, parentGroups).ConfigureAwait(false);
            op.Successful = true;
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
        [Obsolete("Use async version instead.", true)]//UNDONE:xxx0:AsyncSecu: change to true
        public override void RemoveMembersFromSecurityGroup(int groupId, IEnumerable<int> userMembers, IEnumerable<int> groupMembers, IEnumerable<int> parentGroups = null)
        {
            var users = userMembers as int[] ?? userMembers.ToArray();
            using (var op = SnTrace.Security.StartOperation("RemoveMembersFromSecurityGroup: groupId:{0}, userMembers:[{1}], groupMembers:[{2}], parentGroups:[{3}]",
                groupId, string.Join(",", users), string.Join(",", groupMembers ?? new int[0]), string.Join(",", parentGroups ?? new int[0])))
            {
                base.RemoveMembersFromSecurityGroup(groupId, users, groupMembers, parentGroups);
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
        /// <param name="cancel">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public override async Task RemoveMembersFromSecurityGroupAsync(int groupId, IEnumerable<int> userMembers, IEnumerable<int> groupMembers, CancellationToken cancel, IEnumerable<int> parentGroups = null)
        {
            var users = userMembers as int[] ?? userMembers.ToArray();
            using var op = SnTrace.Security.StartOperation(()=>
                "RemoveMembersFromSecurityGroup: " +
                $"groupId:{groupId}, " +
                $"userMembers:[{string.Join(",", users)}], " +
                $"groupMembers:[{string.Join(",", groupMembers ?? Array.Empty<int>())}], " +
                $"parentGroups:[{string.Join(",", parentGroups ?? Array.Empty<int>())}]");
            await base.RemoveMembersFromSecurityGroupAsync(groupId, users, groupMembers, cancel, parentGroups)
                .ConfigureAwait(false);
            op.Successful = true;
        }

        /// <summary>
        /// Add one or more group members to a group. If the main group or any member is unknown it will be created.
        /// This method is a shortcut for AddMembersToSecurityGroup(...).
        /// </summary>
        /// <param name="groupId">Identifier of the container group. Cannot be 0.</param>
        /// <param name="groupMembers">Collection of the group member identifiers. Can be null or empty.</param>
        [Obsolete("Use async version instead.", true)]//UNDONE:xxx0:AsyncSecu: change to true
        public override void AddGroupsToSecurityGroup(int groupId, IEnumerable<int> groupMembers)
        {
            using (var op = SnTrace.Security.StartOperation("AddGroupsToSecurityGroup: groupId:{0}, groupMembers:[{1}]", groupId, string.Join(",", groupMembers ?? new int[0])))
            {
                base.AddGroupsToSecurityGroup(groupId, groupMembers);
                op.Successful = true;
            }
        }
        /// <summary>
        /// Add one or more group members to a group. If the main group or any member is unknown it will be created.
        /// This method is a shortcut for AddMembersToSecurityGroup(...).
        /// </summary>
        /// <param name="groupId">Identifier of the container group. Cannot be 0.</param>
        /// <param name="groupMembers">Collection of the group member identifiers. Can be null or empty.</param>
        /// <param name="cancel">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public override async Task AddGroupsToSecurityGroupAsync(int groupId, IEnumerable<int> groupMembers, CancellationToken cancel)
        {
            using var op = SnTrace.Security.StartOperation(() => 
                    "AddGroupsToSecurityGroup: " +
                    $"groupId:{groupId}, " +
                    $"groupMembers:[{string.Join(",", groupMembers ?? Array.Empty<int>())}]");
            await base.AddGroupsToSecurityGroupAsync(groupId, groupMembers, cancel).ConfigureAwait(false);
            op.Successful = true;
        }

        /// <summary>
        /// Add a group as a member of one or more parent groups. If the main group or any parent is unknown it will be created.
        /// This method is a shortcut for AddMembersToSecurityGroup(...).
        /// </summary>
        /// <param name="groupId">Identifier of the member group. Cannot be 0.</param>
        /// <param name="parentGroups">Collection of the parent group identifiers. Can be null or empty.</param>
        [Obsolete("Use async version instead.", true)]//UNDONE:xxx0:AsyncSecu: change to true
        public override void AddGroupToSecurityGroups(int groupId, IEnumerable<int> parentGroups)
        {
            using (var op = SnTrace.Security.StartOperation("AddGroupToSecurityGroups: groupId:{0}, parentGroups:[{1}]", groupId, string.Join(",", string.Join(",", parentGroups ?? new int[0]))))
            {
                base.AddGroupToSecurityGroups(groupId, parentGroups);
                op.Successful = true;
            }
        }
        /// <summary>
        /// Add a group as a member of one or more parent groups. If the main group or any parent is unknown it will be created.
        /// This method is a shortcut for AddMembersToSecurityGroup(...).
        /// </summary>
        /// <param name="groupId">Identifier of the member group. Cannot be 0.</param>
        /// <param name="parentGroups">Collection of the parent group identifiers. Can be null or empty.</param>
        /// <param name="cancel">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public override async Task AddGroupToSecurityGroupsAsync(int groupId, IEnumerable<int> parentGroups, CancellationToken cancel)
        {
            using var op = SnTrace.Security.StartOperation(() => 
                "AddGroupToSecurityGroups: " +
                $"groupId:{groupId}, " +
                $"parentGroups:[{string.Join(",", string.Join(",", parentGroups ?? new int[0]))}]");
            await base.AddGroupToSecurityGroupsAsync(groupId, parentGroups, cancel).ConfigureAwait(false);
            op.Successful = true;
        }

        /// <summary>
        /// Removes one or more group members from a group in one step.
        /// Non-existing group or member groups will be skipped.
        /// This method is a shortcut for RemoveMembersFromSecurityGroup(...).
        /// </summary>
        /// <param name="groupId">Identifier of the container group. Cannot be 0.</param>
        /// <param name="groupMembers">Collection of the group member identifiers. Can be null or empty.</param>
        [Obsolete("Use async version instead.", true)]//UNDONE:xxx0:AsyncSecu: change to true
        public override void RemoveGroupsFromSecurityGroup(int groupId, IEnumerable<int> groupMembers)
        {
            using (var op = SnTrace.Security.StartOperation("RemoveGroupsFromSecurityGroup: groupId:{0}, groupMembers:[{1}]", groupId, string.Join(",", groupMembers ?? new int[0])))
            {
                base.RemoveGroupsFromSecurityGroup(groupId, groupMembers);
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
        /// <param name="cancel">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public override async Task RemoveGroupsFromSecurityGroupAsync(int groupId, IEnumerable<int> groupMembers, CancellationToken cancel)
        {
            using var op = SnTrace.Security.StartOperation(() => 
                "RemoveGroupsFromSecurityGroup: " +
                $"groupId:{groupId}, " +
                $"groupMembers:[{string.Join(",", groupMembers ?? Array.Empty<int>())}]");
            await base.RemoveGroupsFromSecurityGroupAsync(groupId, groupMembers, cancel).ConfigureAwait(false);
            op.Successful = true;
        }

        /// <summary>
        /// Removes a group from one or more parent groups
        /// Non-existing group or parent groups will be skipped.
        /// This method is a shortcut for RemoveMembersFromSecurityGroup(...).
        /// </summary>
        /// <param name="groupId">Identifier of the member group. Cannot be 0.</param>
        /// <param name="parentGroups">Collection of the parent group identifiers. Can be null or empty.</param>
        [Obsolete("Use async version instead.", true)]//UNDONE:xxx0:AsyncSecu: change to true
        public override void RemoveGroupFromSecurityGroups(int groupId, IEnumerable<int> parentGroups)
        {
            using (var op = SnTrace.Security.StartOperation("RemoveGroupFromSecurityGroups: groupId:{0}, groupMembers:[{1}]", groupId, string.Join(",", parentGroups ?? new int[0])))
            {
                base.RemoveGroupFromSecurityGroups(groupId, parentGroups);
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
        /// <param name="cancel">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public override async Task RemoveGroupFromSecurityGroupsAsync(int groupId, IEnumerable<int> parentGroups,
            CancellationToken cancel)
        {
            using var op = SnTrace.Security.StartOperation(() =>
                "RemoveGroupFromSecurityGroups: " +
                $"groupId:{groupId}, " +
                $"groupMembers:[{string.Join(",", parentGroups ?? Array.Empty<int>())}]");
            await base.RemoveGroupFromSecurityGroupsAsync(groupId, parentGroups, cancel).ConfigureAwait(false);
            op.Successful = true;
        }

        /// <summary>
        /// Adds one or more users to a group in one step.
        /// Non-existing group will be created.
        /// This method is a shortcut for AddMembersToSecurityGroup(...).
        /// </summary>
        /// <param name="groupId">Identifier of the container group. Cannot be 0.</param>
        /// <param name="userMembers">Collection of the user member identifiers. Can be null or empty.</param>
        [Obsolete("Use async version instead.", true)]//UNDONE:xxx0:AsyncSecu: change to true
        public override void AddUsersToSecurityGroup(int groupId, IEnumerable<int> userMembers)
        {
            using (var op = SnTrace.Security.StartOperation("AddUsersToSecurityGroup: groupId:{0}, userMembers:[{1}]", groupId, string.Join(",", userMembers ?? new int[0])))
            {
                base.AddUsersToSecurityGroup(groupId, userMembers);
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
        /// <param name="cancel">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public override async Task AddUsersToSecurityGroupAsync(int groupId, IEnumerable<int> userMembers, CancellationToken cancel)
        {
            using var op = SnTrace.Security.StartOperation(() =>
                $"AddUsersToSecurityGroup: groupId:{groupId}, userMembers:[{string.Join(",", userMembers ?? Array.Empty<int>())}]");
            await base.AddUsersToSecurityGroupAsync(groupId, userMembers, cancel).ConfigureAwait(false);
            op.Successful = true;
        }

        /// <summary>
        /// Add a user to one or more groups in one step.
        /// Non-existing groups will be created.
        /// </summary>
        /// <param name="userId">Identifier of the the user member that will be added. Cannot be 0.</param>
        /// <param name="parentGroups">Collection of the parent group identifiers. Can be null or empty.</param>
        [Obsolete("Use async version instead.", true)]//UNDONE:xxx0:AsyncSecu: change to true
        public override void AddUserToSecurityGroups(int userId, IEnumerable<int> parentGroups)
        {
            using (var op = SnTrace.Security.StartOperation("AddUserToSecurityGroups: userId:{0}, parentGroups:[{1}]", userId, string.Join(",", parentGroups ?? new int[0])))
            {
                base.AddUserToSecurityGroups(userId, parentGroups);
                op.Successful = true;
            }
        }
        /// <summary>
        /// Add a user to one or more groups in one step.
        /// Non-existing groups will be created.
        /// </summary>
        /// <param name="userId">Identifier of the the user member that will be added. Cannot be 0.</param>
        /// <param name="parentGroups">Collection of the parent group identifiers. Can be null or empty.</param>
        /// <param name="cancel">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public override async Task AddUserToSecurityGroupsAsync(int userId, IEnumerable<int> parentGroups, CancellationToken cancel)
        {
            using var op = SnTrace.Security.StartOperation(() =>
                $"AddUserToSecurityGroups: userId:{userId}, parentGroups:[{string.Join(",", parentGroups ?? Array.Empty<int>())}]");
            await base.AddUserToSecurityGroupsAsync(userId, parentGroups, cancel).ConfigureAwait(false);
            op.Successful = true;
        }

        /// <summary>
        /// Removes a user from one or more groups in one step.
        /// Non-existing group or member will be skipped.
        /// </summary>
        /// <param name="userId">Identifier of the user the will be removed. Cannot be 0.</param>
        /// <param name="parentGroups">Collection of the parent group identifiers. Can be null or empty.</param>
        [Obsolete("Use async version instead.", true)]//UNDONE:xxx0:AsyncSecu: change to true
        public override void RemoveUserFromSecurityGroups(int userId, IEnumerable<int> parentGroups)
        {
            using (var op = SnTrace.Security.StartOperation("RemoveUserFromSecurityGroups: userId:{0}, parentGroups:[{1}]", userId, string.Join(",", parentGroups ?? new int[0])))
            {
                base.RemoveUserFromSecurityGroups(userId, parentGroups);
                op.Successful = true;
            }
        }
        /// <summary>
        /// Removes a user from one or more groups in one step.
        /// Non-existing group or member will be skipped.
        /// </summary>
        /// <param name="userId">Identifier of the user the will be removed. Cannot be 0.</param>
        /// <param name="parentGroups">Collection of the parent group identifiers. Can be null or empty.</param>
        /// <param name="cancel">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public override async Task RemoveUserFromSecurityGroupsAsync(int userId, IEnumerable<int> parentGroups, CancellationToken cancel)
        {
            using var op = SnTrace.Security.StartOperation(() =>
                $"RemoveUserFromSecurityGroups: userId:{userId}, parentGroups:[{string.Join(",", parentGroups ?? Array.Empty<int>())}]");
            await base.RemoveUserFromSecurityGroupsAsync(userId, parentGroups, cancel).ConfigureAwait(false);
            op.Successful = true;
        }

        /// <summary>
        /// Removes one or more users from a group in one step.
        /// Non-existing group or member will be skipped.
        /// This method is a shortcut for RemoveMembersFromSecurityGroup(...).
        /// </summary>
        /// <param name="groupId">Identifier of the container group. Cannot be 0.</param>
        /// <param name="userMembers">Collection of the user member identifiers. Can be null or empty.</param>
        [Obsolete("Use async version instead.", true)]//UNDONE:xxx0:AsyncSecu: change to true
        public override void RemoveUsersFromSecurityGroup(int groupId, IEnumerable<int> userMembers)
        {
            using (var op = SnTrace.Security.StartOperation("RemoveUsersFromSecurityGroup: groupId:{0}, userMembers:[{1}]", groupId, string.Join(",", userMembers ?? new int[0])))
            {
                base.RemoveUsersFromSecurityGroup(groupId, userMembers);
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
        /// <param name="cancel">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public override async Task RemoveUsersFromSecurityGroupAsync(int groupId, IEnumerable<int> userMembers, CancellationToken cancel)
        {
            using var op = SnTrace.Security.StartOperation(() =>
                $"RemoveUsersFromSecurityGroup: groupId:{groupId}, userMembers:[{string.Join(",", userMembers ?? Array.Empty<int>())}]");
            await base.RemoveUsersFromSecurityGroupAsync(groupId, userMembers, cancel).ConfigureAwait(false);
            op.Successful = true;
        }

        /// <summary>
        /// Deletes the specified group and its relations including related security entries.
        /// </summary>
        [Obsolete("Use async version instead.", true)]//UNDONE:xxx0:AsyncSecu: change to true
        public override void DeleteSecurityGroup(int groupId)
        {
            using (var op = SnTrace.Security.StartOperation("DeleteSecurityGroup: id:{0}", groupId))
            {
                base.DeleteSecurityGroup(groupId);
                op.Successful = true;
            }
        }
        /// <summary>
        /// Deletes the specified group and its relations including related security entries.
        /// </summary>
        /// <param name="groupId">The Id of the group to be deleted.</param>
        /// <param name="cancel">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public override async Task DeleteSecurityGroupAsync(int groupId, CancellationToken cancel)
        {
            using var op = SnTrace.Security.StartOperation(() => $"DeleteSecurityGroup: id:{groupId}");
            await base.DeleteSecurityGroupAsync(groupId, cancel).ConfigureAwait(false);
            op.Successful = true;
        }

        /// <summary>
        /// Deletes the user from the system by removing all memberships and security entries related to this user.
        /// </summary>
        [Obsolete("Use async version instead.", true)]//UNDONE:xxx0:AsyncSecu: change to true
        public override void DeleteUser(int userId)
        {
            using (var op = SnTrace.Security.StartOperation("DeleteUser: id:{0}", userId))
            {
                base.DeleteUser(userId);
                op.Successful = true;
            }
        }
        /// <summary>
        /// Deletes the user from the system by removing all memberships and security entries related to this user.
        /// </summary>
        /// <param name="userId">The Id of the user to be deleted.</param>
        /// <param name="cancel">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public override async Task DeleteUserAsync(int userId, CancellationToken cancel)
        {
            using var op = SnTrace.Security.StartOperation(() => $"DeleteUser: id:{userId}");
            await base.DeleteUserAsync(userId, cancel).ConfigureAwait(false);
            op.Successful = true;
        }

        /// <summary>
        /// Deletes the specified group or user and its relations including related security entries.
        /// </summary>
        [Obsolete("Use async version instead.", true)]//UNDONE:xxx0:AsyncSecu: change to true
        public override void DeleteIdentity(int id)
        {
            using (var op = SnTrace.Security.StartOperation("DeleteIdentity: id:{0}", id))
            {
                base.DeleteIdentity(id);
                op.Successful = true;
            }
        }
        /// <summary>
        /// Deletes the specified group or user and its relations including related security entries.
        /// </summary>
        /// <param name="id">The Id of the identity to be deleted.</param>
        /// <param name="cancel">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public override async Task DeleteIdentityAsync(int id, CancellationToken cancel)
        {
            using var op = SnTrace.Security.StartOperation(() => "DeleteIdentity: id:{id}");
            await base.DeleteIdentityAsync(id, cancel).ConfigureAwait(false);
            op.Successful = true;
        }

        /// <summary>
        /// Deletes the specified groups or users and their relations including related security entries.
        /// </summary>
        [Obsolete("Use async version instead.", true)]//UNDONE:xxx0:AsyncSecu: change to true
        public override void DeleteIdentities(IEnumerable<int> ids)
        {
            using var op = SnTrace.Security.StartOperation(() =>
                $"DeleteIdentities: ids:[{string.Join(",", ids ?? Array.Empty<int>())}]");
            base.DeleteIdentities(ids);
            op.Successful = true;
        }
        /// <summary>
        /// Deletes the specified groups or users and their relations including related security entries.
        /// </summary>
        /// <param name="ids">Set of identity Ids to be deleted.</param>
        /// <param name="cancel">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public override async Task DeleteIdentitiesAsync(IEnumerable<int> ids, CancellationToken cancel)
        {
            using var op = SnTrace.Security.StartOperation(() =>
                $"DeleteIdentities: ids:[{string.Join(",", ids ?? Array.Empty<int>())}]");
            await base.DeleteIdentitiesAsync(ids, cancel).ConfigureAwait(false);
            op.Successful = true;
        }

        /***************** Debug info ***************/

        /// <summary>
        /// Returns an object that contains information about the execution of the last few SecurityActivities.
        /// </summary>
        public override SecurityActivityHistory GetRecentActivities()
        {
            //TODO: secu: permission check for GetRecentActivities
            return base.GetRecentActivities();
        }
        /// <summary>WARNING! Do not use this method in your code. Used in consistency checker tool.</summary>
        public override IEnumerable<long> GetCachedMembershipForConsistencyCheck()
        {
            //TODO: secu: permission check for GetCachedMembershipForConsistencyCheck
            return base.GetCachedMembershipForConsistencyCheck();
        }
        /// <summary>WARNING! Do not use this method in your code. Used in consistency checker tool.</summary>
        public override void GetFlatteningForConsistencyCheck(out IEnumerable<long> missingInFlattening, out IEnumerable<long> unknownInFlattening)
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
