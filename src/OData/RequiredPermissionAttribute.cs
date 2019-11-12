using System;

namespace SenseNet.OData
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class RequiredPermissionAttribute : Attribute
    {
        public string Permission { get; set; }

        public RequiredPermissionAttribute() { }
        public RequiredPermissionAttribute(string permission)
        {
            Permission = permission;
        }
    }
}
