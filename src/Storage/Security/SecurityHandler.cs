using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SenseNet.Configuration;
using SenseNet.Diagnostics;
using SenseNet.Security;
using SenseNet.Security.Configuration;
using SenseNet.Security.Messaging;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Security
{
    public enum CopyPermissionMode { NoBreak, BreakWithoutClear, BreakAndClear }

    /// <summary>
    /// Central class for handling security-related operations (managing permissions and group memberships). 
    /// Contains both instance API (accessible through the Node.Security or Content.Security properties) and static API.
    /// </summary>
    public sealed class SecurityHandler
	{
        #region /*========================================================== Evaluation related methods */

        /// <summary>
        /// If one or more passed permissions are not allowed (undefined or denied) on the passed content for the current user, SenseNetSecurityException will be thrown.
        /// </summary>
        /// <param name="node">The node. Cannot be null.</param>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing" so SenseNetSecurityException will be thrown.</param>
        public void Assert(Node node, params PermissionType[] permissionTypes)
        {
            if (node == null)
                throw new ArgumentNullException("node");
            Assert(node.Id, node.Path, null, permissionTypes);
        }
        /// <summary>
        /// If one or more passed permissions are not allowed (undefined or denied) on the current content for the current user, SenseNetSecurityException will be thrown with the specified message.
        /// </summary>
        /// <param name="node">The node. Cannot be null.</param>
        /// <param name="message">Text that appears in the SenseNetSecurityException's message.</param>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing" so SenseNetSecurityException will be thrown.</param>
        public void Assert(Node node, string message, params PermissionType[] permissionTypes)
        {
            if (node == null)
                throw new ArgumentNullException("node");
            Assert(node.Id, node.Path, message, permissionTypes);
        }
        /// <summary>
        /// If one or more passed permissions are not allowed (undefined or denied) on the passed content for the current user, SenseNetSecurityException will be thrown.
        /// </summary>
        /// <param name="nodeHead">The node head. Cannot be null.</param>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing" so SenseNetSecurityException will be thrown.</param>
        public void Assert(NodeHead nodeHead, params PermissionType[] permissionTypes)
        {
            if (nodeHead == null)
                throw new ArgumentNullException("nodeHead");
            Assert(nodeHead.Id, nodeHead.Path, null, permissionTypes);
        }
        /// <summary>
        /// If one or more passed permissions are not allowed (undefined or denied) on the passed content for the current user, SenseNetSecurityException will be thrown.
        /// </summary>
        /// <param name="nodeHead">The node head. Cannot be null.</param>
        /// <param name="message">Text that appears in the SenseNetSecurityException's message.</param>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing" so SenseNetSecurityException will be thrown.</param>
        public void Assert(NodeHead nodeHead, string message, params PermissionType[] permissionTypes)
        {
            if (nodeHead == null)
                throw new ArgumentNullException("nodeHead");
            Assert(nodeHead.Id, nodeHead.Path, message, permissionTypes);
        }
        /// <summary>
        /// If one or more passed permissions are not allowed (undefined or denied) on the passed content for the current user, SenseNetSecurityException will be thrown.
        /// </summary>
        /// <param name="contentId">The identifier of the content.</param>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing" so SenseNetSecurityException will be thrown.</param>
        public void Assert(int contentId, params PermissionType[] permissionTypes)
        {
            Assert(contentId, null, null, permissionTypes);
        }
        /// <summary>
        /// If one or more passed permissions are not allowed (undefined or denied) on the passed content for the current user, SenseNetSecurityException will be thrown.
        /// </summary>
        /// <param name="contentId">The identifier of the content.</param>
        /// <param name="message">Text that appears in the SenseNetSecurityException's message.</param>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing" so SenseNetSecurityException will be thrown.</param>
        public void Assert(int contentId, string message, params PermissionType[] permissionTypes)
        {
            Assert(contentId, null, message, permissionTypes);
        }
        private void Assert(int nodeId, string path, string message, params PermissionType[] permissionTypes)
        {
            IUser user = AccessProvider.Current.GetCurrentUser();
            if (user.Id == -1)
                return;
            if (HasPermission(nodeId, permissionTypes))
                return;
            throw GetAccessDeniedException(nodeId, path, message, permissionTypes, user, false);
        }

        /// <summary>
        /// If one or more passed permissions are not allowed (undefined or denied) on every content in the whole subtree of the passed content for the current user, SenseNetSecurityException will be thrown.
        /// </summary>
        /// <param name="node">The node. Cannot be null.</param>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing" so SenseNetSecurityException will be thrown.</param>
        public void AssertSubtree(Node node, params PermissionType[] permissionTypes)
        {
            if (node == null)
                throw new ArgumentNullException("node");
            AssertSubtree(node.Id, node.Path, null, permissionTypes);
        }
        /// <summary>
        /// If one or more passed permissions are not allowed (undefined or denied) on every content in the whole subtree of the passed content for the current user, SenseNetSecurityException will be thrown.
        /// </summary>
        /// <param name="node">The node. Cannot be null.</param>
        /// <param name="message">Text that appears in the SenseNetSecurityException's message.</param>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing" so SenseNetSecurityException will be thrown.</param>
        public void AssertSubtree(Node node, string message, params PermissionType[] permissionTypes)
        {
            if (node == null)
                throw new ArgumentNullException("node");
            AssertSubtree(node.Id, node.Path, message, permissionTypes);
        }
        /// <summary>
        /// If one or more passed permissions are not allowed (undefined or denied) on every content in the whole subtree of the passed content for the current user, SenseNetSecurityException will be thrown.
        /// </summary>
        /// <param name="nodeHead">The node head. Cannot be null.</param>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing" so SenseNetSecurityException will be thrown.</param>
        public void AssertSubtree(NodeHead nodeHead, params PermissionType[] permissionTypes)
        {
            if (nodeHead == null)
                throw new ArgumentNullException("nodeHead");
            AssertSubtree(nodeHead.Id, nodeHead.Path, null, permissionTypes);
        }
        /// <summary>
        /// If one or more passed permissions are not allowed (undefined or denied) on every content in the whole subtree of the passed content for the current user, SenseNetSecurityException will be thrown.
        /// </summary>
        /// <param name="nodeHead">The node head. Cannot be null.</param>
        /// <param name="message">Text that appears in the SenseNetSecurityException's message.</param>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing" so SenseNetSecurityException will be thrown.</param>
        public void AssertSubtree(NodeHead nodeHead, string message, params PermissionType[] permissionTypes)
        {
            if (nodeHead == null)
                throw new ArgumentNullException("nodeHead");
            AssertSubtree(nodeHead.Id, nodeHead.Path, message, permissionTypes);
        }
        /// <summary>
        /// If one or more passed permissions are not allowed (undefined or denied) on every content in the whole subtree of the passed content for the current user, SenseNetSecurityException will be thrown.
        /// </summary>
        /// <param name="contentId">The identifier of the content.</param>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing" so SenseNetSecurityException will be thrown.</param>
        public void AssertSubtree(int contentId, params PermissionType[] permissionTypes)
        {
            AssertSubtree(contentId, null, null, permissionTypes);
        }
        /// <summary>
        /// If one or more passed permissions are not allowed (undefined or denied) on every content in the whole subtree of the passed content for the current user, SenseNetSecurityException will be thrown.
        /// </summary>
        /// <param name="contentId">The identifier of the content.</param>
        /// <param name="message">Text that appears in the SenseNetSecurityException's message.</param>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing" so SenseNetSecurityException will be thrown.</param>
        public void AssertSubtree(int contentId, string message, params PermissionType[] permissionTypes)
        {
            AssertSubtree(contentId, null, message, permissionTypes);
        }
        private void AssertSubtree(int nodeId, string path, string message, params PermissionType[] permissionTypes)
        {
            IUser user = AccessProvider.Current.GetCurrentUser();
            if (user.Id == -1)
                return;
            if (HasSubTreePermission(nodeId, permissionTypes))
                return;
            throw GetAccessDeniedException(nodeId, path, message, permissionTypes, user, false);
        }

        /// <summary>
        /// Returns true if all passed permissions are allowed on the passed content for the current user.
        /// </summary>
        /// <param name="node">The node. Cannot be null.</param>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing".</param>
        public bool HasPermission(Node node, params PermissionType[] permissionTypes)
        {
            if (node == null)
                throw new ArgumentNullException("node");
            return HasPermission(node.Id, permissionTypes);
        }
        /// <summary>
        /// Returns true if all passed permissions are allowed on the passed content for the current user.
        /// </summary>
        /// <param name="nodeHead">The node head. Cannot be null.</param>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing".</param>
        public bool HasPermission(NodeHead nodeHead, params PermissionType[] permissionTypes)
        {
            if (nodeHead == null)
                throw new ArgumentNullException("nodeHead");

            return HasPermission(nodeHead.Id, permissionTypes);
        }
        /// <summary>
        /// Returns true if all passed permissions are allowed on the passed content for the current user.
        /// </summary>
        /// <param name="nodeId">Id of the node. Cannot be 0.</param>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing".</param>
        public bool HasPermission(int nodeId, params PermissionType[] permissionTypes)
        {
            if (permissionTypes == null)
                throw new ArgumentNullException("permissionTypes");
            if (permissionTypes.Length == 0)
                return false;
            var user = AccessProvider.Current.GetCurrentUser();
            if (user.Id == -1)
                return true;
            var ctx = SecurityContext;
            return Retrier.Retry(3, 200, typeof(EntityNotFoundException), () => HasPermissionPrivate(ctx, nodeId, permissionTypes));
        }
        private bool HasPermissionPrivate(SnSecurityContext ctx, int contentId, params PermissionType[] permissionTypes)
        {
            try
            {
                return ctx.HasPermission(contentId, permissionTypes);
            }
            catch (EntityNotFoundException)
            {
                // entity not found in the security component: try to re-create it
                ReCreateSecurityEntityAsync(contentId, CancellationToken.None).GetAwaiter().GetResult();

                // retry the operation
                return ctx.HasPermission(contentId, permissionTypes);
            }
        }

        /// <summary>
        /// Returns true if all passed permissions are allowed on the passed content for the passed user.
        /// </summary>
        /// <param name="user">The user. Cannot be null.</param>
        /// <param name="node">The node. Cannot be null.</param>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing".</param>
        public bool HasPermission(IUser user, Node node, params PermissionType[] permissionTypes)
        {
            if (node == null)
                throw new ArgumentNullException("node");
            return HasPermission(user, node.Id, permissionTypes);
        }
        /// <summary>
        /// Returns true if all passed permissions are allowed on the passed content for the passed user.
        /// </summary>
        /// <param name="user">The user. Cannot be null.</param>
        /// <param name="nodeHead">The node head. Cannot be null.</param>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing".</param>
        public bool HasPermission(IUser user, NodeHead nodeHead, params PermissionType[] permissionTypes)
        {
            if (nodeHead == null)
                throw new ArgumentNullException("nodeHead");
            return HasPermission(user, nodeHead.Id, permissionTypes);
        }
        /// <summary>
        /// Returns true if all passed permissions are allowed on the passed content for the passed user.
        /// </summary>
        /// <param name="user">The user. Cannot be null.</param>
        /// <param name="nodeId">Id of the node. Cannot be 0.</param>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing".</param>
        public bool HasPermission(IUser user, int nodeId, params PermissionType[] permissionTypes)
        {
            if (user == null)
                return false;

            var ctx = SecurityContext;
            var isCurrentUser = user.Id == AccessProvider.Current.GetCurrentUser().Id;
            if (!isCurrentUser)
                ctx.AssertPermission(nodeId, PermissionType.SeePermissions);

            if (permissionTypes == null)
                throw new ArgumentNullException("permissionTypes");
            if (permissionTypes.Length == 0)
                return false;
            if (user.Id == -1)
                return true;

            if (!isCurrentUser)
                ctx = CreateSecurityContextFor(user);

            return Retrier.Retry(3, 200, typeof(EntityNotFoundException), () => HasPermissionPrivate(ctx, nodeId, permissionTypes));
        }

        /// <summary>
        /// Returns true if all passed permissions are allowed for the current user on every content in the whole subtree of the passed content.
        /// </summary>
        /// <param name="node">The node. Cannot be null.</param>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing".</param>
        public bool HasSubTreePermission(Node node, params PermissionType[] permissionTypes)
        {
            if (node == null)
                throw new ArgumentNullException("node");
            return HasSubTreePermission(node.Id, permissionTypes);
        }
        /// <summary>
        /// Returns true if all passed permissions are allowed for the current user on every content in the whole subtree of the passed content.
        /// </summary>
        /// <param name="nodeHead">The node head. Cannot be null.</param>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing".</param>
        public bool HasSubTreePermission(NodeHead nodeHead, params PermissionType[] permissionTypes)
        {
            if (nodeHead == null)
                throw new ArgumentNullException("nodeHead");
            return HasSubTreePermission(nodeHead.Id, permissionTypes);
        }
        /// <summary>
        /// Returns true if all passed permissions are allowed for the current user on every content in the whole subtree of the passed content.
        /// </summary>
        /// <param name="nodeId">Id of the node. Cannot be 0.</param>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing".</param>
        private bool HasSubTreePermission(int nodeId, params PermissionType[] permissionTypes)
        {
            if (permissionTypes == null)
                throw new ArgumentNullException("permissionTypes");
            if (permissionTypes.Length == 0)
                return false;
            var user = AccessProvider.Current.GetCurrentUser();
            if (user.Id == -1)
                return true;

            return SecurityContext.HasSubtreePermission(nodeId, permissionTypes);
        }

        /// <summary>
        /// Returns true if all passed permissions are allowed for the passed user on every content in the whole subtree of the passed content.
        /// </summary>
        /// <param name="user">The user. Cannot be null.</param>
        /// <param name="node">The node. Cannot be null.</param>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing".</param>
        public bool HasSubTreePermission(IUser user, Node node, params PermissionType[] permissionTypes)
        {
            if (node == null)
                throw new ArgumentNullException("node");
            return HasSubTreePermission(user, node.Id, permissionTypes);
        }
        /// <summary>
        /// Returns true if all passed permissions are allowed for the passed user on every content in the whole subtree of the passed content.
        /// </summary>
        /// <param name="user">The user. Cannot be null.</param>
        /// <param name="nodeHead">The node head. Cannot be null.</param>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing".</param>
        public bool HasSubTreePermission(IUser user, NodeHead nodeHead, params PermissionType[] permissionTypes)
        {
            if (nodeHead == null)
                throw new ArgumentNullException("nodeHead");
            return HasSubTreePermission(user, nodeHead.Id, permissionTypes);
        }
        /// <summary>
        /// Returns true if all passed permissions are allowed for the passed user on every content in the whole subtree of the passed content.
        /// </summary>
        /// <param name="user">The user. Cannot be null.</param>
        /// <param name="nodeId">Id of the node. Cannot be 0.</param>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing".</param>
        private bool HasSubTreePermission(IUser user, int nodeId, params PermissionType[] permissionTypes)
        {
            var ctx = SecurityContext;
            var isCurrentUser = user.Id == AccessProvider.Current.GetCurrentUser().Id;
            if (!isCurrentUser)
                ctx.AssertPermission(nodeId, PermissionType.SeePermissions);

            if (permissionTypes == null)
                throw new ArgumentNullException("permissionTypes");
            if (permissionTypes.Length == 0)
                return false;
            if (user.Id == -1)
                return true;

            if (!isCurrentUser)
                ctx = CreateSecurityContextFor(user);

            return ctx.HasSubtreePermission(nodeId, permissionTypes);
        }

        /// <summary>
        /// Returns an aggregated permission value by all passed permissions for the current user on the passed content.
        /// Value is Denied if there is any denied passed permission,
        ///   Undefined if there is any undefined but there is no denied passed permission,
        ///   Allowed if every passed permission is allowed.
        /// </summary>
        /// <param name="node">The node. Cannot be null.</param>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing".</param>
        public PermissionValue GetPermission(Node node, params PermissionType[] permissionTypes)
        {
            if (node == null)
                throw new ArgumentNullException("node");
            return GetPermission(node.Id, permissionTypes);
        }
        /// <summary>
        /// Returns an aggregated permission value by all passed permissions for the current user on the passed content.
        /// Value is Denied if there is any denied passed permission,
        ///   Undefined if there is any undefined but there is no denied passed permission,
        ///   Allowed if every passed permission is allowed.
        /// </summary>
        /// <param name="nodeHead">The node head. Cannot be null.</param>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing".</param>
        public PermissionValue GetPermission(NodeHead nodeHead, params PermissionType[] permissionTypes)
        {
            if (nodeHead == null)
                throw new ArgumentNullException("nodeHead");
            return GetPermission(nodeHead.Id, permissionTypes);
        }
        /// <summary>
        /// Returns an aggregated permission value by all passed permissions for the current user on the passed content.
        /// Value is Denied if there is any denied passed permission,
        ///   Undefined if there is any undefined but there is no denied passed permission,
        ///   Allowed if every passed permission is allowed.
        /// </summary>
        /// <param name="nodeId">Id of the node. Cannot be 0.</param>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing".</param>
        private PermissionValue GetPermission(int nodeId, params PermissionType[] permissionTypes)
        {
            if (permissionTypes == null)
                throw new ArgumentNullException("permissionTypes");
            if (permissionTypes.Length == 0)
                return PermissionValue.Denied;

            return SecurityContext.GetPermission(nodeId, permissionTypes);
        }

        /// <summary>
        /// Returns an aggregated permission value by all passed permissions for the passed user on the passed content.
        /// Value is Denied if there is any denied passed permission,
        ///   Undefined if there is any undefined but there is no denied passed permission,
        ///   Allowed if every passed permission is allowed.
        /// </summary>
        /// <param name="user">The user. Cannot be null.</param>
        /// <param name="nodeId">Id of the node. Cannot be 0.</param>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing".</param>
        internal PermissionValue GetPermission(IUser user, int nodeId, params PermissionType[] permissionTypes)
        {
            if (permissionTypes == null)
                throw new ArgumentNullException("permissionTypes");

            var ctx = SecurityContext;
            var userId = user.Id;
            var userIsCurrent = userId == AccessProvider.Current.GetCurrentUser().Id;
            if (!userIsCurrent)
                ctx.AssertPermission(nodeId, PermissionType.SeePermissions);
            if (permissionTypes.Length == 0)
                return PermissionValue.Denied;
            if (!userIsCurrent)
                ctx = CreateSecurityContextFor(user);

            return ctx.GetPermission(nodeId, permissionTypes);
        }

        /// <summary>
        /// Returns an aggregated permission value by all passed permissions for the current user on every content in the whole subtree of the passed content.
        /// Value is Denied if there is any denied passed permission,
        ///   Undefined if there is any undefined but there is no denied passed permission,
        ///   Allowed if every passed permission is allowed.
        /// </summary>
        /// <param name="node">The node. Cannot be null.</param>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing".</param>
        public PermissionValue GetSubtreePermission(Node node, params PermissionType[] permissionTypes)
        {
            if (node == null)
                throw new ArgumentNullException("node");
            return GetSubtreePermission(node.Id, permissionTypes);
        }
        /// <summary>
        /// Returns an aggregated permission value by all passed permissions for the current user on every content in the whole subtree of the passed content.
        /// Value is Denied if there is any denied passed permission,
        ///   Undefined if there is any undefined but there is no denied passed permission,
        ///   Allowed if every passed permission is allowed.
        /// </summary>
        /// <param name="nodeHead">The node head. Cannot be null.</param>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing".</param>
        public PermissionValue GetSubtreePermission(NodeHead nodeHead, params PermissionType[] permissionTypes)
        {
            if (nodeHead == null)
                throw new ArgumentNullException("nodeHead");
            return GetSubtreePermission(nodeHead.Id, permissionTypes);
        }
        /// <summary>
        /// Returns an aggregated permission value by all passed permissions for the current user on every content in the whole subtree of the passed content.
        /// Value is Denied if there is any denied passed permission,
        ///   Undefined if there is any undefined but there is no denied passed permission,
        ///   Allowed if every passed permission is allowed.
        /// </summary>
        /// <param name="nodeId">Id of the node. Cannot be 0.</param>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing".</param>
        internal PermissionValue GetSubtreePermission(int nodeId, params PermissionType[] permissionTypes)
        {
            if (permissionTypes == null)
                throw new ArgumentNullException("permissionTypes");

            if (permissionTypes.Length == 0)
                return PermissionValue.Denied;
            var user = AccessProvider.Current.GetCurrentUser();
            if (user.Id == -1)
                return PermissionValue.Allowed;

            return SecurityContext.GetSubtreePermission(nodeId, permissionTypes);
        }

        /// <summary>
        /// Returns an aggregated permission value by all passed permissions for the passed user on every content in the whole subtree of the passed content.
        /// Value is Denied if there is any denied passed permission,
        ///   Undefined if there is any undefined but there is no denied passed permission,
        ///   Allowed if every passed permission is allowed.
        /// </summary>
        /// <param name="user">The user. Cannot be null.</param>
        /// <param name="nodeId">Id of the node. Cannot be 0.</param>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing".</param>
        internal PermissionValue GetSubtreePermission(IUser user, int nodeId, params PermissionType[] permissionTypes)
        {
            if (permissionTypes == null)
                throw new ArgumentNullException("permissionTypes");

            var ctx = SecurityContext;
            var userId = user.Id;
            var userIsCurrent = userId == AccessProvider.Current.GetCurrentUser().Id;
            if (!userIsCurrent)
                ctx.AssertPermission(nodeId, PermissionType.SeePermissions);
            if (permissionTypes.Length == 0)
                return PermissionValue.Denied;
            if (userId == -1)
                return PermissionValue.Allowed;
            if (!userIsCurrent)
                ctx = CreateSecurityContextFor(user);

            return ctx.GetSubtreePermission(nodeId, permissionTypes);
        }

        /// <summary>
        /// Helper method that determines the permitted access level of the content (None, HeadOnly, PublicOnly, All) for the current user.
        /// </summary>
        /// <param name="nodeHead">The content.</param>
        /// <returns></returns>
        public PermittedLevel GetPermittedLevel(NodeHead nodeHead)
        {
            // shortcut for system user
            if (AccessProvider.Current.GetCurrentUser().Id == Identifiers.SystemUserId)
                return PermittedLevel.All;
            return GetPermittedLevel(nodeHead.Id, GetIdentitiesByMembership(nodeHead));
        }
        /// <summary>
        /// Helper method that determines the permitted access level of the content (None, HeadOnly, PublicOnly, All) for the current user.
        /// </summary>
        /// <param name="node">The content.</param>
        public PermittedLevel GetPermittedLevel(Node node)
        {
            // shortcut for system user
            if (AccessProvider.Current.GetCurrentUser().Id == Identifiers.SystemUserId)
                return PermittedLevel.All;
            return GetPermittedLevel(node.Id, GetIdentitiesByMembership(node));
        }
        /// <summary>
        /// Helper method that determines the permitted access level of the content (None, HeadOnly, PublicOnly, All) for the current user.
        /// </summary>
        /// <param name="nodeId">The id of the content.</param>
        public PermittedLevel GetPermittedLevel(int nodeId)
        {
            // shortcut for system user
            if (AccessProvider.Current.GetCurrentUser().Id == Identifiers.SystemUserId)
                return PermittedLevel.All;
            return GetPermittedLevel(nodeId, GetIdentitiesByMembership(nodeId));
        }
        /// <summary>
        /// Helper method that determines the permitted access level of the content (None, HeadOnly, PublicOnly, All) for the passed user.
        /// </summary>
        /// <param name="nodeId">The id of the content.</param>
        /// <param name="user">The user.</param>
        public PermittedLevel GetPermittedLevel(int nodeId, IUser user)
        {
            // shortcut for system user
            if (user.Id == Identifiers.SystemUserId)
                return PermittedLevel.All;
            return GetPermittedLevel(nodeId, GetIdentitiesByMembership(user, nodeId));
        }
        internal PermittedLevel GetPermittedLevel(int nodeId, IEnumerable<int> identities)
        {
            if (identities.First() == Identifiers.SystemUserId)
                return PermittedLevel.All;

            var aces = GetEffectiveEntriesAsSystemUser(nodeId, identities);

            if (aces.Count == 0)
                return PermittedLevel.None;

            var bits = AggregateAces(aces);

            var allowedBits = bits.AllowBits & ~bits.DenyBits;
            PermittedLevel level;
            if ((allowedBits & PermissionType.OpenMinor.Mask) != 0)
                level = PermittedLevel.All;
            else if ((allowedBits & PermissionType.Open.Mask) != 0)
                level = PermittedLevel.PublicOnly;
            else if ((allowedBits & PermissionType.See.Mask) != 0)
                level = PermittedLevel.HeadOnly;
            else
                level = PermittedLevel.None;
            return level;
        }

        /// <summary>
        /// Return the passed content's explicit entries. Current user must have SeePermissions permission.
        /// </summary>
        /// <param name="contentId">Id of the content.</param>
        /// <param name="relatedIdentities">If not passed, the current user's related identities is focused.</param>
        /// <param name="entryType">Security entry type. Default: all entries.</param>
        public List<AceInfo> GetExplicitEntries(int contentId, IEnumerable<int> relatedIdentities = null, EntryType? entryType = null)
        {
            SecurityContext.AssertPermission(contentId, PermissionType.SeePermissions);
            return GetExplicitEntriesAsSystemUser(contentId, relatedIdentities, entryType);
        }
	    /// <summary>
	    /// Return the passed content's explicit entries. There is permission check so you must call this method from a safe block.
	    /// </summary>
	    /// <param name="contentId">Id of the content.</param>
	    /// <param name="relatedIdentities">If not passed, the current user's related identities is focused.</param>
	    /// <param name="entryType">Security entry type. Default: all entries.</param>
	    public List<AceInfo> GetExplicitEntriesAsSystemUser(int contentId, IEnumerable<int> relatedIdentities = null, EntryType? entryType = null)
	    {
	        return SecurityContext.GetExplicitEntries(contentId, relatedIdentities, entryType);
	    }

        /// <summary>
        /// Return the passed content's effective entries. Current user must have SeePermissions permission.
        /// </summary>
        /// <param name="contentId">Id of the content.</param>
        /// <param name="relatedIdentities">If not passed, the current user's related identities is focused.</param>
        /// <param name="entryType">Security entry type. Default: all entries.</param>
        public List<AceInfo> GetEffectiveEntries(int contentId, IEnumerable<int> relatedIdentities = null, EntryType? entryType = null)
        {
            SecurityContext.AssertPermission(contentId, PermissionType.SeePermissions);
            return SecurityContext.GetEffectiveEntries(contentId, relatedIdentities, entryType);
        }
        /// <summary>
        /// Return the passed content's effective entries. There is permission check so you must call this method from a safe block.
        /// </summary>
        /// <param name="contentId">Id of the content.</param>
        /// <param name="relatedIdentities">If not passed, the current user's related identities is focused.</param>
        /// <param name="entryType">Security entry type. Default: all entries.</param>
        public List<AceInfo> GetEffectiveEntriesAsSystemUser(int contentId, IEnumerable<int> relatedIdentities = null, EntryType? entryType = null)
        {
            return SecurityContext.GetEffectiveEntries(contentId, relatedIdentities, entryType);
        }

        #endregion

        #region /*========================================================== ACL */

        /// <summary>
        /// Returns the AccessControlList of the requested content.
        /// Required permission: SeePermissions
        /// The result contains only Normal entries.
        /// </summary>
        public AccessControlList GetAcl(int nodeId)
        {
            var ctx = SecurityContext;
            ctx.AssertPermission(nodeId, PermissionType.SeePermissions);
            return ctx.GetAcl(nodeId);
        }

        /// <summary>
        /// Returns a new AclEditor instance.
        /// </summary>
        /// <param name="context">If passed, the method uses that and does not create a new context instance.</param>
        public SnAclEditor CreateAclEditor(SnSecurityContext context = null)
        {
            return new SnAclEditor(context);
        }


        #endregion

        #region /*========================================================== Entities */

        /// <summary>
        /// Loads the content by the passed id and creates the security entity by the properties of the loaded content.
        /// </summary>
        /// <param name="contentId">Id of the content.</param>
        /// <param name="throwIfNodeNotFound">If true and the requested content does not exist,
        /// a ContentNotFoundException will be thrown.</param>
        [Obsolete("Use async version instead.", true)]//UNDONE:xxx0:AsyncSecu: change to true
        public void CreateSecurityEntity(int contentId, bool throwIfNodeNotFound = false)
        {
            var nodeHead = NodeHead.Get(contentId);
            if (nodeHead == null)
            {
                if (throwIfNodeNotFound)
                    throw new ContentNotFoundException(contentId.ToString());
                return;
            }
            CreateSecurityEntity(nodeHead.Id, nodeHead.ParentId, nodeHead.OwnerId);
        }
        /// <summary>
        /// Loads the content by the passed id and creates the security entity by the properties of the loaded content.
        /// </summary>
        /// <param name="contentId">Id of the content.</param>
        /// <param name="cancel">The token to monitor for cancellation requests.</param>
        /// <param name="throwIfNodeNotFound">If true and the requested content does not exist,
        /// a ContentNotFoundException will be thrown.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public async Task CreateSecurityEntityAsync(int contentId, CancellationToken cancel, bool throwIfNodeNotFound = false)
        {
            var nodeHead = await NodeHead.GetAsync(contentId, cancel).ConfigureAwait(false);
            if (nodeHead == null)
            {
                if (throwIfNodeNotFound)
                    throw new ContentNotFoundException(contentId.ToString());
                return;
            }
            await CreateSecurityEntityAsync(nodeHead.Id, nodeHead.ParentId, nodeHead.OwnerId, cancel).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates a new entity in the security component, if it does not exist.
        /// Parent content must exist.
        /// </summary>
        /// <param name="contentId">Id of the created entity. Cannot be 0.</param>
        /// <param name="parentId">Id of the parent entity. Cannot be 0.</param>
        /// <param name="ownerId">Id of the entity's owner identity.</param>
        [Obsolete("Use async version instead.", true)]//UNDONE:xxx0:AsyncSecu: change to true
        public void CreateSecurityEntity(int contentId, int parentId, int ownerId)
        {
            CreateSecurityEntity(contentId, parentId, ownerId, SecurityContext);
        }
        /// <summary>
        /// Creates a new entity in the security component, if it does not exist.
        /// Parent content must exist.
        /// </summary>
        /// <param name="contentId">Id of the created entity. Cannot be 0.</param>
        /// <param name="parentId">Id of the parent entity. Cannot be 0.</param>
        /// <param name="ownerId">Id of the entity's owner identity.</param>
        /// <param name="cancel">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public Task CreateSecurityEntityAsync(int contentId, int parentId, int ownerId, CancellationToken cancel)
        {
            return CreateSecurityEntityAsync(contentId, parentId, ownerId, SecurityContext, cancel);
        }

        /// <summary>
        /// Creates a new entity in the security component, if it does not exist.
        /// Parent must exist.
        /// </summary>
        /// <param name="contentId">Id of the created entity. Cannot be 0.</param>
        /// <param name="parentId">Id of the parent entity. Cannot be 0.</param>
        /// <param name="ownerId">Id of the entity's owner identity.</param>
        /// <param name="context">Uses the passed context and does not create a new one.</param>
        [Obsolete("Use async version instead.", true)]//UNDONE:xxx0:AsyncSecu: change to true
        public void CreateSecurityEntity(int contentId, int parentId, int ownerId, SnSecurityContext context)
        {
            if (CheckSecurityEntityCreationParameters(contentId, parentId, ownerId, context))
                context.CreateSecurityEntity(contentId, parentId, ownerId);
        }
        /// <summary>
        /// Creates a new entity in the security component, if it does not exist.
        /// Parent must exist.
        /// </summary>
        /// <param name="contentId">Id of the created entity. Cannot be 0.</param>
        /// <param name="parentId">Id of the parent entity. Cannot be 0.</param>
        /// <param name="ownerId">Id of the entity's owner identity.</param>
        /// <param name="context">Uses the passed context and does not create a new one.</param>
        /// <param name="cancel">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public Task CreateSecurityEntityAsync(int contentId, int parentId, int ownerId, SnSecurityContext context, CancellationToken cancel)
        {
            if (CheckSecurityEntityCreationParameters(contentId, parentId, ownerId, context))
                return context.CreateSecurityEntityAsync(contentId, parentId, ownerId, cancel);
            return Task.CompletedTask;
        }

        private bool CheckSecurityEntityCreationParameters(int contentId, int parentId, int ownerId, SecurityContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            // exceptional case: valid repository root
            if (contentId == Identifiers.PortalRootId && parentId == 0 && ownerId > 0)
                return true;

            // check id validity
            if (contentId > 0 && parentId > 0 && ownerId > 0)
                return true;

            SnLog.WriteWarning(
                $"Cannot create security entity. Every id must greater than zero: contentId: {contentId}, parentId: {parentId}, ownerId: {ownerId}.\r\n{new StackTrace(0, true)}",
                EventId.Security);

            return false;
        }


        /// <summary>
        /// Modifies the owner of the entity in the security component.
        /// </summary>
        /// <param name="contentId">Id of the entity. Cannot be 0.</param>
        /// <param name="ownerId">Id of the entity's owner identity.</param>
        [Obsolete("Use async version instead.", true)]//UNDONE:xxx0:AsyncSecu: change to true
        public void ModifyEntityOwner(int contentId, int ownerId)
        {
            SecurityContext.ModifyEntityOwner(contentId, ownerId);
        }
        /// <summary>
        /// Modifies the owner of the entity in the security component.
        /// </summary>
        /// <param name="contentId">Id of the entity. Cannot be 0.</param>
        /// <param name="ownerId">Id of the entity's owner identity.</param>
        /// <param name="cancel">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public Task ModifyEntityOwnerAsync(int contentId, int ownerId, CancellationToken cancel)
        {
            return SecurityContext.ModifyEntityOwnerAsync(contentId, ownerId, cancel);
        }

        /// <summary>
        /// Moves the entity and it's whole subtree including the related ACLs in the security component 
        /// after the content was moved in the repository.
        /// </summary>
        /// <param name="sourceId">Id of the source entity. Cannot be 0.</param>
        /// <param name="targetId">Id of the target entity that will contain the source. Cannot be 0.</param>
        [Obsolete("Use async version instead.", true)]//UNDONE:xx0:AsyncSecu: change to true
        public void MoveEntity(int sourceId, int targetId)
        {
            SecurityContext.MoveEntity(sourceId, targetId);
        }
        /// <summary>
        /// Moves the entity and it's whole subtree including the related ACLs in the security component 
        /// after the content was moved in the repository.
        /// </summary>
        /// <param name="sourceId">Id of the source entity. Cannot be 0.</param>
        /// <param name="targetId">Id of the target entity that will contain the source. Cannot be 0.</param>
        /// <param name="cancel">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public Task MoveEntityAsync(int sourceId, int targetId, CancellationToken cancel)
        {
            return SecurityContext.MoveEntityAsync(sourceId, targetId, cancel);
        }

        /// <summary>
        /// Deletes the entity and it's whole subtree including the related ACLs from the security component
        /// after the content was deleted from the repository.
        /// </summary>
        /// <param name="contentId">Id of the entity. Cannot be 0.</param>
        [Obsolete("Use async version instead.", true)]//UNDONE:xx0:AsyncSecu: change to true
        public void DeleteEntity(int contentId)
        {
            SecurityContext.DeleteEntity(contentId);
        }
        /// <summary>
        /// Deletes the entity and it's whole subtree including the related ACLs from the security component
        /// after the content was deleted from the repository.
        /// </summary>
        /// <param name="contentId">Id of the entity. Cannot be 0.</param>
        /// <param name="cancel">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public Task DeleteEntityAsync(int contentId, CancellationToken cancel)
        {
            return SecurityContext.DeleteEntityAsync(contentId, cancel);
        }

        /// <summary>
        /// Tries to re-create the entity in the security component. This is a compensation method,
        /// call it only from where compensation is needed (e.g. there is a chance for a timing issue).
        /// </summary>
        [Obsolete("Use async version instead.", true)]//UNDONE:xxx0:AsyncSecu: change to true
        internal void ReCreateSecurityEntity(int contentId)
        {
            SnLog.WriteWarning("Re-creating entity in security component: " + contentId, EventId.Security);

            try
            {
                // compensation: try to re-create the missing entity in the security component
                CreateSecurityEntity(contentId);
            }
            catch (SecurityStructureException)
            {
                // Another thread already created the entity. No problem,
                // simply retry the original operation in the caller.
            }
        }
        /// <summary>
        /// Tries to re-create the entity in the security component. This is a compensation method,
        /// call it only from where compensation is needed (e.g. there is a chance for a timing issue).
        /// </summary>
        internal async Task ReCreateSecurityEntityAsync(int contentId, CancellationToken cancel)
        {
            SnLog.WriteWarning("Re-creating entity in security component: " + contentId, EventId.Security);

            try
            {
                await CreateSecurityEntityAsync(contentId, cancel).ConfigureAwait(false);
            }
            catch (SecurityStructureException)
            {
                // Another thread already created the entity. No problem,
                // simply retry the original operation in the caller.
            }
        }

        #endregion

        #region /*========================================================== Entity inheritance */

        /// <summary>
        /// Returns false if the content inherits the permissions from it's parent.
        /// </summary>
        /// <param name="contentId">Id of the content. Cannot be 0.</param>
        public bool IsEntityInherited(int contentId)
        {
            return SecurityContext.IsEntityInherited(contentId);
        }

        /// <summary>
        /// Clears the permission inheritance on the passed content.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <param name="convertToExplicit">If true (default), all effective permissions will be copied explicitly.</param>
        [Obsolete("Use async version instead.", true)]//UNDONE:xxx0:AsyncSecu: change to true
        public void BreakInheritance(Node content, bool convertToExplicit = true)
        {
            var contentId = content.Id;
            if (!IsEntityInherited(contentId))
                return;
            SecurityContext.CreateAclEditor()
                .BreakInheritance(contentId, convertToExplicit ? new[] { EntryType.Normal } : new EntryType[0])
                .Apply();
        }
        /// <summary>
        /// Clears the permission inheritance on the passed content.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <param name="cancel">The token to monitor for cancellation requests.</param>
        /// <param name="convertToExplicit">If true (default), all effective permissions will be copied explicitly.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public async Task BreakInheritanceAsync(Node content, CancellationToken cancel, bool convertToExplicit = true)
        {
            var contentId = content.Id;
            if (!IsEntityInherited(contentId))
                return;
            await SecurityContext.CreateAclEditor()
                .BreakInheritance(contentId, convertToExplicit ? new[] { EntryType.Normal } : Array.Empty<EntryType>())
                .ApplyAsync(cancel).ConfigureAwait(false);
        }
        /// <summary>
        /// Restores the permission inheritance on the passed content.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <param name="normalize">If true (default is false), the unnecessary explicit entries will be removed.</param>
        [Obsolete("Use async version instead.", true)]//UNDONE:xxx0:AsyncSecu: change to true
        public void UnbreakInheritance(Node content, bool normalize = false)
        {
            var contentId = content.Id;
            if (IsEntityInherited(contentId))
                return;
            SecurityContext.CreateAclEditor()
                .UnBreakInheritance(contentId,
                    normalize ? new[] { EntryType.Normal } : new EntryType[0])
                .Apply();
        }
        /// <summary>
        /// Restores the permission inheritance on the passed content.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <param name="cancel">The token to monitor for cancellation requests.</param>
        /// <param name="normalize">If true (default is false), the unnecessary explicit entries will be removed.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public async Task UnbreakInheritanceAsync(Node content, CancellationToken cancel, bool normalize = false)
        {
            var contentId = content.Id;
            if (IsEntityInherited(contentId))
                return;
            await SecurityContext.CreateAclEditor()
                .UnBreakInheritance(contentId,
                    normalize ? new[] { EntryType.Normal } : new EntryType[0])
                .ApplyAsync(cancel).ConfigureAwait(false);
        }

        #endregion

        #region /*========================================================== Membership, Collecting related identities */

        /// <summary>
        /// Determines if the provided member (user or group) is a member of a group. This method
        /// is transitive, meaning it will look for relations in the whole group graph, not 
        /// only direct memberships.
        /// </summary>
        /// <param name="identityId">Id of the potential member that can be a user or a group.</param>
        /// <param name="groupId">Id of the container group.</param>
        public bool IsInGroup(int identityId, int groupId)
        {
            return SecurityContext.IsInGroup(identityId, groupId);
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
        public void AddMembers(int groupId, IEnumerable<int> userMembers, IEnumerable<int> groupMembers, IEnumerable<int> parentGroups = null)
        {
            SecurityContext.AddMembersToSecurityGroup(groupId, userMembers, groupMembers, parentGroups);
        }
        /// <summary>
        /// Adds different kinds of members to a group in one step.
        /// Non-existing groups or member groups will be created.
        /// If all the parameters are null or empty, nothing will happen.
        /// </summary>
        /// <param name="groupId">Identifier of the container group. Cannot be 0.</param>
        /// <param name="userMembers">Collection of user member identifiers. Can be null or empty.</param>
        /// <param name="groupMembers">Collection of group member identifiers. Can be null or empty.</param>
        /// <param name="cancel">The token to monitor for cancellation requests.</param>
        /// <param name="parentGroups">Collection of parent group identifiers. Use this if the parent 
        /// <returns>A Task that represents the asynchronous operation.</returns>
        /// group or groups are already known when this method is called. Can be null or empty.</param>
        public Task AddMembersAsync(int groupId, IEnumerable<int> userMembers, IEnumerable<int> groupMembers, CancellationToken cancel, IEnumerable<int> parentGroups = null)
        {
            return SecurityContext.AddMembersToSecurityGroupAsync(groupId, userMembers, groupMembers, cancel, parentGroups);
        }

        /// <summary>
        /// Adds one or more users to a group in one step.
        /// Non-existing group will be created.
        /// This method is a shortcut for AddMembers(...).
        /// </summary>
        /// <param name="groupId">Identifier of the container group. Cannot be 0.</param>
        /// <param name="userMembers">Collection of user member identifiers. Can be null or empty.</param>
        [Obsolete("Use async version instead.", true)]//UNDONE:xxx0:AsyncSecu: change to true
        public void AddUsersToGroup(int groupId, IEnumerable<int> userMembers)
        {
            SecurityContext.AddUsersToSecurityGroup(groupId, userMembers);
        }
        /// <summary>
        /// Adds one or more users to a group in one step.
        /// Non-existing group will be created.
        /// This method is a shortcut for AddMembers(...).
        /// </summary>
        /// <param name="groupId">Identifier of the container group. Cannot be 0.</param>
        /// <param name="userMembers">Collection of user member identifiers. Can be null or empty.</param>
        /// <param name="cancel">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public Task AddUsersToGroupAsync(int groupId, IEnumerable<int> userMembers, CancellationToken cancel)
        {
            return SecurityContext.AddUsersToSecurityGroupAsync(groupId, userMembers, cancel);
        }

        /// <summary>
        /// Add one or more group members to a group. If the main group or any member is unknown it will be created.
        /// This method is a shortcut for AddMembers(...).
        /// </summary>
        /// <param name="groupId">Identifier of the container group. Cannot be 0.</param>
        /// <param name="groupMembers">Collection of group member identifiers. Can be null or empty.</param>
        [Obsolete("Use async version instead.", true)]//UNDONE:xxx0:AsyncSecu: change to true
        public void AddGroupsToGroup(int groupId, IEnumerable<int> groupMembers)
        {
            SecurityContext.AddGroupsToSecurityGroup(groupId, groupMembers);
        }
        /// <summary>
        /// Add one or more group members to a group. If the main group or any member is unknown it will be created.
        /// This method is a shortcut for AddMembers(...).
        /// </summary>
        /// <param name="groupId">Identifier of the container group. Cannot be 0.</param>
        /// <param name="groupMembers">Collection of group member identifiers. Can be null or empty.</param>
        /// <param name="cancel">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public Task AddGroupsToGroupAsync(int groupId, IEnumerable<int> groupMembers, CancellationToken cancel)
        {
            return SecurityContext.AddGroupsToSecurityGroupAsync(groupId, groupMembers, cancel);
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
        public void RemoveMembers(int groupId, IEnumerable<int> userMembers, IEnumerable<int> groupMembers, IEnumerable<int> parentGroups = null)
        {
            SecurityContext.RemoveMembersFromSecurityGroup(groupId, userMembers, groupMembers, parentGroups);
        }
        /// <summary>
        /// Removes multiple kinds of members from a group in one step.
        /// Non-existing groups or member groups will be skipped.
        /// If all the parameters are null or empty, nothing will happen.
        /// </summary>
        /// <param name="groupId">Identifier of the container group. Cannot be 0.</param>
        /// <param name="userMembers">Collection of user member identifiers. Can be null or empty.</param>
        /// <param name="groupMembers">Collection of group member identifiers. Can be null or empty.</param>
        /// <param name="cancel">The token to monitor for cancellation requests.</param>
        /// <param name="parentGroups">Collection of parent group identifiers. Can be null or empty.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public Task RemoveMembersAsync(int groupId, IEnumerable<int> userMembers, IEnumerable<int> groupMembers, CancellationToken cancel, IEnumerable<int> parentGroups = null)
        {
            return SecurityContext.RemoveMembersFromSecurityGroupAsync(groupId, userMembers, groupMembers, cancel, parentGroups);
        }

        /// <summary>
        /// Removes one or more group members from a group in one step.
        /// Non-existing group or member groups will be skipped.
        /// This method is a shortcut for RemoveMembers(...).
        /// </summary>
        /// <param name="groupId">Identifier of the container group. Cannot be 0.</param>
        /// <param name="groupIds">Collection of group member identifiers. Can be null or empty.</param>
        [Obsolete("Use async version instead.", true)]//UNDONE:xxx0:AsyncSecu: change to true
        public void RemoveGroupsFromGroup(int groupId, IEnumerable<int> groupIds)
        {
            SecurityContext.RemoveGroupsFromSecurityGroup(groupId, groupIds);
        }
        /// <summary>
        /// Removes one or more group members from a group in one step.
        /// Non-existing group or member groups will be skipped.
        /// This method is a shortcut for RemoveMembers(...).
        /// </summary>
        /// <param name="groupId">Identifier of the container group. Cannot be 0.</param>
        /// <param name="groupIds">Collection of group member identifiers. Can be null or empty.</param>
        /// <param name="cancel">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public Task RemoveGroupsFromGroupAsync(int groupId, IEnumerable<int> groupIds, CancellationToken cancel)
        {
            return SecurityContext.RemoveGroupsFromSecurityGroupAsync(groupId, groupIds, cancel);
        }

        /// <summary>
        /// Deletes the specified group and its relations including related security entries from the security component.
        /// </summary>
        [Obsolete("Use async version instead.", true)]//UNDONE:xxx0:AsyncSecu: change to true
        public void DeleteGroup(int groupId)
        {
            SecurityContext.DeleteSecurityGroup(groupId);
        }
        /// <summary>
        /// Deletes the specified group and its relations including related security entries from the security component.
        /// </summary>
        /// <param name="groupId">The Id of the group to be deleted.</param>
        /// <param name="cancel">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public Task DeleteGroupAsync(int groupId, CancellationToken cancel)
        {
            return SecurityContext.DeleteSecurityGroupAsync(groupId, cancel);
        }

        /// <summary>
        /// Deletes the user from the security component by removing all memberships and security entries related to this user.
        /// </summary>
        [Obsolete("Use async version instead.", true)]//UNDONE:xxx0:AsyncSecu: change to true
        public void DeleteUser(int userId)
        {
            SecurityContext.DeleteUser(userId);
        }
        /// <summary>
        /// Deletes the user from the security component by removing all memberships and security entries related to this user.
        /// </summary>
        /// <param name="userId">The Id of the user to be deleted.</param>
        /// <param name="cancel">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public Task DeleteUserAsync(int userId, CancellationToken cancel)
        {
            return SecurityContext.DeleteUserAsync(userId, cancel);
        }

        /// <summary>
        /// Deletes the specified group or user and its relations including related security entries from the security component.
        /// </summary>
        [Obsolete("Use async version instead.", true)]//UNDONE:xxx0:AsyncSecu: change to true
        public void DeleteIdentity(int id)
        {
            SecurityContext.DeleteIdentity(id);
        }
        /// <summary>
        /// Deletes the specified group or user and its relations including related security entries from the security component.
        /// </summary>
        /// <param name="id">The Id of the identity to be deleted.</param>
        /// <param name="cancel">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public Task DeleteIdentityAsync(int id, CancellationToken cancel)
        {
            return SecurityContext.DeleteIdentityAsync(id, cancel);
        }

        /// <summary>
        /// Deletes the specified groups or users and their relations including related security entries from the security component.
        /// </summary>
        [Obsolete("Use async version instead.", true)]//UNDONE:xxx0:AsyncSecu: change to true
        public void DeleteIdentities(IEnumerable<int> ids)
        {
            SecurityContext.DeleteIdentities(ids);
        }
        /// <summary>
        /// Deletes the specified groups or users and their relations including related security entries from the security component.
        /// </summary>
        /// <param name="ids">Set of identity Ids to be deleted.</param>
        /// <param name="cancel">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public Task DeleteIdentitiesAsync(IEnumerable<int> ids, CancellationToken cancel)
        {
            return SecurityContext.DeleteIdentitiesAsync(ids, cancel);
        }

        /*========================================================================================================== Collecting related identities */

        /* user is the logged in user */

        /// <summary>
        /// Returns with related identites that contains the current user id and every group id that contains it directly or indirectly.
        /// Everyone group id is added if the current user is not the Visitor.
        /// Extended identities are added if there are any (see SnSecurityContext.GetDynamicGroups).
        /// </summary>
        public List<int> GetIdentitiesByMembership()
        {
            return GetIdentitiesByMembershipPrivate();
        }
        /// <summary>
        /// Returns with related identites that contains the current user id and every group id that contains it directly or indirectly.
        /// Everyone group id is added if the current user is not the Visitor.
        /// Owners group id is added if the current user is owner of the passed content. Real owner is resolved from the security database.
        /// Extended identities are added if there are any (see SnSecurityContext.GetDynamicGroups).
        /// </summary>
        public List<int> GetIdentitiesByMembership(int contentId)
        {
            return GetIdentitiesByMembershipPrivate(contentId);
        }
        /// <summary>
        /// Returns with related identites that contains the current user id and every group id that contains it directly or indirectly.
        /// Everyone group id is added if the current user is not the Visitor.
        /// Owners group id is added if the current user and owner of the passed content are equal.
        /// Extended identities are added if there are any (see SnSecurityContext.GetDynamicGroups).
        /// </summary>
        public List<int> GetIdentitiesByMembership(NodeHead head)
        {
            return GetIdentitiesByMembershipPrivate(head.Id, head.OwnerId);
        }
        /// <summary>
        /// Returns with related identites that contains the current user id and every group id that contains it directly or indirectly.
        /// Everyone group id is added if the current user is not the Visitor.
        /// Owners group id is added if the current user and owner of the passed content are equal.
        /// Extended identities are added if there are any (see SnSecurityContext.GetDynamicGroups).
        /// </summary>
        public List<int> GetIdentitiesByMembership(Node content)
        {
            return GetIdentitiesByMembershipPrivate(content.Id, content.OwnerId);
        }

        /* user can be different from the logged in user */

        /// <summary>
        /// Returns with related identites that contains the passed user id and every group id that contains it directly or indirectly.
        /// Everyone group id is added if the passed user is not the Visitor.
        /// Extended identities are added if there are any (see SnSecurityContext.GetDynamicGroups).
        /// </summary>
        public List<int> GetIdentitiesByMembership(IUser user)
        {
            return GetIdentitiesByMembershipPrivate(user: user);
        }
        /// <summary>
        /// Returns with related identites that contains the passed user id and every group id that contains it directly or indirectly.
        /// Everyone group id is added if the passed user is not the Visitor.
        /// Owners group id is added if the passed user is owner of the passed content. Real owner is resolved from the security database.
        /// Extended identities are added if there are any (see SnSecurityContext.GetDynamicGroups).
        /// </summary>
        public List<int> GetIdentitiesByMembership(IUser user, int contentId)
        {
            return GetIdentitiesByMembershipPrivate(contentId, user: user);
        }
        /// <summary>
        /// Returns with related identites that contains the passed user id and every group id that contains it directly or indirectly.
        /// Everyone group id is added if the passed user is not the Visitor.
        /// Owners group id is added if the passed user and owner of the passed content are equal.
        /// Extended identities are added if there are any (see SnSecurityContext.GetDynamicGroups).
        /// </summary>
        public List<int> GetIdentitiesByMembership(IUser user, NodeHead head)
        {
            return GetIdentitiesByMembershipPrivate(head.Id, head.OwnerId, user);
        }
        /// <summary>
        /// Returns with related identites that contains the passed user id and every group id that contains it directly or indirectly.
        /// Everyone group id is added if the passed user is not the Visitor.
        /// Owners group id is added if the passed user and owner of the passed content are equal.
        /// Extended identities are added if there are any (see SnSecurityContext.GetDynamicGroups).
        /// </summary>
        public List<int> GetIdentitiesByMembership(IUser user, Node content)
        {
            return GetIdentitiesByMembershipPrivate(content.Id, content.OwnerId, user);
        }

        // main function
        private List<int> GetIdentitiesByMembershipPrivate(int contentId = 0, int ownerId = 0, IUser user = null)
        {
            var actualUser = user ?? AccessProvider.Current.GetCurrentUser();
            if (actualUser.Id == Identifiers.SystemUserId)
                return new List<int> { Identifiers.SystemUserId };

            List<int> identities;
            if (contentId == 0)
            {
                identities = GetGroups(actualUser);
            }
            else if (ownerId == 0)
            {
                identities = GetGroupsWithOwnership(contentId, actualUser);
            }
            else
            {
                identities = GetGroups(actualUser);
                if (actualUser.Id == ownerId)
                    identities.Add(Identifiers.OwnersGroupId);
            }
            identities.Add(actualUser.Id);

            return identities;
        }

        /*---------------------------------------------------------------------------------------------------------- */

        /// <summary>
        /// Gets the ids of all the groups that contain the current or provided user as a member, even through other groups.
        /// </summary>
        public IEnumerable<int> GetFlattenedGroups(IUser differentUser = null)
        {
            return (differentUser == null ? SecurityContext : CreateSecurityContextFor(differentUser)).GetFlattenedGroups();
        }
        /// <summary>
        /// Gets the ids of all the groups that contain the current or provided user as a member, even through other groups,
        /// plus Everyone (except in case of a visitor) and the optional dynamic groups provided by the 
        /// membership extender.
        /// </summary>
        public List<int> GetGroups(IUser differentUser = null)
        {
            return (differentUser == null ? SecurityContext : CreateSecurityContextFor(differentUser)).GetGroups();
        }
        /// <summary>
        /// Gets the ids of all the groups that contain the current or provided user as a member, even through other groups,
        /// plus Everyone (except in case of a visitor), plus Owners (if applicable) and the optional 
        /// dynamic groups provided by the membership extender.
        /// </summary>
        public List<int> GetGroupsWithOwnership(int contentId, IUser differentUser = null)
        {
            return (differentUser == null ? SecurityContext : CreateSecurityContextFor(differentUser)).GetGroupsWithOwnership(contentId);
        }

        #endregion

        #region /*========================================================== Permission dependencies */

        private int[][] _permissionDependencyTable;
        private readonly object _dependencyTableLock = new object();
        /// <summary>
        /// Provides technical data for the user interface (for backward compatibility purposes). Do not use this method in your code.
        /// </summary>
        public int[][] PermissionDependencyTable
        {
            get
            {
                if (_permissionDependencyTable == null)
                {
                    lock (_dependencyTableLock)
                    {
                        if (_permissionDependencyTable == null)
                        {
                            var tempTable = new int[PermissionType.PermissionCount][];
                            for (int i = 0; i < PermissionType.PermissionCount; i++)
                            {
                                tempTable[i] = GetPermissionDependencyArray(PermissionType.GetByIndex(i));
                            }

                            _permissionDependencyTable = tempTable;
                        }
                    }
                }
                return _permissionDependencyTable;
            }
        }
        private int[] GetPermissionDependencyArray(PermissionType permissionType)
        {
            var permArray = new int[PermissionType.PermissionCount];

            // zero out all elements
            Array.Clear(permArray, 0, permArray.Length);

            // 1 for the element itself
            permArray[permissionType.Index] = 1;

            // 1 for the allowed elements
            foreach (var item in GetAllowedPermissions(permissionType))
            {
                permArray[item.Index] = 1;
            }

            return permArray;
        }
        /// <summary>
        /// Returns a set that contains all other permission types that will be allowed 
        /// if you allow the passed permission (permission dependencies).
        /// </summary>
        public IEnumerable<PermissionTypeBase> GetAllowedPermissions(PermissionType permission)
        {
            var permissionList = new List<PermissionTypeBase>();
            GetAllowedPermissionsRecursive(permission, permissionList);
            return permissionList;
        }
        private void GetAllowedPermissionsRecursive(PermissionTypeBase permission, List<PermissionTypeBase> permissionList)
        {
            if (permission == null || permission.Allows == null)
                return;

            foreach (var item in permission.Allows)
            {
                permissionList.Add(item);
                GetAllowedPermissionsRecursive(item, permissionList);
            }
        }

        #endregion

        #region /*========================================================== Context, System start */

        private SecuritySystem _securitySystem;
        private ISecurityContextFactory _securityContextFactory;

        /// <summary>
        /// Initializes the security system. Called during system startup.
        /// WARNING! Do not use this method in your code!
        /// </summary>
        public void StartSecurity(bool isWebContext, IServiceProvider services)
        {
            var dummy = PermissionType.Open;
            var securityDataProvider = Providers.Instance.SecurityDataProvider;
            var messageProvider = Providers.Instance.SecurityMessageProvider;
            var securityMessageFormatter = services?.GetService<ISecurityMessageFormatter>();
            var missingEntityHandler = services?.GetService<IMissingEntityHandler>() ??
                new SnMissingEntityHandler();

            var securityConfig = services?.GetService<IOptions<SecurityConfiguration>>()?.Value ??
                                 new SecurityConfiguration
                                 {
                                     SystemUserId = Identifiers.SystemUserId,
                                     VisitorUserId = Identifiers.VisitorUserId,
                                     EveryoneGroupId = Identifiers.EveryoneGroupId,
                                     OwnerGroupId = Identifiers.OwnersGroupId
                                 };

            var messagingOptions = services?.GetService<IOptions<MessagingOptions>>()?.Value ?? new MessagingOptions();

            var securitySystem = new SecuritySystem(securityDataProvider, messageProvider,
                securityMessageFormatter, missingEntityHandler, 
                Options.Create(securityConfig), Options.Create(messagingOptions));
            securitySystem.StartAsync(CancellationToken.None).GetAwaiter().GetResult();

            _securityContextFactory = isWebContext 
                ? (ISecurityContextFactory)new DynamicSecurityContextFactory(securitySystem) 
                : new StaticSecurityContextFactory(securitySystem);

            _securitySystem = securitySystem;

            SnLog.WriteInformation("Security subsystem started", EventId.RepositoryLifecycle,
                properties: new Dictionary<string, object> { 
                    { "DataProvider", securityDataProvider.GetType().FullName },
                    { "MessageProvider", messageProvider.GetType().FullName }
                });
        }

        /// <summary>
        /// The security context related to the logged-in user. Always returns a new instance.
        /// </summary>
        public SnSecurityContext SecurityContext => _securityContextFactory.Create(AccessProvider.Current.GetCurrentUser());

        /// <summary>
        /// Returns with a security context containing the provided user who can be different from the logged-in user.
        /// </summary>
        public SnSecurityContext CreateSecurityContextFor(IUser user) => _securityContextFactory.Create(user);

        #endregion

        #region /*========================================================== Install, Import, Export */


        /*=========================================================================================== Initial-permission parser */


        #endregion

        #region /*========================================================== Tools */

        /// <summary>
        /// Converts a permission type array to the appropriate bitmask.
        /// If a permission type is present in the set, the appropriate bit will be set to 1. 
        /// Any other bits will remain 0.
        /// </summary>
        /// <param name="permissionTypes">Any number of permission types. Null or empty are also allowed.</param>
        /// <returns>The bitmask.</returns>
        public ulong GetPermissionMask(IEnumerable<PermissionType> permissionTypes)
        {
            var mask = 0uL;
            if (permissionTypes == null)
                return mask;
            foreach (var permissionType in permissionTypes)
                mask |= 1uL << (permissionType.Index);
            return mask;
        }

        private PermissionBitMask AggregateAces(IEnumerable<AceInfo> aces)
        {
            var result = new PermissionBitMask();
            foreach (var ace in aces)
            {
                result.AllowBits |= ace.AllowBits;
                result.DenyBits |= ace.DenyBits;
            }
            return result;
        }

        private Exception GetAccessDeniedException(int nodeId, string path, string message, PermissionType[] permissionTypes, IUser user, bool isSubtree)
        {
            PermissionType deniedPermission = null;
            foreach (var permType in permissionTypes)
            {
                if (!(isSubtree ? HasSubTreePermission(nodeId, permType) : HasPermission(nodeId, permType)))
                {
                    deniedPermission = permType;
                    break;
                }
            }

            if (deniedPermission == null)
                throw new SenseNetSecurityException(path, null, user);
            if (message != null)
                throw new SenseNetSecurityException(path, deniedPermission, user, message);
            else
                throw new SenseNetSecurityException(path, deniedPermission, user);
        }

        /// <summary>
        /// Copies effective permissions from the source content to the target as explicite entries.
        /// </summary>
        /// <param name="sourceId">Id of the source content.</param>
        /// <param name="targetId">Id of the target content.</param>
        /// <param name="mode">Whether a break or a permission clean is needed.</param>
        [Obsolete("Use async version instead.", true)]//UNDONE:xxx0:AsyncSecu: change to true
        public void CopyPermissionsFrom(int sourceId, int targetId, CopyPermissionMode mode)
        {
            bool @break, @clear;
            switch (mode)
            {
                case CopyPermissionMode.NoBreak: @break = false; @clear = false; break;
                case CopyPermissionMode.BreakWithoutClear: @break = true; @clear = false; break;
                case CopyPermissionMode.BreakAndClear: @break = true; @clear = true; break;
                default: throw new SnNotSupportedException("Unknown mode: " + mode);
            }
            var aclEd = CreateAclEditor();
            if (@break)
                aclEd.BreakInheritance(targetId, new EntryType[0]);
            if (@clear)
                aclEd.RemoveExplicitEntries(targetId);
            aclEd.CopyEffectivePermissions(sourceId, targetId);
            aclEd.Apply();
        }
        /// <summary>
        /// Copies effective permissions from the source content to the target as explicite entries.
        /// </summary>
        /// <param name="sourceId">Id of the source content.</param>
        /// <param name="targetId">Id of the target content.</param>
        /// <param name="mode">Whether a break or a permission clean is needed.</param>
        /// <param name="cancel">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public async Task CopyPermissionsFromAsync(int sourceId, int targetId, CopyPermissionMode mode, CancellationToken cancel)
        {
            bool @break, @clear;
            switch (mode)
            {
                case CopyPermissionMode.NoBreak: @break = false; @clear = false; break;
                case CopyPermissionMode.BreakWithoutClear: @break = true; @clear = false; break;
                case CopyPermissionMode.BreakAndClear: @break = true; @clear = true; break;
                default: throw new SnNotSupportedException("Unknown mode: " + mode);
            }
            var aclEd = CreateAclEditor();
            if (@break)
                aclEd.BreakInheritance(targetId, new EntryType[0]);
            if (@clear)
                aclEd.RemoveExplicitEntries(targetId);
            aclEd.CopyEffectivePermissions(sourceId, targetId);
            await aclEd.ApplyAsync(cancel).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets all security entities from the security component's in-memory cache for consistency check.
        /// WARNING! Do not use this method in your code!
        /// </summary>
        public IDictionary<int, SecurityEntity> GetCachedEntities()
        {
            using (new SystemAccount())
                return SecurityContext.GetCachedEntitiesForConsistencyCheck();
        }

        #endregion

	    public void ShutDownSecurity()
	    {
            _securitySystem.Shutdown();
	    }
    }
}
