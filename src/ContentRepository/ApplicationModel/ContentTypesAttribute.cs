using System;

// ReSharper disable once CheckNamespace
namespace SenseNet.ApplicationModel
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ContentTypesAttribute : Attribute
    {
        public string[] Names { get; set; }

        public ContentTypesAttribute(params string[] contentTypeNames)
        {
            Names = contentTypeNames;
        }
    }
}
