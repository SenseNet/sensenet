using System;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.OData
{
    public interface IOperationMethodExecutionPolicy
    {
        string Name { get; }
        bool CanExecute(IUser user, OperationCallingContext context);
    }

    internal class InlineOperationMethodExecutionPolicy : IOperationMethodExecutionPolicy
    {
        public string Name { get; }
        public Func<IUser, OperationCallingContext, bool> Callback { get; }

        public bool CanExecute(IUser user, OperationCallingContext context)
        {
            return Callback(user, context);
        }

        public InlineOperationMethodExecutionPolicy(string name, Func<IUser, OperationCallingContext, bool> callback)
        {
            Name = name;
            Callback = callback;
        }
    }
}
