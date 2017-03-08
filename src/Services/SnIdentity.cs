using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.Configuration;

namespace SenseNet.Services
{
    public enum SnIdentityKind { User, Group, OrganizationalUnit }

    [Serializable]
    public class SnIdentity
    {
        public int NodeId { get; set; }
        public string Path { get; set; }
        public string Name { get; set; }
        public SnIdentityKind Kind { get; set; }

        public static SnIdentity Create(int nodeId)
        {
            Node node = null;
            using (new SystemAccount())
                node = Node.LoadNode(nodeId);

            if (node == null || !SecurityHandler.HasPermission(node, PermissionType.See))
                node = Node.LoadNode(Identifiers.SomebodyUserId);

            string name = node.Name;
            SnIdentityKind kind = SnIdentityKind.User;
            var nodeAsUser = node as IUser;
            if (nodeAsUser != null)
            {
                name = nodeAsUser.FullName;
                kind = SnIdentityKind.User;
            }
            else
            {
                var nodeAsGroup = node as IGroup;
                if (nodeAsGroup != null)
                {
                    kind = SnIdentityKind.Group;
                }
                else
                {
                    var nodeAsOrgUnit = node as IOrganizationalUnit;
                    if (nodeAsOrgUnit != null)
                    {
                        kind = SnIdentityKind.OrganizationalUnit;
                    }
                    else
                    {
                        throw new ApplicationException(String.Concat("Cannot create SnIdentity from NodeType ", ActiveSchema.NodeTypes.GetItemById(node.NodeTypeId).Name, ". Path: ", node.Path));
                    }
                }
            }

            return new SnIdentity
            {
                NodeId = node.Id,
                Path = node.Path,
                Name = name,
                Kind = kind
            };
        }
    }
}
