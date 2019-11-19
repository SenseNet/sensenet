using System.Collections.Generic;
using SenseNet.ContentRepository;

// ReSharper disable once CheckNamespace
namespace SenseNet.ApplicationModel
{
    public interface IOperationMethodStorage
    {
        IEnumerable<ActionBase> GetActions(IEnumerable<ActionBase> storedActions, Content context, string scenario);
    }

    internal class DefaultOperationMethodStorage : IOperationMethodStorage
    {
        public IEnumerable<ActionBase> GetActions(IEnumerable<ActionBase> storedActions, Content context, string scenario)
        {
            return storedActions;
        }
    }
}
