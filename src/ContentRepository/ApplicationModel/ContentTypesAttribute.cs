using System;

// ReSharper disable once CheckNamespace
namespace SenseNet.ApplicationModel
{
    /// <summary>
    /// Describes the allowed content types for an operation method.
    /// The annotated operation method can be called only on contents of the specified types.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ContentTypesAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets content type names.
        /// </summary>
        public string[] Names { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentTypesAttribute"/> class with one or more content type names.
        /// </summary>
        /// <param name="contentTypeNames">One or more content type names.
        /// The annotated operation method can be called only on contents of the specified types.
        /// </param>
        public ContentTypesAttribute(params string[] contentTypeNames)
        {
            Names = contentTypeNames;
        }
    }
}
