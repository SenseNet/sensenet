using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace SenseNet.Events
{
    /// <summary>
    /// Defines a class that distributes any <see cref="ISnEvent"/> among the <see cref="IEventProcessor"/>
    /// and the ald school NodeObservers.
    /// </summary>
    public interface IEventDistributor
    {
        /// <summary>
        /// Gets or sets the <see cref="AuditLogEventProcessor"/> implementations.
        /// </summary>
        IEventProcessor AuditLogEventProcessor { get; set; }
        /// <summary>
        /// Gets or sets a set of "Fire And Go" style <see cref="IEventProcessor"/> implementations.
        /// </summary>
        IEventProcessor[] AsyncEventProcessors { get; set; }
        /// <summary>
        /// Fires an <see cref="ISnCancellableEvent"/> event on all old school NodeObservers except the given observers.
        /// Waits for all observer's execution and signs with 'true' value if any observer cancels the operation.
        /// This method always calls all related observers even if any of them cancels or throws an exception.
        /// This method does not call any <see cref="IEventProcessor"/> except the AuditLogEventProcessor if
        /// the event is an <see cref="IAuditLogEvent"/>.
        /// </summary>
        /// <param name="snEvent">A cancellable event representation.</param>
        /// <param name="disabledNodeObservers">A list of NodeObserver types that will not be called.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the cancellation state.</returns>
        Task<bool> FireCancellableNodeObserverEventEventAsync(ISnCancellableEvent snEvent, List<Type> disabledNodeObservers);

        /// <summary>
        /// Fires an <see cref="INodeObserverEvent"/> event on all old school NodeObservers except the given observers.
        /// This method always calls all related observers even if any of them throws an exception.
        /// All known <see cref="IEventProcessor"/> stored in the AsyncEventProcessors property) and AuditLogEventProcessor are called.
        /// The returned task is completed when the AuditLogEventProcessor and all NodeObservers are finished.
        /// </summary>
        /// <param name="snEvent">An <see cref="INodeObserverEvent"/> event representation.</param>
        /// <param name="disabledNodeObservers">A list of NodeObserver types that will not be called.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task FireNodeObserverEventEventAsync(INodeObserverEvent snEvent, List<Type> disabledNodeObservers);

        /// <summary>
        /// Fires an <see cref="ISnEvent"/> event on all known <see cref="IEventProcessor"/> (stored in the
        /// AsyncEventProcessors property) and AuditLogEventProcessor.
        /// The returned task is completed when the AuditLogEventProcessor finishes.
        /// </summary>
        /// <param name="snEvent">An <see cref="ISnEvent"/> event representation.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task FireEventAsync(ISnEvent snEvent);
    }
}
