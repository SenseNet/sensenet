using System;

// ReSharper disable once CheckNamespace
namespace SenseNet.ApplicationModel
{
    /// <summary>
    /// Abstract base class for OData action and function attributes.
    /// </summary>
    public abstract class ODataOperationAttribute : Attribute
    {
        public abstract bool CauseStateChange { get; }

        public string OperationName { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }

        protected ODataOperationAttribute() {}

        protected ODataOperationAttribute(string operationName)
        {
            OperationName = operationName;
        }
    }

    /// <summary>
    /// Declares an operation as accessible through the OData protocol.
    /// These methods cause state change in the repository and should be called only using a POST request.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ODataAction : ODataOperationAttribute
    {
        public override bool CauseStateChange => true;
        /// <summary>
        /// Initializes a new instance of the <see cref="ODataAction"/> class.
        /// </summary>
        public ODataAction() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="ODataAction"/> class.
        /// The provided operation name may be different from the method name and
        /// defines the way this method can be called from the client.
        /// </summary>
        public ODataAction(string operationName) : base(operationName) { }
    }

    /// <summary>
    /// Declares an operation as accessible through the OData protocol.
    /// These methods do not cause state change in the repository and can be called using a GET request.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ODataFunction : ODataOperationAttribute
    {
        public override bool CauseStateChange => false;
        /// <summary>
        /// Initializes a new instance of the <see cref="ODataFunction"/> class.
        /// </summary>
        public ODataFunction() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="ODataFunction"/> class.
        /// The provided operation name may be different from the method name and
        /// defines the way this method can be called from the client.
        /// </summary>
        public ODataFunction(string operationName) : base(operationName) { }
    }
}
