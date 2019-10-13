using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Compatibility.SenseNet.Services;
using Microsoft.AspNetCore.Http;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Services;

namespace SenseNet.OData.Operations
{
    public class GetPermissionsAction : ActionBase
    {
        public override string Uri { get; } = null;
        public bool IsReusable { get; } = true;

        public override bool IsHtmlOperation { get; } = false;
        public override bool IsODataOperation { get; } = true;
        public override bool CausesStateChange { get; } = false;
        public override ActionParameter[] ActionParameters { get; } = { new ActionParameter("identity", typeof(string)) };

        public override object Execute(Content content, params object[] parameters)
        {
            var identityParamValue = parameters[0] == Type.Missing ? null : (string)parameters[0];
            var canSeePermissions = content.Security.HasPermission(PermissionType.SeePermissions);

            // If the user doesn't have SeePermissions right, it can only query its own permissions
            if (!canSeePermissions && (string.IsNullOrEmpty(identityParamValue) || identityParamValue != User.Current.Path))
                throw new Exception("You are only authorized to query your own permissions for this content.");

            // Elevation is required if the user doesn't have SeePermissions right, but
            // in this case, it will only see its own permissions anyway.
            IDisposable sysacc = null;
            try
            {
                if (!canSeePermissions)
                    sysacc = new SystemAccount();

                return string.IsNullOrEmpty(identityParamValue)
                    ? GetAcl(content)
                    : GetAce(content, identityParamValue);
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
    }
}
