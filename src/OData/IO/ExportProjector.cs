using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Fields;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Security;

namespace SenseNet.OData.IO
{
    // No metadata, $expand is irrelevant, $select works.
    internal class ExportProjector : Projector
    {
        internal override void Initialize(Content container)
        {
            // do nothing
        }
        internal override ODataEntity Project(Content content, HttpContext httpContext)
        {
            var entity = new ODataEntity();
            var fields = new Dictionary<string, object>();
            var selfurl = GetSelfUrl(content);

            var fieldNames = content.Fields.Keys;

            var relevantFieldNames = Request.Select.Count == 0 ? fieldNames : fieldNames.Intersect(Request.Select);
            foreach (var fieldName in relevantFieldNames)
            {
                if (ODataMiddleware.DisabledFieldNames.Contains(fieldName))
                    continue;

                if (IsAllowedField(content, fieldName))
                {
                    if (content.Fields.TryGetValue(fieldName, out var field))
                        fields.Add(fieldName, GetJsonObject(field, selfurl, Request));
                    else if (fieldName == ICONPROPERTY)
                        fields.Add(fieldName, content.Icon ?? content.ContentType.Icon);
                    else
                        fields.Add(fieldName, null);
                }
            }

            entity.Add("ContentName", content.Name);
            entity.Add("ContentType", content.ContentType.Name);
            entity.Add("Fields", fields);
            var permissions = ExportPermissions(content);
            if (permissions != null)
                entity.Add("Permissions", permissions);

            return entity;
        }

        protected override bool IsAllowedField(Content content, string fieldName)
        {
            switch (fieldName)
            {
                case "TypeIs":
                case "InTree":
                case "InFolder":
                case "SavingState":
                case "EffectiveAllowedChildTypes":
                case "AllFieldSettingContents":
                case "AvailableContentTypeFields":
                case ACTIONSPROPERTY:
                case ICONPROPERTY:
                case ODataMiddleware.ChildrenPropertyName:
                    return false;
                case "AllowedChildTypes":
                    var ctName = content.ContentType.Name;
                    if (ctName == "Folder" || ctName == "Page")
                        return false;
                    break;
            }
            return base.IsAllowedField(content, fieldName);
        }

        protected override object GetJsonObject(Field field, string selfUrl, ODataRequest oDataRequest)
        {
            if(field is AllowedChildTypesField actField)
                return GetAllowedChildTypes(actField, selfUrl, oDataRequest);
            if (field is ReferenceField refField)
                return GetReference(refField, selfUrl, oDataRequest);
            return base.GetJsonObject(field, selfUrl, oDataRequest);
        }

        private object GetAllowedChildTypes(AllowedChildTypesField field, string selfUrl, ODataRequest oDataRequest)
        {
            var value = field.GetData();

            if (value == null)
                return null;
            if (value is Node node)
                return new[] {node.Name};
            if (value is IEnumerable<Node> nodes)
                return nodes.Where(n => n != null).Select(n => n.Name).ToArray();

            throw new NotSupportedException();
        }

        private object GetReference(ReferenceField field, string selfUrl, ODataRequest oDataRequest)
        {
            var value = field.GetData();

            if (value == null)
                return null;
            if (value is Node node)
                return node.Path;
            if (value is IEnumerable<Node> nodes)
                return nodes.Where(n => n != null).Select(n => n.Path).ToArray();

            throw new NotSupportedException();
        }

        private object ExportPermissions(Content content)
        {
            var canSeePermissions = content.Security.HasPermission(PermissionType.SeePermissions);
            if (!canSeePermissions)
                return null;

            var entries = content.Security.GetExplicitEntries()
                .Select(GetEntry)
                .Where(x => x != null)
                .ToArray();

            var isInherited = content.Security.IsInherited;
            if (entries.Length == 0 && isInherited)
                return null;

            return new PermissionModel
            {
                IsInherited = isInherited,
                Entries = entries
            };
        }
        private EntryModel GetEntry(AceInfo entry)
        {
            var identityPath = NodeHead.Get(entry.IdentityId)?.Path;
            if (identityPath == null)
                return null;

            var perms = new Dictionary<string, object>();
            foreach (var permissionType in PermissionType.PermissionTypes)
            {
                var allow = (entry.AllowBits & permissionType.Mask) != 0;
                var deny = (entry.DenyBits & permissionType.Mask) != 0;
                if (allow || deny)
                    perms.Add(permissionType.Name, deny ? "deny" : "allow");
            }

            return new EntryModel
            {
                Identity = identityPath,
                LocalOnly = entry.LocalOnly,
                Permissions = perms
            };
        }
    }
}
