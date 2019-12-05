using System.Collections.Generic;
using SenseNet.ContentRepository;

// ReSharper disable once CheckNamespace
namespace SenseNet.ApplicationModel
{
    /// <summary>
    /// Defines methods for serving in-memory action implementations.
    /// </summary>
    public interface IOperationMethodStorage
    {
        /// <summary>
        /// Gets an aggregated list of stored and in-memory actions.
        /// This method will return the provided stored action list extended with
        /// additional in-memory operations.
        /// </summary>
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
