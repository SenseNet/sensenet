using SenseNet.ContentRepository.Storage.Events;

// ReSharper disable once CheckNamespace
namespace SenseNet.Events
{
    /// <summary>
    /// Defines a sensenet event and its base set of data.
    /// </summary>
    public interface ISnEvent
    {
        INodeEventArgs NodeEventArgs { get; }
    }

    /// <summary>
    /// Defines a sensenet event and its specialized set of data.
    /// </summary>
    /// <typeparam name="T">The type of the set of data class (<see cref="INodeEventArgs"/> or
    /// any inherited class).</typeparam>
    public interface ISnEvent<out T> : ISnEvent where T : INodeEventArgs
    {
        T EventArgs { get; }
    }
}
