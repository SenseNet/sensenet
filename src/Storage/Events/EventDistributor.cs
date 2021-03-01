﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Events;

// ReSharper disable once CheckNamespace
namespace SenseNet.Events
{
    internal class DevNullEventDistributor : IEventDistributor
    {
        public IEventProcessor AuditLogEventProcessor { get; } = null;
        public IEnumerable<IEventProcessor> AsyncEventProcessors { get; } = new IEventProcessor[0];
        public Task<bool> FireCancellableNodeObserverEventAsync(ISnCancellableEvent snEvent, List<Type> disabledNodeObservers)
        {
            return Task.FromResult(false);
        }
        public Task FireNodeObserverEventAsync(INodeObserverEvent snEvent, List<Type> disabledNodeObservers)
        {
            return Task.CompletedTask;
        }
        public Task FireEventAsync(ISnEvent snEvent)
        {
            return Task.CompletedTask;
        }
    }

    public class EventDistributor : IEventDistributor
    {
        private bool __isFeatureEnabled = false;
        internal bool IsFeatureEnabled(int id)
        {
            //TODO: remove master switch after porting node observers to the SnEvent infrastructure
            //if (!__isFeatureEnabled)
            //    SnTrace.Write($"EventDistributor INACTIVATED ({id}).");
            return __isFeatureEnabled;
        }

        public IEventProcessor AuditLogEventProcessor => Providers.Instance.AuditLogEventProcessor;
        public IEnumerable<IEventProcessor> AsyncEventProcessors => Providers.Instance.AsyncEventProcessors;

        public Task<bool> FireCancellableNodeObserverEventAsync(ISnCancellableEvent snEvent, List<Type> disabledNodeObservers)
        {
            // Cancellable event is used only the NodeObserver infrastructure
            return CallNodeObserversAsync(snEvent, disabledNodeObservers);
        }
        public Task FireNodeObserverEventAsync(INodeObserverEvent snEvent, List<Type> disabledNodeObservers)
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

        private async Task FireEventAsync(ISnEvent snEvent, Task nodeObserverTask, CancellationToken cancel = default)
        {
            // Create a waiting list and add the 'nodeObserverTask' if there is.
            var syncTasks = new List<Task>();
            if(nodeObserverTask != null)
                syncTasks.Add(nodeObserverTask);

            // If the event is IAuditLogEvent, call async, memorize it, and do not wait.
            if (snEvent is IAuditLogEvent auditLogEvent)
                if(AuditLogEventProcessor != null)
                    syncTasks.Add(AuditLogEventProcessor.ProcessEventAsync(auditLogEvent, cancel));

            if (!(snEvent is IInternalEvent))
            {
                // Persists the event
                await SaveEventAsync(snEvent);

                // Call all async processors and forget them
                foreach (var processor in AsyncEventProcessors)
                    #pragma warning disable 4014
                    processor.ProcessEventAsync(snEvent, cancel);
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

            await Task.WhenAll(tasks).ConfigureAwait(false);
            var canceled = tasks.Any(t => t.Result);

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
        protected virtual Task<bool> FireCancellableNodeObserverEventAsync(ISnCancellableEvent snEvent,
            NodeObserver nodeObserver, CancellationToken cancel = default)
        {
            //TODO:<?event: Ensure that this method does not throw any exception (but trace and log).
            //snEvent.NodeObserverAction(nodeObserver);
            //TODO:event: Not implemented yet
            return Task.FromResult(false);
        }
        protected virtual Task FireNodeObserverEventAsync(INodeObserverEvent snEvent,
            NodeObserver nodeObserver, CancellationToken cancel = default)
        {
            //TODO:<?event: Ensure that this method does not throw any exception (but trace and log).
            //snEvent.NodeObserverAction(nodeObserver);
            //TODO:event: Not implemented yet
            return Task.CompletedTask;
        }

        protected virtual Task SaveEventAsync(ISnEvent snEvent)
        {
            //TODO:event: Not implemented yet
            return Task.CompletedTask;
        }
    }
}
