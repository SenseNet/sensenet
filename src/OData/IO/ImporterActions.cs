using System.Collections.Generic;
using System.Threading;
using Newtonsoft.Json.Linq;
using SenseNet.ApplicationModel;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.Security;

namespace SenseNet.OData.IO
{
    public static class ImporterActions
    {
        /// <summary>
        /// Imports a content to the content repository. This action is able to import both new and
        /// existing content items.
        /// </summary>
        /// <snCategory>Content Management</snCategory>
        /// <remarks>
        /// An example request for importing a new content:
        /// <code>
        /// {
        ///    "path": "/Root/ParentPath",
        ///    "data": {
        ///       "ContentType" = "Article",
        ///       "ContentName" = "MyNewContent",
        ///       "Fields" = {},
        ///       "Permissions" = {}
        ///    }
        /// }
        /// </code>
        /// An example response:
        /// <code>
        /// {
        ///    "path": "/Root/ParentPath",
        ///    "name": "MyNewContent",
        ///    "type": "Article",
        ///    "action": "created",
        ///    "brokenReferences": [],
        ///    "retryPermissions": false,
        ///    "messages": []
        /// }
        /// </code>
        /// </remarks>
        /// <param name="content"></param>
        /// <param name="path">Target path. In case of a new content this is the path of the parent. In case of
        /// existing content this is the path of the content itself.</param>
        /// <param name="data">Content metadata (name, type, fields).</param>
        /// <returns>A result object containing basic metadata of the created or modified content, the action that happened
        /// and the postponed references or permission settings.</returns>
        [ODataFunction]
        [ContentTypes(N.CT.PortalRoot)]
        [AllowedRoles(N.R.Administrators, N.R.PublicAdministrators, N.R.Developers)]
        public static object Import(Content content, string path, object data)
        {
            var jData = data as JObject;
            var imported = new ImportedContent(jData);
            var name = imported.Name;
            var type = imported.Type;
            using (var op = SnTrace.ContentOperation.StartOperation($"Import: {path}, {type}, {name}"))
            {
                JObject model = imported.Fields;
                model.Remove("Name");

                string action;
                List<string> brokenReferences;
                var messages = new List<string>();
                var targetContent = Content.Load(path);
                if (targetContent != null)
                {
                    ODataMiddleware.UpdateFields(targetContent, model, true, out brokenReferences);
                    targetContent.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
                    action = "updated";
                }
                else
                {
                    var parentPath = RepositoryPath.GetParentPath(path);
                    targetContent = ODataMiddleware.CreateNewContent(parentPath, type, null, name, null, false, model, true, out brokenReferences);
                    action = "created";
                }

                SetPermissions(targetContent, imported.Permissions, messages, out var retryPermissions);

                op.Successful = true;
                return new { path, name, type, action, brokenReferences, retryPermissions, messages };
            }
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
