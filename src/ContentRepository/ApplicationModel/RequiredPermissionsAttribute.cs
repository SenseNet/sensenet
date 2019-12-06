using System;

// ReSharper disable once CheckNamespace
namespace SenseNet.ApplicationModel
{
    /// <summary>
    /// Describes the required permissions for an operation method.
    /// The annotated operation method can be called only if the current user has
    /// all the defined permissions on the requested content.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class RequiredPermissionsAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets one or more <see cref="SenseNet.ContentRepository.Storage.Security.PermissionType"/> names.
        /// </summary>
        public string[] Names { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequiredPermissionsAttribute"/> class with one or more permissions.
        /// </summary>
        /// <param name="permissions">One or more <see cref="SenseNet.ContentRepository.Storage.Security.PermissionType"/> names.
        /// The annotated operation method can be called only if the current user has
        /// all the defined permissions on the requested content.
        /// </param>
        public RequiredPermissionsAttribute(params string[] permissions)
        {
            Names = permissions;
        }
    }
}
