using System;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.OData
{
    public enum OperationMethodVisibility
    {
        Invisible,
        Disabled,
        Enabled
    }

    /// <summary>
    /// Defines methods and properties for a custom operation method policy. Implement this when
    /// you need to create a reusable or complex policy. Otherwise use a simple function
    /// for adding a custom policy.
    /// </summary>
    public interface IOperationMethodPolicy
    {
        /// <summary>
        /// Name of the policy.
        /// </summary>
        string Name { get; }
        /// <summary>
        /// Determines whether the operation should be accessible for the current user
        /// in the current context.
        /// </summary>
        /// <param name="user">The user to check for.</param>
        /// <param name="context">Current context, including the content to check for.</param>
        /// <returns>The visibility level of the action.</returns>
        OperationMethodVisibility GetMethodVisibility(IUser user, OperationCallingContext context);
    }

    internal class InlineOperationMethodPolicy : IOperationMethodPolicy
    {
        public string Name { get; }
        public Func<IUser, OperationCallingContext, OperationMethodVisibility> Callback { get; }

        public OperationMethodVisibility GetMethodVisibility(IUser user, OperationCallingContext context)
        {
            return Callback(user, context);
        }

        public InlineOperationMethodPolicy(string name, Func<IUser, OperationCallingContext, OperationMethodVisibility> callback)
        {
            Name = name;
            Callback = callback;
        }
    }
}
