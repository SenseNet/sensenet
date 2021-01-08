using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.Diagnostics;

// ReSharper disable once CheckNamespace
namespace SenseNet.Events
{
    public interface ISnEvent { }
    public interface ISnEvent<out T> : ISnEvent where T : INodeEventArgs { T EventArgs { get; } }
    public interface ISnCancellableEvent : INodeObserverEvent { CancellableNodeEventArgs CancellableEventArgs { get; } }
    public interface ISnCancellableEvent<out T> : ISnCancellableEvent, ISnEvent<T> where T : CancellableNodeEventArgs { }

    public interface INodeObserverEvent : ISnEvent { Action<NodeObserver> NodeObserverAction { get; } }
    public interface IAuditLogEvent : ISnEvent { AuditEvent AuditEvent { get; } }

    public class NodeModifyingEvent : ISnCancellableEvent<CancellableNodeEventArgs>
    {
        public NodeModifyingEvent(CancellableNodeEventArgs args)
        {
            EventArgs = args;
        }

        CancellableNodeEventArgs ISnCancellableEvent.CancellableEventArgs => EventArgs;
        public CancellableNodeEventArgs EventArgs { get; }

        public Action<NodeObserver> NodeObserverAction => observer =>
        {
            observer.OnNodeModifying(null, (CancellableNodeEventArgs) EventArgs);
        };
    }
    public class NodeModifiedEvent : ISnEvent<NodeEventArgs>, INodeObserverEvent, IAuditLogEvent
    {
        public AuditEvent AuditEvent => AuditEvent.ContentUpdated;

        public NodeModifiedEvent(NodeEventArgs args)
        {
            EventArgs = args;
        }

        public NodeEventArgs EventArgs { get; }

        public Action<NodeObserver> NodeObserverAction => observer =>
        {
            observer.OnNodeModified(null, EventArgs);
        };
    }

    public interface IEventDistributor
    {
        IEventProcessor[] AsyncEventProcessors { get; set; }
        Task<bool> FireCancellableNodeObserverEventEventAsync(ISnCancellableEvent snEvent, List<Type> disabledNodeObservers);
        Task FireNodeObserverEventEventAsync(INodeObserverEvent snEvent, List<Type> disabledNodeObservers);
        Task FireEventAsync(ISnEvent snEvent);
    }
    public class EventDistributor : IEventDistributor
    {
        //UNDONE:<? Use DependencyInjection
        public AuditLogEventProcessor AuditLogEventProcessor = new AuditLogEventProcessor();
        //UNDONE:<? Use DependencyInjection
        public IEventProcessor[] AsyncEventProcessors { get; set; } = {
            new PushNotificationEventProcessor(), new WebHookEventProcessor(), new EmailSenderEventProcessor()
        };

        public Task<bool> FireCancellableNodeObserverEventEventAsync(ISnCancellableEvent snEvent, List<Type> disabledNodeObservers)
        {
            // Cancellable event is used only the NodeObserver infrastructure
            return CallNodeObserversAsync(snEvent, disabledNodeObservers);
        }
        public Task FireNodeObserverEventEventAsync(INodeObserverEvent snEvent, List<Type> disabledNodeObservers)
        {
            // Call observers but do not wait
            var nodeObserverTask = CallNodeObserversAsync(snEvent, disabledNodeObservers);

            // Call forward to work with more event processor
            var task = FireEventAsync(snEvent, nodeObserverTask);
            return task;
        }
        public Task FireEventAsync(ISnEvent snEvent)
        {
            return FireEventAsync(snEvent, null);
        }

        private async Task FireEventAsync(ISnEvent snEvent, Task nodeObserverTask)
        {
            var syncTasks = new List<Task>();
            if(nodeObserverTask != null)
                syncTasks.Add(nodeObserverTask);

            // If the event is IAuditLogEvent, call async, memorize it and do not wait.
            if (snEvent is IAuditLogEvent auditLogEvent)
                syncTasks.Add(AuditLogEventProcessor.ProcessEventAsync(auditLogEvent));

            // Call all async processors and forget them
            //var _ = AsyncEventProcessors.Select(x => Task.Run(() => { x.ProcessEvent(snEvent); }) ).ToArray();
            foreach (var processor in AsyncEventProcessors)
                #pragma warning disable 4014
                processor.ProcessEventAsync(snEvent);
                #pragma warning restore 4014

            // Wait for all synchronous tasks.
            if (syncTasks.Count > 0)
                await Task.WhenAll(syncTasks.ToArray()).ConfigureAwait(false);
        }
        private async Task<bool> CallNodeObserversAsync(ISnCancellableEvent snEvent, List<Type> disabledNodeObservers)
        {
            var tasks = Providers.Instance.NodeObservers
                .Where(x => !disabledNodeObservers?.Contains(x.GetType()) ?? true)
                .Select(x => FireCancellableNodeObserverEventAsync(snEvent, x))
                .ToArray();

            await Task.WhenAll<bool>(tasks).ConfigureAwait(false);
            var canceled = tasks.Any(t => t.Result == true);

            return canceled;
        }
        private async Task CallNodeObserversAsync(INodeObserverEvent snEvent, List<Type> disabledNodeObservers)
        {
            var tasks = Providers.Instance.NodeObservers
                .Where(x => !disabledNodeObservers?.Contains(x.GetType()) ?? true)
                .Select(x => FireNodeObserverEventAsync(snEvent, x))
                .ToArray();

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        private async Task<bool> FireCancellableNodeObserverEventAsync(ISnCancellableEvent snEvent,
            NodeObserver nodeObserver, CancellationToken cancel = default)
        {
            //snEvent.NodeObserverAction(nodeObserver);
            // NodeObserverAction simulation
            using (var op = SnTrace.StartOperation(
                $"NodeObserverAction simulation: {snEvent.GetType().Name} {nodeObserver.GetType().Name}"))
            {
                await Task.Delay(10, cancel).ConfigureAwait(false);
                op.Successful = true;
            }
            return snEvent.CancellableEventArgs.Cancel;
        }
        private async Task FireNodeObserverEventAsync(INodeObserverEvent snEvent,
            NodeObserver nodeObserver, CancellationToken cancel = default)
        {
            //snEvent.NodeObserverAction(nodeObserver);
            // NodeObserverAction simulation
            using (var op = SnTrace.StartOperation(
                $"NodeObserverAction simulation: {snEvent.GetType().Name} {nodeObserver.GetType().Name}"))
            {
                await Task.Delay(10, cancel).ConfigureAwait(false);
                op.Successful = true;
            }
        }
    }

    public interface IEventProcessor
    {
        Task ProcessEventAsync(ISnEvent snEvent);
    }
    public abstract class EventProcessor : IEventProcessor
    {
        public async Task ProcessEventAsync(ISnEvent snEvent)
        {
            using (var op = SnTrace.StartOperation($"ProcessEvent {GetType().Name} {snEvent.GetType().Name}"))
            {
                await Task.Delay(50).ConfigureAwait(false);
                op.Successful = true;
            }
        }
    }
    public class AuditLogEventProcessor : IEventProcessor
    {
        public async Task ProcessEventAsync(ISnEvent snEvent)
        {
            using (var op = SnTrace.StartOperation($"ProcessEvent {GetType().Name} {snEvent.GetType().Name}"))
            {
                await Task.Delay(5).ConfigureAwait(false);
                op.Successful = true;
            }
        }
    }

    public class PushNotificationEventProcessor : EventProcessor { }
    public class WebHookEventProcessor : EventProcessor { }
    public class EmailSenderEventProcessor : EventProcessor { }
}
