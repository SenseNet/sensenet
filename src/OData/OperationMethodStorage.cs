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

            var operations = OperationCenter.Operations
                .SelectMany(x => x.Value)
                .Where(x => FilterByApplications(x.Name, stored))
                .Where(x => FilterByScenario(x.Scenarios, scenario))
                .Where(x => FilterByContentTypes(inspector, content, x.ContentTypes))
                .Where(x => FilterByRoles(inspector, x.Roles, actualRoles))
                .ToArray();

            var actions = operations
                .Select(x => new ODataOperationMethodDescriptor(x, GenerateUri(content, x.Name)))
                .Where(a => Initialize(a, content))
                .Where(a => FilterByPermissions(inspector, a, content))
                .Where(a => FilterByPolicies(inspector, a, content))
                .ToArray();

            return stored.Union(actions).ToArray();
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
        private bool FilterByPolicies(OperationInspector inspector, ODataOperationMethodDescriptor action, Content content)
        {
            if (action.OperationInfo.Policies.Length == 0)
                return true;

            //UNDONE: set HttpContext in OperationCallingContext
            var context = new OperationCallingContext(content, action.OperationInfo);

            switch (inspector.CheckPolicies(action.OperationInfo.Policies, context))
            {
                case OperationMethodVisibility.Invisible:
                    return false;
                case OperationMethodVisibility.Disabled:
                    // according to the policy this action is visible to the user but cannot be executed
                    action.Forbidden = true;
                    break;
                case OperationMethodVisibility.Enabled:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return true;
        }
        private bool Initialize(ActionBase action, Content content)
        {
            action.Initialize(content, null, null, null);
            return action.Visible;
        }
        private bool FilterByPermissions(OperationInspector inspector, ODataOperationMethodDescriptor action, Content content)
        {
            action.Forbidden |= !IsPermitted(inspector, content, action.OperationInfo.Permissions);

            return true;
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
