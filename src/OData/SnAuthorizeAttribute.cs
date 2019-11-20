using System;
using System.Collections.Generic;
using System.Text;

namespace SenseNet.OData
{
    /// <summary>
    /// Declares authorization rules for an OData Operation Method. Available rule categories: Policy, Role, Permission
    /// </summary>
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

        /// <summary>
        /// Initializes a new instance of the <see cref="SnAuthorizeAttribute"/>.
        /// </summary>
        public SnAuthorizeAttribute() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="SnAuthorizeAttribute"/> with one or more policies.
        /// </summary>
        /// <param name="policy">One or more comma separated role names.
        /// The method can be called if the current user has at least one of them.
        /// </param>
        public SnAuthorizeAttribute(string policy)
        {
            this.Policy = policy;
        }
    }
}
