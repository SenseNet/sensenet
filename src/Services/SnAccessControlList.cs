using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using SenseNet.Configuration;
using SenseNet.Security;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;

namespace SenseNet.Services
{
    [Serializable]
    public class SnAccessControlList
    {
        public int NodeId { get; set; }
        public string Path { get; set; }
        public bool Inherits { get; set; }
        public SnIdentity Creator { get; set; }
        public SnIdentity LastModifier { get; set; }

        public IEnumerable<SnAccessControlEntry> Entries { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendFormat("  NodeId: {0},", NodeId).AppendLine();
            sb.AppendFormat("  Path: \"{0}\",", Path).AppendLine();
            sb.AppendFormat("  Inherits: {0},", Inherits).AppendLine();
            sb.AppendFormat("  Creator: {0},", Creator == null ? 0 : Creator.NodeId).AppendLine();
            sb.AppendFormat("  LastModifier: {0},", LastModifier == null ? 0 : LastModifier.NodeId).AppendLine();
            sb.AppendLine("  Entries: [");
            foreach (var entry in Entries)
            {
                sb.AppendLine("    {");
                sb.AppendLine("      Identity: {");
                sb.AppendFormat("        NodeId: {0},", entry.Identity.NodeId).AppendLine();
                sb.AppendFormat("        Path: \"{0}\",", entry.Identity.Path).AppendLine();
                sb.AppendFormat("        Name: \"{0}\",", entry.Identity.Name).AppendLine();
                sb.AppendFormat("        Kind: \"{0}\",", entry.Identity.Kind).AppendLine();
                sb.AppendLine("      },");
                sb.AppendFormat("      Propagates: {0},", entry.Propagates).AppendLine();
                sb.AppendFormat("      Permissions: [", entry.PermissionsToString()).AppendLine();
                foreach (var perm in entry.Permissions)
                {
                    var value = perm.Allow ? "Allow" : (perm.Deny ? "Deny" : "");
                    var from = perm.Allow ? perm.AllowFrom : (perm.Deny ? perm.DenyFrom : "");
                    sb.AppendLine("        {");
                    sb.AppendFormat("          Value: {0},", value).AppendLine();
                    sb.AppendFormat("          From: {0}", from).AppendLine();
                    sb.AppendLine("        }");
                }
                sb.AppendLine("      ]");
                sb.AppendLine("    }");
            }
            sb.AppendLine("  ]");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private static SnAccessControlList CreateFromAccessControlList(SenseNet.Security.AccessControlList acl)
        {
            var node = Node.LoadNode(acl.EntityId);
            var creator = node.CreatedBy;
            var lasMod = node.ModifiedBy;
            var result = new SnAccessControlList
            {
                NodeId = node.Id,
                Creator = SnIdentity.Create(node.CreatedById),
                LastModifier = SnIdentity.Create(node.ModifiedById),
                Inherits = acl.Inherits,
                Path = node.Path,
                Entries = acl.Entries.Select(e => new SnAccessControlEntry
                {
                    Identity = SnIdentity.Create(e.IdentityId),
                    Propagates = !e.LocalOnly,
                    Permissions = e.Permissions.Select(p => new SnPermission
                    {
                        Name = p.Name,
                        Allow = p.Allow,
                        AllowFrom = p.AllowFrom == 0 ? null : NodeHead.Get(p.AllowFrom).Path,
                        Deny = p.Deny,
                        DenyFrom = p.DenyFrom == 0 ? null : NodeHead.Get(p.DenyFrom).Path
                    }).ToArray()
                }).ToArray()
            };

            return result;
        }
        private AccessControlList ConvertToAccessControlList()
        {
            return new AccessControlList
            {
                EntityId = this.NodeId,
                Inherits = this.Inherits,
                Entries = this.Entries.Select(e => new AccessControlEntry
                {
                    IdentityId = e.Identity.NodeId,
                    LocalOnly = !e.Propagates,
                    Permissions = e.Permissions.Select(p => new Permission
                    {
                        Allow = p.Allow,
                        Deny = p.Deny,
                        Name = p.Name,
                        AllowFrom = GetFromId(p.AllowFrom),
                        DenyFrom = GetFromId(p.DenyFrom),
                    }).ToArray()
                }).ToArray()
            };
        }
        private int GetFromId(string path)
        {
            if (string.IsNullOrEmpty(path))
                return 0;
            var head = NodeHead.Get(path);
            if (head == null)
                return 0;
            return head.Id;
        }

        // ====================================================== STATIC INTERFACE

        public static SnAccessControlList GetAcl(int nodeId)
        {
            var ctx = SecurityHandler.SecurityContext;
            ctx.AssertPermission(nodeId, PermissionType.SeePermissions);
            var acl = ctx.GetAcl(nodeId);
            return SnAccessControlList.CreateFromAccessControlList(acl);
        }

        public static void SetAcl(Node node, SnAccessControlList acl)
        {
            node.Security.Assert(PermissionType.SetPermissions);

            // no need to fire permission changed events here, because Acl apply does that below
            ApplyAclModifications(SecurityHandler.CreateAclEditor(), node.Security.GetAcl(), acl.ConvertToAccessControlList());
        }
        internal static void ApplyAclModifications(SnAclEditor ed, AccessControlList origAcl, AccessControlList acl)
        {
            if (origAcl.EntityId != acl.EntityId)
                throw new InvalidOperationException();

            var newEntries = new List<AceInfo>();
            var entityId = origAcl.EntityId;

            foreach (var entry in acl.Entries)
            {
                if (entry.IdentityId == Identifiers.SomebodyUserId)
                    continue;

                var origEntry = origAcl.Entries.Where(x => x.IdentityId == entry.IdentityId && x.LocalOnly == entry.LocalOnly).FirstOrDefault();

                // play modifications
                var ident = entry.IdentityId;
                var localOnly = entry.LocalOnly;
                var perms = entry.Permissions.ToArray();

                if (origEntry == null)
                {
                    ed.Set(entityId, ident, localOnly, GetEditedBits(entry.Permissions));
                }
                else
                {
                    var origPerms = origEntry.Permissions.ToArray();

                    // reset readonly bits
                    for (int i = PermissionType.PermissionCount - 1; i >= 0; i--)
                    {
                        var perm = perms[i];
                        var origPerm = origPerms[i];
                        if (!perm.DenyEnabled && origPerm.Deny)
                            ed.SetPermission(entityId, ident, localOnly, PermissionType.GetByName(perm.Name), PermissionValue.Undefined);
                        if (!perm.AllowEnabled && origPerm.Allow)
                            ed.SetPermission(entityId, ident, localOnly, PermissionType.GetByName(perm.Name), PermissionValue.Undefined);
                    }


                    // reset deny bits
                    for (int i = PermissionType.PermissionCount - 1; i >= 0; i--)
                    {
                        var perm = perms[i];
                        var origPerm = origPerms[i];
                        if (perm.DenyEnabled)
                            if (origPerm.Deny && !perm.Deny) // reset
                                ed.SetPermission(entityId, ident, localOnly, PermissionType.GetByName(perm.Name), PermissionValue.Undefined);
                    }

                    // reset allow bits
                    for (int i = 0; i < PermissionType.PermissionCount; i++)
                    {
                        var perm = perms[i];
                        var origPerm = origPerms[i];
                        if (perm.AllowEnabled)
                            if (origPerm.Allow && !perm.Allow) // reset
                                ed.SetPermission(entityId, ident, localOnly, PermissionType.GetByName(perm.Name), PermissionValue.Undefined);
                    }
                    // set allow bits
                    for (int i = 0; i < PermissionType.PermissionCount; i++)
                    {
                        var perm = perms[i];
                        var origPerm = origPerms[i];
                        if (origPerm.AllowEnabled)
                            if (!origPerm.Allow && perm.Allow) // set
                                ed.SetPermission(entityId, ident, localOnly, PermissionType.GetByName(perm.Name), PermissionValue.Allowed);
                    }
                    // set deny bits
                    for (int i = PermissionType.PermissionCount - 1; i >= 0; i--)
                    {
                        var perm = perms[i];
                        var origPerm = origPerms[i];
                        if (perm.DenyEnabled)
                            if (!origPerm.Deny && perm.Deny) // set
                                ed.SetPermission(entityId, ident, localOnly, PermissionType.GetByName(perm.Name), PermissionValue.Denied);
                    }
                }
            }
            ed.Apply();
        }
        private static PermissionBitMask GetEditedBits(Permission[] permissions)
        {
            var allowed = 0uL;
            var denied = 0uL;
            var mask = 1uL;
            foreach (var perm in permissions)
            {
                if (perm.DenyEnabled && perm.Deny)
                    denied |= mask;
                else if (perm.AllowEnabled && perm.Allow)
                    allowed |= mask;
                mask = mask << 1;
            }
            return new PermissionBitMask { AllowBits = allowed, DenyBits = denied };
        }

    }
}
