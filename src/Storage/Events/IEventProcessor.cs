using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace SenseNet.Events
{
    /// <summary>
    /// Defines a class that can process a sensenet event (<see cref="ISnEvent"/>).
    /// </summary>
    public interface IEventProcessor
    {
        /// <summary>
        /// Processes the given sensenet event (<see cref="ISnEvent"/>).
        /// </summary>
        /// <param name="snEvent">The event.</param>
        /// <param name="cancel">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task ProcessEventAsync(ISnEvent snEvent, CancellationToken cancel);
    }
}
