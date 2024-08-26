using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using SenseNet.ApplicationModel;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Caching.Dependency;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.Portal.Handlers;
using SenseNet.Security;
using SenseNet.Tools;

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
        [ODataFunction(Category = "Content Management")]
        [ContentTypes(N.CT.PortalRoot)]
        [AllowedRoles(N.R.Administrators, N.R.PublicAdministrators, N.R.Developers)]
        public static async Task<object> Import(Content content, HttpContext context, string path, object data)
        {
            var jData = data as JObject;
            var imported = new ImportedContent(jData);
            var requestedName = imported.Name;
            var realName = ContentNamingProvider.GetNameFromDisplayName(requestedName);
            string realPath;
            if (requestedName == realName)
            {
                realPath = path;
            }
            else
            {
                realPath = RepositoryPath.Combine(RepositoryPath.GetParentPath(path), realName);
                SnTrace.ContentOperation.Write(()=>$"Content will be renamed from {requestedName} to {realName}.");
            }

            var type = imported.Type;
            using var op = SnTrace.ContentOperation.StartOperation($"Import: {realPath}, {type}, {realName}");
            JObject model = imported.Fields;
            model.Remove("Name");
            model.Remove("PageCount");

            string action = null;
            List<string> brokenReferences = null;
            var targetContent = Content.Load(realPath);
            if (targetContent != null)
            {
                try
                {
                    brokenReferences = await ODataMiddleware.UpdateFieldsAsync(targetContent, model, true,
                        context.RequestAborted).ConfigureAwait(false);
                    await targetContent.SaveAsync(context.RequestAborted).ConfigureAwait(false);
                    action = "updated";
                }
                catch (ContentNotFoundException)
                {
                    SnTrace.Repository.Write($"WARNING: Content {realPath} was loaded from the cache but not found in the database: Update failed.");

                    // the content was loaded from the cache but not found in the database
                    NodeIdDependency.FireChanged(targetContent.Id);
                    PathDependency.FireChanged(realPath);

                    // this will make the next block create a new content
                    targetContent = null;
                }
            }

            if (targetContent == null)
            {
                if (type == "File")
                    // maybe there was not metadata file (.Content) in the import material.
                    type = await GetContentTypeName(realPath, context).ConfigureAwait(false);

                var parentPath = RepositoryPath.GetParentPath(realPath);
                var creationResult = await ODataMiddleware.CreateNewContentAsync(parentPath, type, null, requestedName,
                    null, false, model, true, context.RequestAborted, true).ConfigureAwait(false);

                targetContent = creationResult.Content;
                brokenReferences = creationResult.BrokenReferenceFieldNames;

                action = "created";
            }

            var setPermissionResult = await SetPermissionsAsync(targetContent, imported.Permissions, 
                context.RequestAborted).ConfigureAwait(false);

            op.Successful = true;

            return new
            {
                path = realPath,
                name = realName, type, action, brokenReferences,
                retryPermissions = setPermissionResult.HasUnknownIdentity,
                messages = setPermissionResult.Messages
            };
        }
        private static async Task<string> GetContentTypeName(string path, HttpContext httpContext)
        {
            var parentPath = RepositoryPath.GetParentPath(path);
            if (string.IsNullOrEmpty(parentPath) || parentPath == "/")
                return "File";

            var retrier = httpContext.RequestServices.GetRequiredService<IRetrier>();
            var parent = await retrier.RetryAsync(
                () => Node.LoadNodeAsync(parentPath, httpContext.RequestAborted),
                (loaded, _) => loaded == null).ConfigureAwait(false);

            if (parent is not GenericContent node)
                return "File";

            var fileName = RepositoryPath.GetFileName(path);

            // 1. check configured upload types (by extension) and use it if it is allowed
            // 2. otherwise get the first allowed type that is or is derived from file

            string contentTypeName = null;

            var allowedTypes = node.GetAllowedChildTypes().ToArray();

            // check configured upload types (by extension) and use it if it is allowed
            var fileContentType = UploadHelper.GetContentType(fileName, parent.Path);
            if (!string.IsNullOrEmpty(fileContentType))
            {
                if (allowedTypes.Select(ct => ct.Name).Contains(fileContentType))
                    contentTypeName = fileContentType;
            }

            if (string.IsNullOrEmpty(contentTypeName))
            {
                // get the first allowed type that is or is derived from file
                if (allowedTypes.Any(ct => ct.Name == "File"))
                {
                    contentTypeName = "File";
                }
                else
                {
                    var fileDescendant = allowedTypes.FirstOrDefault(ct => ct.IsInstaceOfOrDerivedFrom("File"));
                    if (fileDescendant != null)
                        contentTypeName = fileDescendant.Name;
                }
            }

            return contentTypeName;
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
