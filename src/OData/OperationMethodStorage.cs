using System.Collections.Generic;
using System.Linq;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.OData
{
    internal class OperationMethodStorage : IOperationMethodStorage
    {
        public IEnumerable<ActionBase> GetActions(IEnumerable<ActionBase> storedActions, Content content, string scenario)
        {
            // Local cache
            string[] actualRoles = null;
            // Gets role names of the current user and uses the local cache
            string[] GetRoles()
            {
                if (actualRoles == null)
                    actualRoles = NodeHead.Get(SecurityHandler.GetGroups()).Select(y => y.Name).ToArray();
                return actualRoles;
            }

            var stored = storedActions.ToArray();
            var operationMethods = OperationCenter.Operations
                .SelectMany(x => x.Value)
                .Where(x => FilterByApplications(x.Method.Name, stored))
                .Where(x => FilterByContentTypes(content.ContentType, x.ContentTypes))
                .Where(x => x.Roles.Length == 0 || GetRoles().Intersect(x.Roles).Any())
                .Where(x => FilterByPermissions(content, x.Permissions))
                .Select(x => new ODataOperationMethodAction(x));

            return stored.Union(operationMethods).ToArray();
        }

        private bool FilterByApplications(string operationName, ActionBase[] stored)
        {
            for (int i = 0; i < stored.Length; i++)
                if (stored[i].Name == operationName)
                    return false;
            return true;
        }
        private bool FilterByContentTypes(ContentType contentType, string[] allowedContentTypeNames)
        {
            if (allowedContentTypeNames == null)
                return true;

            for (int i = 0; i < allowedContentTypeNames.Length; i++)
            {
                var existingContentType = ContentType.GetByName(allowedContentTypeNames[i]);
                if (existingContentType == null)
                    continue;
                if (contentType.IsInstaceOfOrDerivedFrom(existingContentType.Name))
                    return true;
            }

            return false;
        }
        private bool FilterByPermissions(Content content, string[] permissionNames)
        {
            if (permissionNames == null)
                return true;

            var permissionTypes = permissionNames.Select(PermissionType.GetByName).ToArray();
            return content.ContentHandler.Security.HasPermission(permissionTypes);
        }
    }
}
