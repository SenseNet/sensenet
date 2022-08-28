﻿using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.OData
{
    public class OperationInspector
    {
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
            if (userId == Identifiers.VisitorUserId && expectedRoles.Contains("/Root/IMS/BuiltIn/Portal/Visitor", StringComparer.InvariantCultureIgnoreCase))
                return true;
            if (expectedRoles.Contains("All"))
                return true;

            if (actualRoles == null)
                actualRoles = NodeHead.Get(Providers.Instance.SecurityHandler.GetGroups()).Select(y => y.Path).ToArray();
            return actualRoles.Intersect(expectedRoles, StringComparer.InvariantCultureIgnoreCase).Any();
        }

        public virtual bool CheckByPermissions(Content content, string[] permissions)
        {
            if (User.Current.Id == Identifiers.SystemUserId)
                return true;

            var permissionTypes = permissions.Select(PermissionType.GetByName).Where(x => x != null).ToArray();
            return permissionTypes.Length == 0 || content.ContentHandler.Security.HasPermission(permissionTypes);
        }

        public virtual OperationMethodVisibility CheckPolicies(string[] policies, OperationCallingContext context)
        {
            if (User.Current.Id == Identifiers.SystemUserId)
                return OperationMethodVisibility.Enabled;

            var visibilityResult = OperationMethodVisibility.Enabled;

            foreach (var policyName in policies)
            {
                if (!OperationCenter.Policies.TryGetValue(policyName, out var policy))
                    throw new UnknownOperationMethodPolicyException("Policy not found: " + policyName);

                // Check visibility according to the current policy. If this policy is more 
                // restrictive than previous ones then degrade the final result value;
                var visibility = policy.GetMethodVisibility(User.Current, context);
                if (visibility < visibilityResult)
                    visibilityResult = visibility;

                // no need to execute remaining policies: the action is already inaccessible
                if (visibilityResult == OperationMethodVisibility.Invisible)
                    return visibilityResult;
            }

            return visibilityResult;
        }
    }
}
