using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using SenseNet.ApplicationModel;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.OData
{
    internal class OperationMethodStorage : IOperationMethodStorage
    {
        private readonly OperationInspector _inspector;

        public OperationMethodStorage(OperationInspector inspector)
        {
            _inspector = inspector;
        }

        public IEnumerable<ActionBase> GetActions(IEnumerable<ActionBase> storedActions, Content content, string scenario, object state)
        {
            var actualRoles =  NodeHead.Get(Providers.Instance.SecurityHandler.GetGroups()).Select(y => y.Path).ToArray();
            var inspector = _inspector;
            var stored = storedActions.ToArray();

            var operations = OperationCenter.Operations
                .SelectMany(x => x.Value)
                .Where(x => FilterByApplications(x.Name, stored))
                .Where(x => FilterByScenario(x.Scenarios, scenario))
                .Where(x => FilterByContentTypes(content, x.ContentTypes))
                .Where(x => FilterByRoles(x.Roles, actualRoles))
                .ToArray();

            var actions = operations
                .Select(x => new ODataOperationMethodDescriptor(x, GenerateUri(content, x.Name)))
                .Where(a => Initialize(a, content))
                .Where(a => FilterByPermissions(a, content))
                .Where(a => FilterByPolicies(a, content, state))
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
        private bool FilterByContentTypes(Content content, string[] allowedContentTypeNames)
        {
            if (allowedContentTypeNames == null || allowedContentTypeNames.Length == 0)
                return true;

            return _inspector.CheckByContentType(content, allowedContentTypeNames);
        }
        private bool FilterByRoles(string[] expectedRoles, IEnumerable<string> actualRoles)
        {
            if (expectedRoles.Length == 0)
                return true;
            return _inspector.CheckByRoles(expectedRoles, actualRoles);
        }
        private bool FilterByPolicies(ODataOperationMethodDescriptor action, Content content, object state)
        {
            if (action.OperationInfo.Policies.Length == 0)
                return true;

            var httpContext = state as HttpContext; // httpContext can be null
            var context = new OperationCallingContext(content, action.OperationInfo){ HttpContext = httpContext};

            switch (_inspector.CheckPolicies(action.OperationInfo.Policies, context))
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
        private bool FilterByPermissions(ODataOperationMethodDescriptor action, Content content)
        {
            action.Forbidden |= !IsPermitted(content, action.OperationInfo.Permissions);

            return true;
        }
        private bool IsPermitted(Content content, string[] permissionNames)
        {
            if (permissionNames == null || permissionNames.Length == 0)
                return true;
            return _inspector.CheckByPermissions(content, permissionNames);
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
