using System;

namespace SenseNet.ApplicationModel
{
    [AttributeUsage(AttributeTargets.Method)]
    public abstract class ODataOperation : Attribute
    {
        public string Description { get; }

        public ODataOperation(string description = null)
        {
            Description = description;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class ODataAction : ODataOperation
    {
        public ODataAction(string description = null) : base(description) { }
    }
    [AttributeUsage(AttributeTargets.Method)]
    public class ODataFunction : ODataOperation
    {
        public ODataFunction(string description = null) : base(description) { }
    }

}
