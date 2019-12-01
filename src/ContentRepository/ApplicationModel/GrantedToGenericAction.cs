using System;

// ReSharper disable once CheckNamespace
namespace SenseNet.ApplicationModel
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ODataAction : Attribute
    {
        public string OperationName { get; set; }
        public ODataAction()
        {
        }
        public ODataAction(string operationName)
        {
            OperationName = operationName;
        }
    }
    [AttributeUsage(AttributeTargets.Method)]
    public class ODataFunction : Attribute
    {
        public string OperationName { get; set; }
        public ODataFunction()
        {
        }
        public ODataFunction(string operationName)
        {
            OperationName = operationName;
        }
    }

}
