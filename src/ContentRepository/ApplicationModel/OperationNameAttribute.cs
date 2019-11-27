using System;

// ReSharper disable once CheckNamespace
namespace SenseNet.ApplicationModel
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
