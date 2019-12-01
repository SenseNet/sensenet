using System;

// ReSharper disable once CheckNamespace
namespace SenseNet.ApplicationModel
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class ContentTypeAttribute : Attribute
    {
        public string[] Names { get; set; }

        public ContentTypeAttribute(params string[] contentTypeNames)
        {
            Names = contentTypeNames;
        }
    }
}
