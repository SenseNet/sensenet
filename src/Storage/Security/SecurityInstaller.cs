using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.DataModel;
using SenseNet.Diagnostics;
using SenseNet.Security;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Security
{
    /// <summary>Contains methods for install scenarios.</summary>
    public static class SecurityInstaller
    {
        /// <summary>
        /// Clears the security storage copies ids of the full content tree structure from the repository 
        /// to the security component. Security component must be available.
        /// WARNING! Use only in install scenarios.
        /// </summary>
        public static void InstallDefaultSecurityStructure(InitialData data = null)
        {
            using (var op = SnTrace.System.StartOperation("Installing default security structure."))
            {
                using (new SystemAccount())
                {
                    CreateEntities();

                    var ed = Providers.Instance.SecurityHandler.CreateAclEditor();
                    ed.Allow(Identifiers.PortalRootId, Identifiers.AdministratorsGroupId, false,
                        // ReSharper disable once CoVariantArrayConversion
                        PermissionType.BuiltInPermissionTypes);

                    var schema = Providers.Instance.StorageSchema;
                    var memberPropertyType = schema.PropertyTypes["Members"];
                    var userNodeType = schema.NodeTypes["User"];
                    var groupNodeType = schema.NodeTypes["Group"];
                    if (data?.DynamicProperties != null)
                    {
                        foreach (var versionData in data.DynamicProperties)
                        {
                            if (versionData.DynamicProperties == null)
                                continue;

                            var properties = versionData.ReferenceProperties;
                            List<int> references = null;
                            foreach (var property in properties)
                            {
                                if (property.Key.Name == "Members")
                                {
                                    references = (List<int>)property.Value;
                                    break;
                                }
                            }

                            if (references == null)
                                continue;

                            var versionId = versionData.VersionId;
                            var nodeId = data.Versions.First(x => x.VersionId == versionId).NodeId;
                            var heads = NodeHead.Get(references);

                            var userMembers = new List<int>();
                            var groupMembers = new List<int>();
                            foreach (var head in heads)
                            {
                                var nodeType = head.GetNodeType();
                                if (nodeType.IsInstaceOfOrDerivedFrom(userNodeType))
                                    userMembers.Add(head.Id);
                                if (nodeType.IsInstaceOfOrDerivedFrom(groupNodeType))
                                    groupMembers.Add(head.Id);
                            }

                            Providers.Instance.SecurityHandler.AddMembers(nodeId, userMembers, groupMembers);
                        }
                    }

                    if (data == null)
                        ed.Apply();
                    else
                        ed.Apply(ParseInitialPermissions(ed.Context, data.Permissions));
                }

                op.Successful = true;
            }
        }
        private static void CreateEntities()
        {
            var securityContext = Providers.Instance.SecurityHandler.SecurityContext;

            securityContext.SecuritySystem.DataProvider.InstallDatabase();

            var entityTreeNodes = Providers.Instance.DataStore
                .LoadEntityTreeAsync(CancellationToken.None).GetAwaiter().GetResult();
            foreach (var entityTreeNode in entityTreeNodes)
                securityContext.CreateSecurityEntity(entityTreeNode.Id, entityTreeNode.ParentId, entityTreeNode.OwnerId);
        }

        internal static IEnumerable<PermissionAction> ParseInitialPermissions(SnSecurityContext context, IList<string> permissionData)
        {
            var actions = new Dictionary<int, PermissionAction>();
            foreach (var action in permissionData.Select(x => ParsePermissions(context, x)))
            {
                var entityId = action.Entries[0].EntityId;
                if (!actions.TryGetValue(entityId, out var existingAction))
                {
                    existingAction = new PermissionAction
                    {
                        EntityId = entityId,
                        Entries = new List<StoredAce>()
                    };
                    var isEntityInherited = context.IsEntityInherited(entityId);
                    if (isEntityInherited && action.Break)
                        existingAction.Break = true;
                    if (!isEntityInherited && !action.Break)
                        existingAction.Unbreak = true;
                    actions.Add(entityId, existingAction);
                }
                existingAction.Entries.AddRange(action.Entries);
            }
            return actions.Values;
        }
        private static PermissionAction ParsePermissions(SnSecurityContext context, string permissionData)
        {
            // "+E1|Normal|+U1:____++++,Normal|+G1:____++++"
            var a = permissionData.Split('|');
            var trimmed = a[0].Trim();
            var isInherited = trimmed[0] == '+';
            var b = trimmed.Substring(1);
            var entityId = int.Parse(b);

            return new PermissionAction()
            {
                Break = !isInherited,
                Entries = ParseEntries(entityId, permissionData.Substring(a[0].Length + 1))
            };
        }
        private static List<StoredAce> ParseEntries(int entityId, string src)
        {
            // "+U1:____++++,+G1:____++++"
            return src.Split(',').Select(x => CreateAce(entityId, x)).ToList();
        }
        private static StoredAce CreateAce(int entityId, string src)
        {
            // "Normal|+U1:____++++
            var segments = src.Split('|');

            Enum.TryParse<EntryType>(segments[0], true, out var entryType);

            var localOnly = segments[1][0] != '+';
            var a = segments[1].Substring(1).Split(':');

            var identityId = int.Parse(a[0]);
            ParsePermissions(a[1], out var allowBits, out var denyBits);

            return new StoredAce
            {
                EntityId = entityId,
                EntryType = entryType,
                IdentityId = identityId,
                LocalOnly = localOnly,
                AllowBits = allowBits,
                DenyBits = denyBits
            };
        }
        private static void ParsePermissions(string src, out ulong allowBits, out ulong denyBits)
        {
            //+_____-____++++
            var mask = 1ul;
            allowBits = denyBits = 0;
            for (int i = src.Length - 1; i >= 0; i--)
            {
                var c = src[i];
                if (c == '+')
                    allowBits |= mask << src.Length - i - 1;
                if (c == '-')
                    denyBits |= mask << src.Length - i - 1;
            }
        }
    }
}
