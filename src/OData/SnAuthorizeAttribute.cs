using System;
using System.Collections.Generic;
using System.Text;

namespace SenseNet.OData
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class SnAuthorizeAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets one or more comma separated policy names that will be executed before calling this method.
        /// Policies need to be registered during the startup sequence of the application.
        /// </summary>
        public string Policy { get; set; }
        /// <summary>
        /// Gets or sets one or more comma separated role names.
        /// The method can be called if the current user has at least one of them.
        /// </summary>
        public string Role { get; set; }
        /// <summary>
        /// Gets or sets one or more comma separated permission names.
        /// The method can be called if the current user has all permission on the requested content.
        /// </summary>
        public string Permission { get; set; }

        public SnAuthorizeAttribute() { }
        public SnAuthorizeAttribute(string policy)
        {
            this.Policy = policy;
        }
    }
}
