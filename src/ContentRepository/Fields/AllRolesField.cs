﻿using System;
using System.Linq;
using System.Xml;
using AngleSharp.Dom;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using Node = SenseNet.ContentRepository.Storage.Node;

namespace SenseNet.ContentRepository.Fields
{
    [ShortName("AllRoles")]
    [DefaultFieldSetting(typeof(NullFieldSetting))]
    public class AllRolesField : ReferenceField
    {
        public override bool ReadOnly => true;

        protected override void ImportData(XmlNode fieldNode, ImportContext context)
        {
            throw new SnNotSupportedException();
        }

        public override object GetData()
        {
            var thisPath = this.Content.Path;
            var orgUnitNt = Providers.Instance.StorageSchema.NodeTypes[nameof(OrganizationalUnit)];

            // loads only containers that the current user has permissions for
            var allParents = Node.LoadNodes(Providers.Instance.SecurityHandler.SecurityContext
                .GetParentGroups(this.Content.Id, false)).ToArray();

            // valid item is all ancestors or any node that is not an OrganizationalUnit
            var filtered = allParents
                .Where(n => !n.NodeType.IsInstaceOfOrDerivedFrom(orgUnitNt) ||
                            thisPath.StartsWith(n.Path, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            return filtered;

        }
    }
}
