using System;
using System.Linq;
using System.Web;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.OData.Operations
{
    public class HasPermissionAction : ActionBase
    {
        public override string Uri { get; } = null;
        public bool IsReusable { get; } = true;


        public override bool IsHtmlOperation { get; } = false;
        public override bool IsODataOperation { get; } = true;
        public override bool CausesStateChange { get; } = false;

        public override ActionParameter[] ActionParameters { get; } = {
            new ActionParameter("user", typeof (string)),
            new ActionParameter("permissions", typeof (string[]), true)
        };

        public override object Execute(Content content, params object[] parameters)
        {
            var userParamValue = parameters[0] == Type.Missing ? null : (string)parameters[0];
            var permissionsParamValue = (string[])parameters[1];
            IUser user = null;
            if (!string.IsNullOrEmpty(userParamValue))
            {
                user = Node.Load<User>(userParamValue);
                if (user == null)
                    throw new ContentNotFoundException("Identity not found: " + userParamValue);
            }

            // ReSharper disable once NotResolvedInText
            if (permissionsParamValue == null)
                throw new ArgumentNullException("permissions");

            var permissionNames = permissionsParamValue;
            var permissions = permissionNames.Select(GetPermissionTypeByName).ToArray();

            return user == null 
                ? content.Security.HasPermission(permissions) 
                : content.Security.HasPermission(user, permissions);
        }
        private static PermissionType GetPermissionTypeByName(string name)
        {
            var permissionType = PermissionType.GetByName(name);
            if (permissionType != null)
                return permissionType;
            throw new ArgumentException("Unknown permission: " + name);
        }
    }
}
