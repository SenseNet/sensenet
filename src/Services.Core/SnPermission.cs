using System;
using SenseNet.Security;
using System.Diagnostics;

// ReSharper disable once CheckNamespace
namespace SenseNet.Services
{
    [DebuggerDisplay("{ToString()}")]
    [Serializable]
    public class SnPermission
    {
        public string Name { get; set; }
        public bool Allow { get; set; }
        public bool Deny { get; set; }
        public string AllowFrom { get; set; }
        public string DenyFrom { get; set; }
        
        public bool AllowEnabled
        {
            get { return string.IsNullOrEmpty(this.AllowFrom); }
        }
        public bool DenyEnabled
        {
            get { return string.IsNullOrEmpty(this.DenyFrom); }
        }

        public PermissionValue ToPermissionValue()
        {
            if (Deny)
                return PermissionValue.Denied;
            if (Allow)
                return PermissionValue.Allowed;
            return PermissionValue.Undefined;
        }
        public override string ToString()
        {
            return String.Format("{0} Allow: {1}, Deny: {2}, AllowFrom: {3}, DenyFrom: {4}", Name, Allow, Deny, AllowFrom, DenyFrom);
        }
    }
}
