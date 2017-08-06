using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Diagnostics;
using SenseNet.Security;
using SenseNet.Security.EF6SecurityStore;
using SenseNet.Security.Messaging;
using SenseNet.Tools;

namespace SenseNet.ContentRepository.Storage.Security
{
    public enum CopyPermissionMode { NoBreak, BreakWithoutClear, BreakAndClear }

    /// <summary>
    /// Central class for handling security-related operations (managing permissions and group memberships). 
    /// Contains both instance API (accessible through the Node.Security or Content.Security properties) and static API.
    /// </summary>
    public sealed class SecurityHandler
	{
		private readonly Node _node;

		internal SecurityHandler(Node node)
		{
			if (node == null)
				throw new ArgumentNullException("node");

			_node = node;
		}

        #region /*========================================================== Evaluation related methods */

        /// <summary>
        /// If one or more passed permissions are not allowed (undefined or denied) on the current content for the current user, SenseNetSecurityException will be thrown.
        /// </summary>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing" so SenseNetSecurityException will be thrown.</param>
        public void Assert(params PermissionType[] permissionTypes)
        {
            Assert(_node, permissionTypes);
        }
        /// <summary>
        /// If one or more passed permissions are not allowed (undefined or denied) on the current content for the current user, SenseNetSecurityException will be thrown with the specified message.
        /// </summary>
        /// <param name="message">Text that appears in the SenseNetSecurityException's message.</param>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing" so SenseNetSecurityException will be thrown.</param>
        public void Assert(string message, params PermissionType[] permissionTypes)
        {
            Assert(_node, message, permissionTypes);
        }
        /// <summary>
        /// If one or more passed permissions are not allowed (undefined or denied) on the passed content for the current user, SenseNetSecurityException will be thrown.
        /// </summary>
        /// <param name="node">The node. Cannot be null.</param>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing" so SenseNetSecurityException will be thrown.</param>
        public static void Assert(Node node, params PermissionType[] permissionTypes)
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
        public static void Assert(Node node, string message, params PermissionType[] permissionTypes)
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
        public static void Assert(NodeHead nodeHead, params PermissionType[] permissionTypes)
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
        public static void Assert(NodeHead nodeHead, string message, params PermissionType[] permissionTypes)
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
        public static void Assert(int contentId, params PermissionType[] permissionTypes)
        {
            Assert(contentId, null, null, permissionTypes);
        }
        /// <summary>
        /// If one or more passed permissions are not allowed (undefined or denied) on the passed content for the current user, SenseNetSecurityException will be thrown.
        /// </summary>
        /// <param name="contentId">The identifier of the content.</param>
        /// <param name="message">Text that appears in the SenseNetSecurityException's message.</param>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing" so SenseNetSecurityException will be thrown.</param>
        public static void Assert(int contentId, string message, params PermissionType[] permissionTypes)
        {
            Assert(contentId, null, message, permissionTypes);
        }
        private static void Assert(int nodeId, string path, string message, params PermissionType[] permissionTypes)
        {
            IUser user = AccessProvider.Current.GetCurrentUser();
            if (user.Id == -1)
                return;
            if (HasPermission(nodeId, permissionTypes))
                return;
            throw GetAccessDeniedException(nodeId, path, message, permissionTypes, user, false);
        }

        /// <summary>
        /// If one or more passed permissions are not allowed (undefined or denied) on every content in the whole subtree of the current content for the current user, SenseNetSecurityException will be thrown.
        /// </summary>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing" so SenseNetSecurityException will be thrown.</param>
        public void AssertSubtree(params PermissionType[] permissionTypes)
        {
            AssertSubtree(_node, permissionTypes);
        }
        /// <summary>
        /// If one or more passed permissions are not allowed (undefined or denied) on every content in the whole subtree of the current content for the current user, SenseNetSecurityException will be thrown.
        /// </summary>
        /// <param name="message">Text that appears in the SenseNetSecurityException's message.</param>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing" so SenseNetSecurityException will be thrown.</param>
        public void AssertSubtree(string message, params PermissionType[] permissionTypes)
        {
            AssertSubtree(_node, message, permissionTypes);
        }
        /// <summary>
        /// If one or more passed permissions are not allowed (undefined or denied) on every content in the whole subtree of the passed content for the current user, SenseNetSecurityException will be thrown.
        /// </summary>
        /// <param name="node">The node. Cannot be null.</param>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing" so SenseNetSecurityException will be thrown.</param>
        public static void AssertSubtree(Node node, params PermissionType[] permissionTypes)
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
        public static void AssertSubtree(Node node, string message, params PermissionType[] permissionTypes)
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
        public static void AssertSubtree(NodeHead nodeHead, params PermissionType[] permissionTypes)
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
        public static void AssertSubtree(NodeHead nodeHead, string message, params PermissionType[] permissionTypes)
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
        public static void AssertSubtree(int contentId, params PermissionType[] permissionTypes)
        {
            AssertSubtree(contentId, null, null, permissionTypes);
        }
        /// <summary>
        /// If one or more passed permissions are not allowed (undefined or denied) on every content in the whole subtree of the passed content for the current user, SenseNetSecurityException will be thrown.
        /// </summary>
        /// <param name="contentId">The identifier of the content.</param>
        /// <param name="message">Text that appears in the SenseNetSecurityException's message.</param>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing" so SenseNetSecurityException will be thrown.</param>
        public static void AssertSubtree(int contentId, string message, params PermissionType[] permissionTypes)
        {
            AssertSubtree(contentId, null, message, permissionTypes);
        }
        private static void AssertSubtree(int nodeId, string path, string message, params PermissionType[] permissionTypes)
        {
            IUser user = AccessProvider.Current.GetCurrentUser();
            if (user.Id == -1)
                return;
            if (HasSubTreePermission(nodeId, permissionTypes))
                return;
            throw GetAccessDeniedException(nodeId, path, message, permissionTypes, user, false);
        }

        /// <summary>
        /// Returns true if all passed permissions are allowed on the current content for the current user.
        /// </summary>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing".</param>
        public bool HasPermission(params PermissionType[] permissionTypes)
        {
            return HasPermission(_node, permissionTypes);
        }
        /// <summary>
        /// Returns true if all passed permissions are allowed on the passed content for the current user.
        /// </summary>
        /// <param name="node">The node. Cannot be null.</param>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing".</param>
        public static bool HasPermission(Node node, params PermissionType[] permissionTypes)
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
        public static bool HasPermission(NodeHead nodeHead, params PermissionType[] permissionTypes)
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
        public static bool HasPermission(int nodeId, params PermissionType[] permissionTypes)
        {
            if (permissionTypes == null)
                throw new ArgumentNullException("permissionTypes");
            if (permissionTypes.Length == 0)
                return false;
            var user = AccessProvider.Current.GetCurrentUser();
            if (user.Id == -1)
                return true;
            var ctx = SnSecurityContext.Create();
            return Retrier.Retry(3, 200, typeof(EntityNotFoundException), () => HasPermissionPrivate(ctx, nodeId, permissionTypes));
        }
        private static bool HasPermissionPrivate(SnSecurityContext ctx, int contentId, params PermissionType[] permissionTypes)
        {
            try
            {
                return ctx.HasPermission(contentId, permissionTypes);
            }
            catch (EntityNotFoundException)
            {
                // entity not found in the security component: try to re-create it
                ReCreateSecurityEntity(contentId);

                // retry the operation
                return ctx.HasPermission(contentId, permissionTypes);
            }
        }

        /// <summary>
        /// Returns true if all passed permissions are allowed on the passed content for the passed user.
        /// </summary>
        /// <param name="user">The user. Cannot be null.</param>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing".</param>
        public bool HasPermission(IUser user, params PermissionType[] permissionTypes)
        {
            return HasPermission(user, _node, permissionTypes);
        }
        /// <summary>
        /// Returns true if all passed permissions are allowed on the passed content for the passed user.
        /// </summary>
        /// <param name="user">The user. Cannot be null.</param>
        /// <param name="node">The node. Cannot be null.</param>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing".</param>
        public static bool HasPermission(IUser user, Node node, params PermissionType[] permissionTypes)
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
        public static bool HasPermission(IUser user, NodeHead nodeHead, params PermissionType[] permissionTypes)
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
        public static bool HasPermission(IUser user, int nodeId, params PermissionType[] permissionTypes)
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
        /// Returns true if all passed permissions are allowed for the current user on every content in the whole subtree of the current content.
        /// </summary>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing".</param>
        public bool HasSubTreePermission(params PermissionType[] permissionTypes)
        {
            return HasSubTreePermission(_node, permissionTypes);
        }
        /// <summary>
        /// Returns true if all passed permissions are allowed for the current user on every content in the whole subtree of the passed content.
        /// </summary>
        /// <param name="node">The node. Cannot be null.</param>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing".</param>
        public static bool HasSubTreePermission(Node node, params PermissionType[] permissionTypes)
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
        public static bool HasSubTreePermission(NodeHead nodeHead, params PermissionType[] permissionTypes)
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
        private static bool HasSubTreePermission(int nodeId, params PermissionType[] permissionTypes)
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
        /// Returns true if all passed permissions are allowed for the passed user on every content in the whole subtree of the current content.
        /// </summary>
        /// <param name="user">The user. Cannot be null.</param>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing".</param>
        public bool HasSubTreePermission(IUser user, params PermissionType[] permissionTypes)
        {
            return HasSubTreePermission(user, _node, permissionTypes);
        }
        /// <summary>
        /// Returns true if all passed permissions are allowed for the passed user on every content in the whole subtree of the passed content.
        /// </summary>
        /// <param name="user">The user. Cannot be null.</param>
        /// <param name="node">The node. Cannot be null.</param>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing".</param>
        public static bool HasSubTreePermission(IUser user, Node node, params PermissionType[] permissionTypes)
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
        public static bool HasSubTreePermission(IUser user, NodeHead nodeHead, params PermissionType[] permissionTypes)
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
        private static bool HasSubTreePermission(IUser user, int nodeId, params PermissionType[] permissionTypes)
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
        /// Returns an aggregated permission value by all passed permissions for the current user on the current content.
        /// Value is Denied if there is any denied passed permission,
        ///   Undefined if there is any undefined but there is no denied passed permission,
        ///   Allowed if every passed permission is allowed.
        /// </summary>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing".</param>
        public PermissionValue GetPermission(params PermissionType[] permissionTypes)
        {
            return GetPermission(_node, permissionTypes);
        }
        /// <summary>
        /// Returns an aggregated permission value by all passed permissions for the current user on the passed content.
        /// Value is Denied if there is any denied passed permission,
        ///   Undefined if there is any undefined but there is no denied passed permission,
        ///   Allowed if every passed permission is allowed.
        /// </summary>
        /// <param name="node">The node. Cannot be null.</param>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing".</param>
        public static PermissionValue GetPermission(Node node, params PermissionType[] permissionTypes)
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
        public static PermissionValue GetPermission(NodeHead nodeHead, params PermissionType[] permissionTypes)
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
        private static PermissionValue GetPermission(int nodeId, params PermissionType[] permissionTypes)
        {
            if (permissionTypes == null)
                throw new ArgumentNullException("permissionTypes");
            if (permissionTypes.Length == 0)
                return PermissionValue.Denied;

            return SecurityContext.GetPermission(nodeId, permissionTypes);
        }

        /// <summary>
        /// Returns an aggregated permission value by all passed permissions for the passed user on the current content.
        /// Value is Denied if there is any denied passed permission,
        ///   Undefined if there is any undefined but there is no denied passed permission,
        ///   Allowed if every passed permission is allowed.
        /// </summary>
        /// <param name="user">The user. Cannot be null.</param>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing".</param>
        public PermissionValue GetPermission(IUser user, params PermissionType[] permissionTypes)
        {
            return GetPermission(user, _node, permissionTypes);
        }
        /// <summary>
        /// Returns an aggregated permission value by all passed permissions for the passed user on the passed content.
        /// Value is Denied if there is any denied passed permission,
        ///   Undefined if there is any undefined but there is no denied passed permission,
        ///   Allowed if every passed permission is allowed.
        /// </summary>
        /// <param name="user">The user. Cannot be null.</param>
        /// <param name="node">The node. Cannot be null.</param>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing".</param>
        public PermissionValue GetPermission(IUser user, Node node, params PermissionType[] permissionTypes)
        {
            if (node == null)
                throw new ArgumentNullException("node");
            return GetPermission(user, node.Id, permissionTypes);
        }
        /// <summary>
        /// Returns an aggregated permission value by all passed permissions for the passed user on the passed content.
        /// Value is Denied if there is any denied passed permission,
        ///   Undefined if there is any undefined but there is no denied passed permission,
        ///   Allowed if every passed permission is allowed.
        /// </summary>
        /// <param name="user">The user. Cannot be null.</param>
        /// <param name="nodeHead">The node head. Cannot be null.</param>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing".</param>
        public PermissionValue GetPermission(IUser user, NodeHead nodeHead, params PermissionType[] permissionTypes)
        {
            if (nodeHead == null)
                throw new ArgumentNullException("nodeHead");
            return GetPermission(user, nodeHead.Id, permissionTypes);
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
        private static PermissionValue GetPermission(IUser user, int nodeId, params PermissionType[] permissionTypes)
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
        /// Returns an aggregated permission value by all passed permissions for the current user on every content in the whole subtree of the current content.
        /// Value is Denied if there is any denied passed permission,
        ///   Undefined if there is any undefined but there is no denied passed permission,
        ///   Allowed if every passed permission is allowed.
        /// </summary>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing".</param>
        public PermissionValue GetSubtreePermission(params PermissionType[] permissionTypes)
        {
            return GetSubtreePermission(_node, permissionTypes);
        }
        /// <summary>
        /// Returns an aggregated permission value by all passed permissions for the current user on every content in the whole subtree of the passed content.
        /// Value is Denied if there is any denied passed permission,
        ///   Undefined if there is any undefined but there is no denied passed permission,
        ///   Allowed if every passed permission is allowed.
        /// </summary>
        /// <param name="node">The node. Cannot be null.</param>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing".</param>
        public static PermissionValue GetSubtreePermission(Node node, params PermissionType[] permissionTypes)
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
        public static PermissionValue GetSubtreePermission(NodeHead nodeHead, params PermissionType[] permissionTypes)
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
        private static PermissionValue GetSubtreePermission(int nodeId, params PermissionType[] permissionTypes)
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
        /// Returns an aggregated permission value by all passed permissions for the passed user on every content in the whole subtree of the current content.
        /// Value is Denied if there is any denied passed permission,
        ///   Undefined if there is any undefined but there is no denied passed permission,
        ///   Allowed if every passed permission is allowed.
        /// </summary>
        /// <param name="user">The user. Cannot be null.</param>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing".</param>
        public PermissionValue GetSubtreePermission(IUser user, params PermissionType[] permissionTypes)
        {
            return GetSubtreePermission(user, _node, permissionTypes);
        }
        /// <summary>
        /// Returns an aggregated permission value by all passed permissions for the passed user on every content in the whole subtree of the passed content.
        /// Value is Denied if there is any denied passed permission,
        ///   Undefined if there is any undefined but there is no denied passed permission,
        ///   Allowed if every passed permission is allowed.
        /// </summary>
        /// <param name="user">The user. Cannot be null.</param>
        /// <param name="node">The node. Cannot be null.</param>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing".</param>
        public PermissionValue GetSubtreePermission(IUser user, Node node, params PermissionType[] permissionTypes)
        {
            if (node == null)
                throw new ArgumentNullException("node");
            return GetSubtreePermission(user, node.Id, permissionTypes);
        }
        /// <summary>
        /// Returns an aggregated permission value by all passed permissions for the passed user on every content in the whole subtree of the passed content.
        /// Value is Denied if there is any denied passed permission,
        ///   Undefined if there is any undefined but there is no denied passed permission,
        ///   Allowed if every passed permission is allowed.
        /// </summary>
        /// <param name="user">The user. Cannot be null.</param>
        /// <param name="nodeHead">The node head. Cannot be null.</param>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing".</param>
        public PermissionValue GetSubtreePermission(IUser user, NodeHead nodeHead, params PermissionType[] permissionTypes)
        {
            if (nodeHead == null)
                throw new ArgumentNullException("nodeHead");
            return GetSubtreePermission(user, nodeHead.Id, permissionTypes);
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
        private static PermissionValue GetSubtreePermission(IUser user, int nodeId, params PermissionType[] permissionTypes)
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
        public static PermittedLevel GetPermittedLevel(NodeHead nodeHead)
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
        public static PermittedLevel GetPermittedLevel(Node node)
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
        public static PermittedLevel GetPermittedLevel(int nodeId)
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
        public static PermittedLevel GetPermittedLevel(int nodeId, IUser user)
        {
            // shortcut for system user
            if (user.Id == Identifiers.SystemUserId)
                return PermittedLevel.All;
            return GetPermittedLevel(nodeId, GetIdentitiesByMembership(user, nodeId));
        }
        internal static PermittedLevel GetPermittedLevel(int nodeId, IEnumerable<int> identities)
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
        /// Return with the current content's explicit entries. Current user must have SeePermissions permission.
        /// </summary>
        public List<AceInfo> GetExplicitEntries()
        {
            return GetExplicitEntries(_node.Id);
        }
        /// <summary>
        /// Return with the passed content's explicit entries. Current user must have SeePermissions permission.
        /// </summary>
        /// <param name="contentId">Id of the content.</param>
        /// <param name="relatedIdentities">If not passed, the current user's related identities is focused.</param>
        public static List<AceInfo> GetExplicitEntries(int contentId, IEnumerable<int> relatedIdentities = null)
        {
            SecurityContext.AssertPermission(contentId, PermissionType.SeePermissions);
            return GetExplicitEntriesAsSystemUser(contentId, relatedIdentities);
        }
        /// <summary>
        /// Return with the passed content's explicit entries. There is permission check so you must call this method from a safe block.
        /// </summary>
        /// <param name="contentId">Id of the content.</param>
        /// <param name="relatedIdentities">If not passed, the current user's related identities is focused.</param>
        public static List<AceInfo> GetExplicitEntriesAsSystemUser(int contentId, IEnumerable<int> relatedIdentities = null)
        {
            return SecurityContext.GetExplicitEntries(contentId, relatedIdentities);
        }

        /// <summary>
        /// Return with the current content's effective entries. Current user must have SeePermissions permission.
        /// </summary>
        public List<AceInfo> GetEffectiveEntries()
        {
            return GetEffectiveEntries(_node.Id);
        }
        /// <summary>
        /// Return with the passed content's effective entries. Current user must have SeePermissions permission.
        /// </summary>
        /// <param name="contentId">Id of the content.</param>
        /// <param name="relatedIdentities">If not passed, the current user's related identities is focused.</param>
        public static List<AceInfo> GetEffectiveEntries(int contentId, IEnumerable<int> relatedIdentities = null)
        {
            SecurityContext.AssertPermission(contentId, PermissionType.SeePermissions);
            return GetEffectiveEntriesAsSystemUser(contentId, relatedIdentities);
        }
        /// <summary>
        /// Return with the passed content's effective entries. There is permission check so you must call this method from a safe block.
        /// </summary>
        /// <param name="contentId">Id of the content.</param>
        /// <param name="relatedIdentities">If not passed, the current user's related identities is focused.</param>
        public static List<AceInfo> GetEffectiveEntriesAsSystemUser(int contentId, IEnumerable<int> relatedIdentities = null)
        {
            return SecurityContext.GetEffectiveEntries(contentId, relatedIdentities);
        }
        #endregion

        #region /*========================================================== ACL */

        /// <summary>
        /// Returns the AccessControlList of the current content.
        /// </summary>
        public AccessControlList GetAcl()
        {
            return GetAcl(_node.Id);
        }
        /// <summary>
        /// Returns the AccessControlList of the requested content.
        /// Required permission: SeePermissions
        /// </summary>
        public static AccessControlList GetAcl(int nodeId)
        {
            var ctx = SecurityContext;
            ctx.AssertPermission(nodeId, PermissionType.SeePermissions);
            return ctx.GetAcl(nodeId);
        }

        /// <summary>
        /// Returns a new AclEditor instance.
        /// </summary>
        /// <param name="context">If passed, the method uses that and does not create a new context instance.</param>
        public static SnAclEditor CreateAclEditor(SnSecurityContext context = null)
        {
            return new SnAclEditor(context);
        }

        /// <summary>
        /// Removes all explicit entries from the current content.
        /// If AclEditor passed, the modification is executed in it
        /// else executed immediately.
        /// </summary>
        public void RemoveExplicitEntries(SnAclEditor aclEditor = null)
        {
            if (aclEditor == null)
                CreateAclEditor().RemoveExplicitEntries(_node.Id).Apply();
            else
                aclEditor.RemoveExplicitEntries(_node.Id);
        }

        #endregion

        #region /*========================================================== Entities */

        /// <summary>
        /// Loads the content by the passed id and creates the security entity by the properties of the loaded content.
        /// </summary>
        /// <param name="contentId">Id of the content.</param>
        /// <param name="throwIfNodeNotFound">If true and the requested content does not exist,
        /// a ContentNotFoundException will be thrown.</param>
        public static void CreateSecurityEntity(int contentId, bool throwIfNodeNotFound = false)
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
        /// Creates a new entity in the security component, if it does not exist.
        /// Parent content must exist.
        /// </summary>
        /// <param name="contentId">Id of the created entity. Cannot be 0.</param>
        /// <param name="parentId">Id of the parent entity. Cannot be 0.</param>
        /// <param name="ownerId">Id of the entity's owner identity.</param>
        public static void CreateSecurityEntity(int contentId, int parentId, int ownerId)
        {
            CreateSecurityEntity(contentId, parentId, ownerId, SecurityContext);
        }
        /// <summary>
        /// Creates a new entity in the security component, if it does not exist.
        /// Parent must exist.
        /// </summary>
        /// <param name="contentId">Id of the created entity. Cannot be 0.</param>
        /// <param name="parentId">Id of the parent entity. Cannot be 0.</param>
        /// <param name="ownerId">Id of the entity's owner identity.</param>
        /// <param name="context">Uses the passed context and does not create a new one.</param>
        public static void CreateSecurityEntity(int contentId, int parentId, int ownerId, SnSecurityContext context)
        {
            if (CheckSecurityEntityCreationParameters(contentId, parentId, ownerId, context))
                context.CreateSecurityEntity(contentId, parentId, ownerId);
        }
        private static bool CheckSecurityEntityCreationParameters(int contentId, int parentId, int ownerId, SecurityContext context)
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
        public static void ModifyEntityOwner(int contentId, int ownerId)
        {
            SecurityContext.ModifyEntityOwner(contentId, ownerId);
        }
        /// <summary>
        /// Moves the entity and it's whole subtree including the related ACLs in the security component 
        /// after the content was moved in the repository.
        /// </summary>
        /// <param name="sourceId">Id of the source entity. Cannot be 0.</param>
        /// <param name="targetId">Id of the target entity that will contain the source. Cannot be 0.</param>
        public static void MoveEntity(int sourceId, int targetId)
        {
            SecurityContext.MoveEntity(sourceId, targetId);
        }
        /// <summary>
        /// Deletes the entity and it's whole subtree including the related ACLs from the security component
        /// after the content was deleted from the repository.
        /// </summary>
        /// <param name="contentId">Id of the entity. Cannot be 0.</param>
        public static void DeleteEntity(int contentId)
        {
            SecurityContext.DeleteEntity(contentId);
        }

        /// <summary>
        /// Tries to re-create the entity in the security component. This is a compensation method,
        /// call it only from where compensation is needed (e.g. there is a chance for a timing issue).
        /// </summary>
        internal static void ReCreateSecurityEntity(int contentId)
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

        #endregion

        #region /*========================================================== Entity inheritance */

        /// <summary>
        /// Returns false if the current content inherits the permissions from it's parent.
        /// </summary>
        public bool IsInherited
        {
            get { return SecurityContext.IsEntityInherited(_node.Id); }
        }
        /// <summary>
        /// Returns false if the content inherits the permissions from it's parent.
        /// </summary>
        /// <param name="contentId">Id of the content. Cannot be 0.</param>
        public static bool IsEntityInherited(int contentId)
        {
            return SecurityContext.IsEntityInherited(contentId);
        }

        /// <summary>
        /// Clear the permission inheritance on the current content.
        /// </summary>
        /// <param name="convertToExplicit">If true (default), all effective permissions will be copied explicitly.</param>
        public void BreakInheritance(bool convertToExplicit = true)
        {
            BreakInheritance(this._node, convertToExplicit);
        }
        /// <summary>
        /// Clears the permission inheritance on the passed content.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <param name="convertToExplicit">If true (default), all effective permissions will be copied explicitly.</param>
        public static void BreakInheritance(Node content, bool convertToExplicit = true)
        {
            var contentId = content.Id;
            if (!IsEntityInherited(contentId))
                return;
            SecurityContext.CreateAclEditor().BreakInheritance(contentId, convertToExplicit).Apply();
        }
        /// <summary>
        /// Restores the permission inheritance on the current content.
        /// </summary>
        /// <param name="normalize">If true (default is false), the unnecessary explicit entries 
        /// (the ones that are the same as the inherited ones) will be removed.</param>
        public void RemoveBreakInheritance(bool normalize = false)
        {
            UnbreakInheritance(this._node, normalize);
        }
        /// <summary>
        /// Restores the permission inheritance on the passed content.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <param name="normalize">If true (default is false), the unnecessary explicit entries will be removed.</param>
        public static void UnbreakInheritance(Node content, bool normalize = false)
        {
            var contentId = content.Id;
            if (IsEntityInherited(contentId))
                return;
            SecurityContext.CreateAclEditor().UnbreakInheritance(contentId, normalize).Apply();
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
        /// <returns></returns>
        public static bool IsInGroup(int identityId, int groupId)
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
        public static void AddMembers(int groupId, IEnumerable<int> userMembers, IEnumerable<int> groupMembers, IEnumerable<int> parentGroups = null)
        {
            SecurityContext.AddMembersToSecurityGroup(groupId, userMembers, groupMembers, parentGroups);
        }
        /// <summary>
        /// Adds one or more users to a group in one step.
        /// Non-existing group will be created.
        /// This method is a shortcut for AddMembers(...).
        /// </summary>
        /// <param name="groupId">Identifier of the container group. Cannot be 0.</param>
        /// <param name="userMembers">Collection of user member identifiers. Can be null or empty.</param>
        public static void AddUsersToGroup(int groupId, IEnumerable<int> userMembers)
        {
            SecurityContext.AddUsersToSecurityGroup(groupId, userMembers);
        }
        /// <summary>
        /// Add one or more group members to a group. If the main group or any member is unknown it will be created.
        /// This method is a shortcut for AddMembers(...).
        /// </summary>
        /// <param name="groupId">Identifier of the container group. Cannot be 0.</param>
        /// <param name="groupMembers">Collection of group member identifiers. Can be null or empty.</param>
        public static void AddGroupsToGroup(int groupId, IEnumerable<int> groupMembers)
        {
            SecurityContext.AddGroupsToSecurityGroup(groupId, groupMembers);
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
        public static void RemoveMembers(int groupId, IEnumerable<int> userMembers, IEnumerable<int> groupMembers, IEnumerable<int> parentGroups = null)
        {
            SecurityContext.RemoveMembersFromSecurityGroup(groupId, userMembers, groupMembers, parentGroups);
        }
        /// <summary>
        /// Removes one or more group members from a group in one step.
        /// Non-existing group or member groups will be skipped.
        /// This method is a shortcut for RemoveMembers(...).
        /// </summary>
        /// <param name="groupId">Identifier of the container group. Cannot be 0.</param>
        /// <param name="groupIds">Collection of group member identifiers. Can be null or empty.</param>
        public static void RemoveGroupsFromGroup(int groupId, IEnumerable<int> groupIds)
        {
            SecurityContext.RemoveGroupsFromSecurityGroup(groupId, groupIds);
        }

        /// <summary>
        /// Deletes the specified group and its relations including related security entries from the security component.
        /// </summary>
        public static void DeleteGroup(int groupId)
        {
            SecurityContext.DeleteSecurityGroup(groupId);
        }
        /// <summary>
        /// Deletes the user from the security component by removing all memberships and security entries related to this user.
        /// </summary>
        public static void DeleteUser(int userId)
        {
            SecurityContext.DeleteUser(userId);
        }
        /// <summary>
        /// Deletes the specified group or user and its relations including related security entries from the security component.
        /// </summary>
        public static void DeleteIdentity(int id)
        {
            SecurityContext.DeleteIdentity(id);
        }
        /// <summary>
        /// Deletes the specified groups or users and their relations including related security entries from the security component.
        /// </summary>
        public static void DeleteIdentities(IEnumerable<int> ids)
        {
            SecurityContext.DeleteIdentities(ids);
        }

        /*========================================================================================================== Collecting related identities */

        /* user is the logged in user */

        /// <summary>
        /// Returns with related identites that contains the current user id and every group id that contains it directly or indirectly.
        /// Everyone group id is added if the current user is not the Visitor.
        /// Extended identities are added if there are any (see SnSecurityContext.GetDynamicGroups).
        /// </summary>
        public static List<int> GetIdentitiesByMembership()
        {
            return GetIdentitiesByMembershipPrivate();
        }
        /// <summary>
        /// Returns with related identites that contains the current user id and every group id that contains it directly or indirectly.
        /// Everyone group id is added if the current user is not the Visitor.
        /// Owners group id is added if the current user is owner of the passed content. Real owner is resolved from the security database.
        /// Extended identities are added if there are any (see SnSecurityContext.GetDynamicGroups).
        /// </summary>
        public static List<int> GetIdentitiesByMembership(int contentId)
        {
            return GetIdentitiesByMembershipPrivate(contentId);
        }
        /// <summary>
        /// Returns with related identites that contains the current user id and every group id that contains it directly or indirectly.
        /// Everyone group id is added if the current user is not the Visitor.
        /// Owners group id is added if the current user and owner of the passed content are equal.
        /// Extended identities are added if there are any (see SnSecurityContext.GetDynamicGroups).
        /// </summary>
        public static List<int> GetIdentitiesByMembership(NodeHead head)
        {
            return GetIdentitiesByMembershipPrivate(head.Id, head.OwnerId);
        }
        /// <summary>
        /// Returns with related identites that contains the current user id and every group id that contains it directly or indirectly.
        /// Everyone group id is added if the current user is not the Visitor.
        /// Owners group id is added if the current user and owner of the passed content are equal.
        /// Extended identities are added if there are any (see SnSecurityContext.GetDynamicGroups).
        /// </summary>
        public static List<int> GetIdentitiesByMembership(Node content)
        {
            return GetIdentitiesByMembershipPrivate(content.Id, content.OwnerId);
        }

        /* user can be different from the logged in user */

        /// <summary>
        /// Returns with related identites that contains the passed user id and every group id that contains it directly or indirectly.
        /// Everyone group id is added if the passed user is not the Visitor.
        /// Extended identities are added if there are any (see SnSecurityContext.GetDynamicGroups).
        /// </summary>
        public static List<int> GetIdentitiesByMembership(IUser user)
        {
            return GetIdentitiesByMembershipPrivate(user: user);
        }
        /// <summary>
        /// Returns with related identites that contains the passed user id and every group id that contains it directly or indirectly.
        /// Everyone group id is added if the passed user is not the Visitor.
        /// Owners group id is added if the passed user is owner of the passed content. Real owner is resolved from the security database.
        /// Extended identities are added if there are any (see SnSecurityContext.GetDynamicGroups).
        /// </summary>
        public static List<int> GetIdentitiesByMembership(IUser user, int contentId)
        {
            return GetIdentitiesByMembershipPrivate(contentId, user: user);
        }
        /// <summary>
        /// Returns with related identites that contains the passed user id and every group id that contains it directly or indirectly.
        /// Everyone group id is added if the passed user is not the Visitor.
        /// Owners group id is added if the passed user and owner of the passed content are equal.
        /// Extended identities are added if there are any (see SnSecurityContext.GetDynamicGroups).
        /// </summary>
        public static List<int> GetIdentitiesByMembership(IUser user, NodeHead head)
        {
            return GetIdentitiesByMembershipPrivate(head.Id, head.OwnerId, user);
        }
        /// <summary>
        /// Returns with related identites that contains the passed user id and every group id that contains it directly or indirectly.
        /// Everyone group id is added if the passed user is not the Visitor.
        /// Owners group id is added if the passed user and owner of the passed content are equal.
        /// Extended identities are added if there are any (see SnSecurityContext.GetDynamicGroups).
        /// </summary>
        public static List<int> GetIdentitiesByMembership(IUser user, Node content)
        {
            return GetIdentitiesByMembershipPrivate(content.Id, content.OwnerId, user);
        }

        // main function
        private static List<int> GetIdentitiesByMembershipPrivate(int contentId = 0, int ownerId = 0, IUser user = null)
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
        public static IEnumerable<int> GetFlattenedGroups(IUser differentUser = null)
        {
            return (differentUser == null ? SecurityContext : CreateSecurityContextFor(differentUser)).GetFlattenedGroups();
        }
        /// <summary>
        /// Gets the ids of all the groups that contain the current or provided user as a member, even through other groups,
        /// plus Everyone (except in case of a visitor) and the optional dynamic groups provided by the 
        /// membership extender.
        /// </summary>
        public static List<int> GetGroups(IUser differentUser = null)
        {
            return (differentUser == null ? SecurityContext : CreateSecurityContextFor(differentUser)).GetGroups();
        }
        /// <summary>
        /// Gets the ids of all the groups that contain the current or provided user as a member, even through other groups,
        /// plus Everyone (except in case of a visitor), plus Owners (if applicable) and the optional 
        /// dynamic groups provided by the membership extender.
        /// </summary>
        public static List<int> GetGroupsWithOwnership(int contentId, IUser differentUser = null)
        {
            return (differentUser == null ? SecurityContext : CreateSecurityContextFor(differentUser)).GetGroupsWithOwnership(contentId);
        }

        #endregion

        #region /*========================================================== Permission queries */

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
                var identityIds = SecurityContext.GetRelatedIdentities(contentId, level);
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
                var counters = SecurityContext.GetRelatedPermissions(contentId, level, explicitOnly, identityId, filter.IsEnabled);
                var result = new Dictionary<PermissionType, int>(PermissionType.PermissionCount);
                foreach (var item in counters)
                    result.Add(PermissionType.GetByIndex(item.Key.Index), item.Value);
                return result;
            }
            public static IDictionary<PermissionType, int> GetExplicitPermissionsInSubtree(int contentId, int[] identities, bool includeRoot)
            {
                var counters = SecurityContext.GetExplicitPermissionsInSubtree(contentId, identities, includeRoot);
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
                var contentIds = SecurityContext.GetRelatedEntities(contentId, level, explicitOnly, identityId, permissions);
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
                var identityIds = SecurityContext.GetRelatedIdentities(contentId, level, permissions);
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

                var entityIds = SecurityContext.GetRelatedEntitiesOneLevel(contentId, level, identityId, permissions);
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
                var contentIds = SecurityContext.GetAllowedUsers(contentId, permissions);
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
                var contentIds = SecurityContext.GetParentGroups(contentId, directOnly);
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

                    var nodeType = ActiveSchema.NodeTypes.GetItemById(head.NodeTypeId);
                    if (nodeType == null)
                        return false;

                    return _enabledTypes.Contains(nodeType.Name);
                }
            }
        }

        #endregion

        #region /*========================================================== Permission dependencies */

        private static int[][] _permissionDependencyTable;
        private static readonly object _dependencyTableLock = new object();
        /// <summary>
        /// Provides technical data for the user interface (for backward compatibility purposes). Do not use this method in your code.
        /// </summary>
        public static int[][] PermissionDependencyTable
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
        private static int[] GetPermissionDependencyArray(PermissionType permissionType)
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
        public static IEnumerable<PermissionTypeBase> GetAllowedPermissions(PermissionType permission)
        {
            var permissionList = new List<PermissionTypeBase>();
            GetAllowedPermissionsRecursive(permission, permissionList);
            return permissionList;
        }
        private static void GetAllowedPermissionsRecursive(PermissionTypeBase permission, List<PermissionTypeBase> permissionList)
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

        private static ISecurityContextFactory _securityContextFactory;

        /// <summary>
        /// Initializes the security system. Called during system startup.
        /// WARNING! Do not use this method in your code!
        /// </summary>
        public static void StartSecurity(bool isWebContext,
            ISecurityDataProvider securityDataProvider = null,
            IMessageProvider messageProvider = null)
        {
            var dummy = PermissionType.Open;

            securityDataProvider = securityDataProvider ?? new EF6SecurityDataProvider(
                Configuration.Security.SecurityDatabaseCommandTimeoutInSeconds,
                ConnectionStrings.SecurityDatabaseConnectionString);

            messageProvider = messageProvider ?? (IMessageProvider)Activator.CreateInstance(GetMessageProviderType());
            messageProvider.Initialize();

            var startingThesystem = DateTime.UtcNow;

            SnSecurityContext.StartTheSystem(new SecurityConfiguration
            {
                SecurityDataProvider = securityDataProvider,
                MessageProvider = messageProvider,
                SystemUserId = Identifiers.SystemUserId,
                VisitorUserId = Identifiers.VisitorUserId,
                EveryoneGroupId = Identifiers.EveryoneGroupId,
                OwnerGroupId = Identifiers.OwnersGroupId,
                SecuritActivityTimeoutInSeconds = Configuration.Security.SecuritActivityTimeoutInSeconds,
                SecuritActivityLifetimeInMinutes = Configuration.Security.SecuritActivityLifetimeInMinutes,
                CommunicationMonitorRunningPeriodInSeconds = Configuration.Security.SecurityMonitorRunningPeriodInSeconds
            });
            _securityContextFactory = isWebContext ? (ISecurityContextFactory)new DynamicSecurityContextFactory() : new StaticSecurityContextFactory();

            messageProvider.Start(startingThesystem);

            SnLog.WriteInformation("Security subsystem started", EventId.RepositoryLifecycle,
                properties: new Dictionary<string, object> { 
                    { "DataProvider", securityDataProvider.GetType().FullName },
                    { "MessageProvider", messageProvider.GetType().FullName }
                });
        }
        private static Type GetMessageProviderType()
        {
            var messageProviderTypeName = Providers.SecurityMessageProviderClassName;
            var t = TypeResolver.GetType(messageProviderTypeName);
            if (t == null)
                throw new InvalidOperationException("Unknown security message provider: " + messageProviderTypeName);

            return t;
        }

        internal static void DeleteEverythingAndRestart()
        {
            using (new SystemAccount())
                SecurityContext.DeleteAllAndRestart();
        }

        /// <summary>
        /// The security context related to the logged-in user. Always returns a new instance.
        /// </summary>
        public static SnSecurityContext SecurityContext
        {
            get { return _securityContextFactory.Create(AccessProvider.Current.GetCurrentUser()); }
        }

        /// <summary>
        /// Returns with a security context containing the provided user who can be different from the logged-in user.
        /// </summary>
        public static SnSecurityContext CreateSecurityContextFor(IUser user)
        {
            return _securityContextFactory.Create(user);
        }

        #endregion

        #region /*========================================================== Install, Import, Export */

        /// <summary>Contains methods for install scenarios.</summary>
        public static class SecurityInstaller
        {
            /// <summary>
            /// Clear security tables and copies ids of the full content tree structure from the repository to the security component.
            /// Security component must be available.
            /// WARNING! Use only in install scenarios.
            /// </summary>
            public static void InstallDefaultSecurityStructure()
            {
                using (new SystemAccount())
                {
                    DataProvider.Current.InstallDefaultSecurityStructure();

                    CreateAclEditor()
                        .Allow(Identifiers.PortalRootId, Identifiers.AdministratorsGroupId, false, PermissionType.BuiltInPermissionTypes)
                        .Apply();
                }
            }
        }

        /// <summary>
        /// Parses the permission section (that usually comes from a .Content file in the file system) 
        /// and imports all permission settings (including break and unbreak) into the security component.
        /// WARNING! Do not use this method in your code!
        /// </summary>
        public void ImportPermissions(XmlNode permissionsNode, string metadataPath)
        {
            Assert(PermissionType.SetPermissions);

            var permissionTypes = PermissionType.PermissionTypes;
            var aclEditor = CreateAclEditor();

            // parsing and executing 'Break' and 'Clear'
            var breakNode = permissionsNode.SelectSingleNode("Break");
            var clearNode = permissionsNode.SelectSingleNode("Clear");
            if (breakNode != null)
            {
                var convertToExplicit = clearNode == null;
                aclEditor.BreakInheritance(_node.Id, convertToExplicit);
            }
            else
            {
                aclEditor.UnbreakInheritance(_node.Id);
            }
            // executing 'Clear'
            if (clearNode != null)
                aclEditor.RemoveExplicitEntries(_node.Id);

            var identityElementIndex = 0;
            foreach (XmlElement identityElement in permissionsNode.SelectNodes("Identity"))
            {
                identityElementIndex++;

                // checking identity path
                var path = identityElement.GetAttribute("path");
                var propagationAttr = identityElement.GetAttribute("propagation");
                var localOnly = propagationAttr == null ? false : propagationAttr.ToLowerInvariant() == "localonly";
                if (String.IsNullOrEmpty(path))
                    throw ImportPermissionExceptionHelper(String.Concat("Missing or empty path attribute of the Identity element ", identityElementIndex, "."), metadataPath, null);
                var pathCheck = RepositoryPath.IsValidPath(path);
                if (pathCheck != RepositoryPath.PathResult.Correct)
                    throw ImportPermissionExceptionHelper(String.Concat("Invalid path of the Identity element ", identityElementIndex, ": ", path, " (", pathCheck, ")."), metadataPath, null);

                // getting identity node
                var identityNode = Node.LoadNode(path);
                if (identityNode == null)
                    throw ImportPermissionExceptionHelper(String.Concat("Identity ", identityElementIndex, " was not found: ", path, "."), metadataPath, null);

                // parsing value array
                foreach (XmlElement permissionElement in identityElement.SelectNodes("*"))
                {
                    var permName = permissionElement.LocalName;
                    var permType = permissionTypes.Where(p => String.Compare(p.Name, permName, true) == 0).FirstOrDefault();
                    if (permType == null)
                        throw ImportPermissionExceptionHelper(String.Concat("Permission type was not found in Identity ", identityElementIndex, "."), metadataPath, null);

                    switch (permissionElement.InnerText.ToLower())
                    {
                        case "allow":
                        case "allowed":
                            aclEditor.Allow(_node.Id, identityNode.Id, localOnly, permType);
                            break;
                        case "deny":
                        case "denied":
                            aclEditor.Deny(_node.Id, identityNode.Id, localOnly, permType);
                            break;
                        default:
                            throw ImportPermissionExceptionHelper(String.Concat("Invalid permission value in Identity ", identityElementIndex, ": ", permissionElement.InnerText, ". Allowed values: Allowed, Denied"), metadataPath, null);
                    }
                }
            }
            aclEditor.Apply();
        }
        private Exception ImportPermissionExceptionHelper(string message, string metadataPath, Exception innerException)
        {
            var msg = String.Concat("Importing permissions failed. Metadata: ", metadataPath, ". Reason: ", message);
            return new ApplicationException(msg, innerException);
        }
        /// <summary>
        /// Writes the permission section in a .Content file including all permission settings and a break if it is present.
        /// WARNING! Do not use this method in your code!
        /// </summary>
        public void ExportPermissions(XmlWriter writer)
        {
            if (!_node.IsInherited)
                writer.WriteElementString("Break", null);
            var entries = _node.Security.GetExplicitEntries();
            foreach (var entry in entries)
                entry.Export(writer);
        }

        #endregion

        #region /*========================================================== Tools */

        /// <summary>
        /// Converts a permission type array to the appropriate bitmask.
        /// If a permission type is present in the set, the appropriate bit will be set to 1. 
        /// Any other bits will remain 0.
        /// </summary>
        /// <param name="permissionTypes">Any number of permission types. Null or empty are also allowed.</param>
        /// <returns>The bitmask.</returns>
        public static ulong GetPermissionMask(IEnumerable<PermissionType> permissionTypes)
        {
            var mask = 0uL;
            if (permissionTypes == null)
                return mask;
            foreach (var permissionType in permissionTypes)
                mask |= 1uL << (permissionType.Index);
            return mask;
        }

        private static PermissionBitMask AggregateAces(IEnumerable<AceInfo> aces)
        {
            var result = new PermissionBitMask();
            foreach (var ace in aces)
            {
                result.AllowBits |= ace.AllowBits;
                result.DenyBits |= ace.DenyBits;
            }
            return result;
        }

        private static Exception GetAccessDeniedException(int nodeId, string path, string message, PermissionType[] permissionTypes, IUser user, bool isSubtree)
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
        public static void CopyPermissionsFrom(int sourceId, int targetId, CopyPermissionMode mode)
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
                aclEd.BreakInheritance(targetId, false);
            if (@clear)
                aclEd.RemoveExplicitEntries(targetId);
            aclEd.CopyEffectivePermissions(sourceId, targetId);
            aclEd.Apply();
        }

        /// <summary>
        /// Gets all security entities from the security component's in-memory cache for consistency check.
        /// WARNING! Do not use this method in your code!
        /// </summary>
        public static IDictionary<int, SecurityEntity> GetCachedEntities()
        {
            using (new SystemAccount())
                return SecurityContext.GetCachedEntitiesForConsistencyCheck();
        }

        #endregion

        #region Not supported anymore

        [Obsolete("Use HasPermission(IUser user, int nodeId, params PermissionType[] permissionTypes) instead.", true)]
        public static bool HasPermission(IUser user, string path, params PermissionType[] permissionTypes)
        {
            throw new NotSupportedException();
        }


        [Obsolete("Use AclEditor instead.", true)]
        public void SetPermission(IUser user, bool isInheritable, PermissionType permissionType, PermissionValue permissionValue)
        {
            throw new NotSupportedException();
        }
        [Obsolete("Use AclEditor instead.", true)]
        public void SetPermission(IGroup group, bool isInheritable, PermissionType permissionType, PermissionValue permissionValue)
        {
            throw new NotSupportedException();
        }
        [Obsolete("Use AclEditor instead.", true)]
        public void SetPermission(IOrganizationalUnit orgUnit, bool isInheritable, PermissionType permissionType, PermissionValue permissionValue)
        {
            throw new NotSupportedException();
        }
        [Obsolete("Use AclEditor instead.", true)]
        public void SetPermission(ISecurityMember securityMember, bool isInheritable, PermissionType permissionType, PermissionValue permissionValue)
        {
            throw new NotSupportedException();
        }
        [Obsolete("Use AclEditor instead.", true)]
        public void SetPermissions(int principalId, bool isInheritable, PermissionValue[] permissionValues)
        {
            throw new NotSupportedException();
        }

        // ======================================================================================================== Permission queries

        [Obsolete("Call SecurityHandler.PermissionQuery.GetRelatedIdentities method", true)]
        public static IEnumerable<Node> GetRelatedIdentities(string contentPath, PermissionLevel level, IdentityKind identityKind)
        {
            return PermissionQuery.GetRelatedIdentities(contentPath, level, identityKind);
        }
        [Obsolete("Call SecurityHandler.PermissionQuery.GetRelatedPermissions method", true)]
        public static IDictionary<PermissionType, int> GetRelatedPermissions(string contentPath, PermissionLevel level, bool explicitOnly, ISecurityMember member, IEnumerable<string> includedTypes)
        {
            return PermissionQuery.GetRelatedPermissions(contentPath, level, explicitOnly, member.Id, includedTypes);
        }
        [Obsolete("Call SecurityHandler.PermissionQuery.GetRelatedNodes method", true)]
        public static IEnumerable<Node> GetRelatedNodes(string contentPath, PermissionLevel level, bool explicitOnly, ISecurityMember member, IEnumerable<PermissionType> permissions)
        {
            return PermissionQuery.GetRelatedNodes(contentPath, level, explicitOnly, member.Id, permissions);
        }

        [Obsolete("Call SecurityHandler.PermissionQuery.GetRelatedIdentities method", true)]
        public static IEnumerable<Node> GetRelatedIdentities(string contentPath, PermissionLevel level, IdentityKind identityKind, IEnumerable<PermissionType> permissions)
        {
            return PermissionQuery.GetRelatedIdentities(contentPath, level, identityKind, permissions);
        }
        [Obsolete("Call SecurityHandler.PermissionQuery.GetRelatedNodesOneLevel method", true)]
        public static IEnumerable<Node> GetRelatedNodesOneLevel(string contentPath, PermissionLevel level, ISecurityMember member, IEnumerable<PermissionType> permissions)
        {
            return PermissionQuery.GetRelatedNodesOneLevel(contentPath, level, member.Id, permissions);
        }

        // ======================================================================================================== 

        [Obsolete("Use GetGroups(IUser).", true)]
        public List<int> GetPrincipals()
        {
            throw new NotSupportedException("Use GetGroups(IUser).");
        }
        [Obsolete("Use GetGroupsWithOwnership(int, IUser).", true)]
        public List<int> GetPrincipals(bool isOwner)
        {
            throw new NotSupportedException("Use GetGroups(IUser).");
        }

        // ======================================================================================================== 

        [Obsolete("Use GetEffectiveEntries methods.", true)]
        public PermissionValue[] GetAllPermissions()
        {
            throw new NotSupportedException();
        }
        [Obsolete("Use SecurityHandler.GetEffectiveEntries methods.", true)]
        public static PermissionValue[] GetAllPermissions(Node node)
        {
            throw new NotSupportedException();
        }
        [Obsolete("Use SecurityHandler.GetEffectiveEntries methods.", true)]
        public static PermissionValue[] GetAllPermissions(NodeHead nodeHead)
        {
            throw new NotSupportedException();
        }

        [Obsolete("Use SecurityHandler.GetEffectiveEntries methods.", true)]
        public PermissionValue[] GetAllPermissions(IUser user)
        {
            throw new NotSupportedException();
        }
        [Obsolete("Use SecurityHandler.GetEffectiveEntries methods.", true)]
        public PermissionValue[] GetAllPermissions(IUser user, Node node)
        {
            throw new NotSupportedException();
        }
        [Obsolete("Use SecurityHandler.GetEffectiveEntries methods.", true)]
        public PermissionValue[] GetAllPermissions(IUser user, NodeHead nodeHead)
        {
            throw new NotSupportedException();
        }

        [Obsolete("Use CopyPermissionsFrom(int sourceId, int targetId, CopyPermissionMode mode)", true)]
        public static void CopyPermissionsFrom(int sourceId, int targetId, CopyPermissionMode mode, bool reset) { }

        #endregion

    }
}
