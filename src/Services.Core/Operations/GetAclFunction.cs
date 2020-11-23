
using System.Collections.Generic;
using System.Linq;
using SenseNet.ApplicationModel;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Security;

namespace SenseNet.Services.Core.Operations
{
    public static class GetAclFunction
    {
        //UNDONE:Doc:
        /// <summary></summary>
        /// <snCategory>Permissions</snCategory>
        /// <param name="content"></param>
        /// <returns></returns>
        [ODataFunction(Description = "$Action,GetAcl", DisplayName = "$Action,GetAcl-DisplayName")]
        [ContentTypes(N.CT.GenericContent, N.CT.ContentType)]
        [AllowedRoles(N.R.Everyone)]
        public static object GetAcl(Content content)
        {
            if (!content.Security.HasPermission(PermissionType.SeePermissions))
                throw new AccessDeniedException("Access denied.", content.Path, content.Id, User.Current,
                    new PermissionTypeBase[] { PermissionType.SeePermissions });

            var isPublic = content.Security.HasPermission(User.Visitor, PermissionType.Open);

            var acl = SnAccessControlList.GetAcl(content.Id);
            var entries = acl.Entries
                .Where(e=>e.Identity.NodeId!=Identifiers.SomebodyUserId)
                .Select(CreateAce)
                .ToList();

            var result = new Dictionary<string, object>(){
                {"id", content.Id},
                {"path", content.Path},
                {"inherits", acl.Inherits},
                {"isPublic", isPublic},
                {"entries", entries}
            };
            return result;
        }

        private static Dictionary<string, object> CreateAce(SnAccessControlEntry entry)
        {
            string ancestor = null;
            var isInherited = true;
            var perms = new Dictionary<string, object>();
            foreach (var perm in entry.Permissions)
            {
                if (perm.Allow || perm.Deny)
                {
                    var from = GetSafeAncestorPath(perm.AllowFrom ?? perm.DenyFrom);
                    if (from != null && from.Length > (ancestor?.Length ?? 0))
                        ancestor = from;
                    if (from == null)
                        isInherited = false;

                    perms.Add(perm.Name, new Dictionary<string, object>
                    {
                        {"value", perm.Allow ? "allow" : "deny"},
                        {"from", from},
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
                { "ancestor", ancestor },
                { "inherited", isInherited },
                { "propagates", entry.Propagates },
                { "permissions", perms }
            };

            return ace;
        }

        private static string GetSafeAncestorPath(string path)
        {
            if (path == null)
                return null;

            var id = NodeHead.Get(path).Id;
            return SecurityHandler.HasPermission(User.Current, id, PermissionType.See)
                ? path: "Somewhere";
        }
    }
}
