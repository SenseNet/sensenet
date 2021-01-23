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
    public class EventDistributor : IEventDistributor
    {
        //UNDONE:<?event Remove the master switch
        private bool __isFeatureEnabled = true;
        private bool IsFeatureEnabled(int id)
        {
            if (!__isFeatureEnabled)
                SnTrace.Write($"EventDistributor INACTIVATED ({id}).");
            return __isFeatureEnabled;
        }

        //UNDONE:<?event Use DependencyInjection
        public AuditLogEventProcessor AuditLogEventProcessor = new AuditLogEventProcessor();
        //UNDONE:<?event Use DependencyInjection
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
            if (!IsFeatureEnabled(3))
                return;

            var syncTasks = new List<Task>();
            if(nodeObserverTask != null)
                syncTasks.Add(nodeObserverTask);

            // If the event is IAuditLogEvent, call async, memorize it and do not wait.
            if (snEvent is IAuditLogEvent auditLogEvent)
                syncTasks.Add(AuditLogEventProcessor.ProcessEventAsync(auditLogEvent));

            if (!(snEvent is IInternalEvent))
            {
                // Persists the event
                await SaveEventAsync(snEvent); // saveevent(wait, allprocessors, snEvent)

                // Call all async processors and forget them
                foreach (var processor in AsyncEventProcessors)
                    #pragma warning disable 4014

                    // if(processor.IsRelevant(snEvent))
                    //    if (Acquire(processor, snEvent))
                    //        processor.ProcessEventAsync(snEvent);
                    //        saveevent(done, processor, snEvent)
                    // else
                    //     saveevent(done, processor, snEvent)

                    processor.ProcessEventAsync(snEvent);
                    #pragma warning restore 4014
            }

            // Wait for all synchronous tasks.
            if (syncTasks.Count > 0)
                await Task.WhenAll(syncTasks.ToArray()).ConfigureAwait(false);
        }
        private async Task<bool> CallNodeObserversAsync(ISnCancellableEvent snEvent, List<Type> disabledNodeObservers)
        {
            if (!IsFeatureEnabled(1))
                return false;

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
            if (!IsFeatureEnabled(2))
                return;

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
                await Task.Delay(20, cancel).ConfigureAwait(false);
                op.Successful = true;
            }
        }

        private async Task SaveEventAsync(ISnEvent snEvent)
        {
            using (var op = SnTrace.StartOperation("Save event"))
            {
                await Task.Delay(10);
                op.Successful = true;
            }
        }
    }

    //UNDONE:<?event: Remove the demo classes: AuditLogEventProcessor, EventProcessor and so on.

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
