using System.Collections.Generic;
using System.Linq;
using System.Xml.Schema;
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
            var actualRoles =  NodeHead.Get(SecurityHandler.GetGroups()).Select(y => y.Name).ToArray();

            var inspector = OperationInspector.Instance;

            var stored = storedActions.ToArray();
            var operationMethodActions = OperationCenter.Operations
                .SelectMany(x => x.Value)
                .Where(x => FilterByApplications(x.Method.Name, stored))
                .Where(x => FilterByContentTypes(inspector, content, x.ContentTypes))
                .Where(x => FilterByRoles(inspector, x.Roles, actualRoles))
                .Select(x => new ODataOperationMethodAction(x, GenerateUri(content, x.Method.Name)))
                .ToArray();

            foreach (var operationMethodAction in operationMethodActions)
            {
                operationMethodAction.Initialize(content, null, null, null);
                operationMethodAction.Forbidden = !IsPermitted(inspector, content, operationMethodAction.OperationInfo.Permissions);
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
        private bool FilterByContentTypes(OperationInspector inspector, Content content, string[] allowedContentTypeNames)
        {
            if (allowedContentTypeNames == null || allowedContentTypeNames.Length == 0)
                return true;

            return inspector.CheckByContentType(content, allowedContentTypeNames);
        }
        private bool FilterByRoles(OperationInspector inspector, string[] expectedRoles, IEnumerable<string> actualRoles)
        {
            if (expectedRoles.Length == 0)
                return true;
            return inspector.CheckByRoles(expectedRoles, actualRoles);
        }
        private bool IsPermitted(OperationInspector inspector, Content content, string[] permissionNames)
        {
            if (permissionNames == null || permissionNames.Length == 0)
                return true;
            return inspector.CheckByPermissions(content, permissionNames);
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
