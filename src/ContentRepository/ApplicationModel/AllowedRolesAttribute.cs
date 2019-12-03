using System;

// ReSharper disable once CheckNamespace
namespace SenseNet.ApplicationModel
{
    /// <summary>
    /// Declares an attribute that describes the allowed roles for an operation method.
    /// The annotated operation method can be called if the current user has at least one of them.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class AllowedRolesAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets one or more role names.
        /// The annotated operation method can be called if the current user has at least one of them.
        /// </summary>
        public string[] Names { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AllowedRolesAttribute"/>.
        /// </summary>
        public AllowedRolesAttribute() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="AllowedRolesAttribute"/> with one or more policies.
        /// </summary>
        /// <param name="roleNames">One or more role names.
        /// The annotated operation method can be called if the current user has at least one of them.
        /// </param>
        public AllowedRolesAttribute(params string[] roleNames)
        {
            Names = roleNames;
        }
    }
}
