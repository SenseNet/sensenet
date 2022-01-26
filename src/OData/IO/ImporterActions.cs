using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using SenseNet.ApplicationModel;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Security;

namespace SenseNet.OData.IO
{
    public static class ImporterActions
    {
        [ODataFunction]
        [ContentTypes(N.CT.PortalRoot)]
        [AllowedRoles(N.R.Administrators, N.R.Developers)]
        public static object Import(Content content, string path, object data)
        {
            var jData = data as JObject;
            var imported = new ImportedContent(jData);
            var name = imported.Name;
            var type = imported.Type;

            JObject model = imported.Fields;
            model.Remove("Name");

            string action;
            List<string> brokenReferences;
            var messages = new List<string>();
            var targetContent = Content.Load(path);
            if (targetContent != null)
            {
                ODataMiddleware.UpdateFields(targetContent, model, true, out brokenReferences);
                targetContent.Save();
                action = "updated";
            }
            else
            {
                var parentPath = RepositoryPath.GetParentPath(path);
                targetContent = ODataMiddleware.CreateNewContent(parentPath, type, null, name, null, false, model, true, out brokenReferences);
                action = "created";
            }

            SetPermissions(targetContent, imported.Permissions, messages, out var retryPermissions);

            return new {path, name, type, action, brokenReferences, retryPermissions, messages };
        }

        private static void SetPermissions(Content content, PermissionModel permissions, List<string> messages, out bool hasUnkownIdentity)
        {
            hasUnkownIdentity = false;
            if (permissions == null)
                return;

            var controller = content.ContentHandler.Security;
            var aclEditor = Providers.Instance.SecurityHandler.CreateAclEditor();
            if (controller.IsInherited != permissions.IsInherited)
            {
                if (permissions.IsInherited)
                    aclEditor.UnbreakInheritance(content.Id, new[] {EntryType.Normal});
                else
                    aclEditor.BreakInheritance(content.Id, new[] {EntryType.Normal});
            }

            foreach (var entryModel in permissions.Entries)
            {
                var identity = Node.LoadNode(entryModel.Identity);
                if (identity == null)
                {
                    hasUnkownIdentity = true;
                    //messages.Add($"Unknown identity: {entryModel.Identity}");
                    continue;
                }

                SetPermissions(aclEditor, content.Id, identity.Id, entryModel.LocalOnly, entryModel.Permissions, messages);
            }
            aclEditor.Apply();
        }

        private static void SetPermissions(AclEditor aclEditor, int contentId, int identityId, bool localOnly,
            Dictionary<string, object> permissions, List<string> messages)
        {
            foreach (var item in permissions)
            {
                var permissionType = PermissionType.GetByName(item.Key);
                if (permissionType == null)
                {
                    messages.Add($"WARING: Unknown permission: {item.Key}");
                    continue;
                }
                SetPermission(aclEditor, contentId, identityId, localOnly, permissionType, item.Value, messages);
            }
        }
        private static void SetPermission(AclEditor editor, int contentId, int identityId, bool localOnly,
            PermissionType permissionType, object permissionValue, List<string> messages)
        {
            switch (permissionValue.ToString().ToLowerInvariant())
            {
                case "0":
                case "u":
                case "undefined":
                    // PermissionValue.Undefined;
                    editor.ClearPermission(contentId, identityId, localOnly, permissionType);
                    break;
                case "1":
                case "a":
                case "allow":
                    // PermissionValue.Allowed;
                    editor.Allow(contentId, identityId, localOnly, permissionType);
                    break;
                case "2":
                case "d":
                case "deny":
                    // PermissionValue.Denied;
                    editor.Deny(contentId, identityId, localOnly, permissionType);
                    break;
                default:
                    messages.Add($"WARING: Unknown permissionValue: {permissionValue}");
                    break;
            }
        }

    }
}
