using System;

// ReSharper disable once CheckNamespace
namespace SenseNet.ApplicationModel
{
    /// <summary>
    /// Declares required permissions for an operation method.
    /// The annotated operation method can be called if the current user has all permission on the requested content.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class RequiredPermissionsAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets one or more comma separated <see cref="SenseNet.ContentRepository.Storage.Security.PermissionType"/> names.
        /// The annotated operation method can be called if the current user has all permission on the requested content.
        /// </summary>
        public string[] Names { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequiredPermissionsAttribute"/>.
        /// </summary>
        public RequiredPermissionsAttribute() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="RequiredPermissionsAttribute"/> with one or more policies.
        /// </summary>
        /// <param name="permissions">One or more <see cref="SenseNet.ContentRepository.Storage.Security.PermissionType"/> name.
        /// The annotated operation method can be called if the current user has all permission on the requested content.
        /// </param>
        public RequiredPermissionsAttribute(params string[] permissions)
        {
            Names = permissions;
        }
    }
}
