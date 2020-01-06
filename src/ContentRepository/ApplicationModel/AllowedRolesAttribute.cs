using System;

// ReSharper disable once CheckNamespace
namespace SenseNet.ApplicationModel
{
    /// <summary>
    /// Describes the allowed roles for an operation method.
    /// The annotated operation method can be called only if the current user has at least one of them.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class AllowedRolesAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets role names.
        /// </summary>
        public string[] Names { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AllowedRolesAttribute"/> class with one or more roles.
        /// </summary>
        /// <param name="roleNames">One or more role names.
        /// The annotated operation method can be called only if the current user has at least one of them.
        /// </param>
        public AllowedRolesAttribute(params string[] roleNames)
        {
            Names = roleNames;
        }
    }
}
