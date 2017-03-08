using System;

namespace SenseNet.ContentRepository.Schema
{
    [global::System.AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class FieldDataTypeAttribute : Attribute
    {
        public Type DataType { get; set; }

        public FieldDataTypeAttribute(Type dataType)
        {
            this.DataType = dataType;
        }
    }
}
