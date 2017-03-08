using SenseNet.ContentRepository.Storage.Events;
using SenseNet.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.ContentRepository.Storage.Security
{
    /// <summary>
    /// Provides methods for modifying permission settings and inheritance of one or more entities.
    /// Coding modifications can be very easy with using the fluent API.
    /// Execution is atomic and affect in the security database and every memory cache in the whole distributed system.
    /// This class is reimplementation of the SenseNet.Security.AclEditor. Base functionality is not changed but extended.
    /// </summary>
    public class SnAclEditor : SenseNet.Security.AclEditor
    {
        /// <summary>
        /// Gets the current SnSecurityContext
        /// </summary>
        public new SnSecurityContext Context { get { return (SnSecurityContext)base.Context; } }

        internal SnAclEditor(SnSecurityContext context) : base(context ?? SecurityHandler.SecurityContext) { }

        private static new SenseNet.Security.AclEditor Create(SenseNet.Security.SecurityContext context)
        {
            throw new NotSupportedException();
        }
        /// <summary>
        /// Returns with a new instance of the SnAclEditor with a SecurityContext as the current context.
        /// </summary>
        public static SnAclEditor Create(SnSecurityContext context)
        {
            return new SnAclEditor(context);
        }

        /*=========================================================================================== method for backward compatibility */

        /// <summary>
        /// Allowes, denies or clears a permission on the requested entity for the requested identity.
        /// This is a backward compatible legacy method.
        /// </summary>
        /// <param name="entityId">The requested entity.</param>
        /// <param name="identityId">The requested identity.</param>
        /// <param name="localOnly">Determines whether the edited entry is inheritable or not.</param>
        /// <param name="permission">Permission that will be modified.</param>
        /// <param name="value">Value that will be set. It can be Undefined, Allowed or Denied.</param>
        /// <returns>A reference to this instance for calling more operations.</returns>
        public SnAclEditor SetPermission(int entityId, int identityId, bool localOnly, SenseNet.Security.PermissionTypeBase permission, SenseNet.Security.PermissionValue value)
        {
            switch (value)
            {
                case SenseNet.Security.PermissionValue.Allowed: Allow(entityId, identityId, localOnly, permission); break;
                case SenseNet.Security.PermissionValue.Denied: Deny(entityId, identityId, localOnly, permission); break;
                case SenseNet.Security.PermissionValue.Undefined: ClearPermission(entityId, identityId, localOnly, permission); break;
                default: throw new SnNotSupportedException("Unknown PermissionValue: " + value);
            }
            return this;
        }

        /*=========================================================================================== overridden methods for forced settings and correct fluent api */

        /// <summary>
        /// Allows one or more permissions on the requested entity for the requested identity.
        /// Empty or null permission set is ineffective so this method cannot be used
        /// for reset the explicitly allowed permissions.
        /// </summary>
        /// <param name="entityId">The requested entity.</param>
        /// <param name="identityId">The requested identity.</param>
        /// <param name="localOnly">Determines whether the edited entry is inheritable or not.</param>
        /// <param name="permissions">One or more permissions.</param>
        /// <returns>A reference to this instance for calling more operations.</returns>
        public new SnAclEditor Allow(int entityId, int identityId, bool localOnly, params SenseNet.Security.PermissionTypeBase[] permissions)
        {
            base.Allow(entityId, identityId, localOnly, permissions);
            return this;
        }
        /// <summary>
        /// Denies one or more permissions on the requested entity for the requested identity.
        /// Empty or null permission set is ineffective so this method cannot be used
        /// for reset the explicitly denied permissions.
        /// </summary>
        /// <param name="entityId">The requested entity.</param>
        /// <param name="identityId">The requested identity.</param>
        /// <param name="localOnly">Determines whether the edited entry is inheritable or not.</param>
        /// <param name="permissions">One or more permissions.</param>
        /// <returns>A reference to this instance for calling more operations.</returns>
        public new SnAclEditor Deny(int entityId, int identityId, bool localOnly, params SenseNet.Security.PermissionTypeBase[] permissions)
        {
            base.Deny(entityId, identityId, localOnly, permissions);
            return this;
        }
        /// <summary>
        /// Clears one or more permissions on the requested entity for the requested identity.
        /// Cleared permission is "Undefined" that means and not "Allowed" and not "Denied".
        /// Empty or null permission set is ineffective.
        /// Entry will be deleted if it will be empty after clearing.
        /// </summary>
        /// <param name="entityId">The requested entity.</param>
        /// <param name="identityId">The requested identity.</param>
        /// <param name="localOnly">Determines whether the edited entry is inheritable or not.</param>
        /// <param name="permissions">One or more permissions.</param>
        /// <returns>A reference to this instance for calling more operations.</returns>
        public new SnAclEditor ClearPermission(int entityId, int identityId, bool localOnly, params SenseNet.Security.PermissionTypeBase[] permissions)
        {
            base.ClearPermission(entityId, identityId, localOnly, permissions);
            return this;
        }
        /// <summary>
        /// Sets the allowed and denied permissions by the passed bitmasks.
        /// This method can not reset any allowed or denied.
        /// </summary>
        /// <param name="entityId">The requested entity.</param>
        /// <param name="identityId">The requested identity.</param>
        /// <param name="localOnly">Determines whether the edited entry is inheritable or not.</param>
        /// <param name="allowBits">Aggregated bitmask. Every bit means a permission. The bit number is derived from the Index of the PermissionTypeBase.
        /// In the bitmask the 1 means the permission that must be allowed.</param>
        /// <param name="allowBits">Aggregated bitmask. Every bit means a permission. The bit number is derived from the Index of the PermissionTypeBase.
        /// In the bitmask the 1 means the permission that must be allowed. Deny bits override the allow bits</param>
        /// <returns>A reference to this instance for calling more operations.</returns>
        public SnAclEditor Set(int entityId, int identityId, bool localOnly, ulong allowBits, ulong denyBits)
        {
            return Set(entityId, identityId, localOnly, new SenseNet.Security.PermissionBitMask { AllowBits = allowBits, DenyBits = denyBits });
        }
        /// <summary>
        /// Sets the allowed and denied permissions by the passed bitmask.
        /// This method can not reset any allowed or denied.
        /// </summary>
        /// <param name="entityId">The requested entity.</param>
        /// <param name="identityId">The requested identity.</param>
        /// <param name="localOnly">Determines whether the edited entry is inheritable or not.</param>
        /// <param name="permissionMask">Contains one or more permissions to allow or deny.</param>
        /// <returns>A reference to this instance for calling more operations.</returns>
        public new SnAclEditor Set(int entityId, int identityId, bool localOnly, SenseNet.Security.PermissionBitMask permissionMask)
        {
            SetBits(permissionMask);

            base.Set(entityId, identityId, localOnly, permissionMask);
            return this;
        }
        /// <summary>
        /// Resets the allowed and denied permissions by the passed bitmask.
        /// </summary>
        /// <param name="entityId">The requested entity.</param>
        /// <param name="identityId">The requested identity.</param>
        /// <param name="localOnly">Determines whether the edited entry is inheritable or not.</param>
        /// <param name="allowBits">Aggregated bitmask. Every bit means a permission. The bit number is derived from the Index of the PermissionTypeBase.
        /// In the bitmask the 1 means the permission that must be cleared in the allowed set.</param>
        /// <param name="allowBits">Aggregated bitmask. Every bit means a permission. The bit number is derived from the Index of the PermissionTypeBase.
        /// In the bitmask the 1 means the permission that must be cleared in the denied set.</param>
        /// <returns>A reference to this instance for calling more operations.</returns>
        public SnAclEditor Reset(int entityId, int identityId, bool localOnly, ulong allowBits, ulong denyBits)
        {
            return Reset(entityId, identityId, localOnly, new SenseNet.Security.PermissionBitMask { AllowBits = allowBits, DenyBits = denyBits });
        }
        /// <summary>
        /// Resets the allowed and denied permissions by the passed bitmask.
        /// </summary>
        /// <param name="entityId">The requested entity.</param>
        /// <param name="identityId">The requested identity.</param>
        /// <param name="localOnly">Determines whether the edited entry is inheritable or not.</param>
        /// <param name="permissionMask">Contains one or more permissions to allow or deny.</param>
        /// <returns>A reference to this instance for calling more operations.</returns>
        public new SnAclEditor Reset(int entityId, int identityId, bool localOnly, SenseNet.Security.PermissionBitMask permissionMask)
        {
            var permissionsToReset = new List<SenseNet.Security.PermissionTypeBase>();
            var bits = permissionMask.AllowBits | permissionMask.DenyBits;
            for (var i = 0; i < PermissionType.PermissionCount; i++)
                if ((bits & (1uL << i)) != 0)
                    permissionsToReset.Add(PermissionType.GetByIndex(i));
            ClearPermission(entityId, identityId, localOnly, permissionsToReset.ToArray());
            return this;
        }
        /// <summary>
        /// Cancels the permission inheritance on the requested entity.
        /// </summary>
        /// <param name="entityId">The requested entity.</param>
        /// <param name="convertToExplicit">If true (default), all effective permissions will be copied explicitly.</param>
        /// <returns>A reference to this instance for calling more operations.</returns>
        public new SnAclEditor BreakInheritance(int entityId, bool convertToExplicit = true)
        {
            base.BreakInheritance(entityId, convertToExplicit);
            return this;
        }
        /// <summary>
        /// Restores the permission inheritance on the requested entity.
        /// </summary>
        /// <param name="entityId">The requested entity.</param>
        /// <param name="normalize">If true (default is false), the unnecessary explicit entries will be removed.</param>
        /// <returns>A reference to this instance for calling more operations.</returns>
        public new SnAclEditor UnbreakInheritance(int entityId, bool normalize = false)
        {
            base.UnbreakInheritance(entityId, normalize);
            return this;
        }

        /// <summary>
        /// Executes all modifications.
        /// Current user must have SetPermissions permission on any modified entity.
        /// An Auditlog record with changed data will be writen.
        /// OnPermissionChanging and OnPermissionChanged events are fired on any active NodeObserver.
        /// </summary>
        public override void Apply()
        {
            Apply(null);
        }

        /// <summary>
        /// Executes all modifications.
        /// Current user must have SetPermissions permission on any modified entity.
        /// An Auditlog record with changed data will be writen.
        /// OnPermissionChanging and OnPermissionChanged events are fired on any active NodeObserver that is not in the exclusion list (see "disabledObservers" parameter).
        /// </summary>
        /// <param name="disabledObservers">NodeObserver exclusion list.</param>
        public void Apply(List<Type> disabledObservers)
        {
            foreach (var entityId in this._acls.Keys)
                this.Context.AssertPermission(entityId, PermissionType.SetPermissions);
            using (var audit = new AuditBlock(AuditEvent.PermissionChanged, "Trying to execute permission modifications",
                new Dictionary<string, object>
                {
                    { "Entities", this._acls.Count },
                    { "Breaks", this._breaks.Count },
                    { "Unbreaks", this._unbreaks.Count }
                }))
            {
                using (var op = SnTrace.Security.StartOperation("AclEditor.Apply (acl count: {0})", _acls.Count))
                {
                    string msg = null;
                    if ((msg = Validate(this._acls)) != null)
                    {
                        // Log the error, but allow the operation to continue, because acl editor
                        // may contain many different operations that we do not want to lose.
                        SnLog.WriteWarning("Invalid ACL: " + msg, EventId.Security);
                    }

                    var relatedEntities = new List<int>();
                    // collect related aces
                    var originalAces = new List<string>();
                    // changed acls
                    foreach (var entityId in this._acls.Keys)
                    {
                        relatedEntities.Add(entityId);
                        originalAces.Add(AcesToString(entityId, this.Context.GetExplicitEntries(entityId)));
                    }
                    // breaks that are not in changed aces
                    foreach (var entityId in this._breaks)
                    {
                        if (!this._acls.ContainsKey(entityId))
                        {
                            relatedEntities.Add(entityId);
                            originalAces.Add(AcesToString(entityId, this.Context.GetExplicitEntries(entityId)));
                        }
                    }
                    // unbreaks that are not in changed aces
                    foreach (var entityId in this._unbreaks)
                    {
                        if (!this._acls.ContainsKey(entityId))
                        {
                            relatedEntities.Add(entityId);
                            originalAces.Add(AcesToString(entityId, this.Context.GetExplicitEntries(entityId)));
                        }
                    }

                    var relatedNodeHeads = relatedEntities.Select(NodeHead.Get).ToArray();
                    var changedData = new[] { new ChangedData { Name = "SetPermissions", Original = originalAces } };

                    // fire "before" event
                    var args1 = new CancellablePermissionChangingEventArgs(relatedNodeHeads, changedData);
                    using (var op1 = SnTrace.Security.StartOperation("AclEditor.Apply / FireOnPermissionChanging"))
                    {
                        NodeObserver.FireOnPermissionChanging(null, null, args1, disabledObservers);
                        if (args1.Cancel)
                            throw new CancelNodeEventException(args1.CancelMessage, args1.EventType, null);
                        op1.Successful = true;
                    }

                    var customData = args1.CustomData;

                    // main operation
                    base.Apply();

                    // collect new values
                    changedData[0].Value = relatedEntities.Select(x => AcesToString(x, this.Context.GetExplicitEntries(x))).ToList();

                    // fire "after" event
                    var args2 = new PermissionChangedEventArgs(relatedNodeHeads, customData, changedData);
                    using (var op2 = SnTrace.Security.StartOperation("AclEditor.Apply / FireOnPermissionChanged"))
                    {
                        NodeObserver.FireOnPermissionChanged(null, null, args2, disabledObservers);
                        op2.Successful = true;
                    }

                    // iterate through all edited entities and log changes one by one
                    for (var i = 0; i < relatedEntities.Count; i++)
                    {
                        var entity = relatedNodeHeads[i];
                        SnLog.WriteAudit(AuditEvent.PermissionChanged, new Dictionary<string, object>
                        {
                            { "Id", entity != null ? entity.Id : 0 },
                            { "Path", entity != null ? entity.Path : string.Empty},
                            { "Type",  changedData[0].Name },
                            { "OldAcl",  (changedData[0].Original as List<string>)[i] }, // changed data lists are in the same order as relatedentities
                            { "NewAcl", (changedData[0].Value as List<string>)[i] }
                        });
                    }
                    op.Successful = true;
                }
                audit.Successful = true;
            }
        }

        private string AcesToString(int entityId, List<SenseNet.Security.AceInfo> aces)
        {
            var broken = !this.Context.IsEntityInherited(entityId);
            return (broken ? "-" : "+") + "(" + entityId + ")|" + String.Join(",", aces);
        }
        private string Validate(Dictionary<int, SenseNet.Security.AclInfo> acls)
        {
            foreach (var item in acls)
            {
                var acl = item.Value;
                string msg = null;
                foreach (var ace in acl.Entries)
                    if ((msg = ValidateIdentity(ace.IdentityId)) != null)
                        return "ace identity is " + msg;
            }
            return null;
        }
        private string ValidateIdentity(int id)
        {
            using (new SystemAccount())
            {
                var node = Node.LoadNode(id);
                var ident = node as SenseNet.Security.ISecurityIdentity;
                if (ident != null)
                    return null;
                if(node == null)
                    return "'Node not found. Id: " + id + "'";
                return node.NodeType.Name + "(" + node.GetType().Name + ")";
            }
        }

        /*=========================================================================================== Additional API */

        /// <summary>
        /// Copies all effective entries of the source entity to the target entity as explicite entries.
        /// </summary>
        /// <param name="sourceEntityId">Id of the source entity.</param>
        /// <param name="targetEntityId">Id of the target entity.</param>
        /// <returns>A reference to this instance for calling more operations.</returns>
        public SnAclEditor CopyEffectivePermissions(int sourceEntityId, int targetEntityId)
        {
            var aces = this.Context.GetEffectiveEntries(sourceEntityId);
            foreach (var ace in aces)
                Set(targetEntityId, ace.IdentityId, ace.LocalOnly, ace.AllowBits, ace.DenyBits);

            return this;
        }
        /// <summary>
        /// Resets all explicite permission settings on the requested entity.
        /// Permission inheritance is not changed.
        /// </summary>
        /// <param name="entityId">Id of the requested entity.</param>
        /// <returns>A reference to this instance for calling more operations.</returns>
        public SnAclEditor RemoveExplicitEntries(int entityId)
        {
            var aces = this.Context.GetExplicitEntries(entityId);
            foreach (var ace in aces)
                Reset(entityId, ace.IdentityId, ace.LocalOnly, ace.AllowBits, ace.DenyBits);
            return this;
        }

        /*=========================================================================================== Tools */

        internal static void SetBits(ref ulong allowBits, ref ulong denyBits, SenseNet.Security.PermissionTypeBase permissionType, SenseNet.Security.PermissionValue permissionValue)
        {
            var permCount = PermissionType.PermissionCount;
            var y = permissionType.Index;
            var thisbit = 1uL << y;
            var allowedBefore = (allowBits & thisbit) != 0uL;
            var deniedBefore = (denyBits & thisbit) != 0uL;

            var dependencyTable = SecurityHandler.PermissionDependencyTable;
            switch (permissionValue)
            {
                case SenseNet.Security.PermissionValue.Allowed:
                    for (var x = 0; x < permCount; x++)
                        if (dependencyTable[y][x] == 1)
                        {
                            allowBits |= 1uL << x;
                            denyBits &= ~(1uL << x);
                        }
                    break;
                case SenseNet.Security.PermissionValue.Denied:
                    for (var x = 0; x < permCount; x++)
                        if (dependencyTable[x][y] == 1)
                        {
                            allowBits &= ~(1uL << x);
                            denyBits |= 1uL << x;
                        }
                    break;
                case SenseNet.Security.PermissionValue.Undefined:
                    if (allowedBefore)
                    {
                        for (var x = 0; x < permCount; x++)
                            if (dependencyTable[x][y] == 1)
                                allowBits &= ~(1uL << x);
                    }
                    else if (deniedBefore)
                    {
                        for (var x = 0; x < permCount; x++)
                            if (dependencyTable[y][x] == 1)
                                denyBits &= ~(1uL << x);
                    }
                    break;
                default:
                    throw new NotSupportedException("Unknown PermissionValue: " + permissionValue);
            }
        }
        internal static void SetBits(SenseNet.Security.PermissionBitMask permissionMask)
        {
            ulong allowBits = permissionMask.AllowBits;
            ulong denyBits = permissionMask.DenyBits;

            var perms = PermissionType.PermissionTypes.ToArray();
            var values = new SenseNet.Security.PermissionValue[perms.Length];
            foreach (var perm in perms)
                values[perm.Index] = GetValue(allowBits, denyBits, perm);
            foreach (var perm in perms)
                if (values[perm.Index] == SenseNet.Security.PermissionValue.Allowed)
                    SetBits(ref allowBits, ref denyBits, perm, SenseNet.Security.PermissionValue.Allowed);
            foreach (var perm in perms)
                if (values[perm.Index] == SenseNet.Security.PermissionValue.Denied)
                    SetBits(ref allowBits, ref denyBits, perm, SenseNet.Security.PermissionValue.Denied);

            permissionMask.AllowBits = allowBits;
            permissionMask.DenyBits = denyBits;
        }
        private static SenseNet.Security.PermissionValue GetValue(ulong allowBits, ulong denyBits, SenseNet.Security.PermissionTypeBase perm)
        {
            var mask = 1uL << (perm.Index);
            if ((denyBits & mask) != 0)
                return SenseNet.Security.PermissionValue.Denied;
            if ((allowBits & mask) == mask)
                return SenseNet.Security.PermissionValue.Allowed;
            return SenseNet.Security.PermissionValue.Undefined;
        }

        internal SenseNet.Security.AceInfo GetEntry(int entityId, int identityId, bool localOnly)
        {
            SenseNet.Security.AclInfo acl;
            if(!_acls.TryGetValue(entityId, out acl))
                return null;
            var ace = acl.Entries.Where(e => e.IdentityId == identityId && e.LocalOnly == localOnly).FirstOrDefault();
            return ace;
        }
    }
}
