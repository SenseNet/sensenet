﻿using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using System;
using SenseNet.Configuration;
using SenseNet.ContentRepository;

// ReSharper disable once CheckNamespace
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
            Node node;
            using (new SystemAccount())
                node = Node.LoadNode(nodeId);

            if (node == null || !Providers.Instance.SecurityHandler.HasPermission(node, PermissionType.See))
                node = User.Somebody;

            string name = node.Name;
            SnIdentityKind kind;
            switch (node)
            {
                case IUser nodeAsUser:
                    name = nodeAsUser.FullName;
                    kind = SnIdentityKind.User;
                    break;
                case IGroup _:
                    kind = SnIdentityKind.Group;
                    break;
                case IOrganizationalUnit _:
                    kind = SnIdentityKind.OrganizationalUnit;
                    break;
                default:
                    throw new ApplicationException(string.Concat("Cannot create SnIdentity from NodeType ", 
                        Providers.Instance.StorageSchema.NodeTypes.GetItemById(node.NodeTypeId).Name, ". Path: ", node.Path));
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
