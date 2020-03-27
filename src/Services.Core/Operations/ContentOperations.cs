using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.Security;

namespace SenseNet.Services.Core.Operations
{
    public static class ContentOperations
    {
        [ODataAction(Icon = "copy", Description = "$Action,CopyBatch")]
        [ContentTypes(N.CT.Folder)]
        [AllowedRoles(N.R.Everyone)]
        [Scenario(N.S.GridToolbar)]
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
                try
                {
                    // Collect already found identifiers in a separate list otherwise the error list
                    // would contain multiple errors for the same content.
                    foundIdentifiers.Add(NodeIdentifier.Get(node));

                    var copy = node.CopyToAndGetCopy(targetNode);
                    results.Add(new { copy.Id, copy.Path, copy.Name });
                }
                catch (Exception e)
                {
                    //TODO: we should log only relevant exceptions here and skip
                    // business logic-related errors, e.g. lack of permissions or
                    // existing target content path.
                    SnLog.WriteException(e);

                    errors.Add(new ErrorContent
                    {
                        Content = new { node?.Id, node?.Path, node?.Name },
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

        [ODataAction(Icon = "move", Description = "$Action,MoveBatch")]
        [ContentTypes(N.CT.Folder)]
        [AllowedRoles(N.R.Everyone)]
        [Scenario(N.S.GridToolbar)]
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
                try
                {
                    // Collect already found identifiers in a separate list otherwise the error list
                    // would contain multiple errors for the same content.
                    foundIdentifiers.Add(NodeIdentifier.Get(node));

                    node.MoveTo(targetNode);
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
                        Content = new { node?.Id, node?.Path, node?.Name },
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

        [ODataAction(Icon = "delete", Description = "$Action,Delete")]
        [ContentTypes(N.CT.GenericContent)]
        [AllowedRoles(N.R.Everyone)]
        [Scenario(N.S.ListItem)]
        [RequiredPermissions(N.P.Delete)]
        public static object Delete(Content content, bool permanent = false)
        {
            content.Delete(permanent);
            return null;
        }

        [ODataAction(Icon = "delete", Description = "$Action,DeleteBatch")]
        [ContentTypes(N.CT.Folder)]
        [AllowedRoles(N.R.Everyone)]
        [Scenario(N.S.GridToolbar)]
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


        [ODataFunction(Description = "$Action,GetPermissions")]
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

        [ODataFunction(Description = "$Action,HasPermission")]
        [ContentTypes(N.CT.GenericContent, N.CT.ContentType)]
        [AllowedRoles(N.R.Everyone)]
        [RequiredPermissions(N.P.SeePermissions)]
        public static bool HasPermission(Content content, string[] permissions, string user = null)
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


        [ODataAction(Icon = "security", Description = "$Action,SetPermissions")]
        [ContentTypes(N.CT.GenericContent, N.CT.ContentType)]
        [AllowedRoles(N.R.Everyone)]
        [RequiredPermissions(N.P.Open, N.P.SeePermissions, N.P.SetPermissions)]
        [Scenario("WorkspaceActions", "ListItem", "ExploreActions")]
        public static object SetPermissions(Content content, string inheritance)
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

            return null;
        }

        [ODataAction(Icon = "security", Description = "$Action,SetPermissions")]
        [ContentTypes(N.CT.GenericContent, N.CT.ContentType)]
        [AllowedRoles(N.R.Everyone)]
        [RequiredPermissions(N.P.Open, N.P.SeePermissions, N.P.SetPermissions)]
        [Scenario("WorkspaceActions", "ListItem", "ExploreActions")]
        public static object SetPermissions(Content content, SetPermissionsRequest r)
        {
            var request = r;
            var editor = SecurityHandler.CreateAclEditor();
            SetPermissions(content, request, editor);
            editor.Apply();
            return null;
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

    }
}
