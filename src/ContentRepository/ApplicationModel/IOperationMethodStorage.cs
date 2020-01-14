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
        /// <param name="storedActions">Actions loaded from the database.</param>
        /// <param name="content">Related <see cref="Content"/>.</param>
        /// <param name="scenario">Optional scenario filter. Can be null.</param>
        /// <param name="state">A state object. Pass the current HttpContext instance if it is available.</param>
        IEnumerable<ActionBase> GetActions(IEnumerable<ActionBase> storedActions, Content content, string scenario, object state = null);
    }

    internal class DefaultOperationMethodStorage : IOperationMethodStorage
    {
        public IEnumerable<ActionBase> GetActions(IEnumerable<ActionBase> storedActions, Content content, string scenario, object state = null)
        {
            return storedActions;
        }
    }
}
