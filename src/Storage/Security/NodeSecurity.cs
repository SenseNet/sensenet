using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using SenseNet.Diagnostics;
using SenseNet.Security;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Security
{
    /// <summary>
    /// Central class for handling security-related operations for the given context Node (managing permissions and group memberships). 
    /// </summary>
    public sealed class NodeSecurity
    {
        private readonly Node _node;
        private readonly SecurityHandler _securityHandler;

        internal NodeSecurity(Node node, SecurityHandler securityHandler)
        {
            _node = node ?? throw new ArgumentNullException(nameof(node));
            _securityHandler = securityHandler ?? throw new ArgumentNullException(nameof(securityHandler));
        }

        /*========================================================== Evaluation related methods */

        /// <summary>
        /// If one or more passed permissions are not allowed (undefined or denied) on the current content for the current user, SenseNetSecurityException will be thrown.
        /// </summary>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing" so SenseNetSecurityException will be thrown.</param>
        public void Assert(params PermissionType[] permissionTypes)
        {
            _securityHandler.Assert(_node, permissionTypes);
        }
        /// <summary>
        /// If one or more passed permissions are not allowed (undefined or denied) on the current content for the current user, SenseNetSecurityException will be thrown with the specified message.
        /// </summary>
        /// <param name="message">Text that appears in the SenseNetSecurityException's message.</param>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing" so SenseNetSecurityException will be thrown.</param>
        public void Assert(string message, params PermissionType[] permissionTypes)
        {
            _securityHandler.Assert(_node, message, permissionTypes);
        }

        /// <summary>
        /// If one or more passed permissions are not allowed (undefined or denied) on every content in the whole subtree of the current content for the current user, SenseNetSecurityException will be thrown.
        /// </summary>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing" so SenseNetSecurityException will be thrown.</param>
        public void AssertSubtree(params PermissionType[] permissionTypes)
        {
            _securityHandler.AssertSubtree(_node, permissionTypes);
        }
        /// <summary>
        /// If one or more passed permissions are not allowed (undefined or denied) on every content in the whole subtree of the current content for the current user, SenseNetSecurityException will be thrown.
        /// </summary>
        /// <param name="message">Text that appears in the SenseNetSecurityException's message.</param>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing" so SenseNetSecurityException will be thrown.</param>
        public void AssertSubtree(string message, params PermissionType[] permissionTypes)
        {
            _securityHandler.AssertSubtree(_node, message, permissionTypes);
        }

        /// <summary>
        /// Returns true if all passed permissions are allowed on the current content for the current user.
        /// </summary>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing".</param>
        public bool HasPermission(params PermissionType[] permissionTypes)
        {
            return _securityHandler.HasPermission(_node, permissionTypes);
        }
        /// <summary>
        /// Returns true if all passed permissions are allowed on the passed content for the passed user.
        /// </summary>
        /// <param name="user">The user. Cannot be null.</param>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing".</param>
        public bool HasPermission(IUser user, params PermissionType[] permissionTypes)
        {
            return _securityHandler.HasPermission(user, _node, permissionTypes);
        }

        /// <summary>
        /// Returns true if all passed permissions are allowed for the current user on every content in the whole subtree of the current content.
        /// </summary>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing".</param>
        public bool HasSubTreePermission(params PermissionType[] permissionTypes)
        {
            return _securityHandler.HasSubTreePermission(_node, permissionTypes);
        }
        /// <summary>
        /// Returns true if all passed permissions are allowed for the passed user on every content in the whole subtree of the current content.
        /// </summary>
        /// <param name="user">The user. Cannot be null.</param>
        /// <param name="permissionTypes">Set of related permissions. Cannot be null. Empty set means "allowed nothing".</param>
        public bool HasSubTreePermission(IUser user, params PermissionType[] permissionTypes)
        {
            return _securityHandler.HasSubTreePermission(user, _node, permissionTypes);
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
            return _securityHandler.GetPermission(_node, permissionTypes);
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
                throw new ArgumentNullException(nameof(node));
            return _securityHandler.GetPermission(user, node.Id, permissionTypes);
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
                throw new ArgumentNullException(nameof(nodeHead));
            return _securityHandler.GetPermission(user, nodeHead.Id, permissionTypes);
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
            return _securityHandler.GetSubtreePermission(_node, permissionTypes);
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
                throw new ArgumentNullException(nameof(node));
            return _securityHandler.GetSubtreePermission(user, node.Id, permissionTypes);
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
                throw new ArgumentNullException(nameof(nodeHead));
            return _securityHandler.GetSubtreePermission(user, nodeHead.Id, permissionTypes);
        }

        /// <summary>
        /// Returns the current content's explicit entries. Current user must have SeePermissions permission.
        /// </summary>
        /// <param name="entryType">Security entry type. Default: all entries.</param>
        public List<AceInfo> GetExplicitEntries(EntryType? entryType = null)
        {
            return _securityHandler.GetExplicitEntries(_node.Id, null, entryType);
        }

        /// <summary>
        /// Returns the current content's effective entries. Current user must have SeePermissions permission.
        /// </summary>
        /// <param name="entryType">Security entry type. Default: all entries.</param>
        public List<AceInfo> GetEffectiveEntries(EntryType? entryType = null)
        {
            return _securityHandler.GetEffectiveEntries(_node.Id, null, entryType);
        }

        /*========================================================== ACL */

        /// <summary>
        /// Returns the AccessControlList of the current content.
        /// The result contains only Normal entries.
        /// </summary>
        public AccessControlList GetAcl()
        {
            return _securityHandler.GetAcl(_node.Id);
        }

        /// <summary>
        /// Removes all explicit entries from the current content.
        /// If AclEditor passed, the modification is executed in it
        /// else executed immediately.
        /// </summary>
        public void RemoveExplicitEntries(SnAclEditor aclEditor = null)
        {
            if (aclEditor == null)
            {
                _securityHandler.CreateAclEditor()
                    .RemoveExplicitEntries(_node.Id)
                    .ApplyAsync(CancellationToken.None).GetAwaiter().GetResult();
                return;
            }
            if (aclEditor.EntryType != EntryType.Normal)
                throw new InvalidOperationException(
                    "EntryType mismatch int the passed AclEditor. Only the EntryType.Normal category is allowed in this context.");
            aclEditor.RemoveExplicitEntries(_node.Id);
        }

        /*========================================================== Entity inheritance */

        /// <summary>
        /// Returns false if the current content inherits the permissions from it's parent.
        /// </summary>
        public bool IsInherited => _securityHandler.SecurityContext.IsEntityInherited(_node.Id);

        /// <summary>
        /// Clear the permission inheritance on the current content.
        /// </summary>
        /// <param name="convertToExplicit">If true (default), all effective permissions will be copied explicitly.</param>
        [Obsolete("Use async version instead.", true)]//UNDONE:xxx0:AsyncSecu: change to true
        public void BreakInheritance(bool convertToExplicit = true)
        {
            _securityHandler.BreakInheritance(this._node, convertToExplicit);
        }
        /// <summary>
        /// Clear the permission inheritance on the current content.
        /// </summary>
        /// <param name="cancel">The token to monitor for cancellation requests.</param>
        /// <param name="convertToExplicit">If true (default), all effective permissions will be copied explicitly.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public Task BreakInheritanceAsync(CancellationToken cancel, bool convertToExplicit = true)
        {
            return _securityHandler.BreakInheritanceAsync(this._node, cancel, convertToExplicit);
        }

        /// <summary>
        /// Restores the permission inheritance on the current content.
        /// </summary>
        /// <param name="normalize">If true (default is false), the unnecessary explicit entries 
        /// (the ones that are the same as the inherited ones) will be removed.</param>
        [Obsolete("Use async version instead.", true)]//UNDONE:xxx0:AsyncSecu: change to true
        public void RemoveBreakInheritance(bool normalize = false)
        {
            _securityHandler.UnbreakInheritance(this._node, normalize);
        }
        /// <summary>
        /// Restores the permission inheritance on the current content.
        /// </summary>
        /// <param name="cancel">The token to monitor for cancellation requests.</param>
        /// <param name="normalize">If true (default is false), the unnecessary explicit entries 
        /// (the ones that are the same as the inherited ones) will be removed.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public Task RemoveBreakInheritanceAsync(CancellationToken cancel, bool normalize = false)
        {
            return _securityHandler.UnbreakInheritanceAsync(this._node, cancel, normalize);
        }

        /*========================================================== Install, Import, Export */

        /// <summary>
        /// Parses the permission section (that usually comes from a .Content file in the file system) 
        /// and imports all permission settings (including break and unbreak) into the security component.
        /// WARNING! Do not use this method in your code!
        /// </summary>
        public void ImportPermissions(XmlNode permissionsNode, string metadataPath)
        {
            Assert(PermissionType.SetPermissions);

            var permissionTypes = PermissionType.PermissionTypes;
            var aclEditor = _securityHandler.CreateAclEditor();

            // parsing and executing 'Break' and 'Clear'
            var breakNode = permissionsNode.SelectSingleNode("Break");
            var clearNode = permissionsNode.SelectSingleNode("Clear");
            if (breakNode != null)
            {
                var convertToExplicit = clearNode == null;
                aclEditor.BreakInheritance(_node.Id, convertToExplicit ? new[] { EntryType.Normal } : new EntryType[0]);
            }
            else
            {
                aclEditor.UnbreakInheritance(_node.Id, new[] { EntryType.Normal });
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
                {
                    SnLog.WriteWarning($"Identity {identityElementIndex} was not found: {path}.");
                    continue;
                }

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
            aclEditor.ApplyAsync(CancellationToken.None).GetAwaiter().GetResult();
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
            var entries = _node.Security.GetExplicitEntries(EntryType.Normal);
            foreach (var entry in entries)
                entry.Export(writer);
        }

    }
}
