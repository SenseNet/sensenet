
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
        /// <summary>Returns the access control list for the requested content.</summary>
        /// <snCategory>Permissions</snCategory>
        /// <remarks>
        /// The returned object contains information about the permission inheritance state of the content, and
        /// all related permissions. 
        /// </remarks>
        /// <example>
        /// Here is an abbreviated and annotated return value example:
        /// <code>
        /// {
        ///   "id": 1347,                                 // Id of the requested content
        ///   "path": "/Root/Content",                    // Path of the requested content
        ///   "inherits": true,                           // Permission inheritance state
        ///   "isPublic": true,                           // True if the Visitor has Open permission on the requested content.
        ///   "entries": [                                // array of the combined (effective and explicit) entries
        ///     {                                         // First entry
        ///       "identity": {                           // Identity of the entry
        ///         "id": 1,                              // Id of the identity content
        ///         "path": "/Root/IMS/Public/johnny42",  // Path of the identity content
        ///         "name": "Johnny42",                   //
        ///         "displayName": "Johnny42",            //
        ///         "domain": "Public",                   //
        ///         "kind": "user",                       // simplified type: "user" or "group"
        ///         "avatar": ""                          //
        ///       },                                      //
        ///       "ancestor": "/Root",                    // Path of the parent entry
        ///       "inherited": true,                      // If true, this entry does not have explicit permissions.
        ///       "propagates": true,                     // This entry is inheritable or not (in other terminology: "localOnly")
        ///       "permissions": {                        // Permissions as an associative array
        ///         "See": {                              // "See" permission descriptor. The sub object is null if it is not set
        ///           "value": "allow",                   // Permission value. Can be "allow", "deny"
        ///           "from": "/Root"                     // Path of the content where this permission is explicitly granted.
        ///         },                                    //
        ///         "Open": {                             //
        ///           "value": "allow",                   //
        ///           "from": "/Root"                     //
        ///         },                                    //
        ///         "Publish": {                          //
        ///           "value": "allow",                   // The "Publish" permission is allowed...
        ///           "from": null                        // ... and granted on this content
        ///         },
        ///         ...
        ///       }
        ///     },
        ///     ...
        ///   ]
        /// }
        /// </code>
        /// </example>
        /// <param name="content"></param>
        /// <returns>The access control list for the requested content.</returns>
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
