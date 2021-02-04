using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace SenseNet.Events
{
    /// <summary>
    /// Defines a class that can process a sensenet event (<see cref="ISnEvent"/>).
    /// </summary>
    public interface IEventProcessor
    {
        //UNDONE: [event] this method should get a CancellationToken parameter

        /// <summary>
        /// Processes the given sensenet event (<see cref="ISnEvent"/>).
        /// </summary>
        /// <param name="snEvent">The event.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task ProcessEventAsync(ISnEvent snEvent);
    }
}
