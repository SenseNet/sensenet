﻿using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using SenseNet.ApplicationModel;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Caching.Dependency;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.Security;

namespace SenseNet.OData.IO
{
    public static class ImporterActions
    {
        internal class SetPermissionResult
        {
            public bool HasUnknownIdentity { get; set; }
            public List<string> Messages { get; set; } = new();
        }

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
        /// <param name="context"></param>
        /// <param name="path">Target path. In case of a new content this is the path of the parent. In case of
        /// existing content this is the path of the content itself.</param>
        /// <param name="data">Content metadata (name, type, fields).</param>
        /// <returns>A result object containing basic metadata of the created or modified content, the action that happened
        /// and the postponed references or permission settings.</returns>
        [ODataFunction]
        [ContentTypes(N.CT.PortalRoot)]
        [AllowedRoles(N.R.Administrators, N.R.PublicAdministrators, N.R.Developers)]
        public static async Task<object> Import(Content content, HttpContext context, string path, object data)
        {
            var jData = data as JObject;
            var imported = new ImportedContent(jData);
            var name = imported.Name;
            var type = imported.Type;
            using var op = SnTrace.ContentOperation.StartOperation($"Import: {path}, {type}, {name}");
            JObject model = imported.Fields;
            model.Remove("Name");

            string action = null;
            List<string> brokenReferences = null;
            var targetContent = Content.Load(path);
            if (targetContent != null)
            {
                try
                {
                    ODataMiddleware.UpdateFields(targetContent, model, true, out brokenReferences);
                    await targetContent.SaveAsync(context.RequestAborted).ConfigureAwait(false);
                    action = "updated";
                }
                catch (ContentNotFoundException)
                {
                    SnTrace.Repository.Write($"WARNING: Content {path} was loaded from the cache but not found in the database: Update failed.");

                    // the content was loaded from the cache but not found in the database
                    NodeIdDependency.FireChanged(targetContent.Id);
                    PathDependency.FireChanged(path);

                    // this will make the next block create a new content
                    targetContent = null;
                }
            }

            if (targetContent == null)
            {
                var parentPath = RepositoryPath.GetParentPath(path);
                var creationResult = await ODataMiddleware.CreateNewContentAsync(parentPath, type, null, name,
                    null, false, model, true, context.RequestAborted).ConfigureAwait(false);

                targetContent = creationResult.Content;
                brokenReferences = creationResult.BrokenReferenceFieldNames;

                action = "created";
            }

            var setPermissionResult = await SetPermissionsAsync(targetContent, imported.Permissions, 
                context.RequestAborted).ConfigureAwait(false);

            op.Successful = true;

            return new
            {
                path, name, type, action, brokenReferences,
                retryPermissions = setPermissionResult.HasUnknownIdentity,
                messages = setPermissionResult.Messages
            };
        }

        private static async Task<SetPermissionResult> SetPermissionsAsync(Content content, 
            PermissionModel permissions, CancellationToken cancel)
        {
            var result = new SetPermissionResult();
            if (permissions == null)
                return result;

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
                var identity = await Node.LoadNodeAsync(entryModel.Identity, cancel).ConfigureAwait(false);
                if (identity == null)
                {
                    result.HasUnknownIdentity = true;
                    //messages.Add($"Unknown identity: {entryModel.Identity}");
                    continue;
                }

                var messages = new List<string>();

                SetPermissions(aclEditor, content.Id, identity.Id, entryModel.LocalOnly, entryModel.Permissions, ref messages);

                result.Messages = messages;
            }
            await aclEditor.ApplyAsync(cancel).ConfigureAwait(false);

            return result;
        }

        private static void SetPermissions(AclEditor aclEditor, int contentId, int identityId, bool localOnly,
            Dictionary<string, object> permissions, ref List<string> messages)
        {
            foreach (var item in permissions)
            {
                var permissionType = PermissionType.GetByName(item.Key);
                if (permissionType == null)
                {
                    messages.Add($"WARING: Unknown permission: {item.Key}");
                    continue;
                }
                SetPermission(aclEditor, contentId, identityId, localOnly, permissionType, item.Value, ref messages);
            }
        }
        private static void SetPermission(AclEditor editor, int contentId, int identityId, bool localOnly,
            PermissionType permissionType, object permissionValue, ref List<string> messages)
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
