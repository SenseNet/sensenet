using System;

namespace SenseNet.OData
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class OperationNameAttribute : Attribute
    {
        public string Name { get; }

        public OperationNameAttribute() { }
        public OperationNameAttribute(string operationName)
        {
            Name = operationName;
        }
    }
}
