using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
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
                .Where(x => FilterByApplications(x.Name, stored))
                .Where(x => FilterByScenario(x.Scenarios, scenario))
                .Where(x => FilterByContentTypes(inspector, content, x.ContentTypes))
                .Where(x => FilterByRoles(inspector, x.Roles, actualRoles))
                .Select(x => new ODataOperationMethodAction(x, GenerateUri(content, x.Name)))
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
            var comparison = OperationCenter.IsCaseInsensitiveOperationNameEnabled
                ? StringComparison.InvariantCultureIgnoreCase 
                : StringComparison.InvariantCulture;

            for (var i = 0; i < stored.Length; i++)
                if (stored[i].Name.Equals(operationName, comparison))
                    return false;

            return true;
        }
        private bool FilterByScenario(string[] allowedScenarios, string scenario)
        {
            if (string.IsNullOrEmpty(scenario))
                return true;
            if (allowedScenarios == null || allowedScenarios.Length == 0)
                return false;
            return allowedScenarios.Contains(scenario, StringComparer.InvariantCultureIgnoreCase);
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
