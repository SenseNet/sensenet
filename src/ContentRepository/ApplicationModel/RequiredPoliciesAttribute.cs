using System;

// ReSharper disable once CheckNamespace
namespace SenseNet.ApplicationModel
{
    /// <summary>
    /// Describes the allowed policies for an operation method.
    /// A policy is a code fragment identified by the name provided using this attribute.
    /// Use the UseOperationMethodExecutionPolicy methods for defining policies during app start.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class RequiredPoliciesAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets one or more policy names that will be executed before calling this method.
        /// Policies need to be registered during the startup sequence of the application.
        /// </summary>
        public string[] Names { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequiredPoliciesAttribute"/> class.
        /// </summary>
        public RequiredPoliciesAttribute() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="RequiredPoliciesAttribute"/> class with one or more policies.
        /// </summary>
        /// <param name="policyNames">One or more policy names.
        /// The operation method can be called after the policies are successfully executed.
        /// </param>
        public RequiredPoliciesAttribute(params string[] policyNames)
        {
            Names = policyNames;
        }
    }
}
