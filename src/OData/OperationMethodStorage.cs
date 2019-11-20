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
                return actualRoles ??
                       (actualRoles = NodeHead.Get(SecurityHandler.GetGroups()).Select(y => y.Name).ToArray());
            }

            var stored = storedActions.ToArray();
            var operationMethodActions = OperationCenter.Operations
                .SelectMany(x => x.Value)
                .Where(x => FilterByApplications(x.Method.Name, stored))
                .Where(x => FilterByContentTypes(content.ContentType, x.ContentTypes))
                .Where(x => x.Roles.Length == 0 || GetRoles().Intersect(x.Roles).Any())
                .Select(x => new ODataOperationMethodAction(x, GenerateUri(content, x.Method.Name)))
                .ToArray();

            foreach (var operationMethodAction in operationMethodActions)
            {
                operationMethodAction.Initialize(content, null, null, null);
                operationMethodAction.Forbidden = !IsPermitted(content, operationMethodAction.OperationInfo.Permissions);
            }

            return stored.Union(operationMethodActions).ToArray();
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
            if (allowedContentTypeNames == null || allowedContentTypeNames.Length == 0)
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
        private bool IsPermitted(Content content, string[] permissionNames)
        {
            if (permissionNames == null || permissionNames.Length == 0)
                return true;

            var permissionTypes = permissionNames.Select(PermissionType.GetByName).ToArray();
            return content.ContentHandler.Security.HasPermission(permissionTypes);
        }

        internal static string GenerateUri(Content content, string operationName)
        {
            var resource = string.IsNullOrEmpty(content.ContentHandler.ParentPath)
            ? Configuration.Services.ODataAndRoot
            : $"{Configuration.Services.ODataServiceToken}" +
              $"{ODataMiddleware.GetODataPath(content.ContentHandler.ParentPath, content.Name)}";
            return $"/{resource}/{operationName}";
        }
    }
}
