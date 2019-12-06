using System.Collections.Generic;
using System.Linq;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.OData
{
    internal class OperationInspector
    {
        public static OperationInspector Instance { get; set; } = new OperationInspector();

        public virtual bool CheckByContentType(Content content, string[] contentTypes)
        {
            var contentType = content.ContentType;
            for (var i = 0; i < contentTypes.Length; i++)
            {
                var existingContentType = ContentType.GetByName(contentTypes[i]);
                if (existingContentType == null)
                    continue;
                if (contentType.IsInstaceOfOrDerivedFrom(existingContentType.Name))
                    return true;
            }

            return false;
        }

        public virtual bool CheckByRoles(string[] expectedRoles, IEnumerable<string> actualRoles = null)
        {
            var userId = User.Current.Id;
            if (User.Current.Id == Identifiers.SystemUserId)
                return true;
            if (userId == Identifiers.VisitorUserId && expectedRoles.Contains("Visitor"))
                return true;
            if (expectedRoles.Contains("All"))
                return true;

            if (actualRoles == null)
                actualRoles = NodeHead.Get(SecurityHandler.GetGroups()).Select(y => y.Name).ToArray();
            return actualRoles.Intersect(expectedRoles).Any();
        }

        public virtual bool CheckByPermissions(Content content, string[] permissions)
        {
            if (User.Current.Id == Identifiers.SystemUserId)
                return true;

            var permissionTypes = permissions.Select(PermissionType.GetByName).Where(x => x != null).ToArray();
            return permissionTypes.Length == 0 || content.ContentHandler.Security.HasPermission(permissionTypes);
        }

        public virtual bool CheckPolicies(string[] policies, OperationCallingContext context)
        {
            if (User.Current.Id == Identifiers.SystemUserId)
                return true;

            foreach (var policyName in policies)
            {
                if (!OperationCenter.Policies.TryGetValue(policyName, out var policy))
                    throw new UnknownOperationMethodExecutionPolicyException("Policy not found: " + policyName);
                if (!policy.CanExecute(User.Current, context))
                    return false;
            }

            return true;
        }

    }
}
