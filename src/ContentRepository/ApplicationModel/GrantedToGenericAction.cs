using System;

// ReSharper disable once CheckNamespace
namespace SenseNet.ApplicationModel
{
    public abstract class ODataOperationAttribute : Attribute
    {
        public abstract bool CauseStateChange { get; }

        public string OperationName { get; set; }

        protected ODataOperationAttribute() {}

        protected ODataOperationAttribute(string operationName)
        {
            OperationName = operationName;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class ODataAction : ODataOperationAttribute
    {
        public override bool CauseStateChange => true;
        public ODataAction() { }
        public ODataAction(string operationName) : base(operationName) { }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class ODataFunction : ODataOperationAttribute
    {
        public override bool CauseStateChange => false;
        public ODataFunction() { }
        public ODataFunction(string operationName) : base(operationName) { }
    }

}
