using System;

namespace SenseNet.OData
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class ContentTypeAttribute : Attribute
    {
        public string ContentTypeName { get; }

        public ContentTypeAttribute(string contentTypeName)
        {
            ContentTypeName = contentTypeName;
        }
    }
}
