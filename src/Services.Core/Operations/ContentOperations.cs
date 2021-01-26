using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.OData;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.Security;
using STT = System.Threading.Tasks;

namespace SenseNet.Services.Core.Operations
{
    public static class ContentOperations
    {
        /// <summary>
        /// Copies one or more items recursively to the given target.
        /// The source items can be identified by their Id or Path. Ids and paths can also be mixed.
        /// <para>Always check the allowed child types set on the chosen target container, because it can result in
        /// an unsuccessful copy if the target does not allow the types you want to copy.</para>
        /// <para>Another limitation is that a children of a content list cannot be copied to another content list
        /// since there could be custom local fields on the source list that are not available on the target list and
        /// could cause data loss. A workaround for this (if you do not mind losing list field data) is to first copy the
        /// content to a temporary folder outside of the source list than move them to the target location.</para>
        /// </summary>
        /// <snCategory>Content Management</snCategory>
        /// <remarks>
        /// The response contains information about all copied items (subtree roots) and all errors if there is any.
        /// <code>
        /// {
        ///   "d": {
        ///     "__count": 3,
        ///     "results": [
        ///       {
        ///         "Id": 78944,
        ///         "Path": "/Root/Target/MyDoc1.docx",
        ///         "Name": "MyDoc1.docx"
        ///       }
        ///       {
        ///         "Id": 78945,
        ///         "Path": "/Root/Target/MyDoc2.docx",
        ///         "Name": "MyDoc2.docx"
        ///       },
        ///       {
        ///         "Id": 78946,
        ///         "Path": "/Root/Target/MyDoc3.docx",
        ///         "Name": "MyDoc3.docx"
        ///       }
        ///     ],
        ///     "errors": []
        ///   }
        /// }
        /// </code>
        /// </remarks>
        /// <param name="content">The requested resource is irrelevant in this case.</param>
        /// <param name="targetPath" example="/Root/Target">Path of the existing destination content.</param>
        /// <param name="paths" example='["/Root/Content/IT/MyDocs/MyDoc1", "78945", "78946"]'>
        /// Array of the id or full path of source items.</param>
        /// <returns></returns>
        [ODataAction(Icon = "copy", Description = "$Action,CopyBatch", DisplayName = "$Action,CopyBatch-DisplayName")]
        [ContentTypes(N.CT.Folder)]
        [AllowedRoles(N.R.Everyone)]
        [Scenario(N.S.GridToolbar, N.S.BatchActions)]
        public static BatchActionResponse CopyBatch(Content content, string targetPath, object[] paths)
        {
            var targetNode = Node.LoadNode(targetPath);
            if (targetNode == null)
                throw new ContentNotFoundException(targetPath);

            var results = new List<object>();
            var errors = new List<ErrorContent>();
            var identifiers = paths.Select(NodeIdentifier.Get).ToList();
            var foundIdentifiers = new List<NodeIdentifier>();
            var nodes = Node.LoadNodes(identifiers);

            foreach (var node in nodes)
            {
                var originalPath = node?.Path;

                try
                {
                    // Collect already found identifiers in a separate list otherwise the error list
                    // would contain multiple errors for the same content.
                    foundIdentifiers.Add(NodeIdentifier.Get(node));

                    var copy = node.CopyToAndGetCopy(targetNode);
                    results.Add(new
                    {
                        copy.Id, 
                        copy.Path, 
                        copy.Name,
                        OriginalPath = originalPath
                    });
                }
                catch (Exception e)
                {
                    //TODO: we should log only relevant exceptions here and skip
                    // business logic-related errors, e.g. lack of permissions or
                    // existing target content path.
                    SnLog.WriteException(e);

                    errors.Add(new ErrorContent
                    {
                        Content = new
                        {
                            node?.Id, 
                            node?.Path, 
                            node?.Name,
                            OriginalPath = originalPath
                        },
                        Error = new Error
                        {
                            Code = "NotSpecified",
                            ExceptionType = e.GetType().FullName,
                            InnerError = new StackInfo { Trace = e.StackTrace },
                            Message = new ErrorMessage
                            {
                                Lang = System.Globalization.CultureInfo.CurrentUICulture.Name.ToLower(),
                                Value = e.Message
                            }
                        }
                    });
                }
            }

            // iterating through the missing identifiers and making error items for them
            errors.AddRange(identifiers.Where(id => !foundIdentifiers.Exists(f => f.Id == id.Id || f.Path == id.Path))
                .Select(missing => new ErrorContent
                {
                    Content = new { missing?.Id, missing?.Path },
                    Error = new Error
                    {
                        Code = "ResourceNotFound",
                        ExceptionType = "ContentNotFoundException",
                        InnerError = null,
                        Message = new ErrorMessage
                        {
                            Lang = System.Globalization.CultureInfo.CurrentUICulture.Name.ToLower(),
                            Value = string.Format(SNSR.GetString(SNSR.Exceptions.OData.ErrorContentNotFound),
                                missing?.Path)
                        }
                    }
                }));

            return BatchActionResponse.Create(results, errors, results.Count + errors.Count);
        }

        /// <summary>
        /// Moves one or more items recursively to the given target.
        /// The source items can be identified by their Id or Path. Ids and paths can also be mixed.
        /// <para>Always check the allowed child types set on the chosen target container, because it can result in
        /// an unsuccessful move if the target does not allow the types you want to move.</para>
        /// <para>Another limitation is that a children of a content list cannot be moved to another content list
        /// since there could be custom local fields on the source list that are not available on the target list and
        /// could cause data loss. A workaround for this (if you do not mind losing list field data) is to first move the
        /// content to a temporary folder outside of the source list than move them to the target location.</para>
        /// </summary>
        /// <snCategory>Content Management</snCategory>
        /// <remarks>
        /// The response contains information about all moved items (subtree roots) and all errors if there is any.
        /// <code>
        /// {
        ///   "d": {
        ///     "__count": 3,
        ///     "results": [
        ///       {
        ///         "Id": 78944,
        ///         "Path": "/Root/Target/MyDoc1.docx",
        ///         "Name": "MyDoc1.docx"
        ///       }
        ///       {
        ///         "Id": 78945,
        ///         "Path": "/Root/Target/MyDoc2.docx",
        ///         "Name": "MyDoc2.docx"
        ///       },
        ///       {
        ///         "Id": 78946,
        ///         "Path": "/Root/Target/MyDoc3.docx",
        ///         "Name": "MyDoc3.docx"
        ///       }
        ///     ],
        ///     "errors": []
        ///   }
        /// }
        /// </code>
        /// </remarks>
        /// <param name="content">The requested resource is irrelevant in this case.</param>
        /// <param name="targetPath" example="/Root/Target">Path of the existing destination content.</param>
        /// <param name="paths" example='["/Root/Content/IT/MyDocs/MyDoc1", "78945", "78946"]'>
        /// Array of the id or full path of the source items.</param>
        /// <returns></returns>
        [ODataAction(Icon = "move", Description = "$Action,MoveBatch", DisplayName = "$Action,MoveBatch-DisplayName")]
        [ContentTypes(N.CT.Folder)]
        [AllowedRoles(N.R.Everyone)]
        [Scenario(N.S.GridToolbar, N.S.BatchActions)]
        public static BatchActionResponse MoveBatch(Content content, string targetPath, object[] paths)
        {
            var targetNode = Node.LoadNode(targetPath);
            if (targetNode == null)
                throw new ContentNotFoundException(targetPath);

            var results = new List<object>();
            var errors = new List<ErrorContent>();
            var identifiers = paths.Select(NodeIdentifier.Get).ToList();
            var foundIdentifiers = new List<NodeIdentifier>();
            var nodes = Node.LoadNodes(identifiers);

            foreach (var node in nodes)
            {
                var originalPath = node?.Path;

                try
                {
                    // Collect already found identifiers in a separate list otherwise the error list
                    // would contain multiple errors for the same content.
                    foundIdentifiers.Add(NodeIdentifier.Get(node));

                    node.MoveTo(targetNode);
                    results.Add(new
                    {
                        node.Id, 
                        node.Path, 
                        node.Name,
                        OriginalPath = originalPath
                    });
                }
                catch (Exception e)
                {
                    //TODO: we should log only relevant exceptions here and skip
                    // business logic-related errors, e.g. lack of permissions or
                    // existing target content path.
                    SnLog.WriteException(e);

                    errors.Add(new ErrorContent
                    {
                        Content = new
                        {
                            node?.Id, 
                            node?.Path, 
                            node?.Name,
                            OriginalPath = originalPath
                        },
                        Error = new Error
                        {
                            Code = "NotSpecified",
                            ExceptionType = e.GetType().FullName,
                            InnerError = new StackInfo { Trace = e.StackTrace },
                            Message = new ErrorMessage
                            {
                                Lang = System.Globalization.CultureInfo.CurrentUICulture.Name.ToLower(),
                                Value = e.Message
                            }
                        }
                    });
                }
            }

            // iterating through the missing identifiers and making error items for them
            errors.AddRange(identifiers.Where(id => !foundIdentifiers.Exists(f => f.Id == id.Id || f.Path == id.Path))
                .Select(missing => new ErrorContent
                {
                    Content = new { missing?.Id, missing?.Path },
                    Error = new Error
                    {
                        Code = "ResourceNotFound",
                        ExceptionType = "ContentNotFoundException",
                        InnerError = null,
                        Message = new ErrorMessage
                        {
                            Lang = System.Globalization.CultureInfo.CurrentUICulture.Name.ToLower(),
                            Value = string.Format(SNSR.GetString(SNSR.Exceptions.OData.ErrorContentNotFound),
                                missing?.Path)
                        }
                    }
                }));

            return BatchActionResponse.Create(results, errors, results.Count + errors.Count);
        }

        /// <summary>
        /// Deletes the requested content permanently or moves it to the Trash, depending on the <paramref name="permanent"/> parameter.
        /// </summary>
        /// <snCategory>Content Management</snCategory>
        /// <param name="content"></param>
        /// <param name="permanent" example="true">True if the content must be deleted permanently.</param>
        /// <returns>This method returns nothing.</returns>
        [ODataAction(Icon = "delete", Description = "$Action,Delete", DisplayName = "$Action,Delete-DisplayName")]
        [ContentTypes(N.CT.GenericContent, N.CT.ContentType)]
        [AllowedRoles(N.R.Everyone)]
        [Scenario(N.S.ListItem, N.S.ContextMenu)]
        [RequiredPermissions(N.P.Delete)]
        public static object Delete(Content content, bool permanent = false)
        {
            content.Delete(permanent);
            return null;
        }

        /// <summary>
        /// Deletes one or more content permanently or moves them to the Trash, depending on the <paramref name="permanent"/> parameter.
        /// The deletable items can be identified by their Id or Path. Ids and paths can also be mixed.
        /// </summary>
        /// <snCategory>Content Management</snCategory>
        /// <remarks>
        /// The response contains information about all deleted items (subtree roots) and all errors if there is any.
        /// <code>
        /// {
        ///   "d": {
        ///     "__count": 3,
        ///     "results": [
        ///       {
        ///         "Id": 78944,
        ///         "Path": "/Root/Target/MyDoc1.docx",
        ///         "Name": "MyDoc1.docx"
        ///       }
        ///       {
        ///         "Id": 78945,
        ///         "Path": "/Root/Target/MyDoc2.docx",
        ///         "Name": "MyDoc2.docx"
        ///       },
        ///       {
        ///         "Id": 78946,
        ///         "Path": "/Root/Target/MyDoc3.docx",
        ///         "Name": "MyDoc3.docx"
        ///       }
        ///     ],
        ///     "errors": []
        ///   }
        /// }
        /// </code>
        /// </remarks>
        /// <param name="content">The requested resource is irrelevant in this case.</param>
        /// <param name="permanent" example="false">True if the content must be deleted permanently.</param>
        /// <param name="paths" example='["/Root/Content/IT/MyDocs/MyDoc1", "78945", "78946"]'>
        /// Array of the id or full path of the deletable items.</param>
        /// <returns></returns>
        [ODataAction(Icon = "delete", Description = "$Action,DeleteBatch", DisplayName = "$Action,DeleteBatch-DisplayName")]
        [ContentTypes(N.CT.Folder)]
        [AllowedRoles(N.R.Everyone)]
        [Scenario(N.S.GridToolbar, N.S.BatchActions)]
        public static BatchActionResponse DeleteBatch(Content content, bool permanent, object[] paths)
        {
            // no need to throw an exception if no ids are provided: we simply do not have to delete anything
            if(paths == null || paths.Length == 0)
                return null;

            var results = new List<object>();
            var errors = new List<ErrorContent>();
            var identifiers = paths.Select(NodeIdentifier.Get).ToList();
            var foundIdentifiers = new List<NodeIdentifier>();
            var nodes = Node.LoadNodes(identifiers);

            foreach (var node in nodes)
            {
                try
                {
                    // Collect already found identifiers in a separate list otherwise the error list
                    // would contain multiple errors for the same content.
                    foundIdentifiers.Add(NodeIdentifier.Get(node));

                    switch (node)
                    {
                        case GenericContent gc:
                            gc.Delete(permanent);
                            break;
                        case ContentType ct:
                            ct.Delete();
                            break;
                    }

                    results.Add(new { node.Id, node.Path, node.Name });
                }
                catch (Exception e)
                {
                    //TODO: we should log only relevant exceptions here and skip
                    // business logic-related errors, e.g. lack of permissions or
                    // existing target content path.
                    SnLog.WriteException(e);

                    errors.Add(new ErrorContent
                    {
                        Content = new { node?.Id, node?.Path },
                        Error = new Error
                        {
                            Code = "NotSpecified",
                            ExceptionType = e.GetType().FullName,
                            InnerError = new StackInfo { Trace = e.StackTrace },
                            Message = new ErrorMessage
                            {
                                Lang = System.Globalization.CultureInfo.CurrentUICulture.Name.ToLower(),
                                Value = e.Message
                            }
                        }
                    });
                }
            }

            // iterating through the missing identifiers and making error items for them
            errors.AddRange(identifiers.Where(id => !foundIdentifiers.Exists(f => f.Id == id.Id || f.Path == id.Path))
                .Select(missing => new ErrorContent
                {
                    Content = new { missing?.Id, missing?.Path },
                    Error = new Error
                    {
                        Code = "ResourceNotFound",
                        ExceptionType = "ContentNotFoundException",
                        InnerError = null,
                        Message = new ErrorMessage
                        {
                            Lang = System.Globalization.CultureInfo.CurrentUICulture.Name.ToLower(),
                            Value = string.Format(SNSR.GetString(SNSR.Exceptions.OData.ErrorContentNotFound),
                                missing?.Path)
                        }
                    }
                }));

            return BatchActionResponse.Create(results, errors, results.Count + errors.Count);
        }

        /// <summary>
        /// Returns the effective permission information of the requested content grouped by identities.
        /// The output can be filtered by the <paramref name="identity"/> parameter.
        /// </summary>
        /// <snCategory>Permissions</snCategory>
        /// <remarks>
        /// If the current user does not have <c>SeePermissions</c> right, the provided identity must be the current user
        /// in which case they will get only their own permission entries.
        /// This is a possible response:
        /// <code>
        /// {
        ///   "id": 1347,
        ///   "path": "/Root/Content",
        ///   "inherits": false,
        ///   "entries": [
        ///     {
        ///       "identity": {
        ///         "id": 7,
        ///         "path": "/Root/IMS/BuiltIn/Portal/Administrators",
        ///         "name": "Administrators",
        ///         "displayName": "\"\"",
        ///         "domain": "BuiltIn",
        ///         "kind": "group",
        ///         "avatar": null
        ///       },
        ///       "propagates": true,
        ///       "permissions": {
        ///         "See": {
        ///           "value": "allow",
        ///           "from": null,
        ///           "identity": "/Root/IMS/BuiltIn/Portal/Administrators"
        ///         },
        ///         "Preview": {
        ///           "value": "allow",
        ///           "from": null,
        ///           "identity": "/Root/IMS/BuiltIn/Portal/Administrators"
        ///         },
        ///         ...
        ///         "Custom30": null,
        ///         "Custom31": null,
        ///         "Custom32": null
        ///       }
        ///     },
        ///     {
        ///       "identity": {
        ///         "id": 8,
        ///         "path": "/Root/IMS/BuiltIn/Portal/Everyone",
        ///         ...
        ///       },
        ///       "propagates": false,
        ///       "permissions": {
        ///         ...
        ///       }
        ///     }
        ///   ]
        /// }
        /// </code>
        /// </remarks>
        /// <param name="content"></param>
        /// <param name="identity" example="/Root/IMS/BuiltIn/Portal/Everyone">Full path of an identity (group or user).
        /// </param>
        /// <returns></returns>
        /// <exception cref="Exception">Throws if the user doesn't have <c>SeePermissions</c> right
        /// and <paramref name="identity"/> is not the current user.</exception>
        [ODataFunction(Description = "$Action,GetPermissions", DisplayName = "$Action,GetPermissions-DisplayName")]
        [ContentTypes(N.CT.GenericContent, N.CT.ContentType)]
        [AllowedRoles(N.R.Everyone)]
        public static object GetPermissions(Content content, string identity = null)
        {
            var canSeePermissions = content.Security.HasPermission(PermissionType.SeePermissions);

            // If the user doesn't have SeePermissions right, it can only query its own permissions
            if (!canSeePermissions && (string.IsNullOrEmpty(identity) || identity != User.Current.Path))
                throw new Exception("You are only authorized to query your own permissions for this content.");

            // Elevation is required if the user doesn't have SeePermissions right, but
            // in this case, it will only see its own permissions anyway.
            IDisposable sysacc = null;
            try
            {
                if (!canSeePermissions)
                    sysacc = new SystemAccount();

                return string.IsNullOrEmpty(identity)
                    ? GetAcl(content)
                    : GetAce(content, identity);
            }
            finally
            {
                sysacc?.Dispose();
            }
        }
        internal static object GetAcl(Content content)
        {
            var acl = SnAccessControlList.GetAcl(content.Id);
            var entries = acl.Entries.Select(CreateAce).ToList();

            var aclout = new Dictionary<string, object>(){
                {"id", content.Id},
                {"path", content.Path},
                {"inherits", acl.Inherits},
                {"entries", entries}
            };
            return aclout;
        }
        internal static Dictionary<string, object>[] GetAce(Content content, string identityPath)
        {
            var acl = SnAccessControlList.GetAcl(content.Id);
            var entries =
                acl.Entries.Where(
                        e => string.Compare(e.Identity.Path, identityPath, StringComparison.InvariantCultureIgnoreCase) == 0)
                    .Select(CreateAce)
                    .ToArray();

            return entries.Length == 0
                ? new[] { GetEmptyEntry(identityPath) }
                : entries;
        }
        private static Dictionary<string, object> CreateAce(SnAccessControlEntry entry)
        {
            var perms = new Dictionary<string, object>();
            foreach (var perm in entry.Permissions)
            {
                if (perm.Allow || perm.Deny)
                {
                    perms.Add(perm.Name, new Dictionary<string, object>
                    {
                        {"value", perm.Allow ? "allow" : "deny"},
                        {"from", perm.AllowFrom ?? perm.DenyFrom},
                        {"identity", entry.Identity.Path}
                    });
                }
                else
                {
                    perms.Add(perm.Name, null);
                }
            }
            var ace = new Dictionary<string, object>
            {
                { "identity", PermissionQuery.GetIdentity(entry) },
                { "propagates", entry.Propagates },
                { "permissions", perms }
            };

            return ace;
        }
        private static Dictionary<string, object> GetEmptyEntry(string identityPath)
        {
            var perms = PermissionType.PermissionTypes.ToDictionary<PermissionType, string, object>(pt => pt.Name, pt => null);

            return new Dictionary<string, object>
            {
                {"identity", PermissionQuery.GetIdentity(Node.LoadNode(identityPath)) },
                {"propagates", true},
                {"permissions", perms}
            };
        }

        /// <summary>
        /// Returns whether the current or given user has the provided permissions on the requested content.
        /// The value is <c>true</c> if all requested permissions are allowed.
        /// </summary>
        /// <snCategory>Permissions</snCategory>
        /// <param name="content"></param>
        /// <param name="permissions" example='["Open", "RunApplication"]'>Permission name array.</param>
        /// <param name="user" example="/Root/IMS/BuiltIn/Portal/Visitor">Path of an existing user. If not specified,
        /// the current user's permission value will be returned.</param>
        /// <returns></returns>
        [ODataFunction(Description = "$Action,HasPermission", DisplayName = "$Action,HasPermission-DisplayName")]
        [ContentTypes(N.CT.GenericContent, N.CT.ContentType)]
        [AllowedRoles(N.R.Everyone)]
        [RequiredPermissions(N.P.SeePermissions)]
        public static bool HasPermission(Content content, ODataArray<string> permissions, string user = null)
        {
            IUser userObject = null;
            if (!string.IsNullOrEmpty(user))
            {
                userObject = Node.Load<User>(user);
                if (userObject == null)
                    throw new ContentNotFoundException("Identity not found: " + user);
            }

            if (permissions == null)
                throw new ArgumentNullException(nameof(permissions));

            var permissionNames = permissions;
            var permissionArray = permissionNames.Select(GetPermissionTypeByName).ToArray();

            return user == null
                ? content.Security.HasPermission(permissionArray)
                : content.Security.HasPermission(userObject, permissionArray);
        }
        private static PermissionType GetPermissionTypeByName(string name)
        {
            var permissionType = PermissionType.GetByName(name);
            if (permissionType != null)
                return permissionType;
            throw new ArgumentException("Unknown permission: " + name);
        }

        /// <summary>
        /// Changes the permission inheritance on the requested content.
        /// </summary>
        /// <snCategory>Permissions</snCategory>
        /// <remarks> After the <c>break</c> operation, all previous
        /// effective permissions will be  copied explicitly that are matched any of the given entry types.
        /// After the <c>unbreak</c> operation, the unnecessary explicit entries will be removed.
        /// The method is ineffective if the content's inheritance state matches the requested operation
        /// (<c>break</c> operation on broken inheritance or <c>unbreak</c> operation on not broken inheritance).</remarks>
        /// <param name="content"></param>
        /// <param name="inheritance">The inheritance value as string. Available values: "break" or "unbreak"</param>
        /// <returns>The requested resource.</returns>
        /// <exception cref="ArgumentException">Throws <see cref="ArgumentException"/> if the <paramref name="inheritance"/> is
        /// invalid.</exception>
        [ODataAction(Icon = "security", Description = "$Action,SetPermissions", DisplayName = "$Action,SetPermissions-DisplayName")]
        [ContentTypes(N.CT.GenericContent, N.CT.ContentType)]
        [AllowedRoles(N.R.Everyone)]
        [RequiredPermissions(N.P.Open, N.P.SeePermissions, N.P.SetPermissions)]
        [Scenario( N.S.WorkspaceActions, N.S.ListItem, N.S.ExploreActions, N.S.ContextMenu)]
        public static Content SetPermissions(Content content, string inheritance)
        {
            var editor = SecurityHandler.CreateAclEditor();

            switch (inheritance.ToLower())
            {
                default:
                    throw new ArgumentException("The value of the 'inheritance' must be 'break' or 'unbreak'.");
                case "break":
                    editor.BreakInheritance(content.Id, new[] { EntryType.Normal });
                    break;
                case "unbreak":
                    editor.UnbreakInheritance(content.Id, new[] { EntryType.Normal });
                    break;
            }

            editor.Apply();

            return content;
        }

        /// <summary>
        /// Modifies the explicit permission set of the requested content.
        /// </summary>
        /// <snCategory>Permissions</snCategory>
        /// <remarks>
        /// <para>
        /// The given <paramref name="r"/> parameter is a <see cref="SetPermissionsRequest"/> that has
        /// an array of the complex request objects.
        /// Every item (<see cref="SetPermissionRequest"/>) contains the followings:
        /// - identity: id or path of a user or group.
        /// - localOnly: optional bool value (default: false).
        /// - one optional property for all available permission types (See, Open, Save, etc.) that describes the desired
        /// permission value.
        /// </para>
        /// <para>
        /// The permission value can be:
        /// - "undefined" alias "u" or "0"
        /// - "allow" alias "a" or "1"
        /// - "deny" alias "d" or "2"
        /// </para>
        /// <example>
        /// The following request body sets some permissions for an user and a group.
        /// <code>
        /// {
        ///   r: [
        ///     {identity:"/Root/IMS/BuiltIn/Portal/Visitor", OpenMinor:"allow", Save:"deny"},
        ///     {identity:"/Root/IMS/BuiltIn/Portal/Owners", Save:"A"}
        ///   ]
        /// }
        /// </code>
        /// </example>
        /// <example>
        /// The following request body sets a local only Open permission for the Visitor.
        /// <code>
        /// {r: [{identity:"/Root/IMS/BuiltIn/Portal/Visitor", localOnly: true, Open:"allow"}]}
        /// </code>
        /// </example>
        /// </remarks>
        /// <param name="content"></param>
        /// <param name="r">A named array of <c>SetPermissionRequest</c> objects that describe the modifications.</param>
        /// <returns>The requested resource.</returns>
        /// <exception cref="ArgumentException">In case of invalid permission state.</exception>
        /// <exception cref="ContentNotFoundException">If the identity is not found in any request item.</exception>
        [ODataAction(Icon = "security", Description = "$Action,SetPermissions", DisplayName = "$Action,SetPermissions-DisplayName")]
        [ContentTypes(N.CT.GenericContent, N.CT.ContentType)]
        [AllowedRoles(N.R.Everyone)]
        [RequiredPermissions(N.P.Open, N.P.SeePermissions, N.P.SetPermissions)]
        [Scenario( N.S.WorkspaceActions, N.S.ListItem, N.S.ExploreActions, N.S.ContextMenu)]
        public static Content SetPermissions(Content content, SetPermissionsRequest r)
        {
            var request = r;
            var editor = SecurityHandler.CreateAclEditor();
            SetPermissions(content, request, editor);
            editor.Apply();
            return content;
        }
        private static void SetPermissions(Content content, SetPermissionsRequest request, SnAclEditor editor)
        {
            var contentId = content.Id;
            foreach (var permReq in request.r)
            {
                var member = LoadMember(permReq.identity);
                var localOnly = permReq.localOnly.HasValue ? permReq.localOnly.Value : false;

                if (permReq.See != null)                      /**/ ProcessPermission(editor, contentId, member.Id, localOnly, PermissionType.See, permReq.See);
                if (permReq.Preview != null)                  /**/ ProcessPermission(editor, contentId, member.Id, localOnly, PermissionType.Preview, permReq.Preview);
                if (permReq.PreviewWithoutWatermark != null)  /**/ ProcessPermission(editor, contentId, member.Id, localOnly, PermissionType.PreviewWithoutWatermark, permReq.PreviewWithoutWatermark);
                if (permReq.PreviewWithoutRedaction != null)  /**/ ProcessPermission(editor, contentId, member.Id, localOnly, PermissionType.PreviewWithoutRedaction, permReq.PreviewWithoutRedaction);
                if (permReq.Open != null)                     /**/ ProcessPermission(editor, contentId, member.Id, localOnly, PermissionType.Open, permReq.Open);
                if (permReq.OpenMinor != null)                /**/ ProcessPermission(editor, contentId, member.Id, localOnly, PermissionType.OpenMinor, permReq.OpenMinor);
                if (permReq.Save != null)                     /**/ ProcessPermission(editor, contentId, member.Id, localOnly, PermissionType.Save, permReq.Save);
                if (permReq.Publish != null)                  /**/ ProcessPermission(editor, contentId, member.Id, localOnly, PermissionType.Publish, permReq.Publish);
                if (permReq.ForceCheckin != null)             /**/ ProcessPermission(editor, contentId, member.Id, localOnly, PermissionType.ForceCheckin, permReq.ForceCheckin);
                if (permReq.AddNew != null)                   /**/ ProcessPermission(editor, contentId, member.Id, localOnly, PermissionType.AddNew, permReq.AddNew);
                if (permReq.Approve != null)                  /**/ ProcessPermission(editor, contentId, member.Id, localOnly, PermissionType.Approve, permReq.Approve);
                if (permReq.Delete != null)                   /**/ ProcessPermission(editor, contentId, member.Id, localOnly, PermissionType.Delete, permReq.Delete);
                if (permReq.RecallOldVersion != null)         /**/ ProcessPermission(editor, contentId, member.Id, localOnly, PermissionType.RecallOldVersion, permReq.RecallOldVersion);
                if (permReq.DeleteOldVersion != null)         /**/ ProcessPermission(editor, contentId, member.Id, localOnly, PermissionType.DeleteOldVersion, permReq.DeleteOldVersion);
                if (permReq.SeePermissions != null)           /**/ ProcessPermission(editor, contentId, member.Id, localOnly, PermissionType.SeePermissions, permReq.SeePermissions);
                if (permReq.SetPermissions != null)           /**/ ProcessPermission(editor, contentId, member.Id, localOnly, PermissionType.SetPermissions, permReq.SetPermissions);
                if (permReq.RunApplication != null)           /**/ ProcessPermission(editor, contentId, member.Id, localOnly, PermissionType.RunApplication, permReq.RunApplication);
                if (permReq.ManageListsAndWorkspaces != null) /**/ ProcessPermission(editor, contentId, member.Id, localOnly, PermissionType.ManageListsAndWorkspaces, permReq.ManageListsAndWorkspaces);
                if (permReq.TakeOwnership != null)            /**/ ProcessPermission(editor, contentId, member.Id, localOnly, PermissionType.TakeOwnership, permReq.TakeOwnership);

                if (permReq.Custom01 != null) ProcessPermission(editor, contentId, member.Id, localOnly, PermissionType.Custom01, permReq.Custom01);
                if (permReq.Custom02 != null) ProcessPermission(editor, contentId, member.Id, localOnly, PermissionType.Custom02, permReq.Custom02);
                if (permReq.Custom03 != null) ProcessPermission(editor, contentId, member.Id, localOnly, PermissionType.Custom03, permReq.Custom03);
                if (permReq.Custom04 != null) ProcessPermission(editor, contentId, member.Id, localOnly, PermissionType.Custom04, permReq.Custom04);
                if (permReq.Custom05 != null) ProcessPermission(editor, contentId, member.Id, localOnly, PermissionType.Custom05, permReq.Custom05);
                if (permReq.Custom06 != null) ProcessPermission(editor, contentId, member.Id, localOnly, PermissionType.Custom06, permReq.Custom06);
                if (permReq.Custom07 != null) ProcessPermission(editor, contentId, member.Id, localOnly, PermissionType.Custom07, permReq.Custom07);
                if (permReq.Custom08 != null) ProcessPermission(editor, contentId, member.Id, localOnly, PermissionType.Custom08, permReq.Custom08);
                if (permReq.Custom09 != null) ProcessPermission(editor, contentId, member.Id, localOnly, PermissionType.Custom09, permReq.Custom09);
                if (permReq.Custom10 != null) ProcessPermission(editor, contentId, member.Id, localOnly, PermissionType.Custom10, permReq.Custom10);
                if (permReq.Custom11 != null) ProcessPermission(editor, contentId, member.Id, localOnly, PermissionType.Custom11, permReq.Custom11);
                if (permReq.Custom12 != null) ProcessPermission(editor, contentId, member.Id, localOnly, PermissionType.Custom12, permReq.Custom12);
                if (permReq.Custom13 != null) ProcessPermission(editor, contentId, member.Id, localOnly, PermissionType.Custom13, permReq.Custom13);
                if (permReq.Custom14 != null) ProcessPermission(editor, contentId, member.Id, localOnly, PermissionType.Custom14, permReq.Custom14);
                if (permReq.Custom15 != null) ProcessPermission(editor, contentId, member.Id, localOnly, PermissionType.Custom15, permReq.Custom15);
                if (permReq.Custom16 != null) ProcessPermission(editor, contentId, member.Id, localOnly, PermissionType.Custom16, permReq.Custom16);
                if (permReq.Custom17 != null) ProcessPermission(editor, contentId, member.Id, localOnly, PermissionType.Custom17, permReq.Custom17);
                if (permReq.Custom18 != null) ProcessPermission(editor, contentId, member.Id, localOnly, PermissionType.Custom18, permReq.Custom18);
                if (permReq.Custom19 != null) ProcessPermission(editor, contentId, member.Id, localOnly, PermissionType.Custom19, permReq.Custom19);
                if (permReq.Custom20 != null) ProcessPermission(editor, contentId, member.Id, localOnly, PermissionType.Custom20, permReq.Custom20);
                if (permReq.Custom21 != null) ProcessPermission(editor, contentId, member.Id, localOnly, PermissionType.Custom21, permReq.Custom21);
                if (permReq.Custom22 != null) ProcessPermission(editor, contentId, member.Id, localOnly, PermissionType.Custom22, permReq.Custom22);
                if (permReq.Custom23 != null) ProcessPermission(editor, contentId, member.Id, localOnly, PermissionType.Custom23, permReq.Custom23);
                if (permReq.Custom24 != null) ProcessPermission(editor, contentId, member.Id, localOnly, PermissionType.Custom24, permReq.Custom24);
                if (permReq.Custom25 != null) ProcessPermission(editor, contentId, member.Id, localOnly, PermissionType.Custom25, permReq.Custom25);
                if (permReq.Custom26 != null) ProcessPermission(editor, contentId, member.Id, localOnly, PermissionType.Custom26, permReq.Custom26);
                if (permReq.Custom27 != null) ProcessPermission(editor, contentId, member.Id, localOnly, PermissionType.Custom27, permReq.Custom27);
                if (permReq.Custom28 != null) ProcessPermission(editor, contentId, member.Id, localOnly, PermissionType.Custom28, permReq.Custom28);
                if (permReq.Custom29 != null) ProcessPermission(editor, contentId, member.Id, localOnly, PermissionType.Custom29, permReq.Custom29);
                if (permReq.Custom30 != null) ProcessPermission(editor, contentId, member.Id, localOnly, PermissionType.Custom30, permReq.Custom30);
                if (permReq.Custom31 != null) ProcessPermission(editor, contentId, member.Id, localOnly, PermissionType.Custom31, permReq.Custom31);
                if (permReq.Custom32 != null) ProcessPermission(editor, contentId, member.Id, localOnly, PermissionType.Custom32, permReq.Custom32);
            }
            editor.Apply();
        }
        private static void ProcessPermission(SnAclEditor editor, int contentId, int identityId, bool localOnly, PermissionType permissionType, string requestValue)
        {
            switch (requestValue.ToLower())
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
                    throw new ArgumentException("Invalid permission value: " + requestValue);
            }
        }

        private static ISecurityMember LoadMember(string idstr)
        {
            int id;
            ISecurityMember ident;
            if ((ident = (int.TryParse(idstr, out id) ? Node.LoadNode(id) : Node.LoadNode(idstr)) as ISecurityMember) != null)
                return ident;
            throw new ContentNotFoundException("Identity not found: " + idstr);
        }
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public class SetPermissionsRequest
        {
            public SetPermissionRequest[] r;
        }
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public class SetPermissionRequest
        {
            // {r:[{identity:"/Root/IMS/BuiltIn/Portal/Visitor", OpenMinor:"allow", Save:"deny"},{identity:"/Root/IMS/BuiltIn/Portal/Creators", OpenMinor:"A", Save:"1"}]}

            public string identity;  // Id or Path
            public bool? localOnly;

            public string See;       // Insensitive. Available values: "u", "a", "d", "undefined", "allow", "deny", "0", "1", "2" 
            public string Preview;
            public string PreviewWithoutWatermark;
            public string PreviewWithoutRedaction;
            public string Open;
            public string OpenMinor;
            public string Save;
            public string Publish;
            public string ForceCheckin;
            public string AddNew;
            public string Approve;
            public string Delete;
            public string RecallOldVersion;
            public string DeleteOldVersion;
            public string SeePermissions;
            public string SetPermissions;
            public string RunApplication;
            public string ManageListsAndWorkspaces;
            public string TakeOwnership;

            public string Custom01;
            public string Custom02;
            public string Custom03;
            public string Custom04;
            public string Custom05;
            public string Custom06;
            public string Custom07;
            public string Custom08;
            public string Custom09;
            public string Custom10;
            public string Custom11;
            public string Custom12;
            public string Custom13;
            public string Custom14;
            public string Custom15;
            public string Custom16;
            public string Custom17;
            public string Custom18;
            public string Custom19;
            public string Custom20;
            public string Custom21;
            public string Custom22;
            public string Custom23;
            public string Custom24;
            public string Custom25;
            public string Custom26;
            public string Custom27;
            public string Custom28;
            public string Custom29;
            public string Custom30;
            public string Custom31;
            public string Custom32;
        }

        /// <summary>
        /// Restores the Content from the Trash.
        /// WARNING: Known issue that you may get errors restoring a ContentListItem whose
        /// ContentListField structure has changed since it was deleted.
        /// </summary>
        /// <snCategory>Content Management</snCategory>
        /// <param name="content"></param>
        /// <param name="destination" example="/Root/DifferentTarget">The path where the content should be restored,
        /// if it is not the same one from which it was deleted.</param>
        /// <param name="newname" example="true">If true, allows renaming the restored content automatically
        /// if the name already exists in the destination folder.</param>
        /// <returns></returns>
        [ODataAction(Icon = "restore", Description = "$Action,Restore", DisplayName = "$Action,Restore-DisplayName")]
        [ContentTypes(N.CT.TrashBag)]
        [AllowedRoles(N.R.Everyone)]
        [RequiredPermissions(N.P.Save)]
        [Scenario(N.S.ListItem, N.S.ExploreToolbar, N.S.ContextMenu)]
        public static async STT.Task Restore(Content content, string destination = null, bool? newname = null)
        {
            if (!(content?.ContentHandler is TrashBag tb))
                throw new InvalidContentActionException("The resource content must be a TrashBag.");

            if (string.IsNullOrEmpty(destination))
                destination = tb.OriginalPath;

            // remember the id to load the content later
            var originalId = tb.DeletedContent.Id;

            try
            {
                if (newname.HasValue)
                    TrashBin.Restore(tb, destination, newname.Value);
                else
                    TrashBin.Restore(tb, destination);
            }
            catch (RestoreException rex)
            {
                string msg;

                switch (rex.ResultType)
                {
                    case RestoreResultType.ExistingName:
                        msg = SNSR.GetString(SNSR.Exceptions.OData.RestoreExistingName);
                        break;
                    case RestoreResultType.ForbiddenContentType:
                        msg = SNSR.GetString(SNSR.Exceptions.OData.RestoreForbiddenContentType);
                        break;
                    case RestoreResultType.NoParent:
                        msg = SNSR.GetString(SNSR.Exceptions.OData.RestoreNoParent);
                        break;
                    case RestoreResultType.PermissionError:
                        msg = SNSR.GetString(SNSR.Exceptions.OData.RestorePermissionError);
                        break;
                    default:
                        msg = rex.Message;
                        break;
                }

                throw new Exception(msg);
            }

            await Content.LoadAsync(originalId, CancellationToken.None);
        }
    }
}
