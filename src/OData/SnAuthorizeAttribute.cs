using System;
using System.Collections.Generic;
using System.Text;

namespace SenseNet.OData
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class SnAuthorizeAttribute : Attribute
    {
        public string Policy { get; set; }
        public string Role { get; set; }

        public SnAuthorizeAttribute() { }
        public SnAuthorizeAttribute(string policy)
        {
            this.Policy = policy;
        }
    }
}
