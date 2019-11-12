using System;

namespace SenseNet.OData
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class ContentTypeAttribute : Attribute
    {
        public string ContentTypeName { get; set; }

        public ContentTypeAttribute() { }
        public ContentTypeAttribute(string contentType)
        {
            ContentTypeName = contentType;
        }
    }
}
