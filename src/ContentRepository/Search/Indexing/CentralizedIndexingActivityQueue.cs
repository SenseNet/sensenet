using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Search.Indexing.Activities;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Diagnostics;

namespace SenseNet.ContentRepository.Search.Indexing
{
    internal class CentralizedIndexingActivityQueue
    {
        private IDataStore DataStore => Providers.Instance.DataStore;
        private readonly IIndexingActivityFactory _indexingActivityFactory;

        private readonly int _maxCount = 10;
        private readonly int _runningTimeoutInSeconds = Configuration.Indexing.IndexingActivityTimeoutInSeconds;
        private readonly int _lockRefreshPeriodInMilliseconds;
        private readonly int _hearthBeatMilliseconds = 1000;

        private readonly TimeSpan _waitingPollingPeriod = TimeSpan.FromSeconds(2);
        private readonly TimeSpan _healthCheckPeriod = TimeSpan.FromMinutes(2);
        private readonly TimeSpan _deleteFinishedPeriod = TimeSpan.FromMinutes(Configuration.Indexing.IndexingActivityDeletionPeriodInMinutes);
        private readonly int _maxAgeInMinutes = Configuration.Indexing.IndexingActivityMaxAgeInMinutes;
        private const int ActiveTaskLimit = 43;

        private System.Timers.Timer _timer;
        private DateTime _lastExecutionTime;
        private DateTime _lastLockRefreshTime;
        private DateTime _lastDeleteFinishedTime;
        private volatile int _activeTasks;
        private int _pollingBlockerCounter;

        private readonly object _waitingActivitiesSync = new object();
        private readonly Dictionary<int, IndexingActivityBase> _waitingActivities = new Dictionary<int, IndexingActivityBase>();

        public CentralizedIndexingActivityQueue(IIndexingActivityFactory indexingActivityFactory)
        {
            _lockRefreshPeriodInMilliseconds = _runningTimeoutInSeconds * 3000 / 4;
            _indexingActivityFactory = indexingActivityFactory;
        }

        public void Startup(TextWriter consoleOut)
        {
            //TODO: [async] rewrite this using async APIs instead of Thread.Sleep.
            using (var op = SnTrace.Index.StartOperation("CIAQ: STARTUP."))
            {
                var loadedCount = 0;
                // executing unprocessed activities in system start sequence.
                while (true)
                {
                    // execute one charge in async way
                    if (_activeTasks < ActiveTaskLimit)
                        loadedCount = ExecuteActivities(null, true);

                    if (loadedCount > 0)
                        Thread.Sleep(_hearthBeatMilliseconds);

                    // finish execution cycle if everything is done.
                    if (_activeTasks == 0 && loadedCount == 0)
                        break;

                    // wait a bit in case of too many active tasks
                    while (_activeTasks > ActiveTaskLimit)
                        Thread.Sleep(_hearthBeatMilliseconds);
                }

                // every period starts now
                _lastLockRefreshTime = DateTime.UtcNow;
                _lastExecutionTime = DateTime.UtcNow;
                _lastDeleteFinishedTime = DateTime.UtcNow;

                _timer = new System.Timers.Timer(_hearthBeatMilliseconds);
                _timer.Elapsed += Timer_Elapsed;
                _timer.Disposed += Timer_Disposed;
                // awakening (this is the judgement day)
                _timer.Enabled = true;

                var msg = $"CIAQ: polling timer started. Heartbeat: {_hearthBeatMilliseconds} milliseconds";
                SnTrace.IndexQueue.Write(msg);
                consoleOut?.WriteLine(msg);

                op.Successful = true;
            }
        }
        internal void ShutDown()
        {
            SnTrace.IndexQueue.Write("Shutting down CentralizedIndexingActivityQueue.");

            if (_timer == null)
                return;

            _timer.Enabled = false;
            _timer.Dispose();
        }

        private void Timer_Disposed(object sender, EventArgs e)
        {
            _timer.Elapsed -= Timer_Elapsed;
            _timer.Disposed -= Timer_Disposed;
        }
        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Polling();
        }

        /// <summary>
        /// Entry point of the centralized indexing activity queue.
        /// Memorizes the activity in a waiting list and executes some available activities in asynchron way.
        /// </summary>
        public void ExecuteActivity(IndexingActivityBase activity)
        {
            try
            {
                DisablePolling();
                ExecuteActivities(activity, false);
            }
            finally
            {
                EnablePolling();
            }
        }

        private void Polling()
        {
            RefreshLocks();

            DeleteFinishedActivitiesOccasionally();

            if (!IsPollingEnabled())
                return;
            
            int waitingListLength;
            lock (_waitingActivitiesSync)
                waitingListLength = _waitingActivities.Count;
            var timeLimit = waitingListLength > 0 ? _waitingPollingPeriod : _healthCheckPeriod;

            if ((DateTime.UtcNow - _lastExecutionTime) > timeLimit && _activeTasks < ActiveTaskLimit)
            {
                try
                {
                    DisablePolling();
                    SnTrace.IndexQueue.Write("CIAQ: '{0}' polling timer beats.", (waitingListLength > 0) ? "wait" : "health");
                    ExecuteActivities(null, false);
                }
                finally
                {
                    EnablePolling();
                }
            }
        }

        private void RefreshLocks()
        {
            if (_lastLockRefreshTime.AddMilliseconds(_lockRefreshPeriodInMilliseconds) > DateTime.UtcNow)
                return;

            _lastLockRefreshTime = DateTime.UtcNow;

            int[] waitingIds;
            lock (_waitingActivitiesSync)
                waitingIds = _waitingActivities.Keys.ToArray();

            if (waitingIds.Length == 0)
                return;

            SnTrace.IndexQueue.Write($"CIAQ: Refreshing indexing activity locks: {string.Join(", ", waitingIds)}");

            DataStore.RefreshIndexingActivityLockTimeAsync(waitingIds, CancellationToken.None)
                .GetAwaiter().GetResult();
        }
        private void DeleteFinishedActivitiesOccasionally()
        {
            if (DateTime.UtcNow - _lastDeleteFinishedTime >= _deleteFinishedPeriod)
            {
                using (var op = SnTrace.IndexQueue.StartOperation("CIAQ: DeleteFinishedActivities"))
                {
                    DataStore.DeleteFinishedIndexingActivitiesAsync(_maxAgeInMinutes, CancellationToken.None)
                        .GetAwaiter().GetResult();
                    _lastDeleteFinishedTime = DateTime.UtcNow;
                    op.Successful = true;
                }
            }
        }

        private void EnablePolling()
        {
            Interlocked.Decrement(ref _pollingBlockerCounter);
        }
        private void DisablePolling()
        {
            Interlocked.Increment(ref _pollingBlockerCounter);
        }
        private bool IsPollingEnabled()
        {
            return _pollingBlockerCounter == 0;
        }

        /// <summary>
        /// Loads some executable activities and queries the state of the waiting activities in one database request.
        /// Releases the finished activities and executes the loaded ones in asynchron way.
        /// </summary>
        private int ExecuteActivities(IndexingActivityBase waitingActivity, bool systemStart)
        {
            int[] waitingActivityIds;
            lock (_waitingActivitiesSync)
            {
                if (waitingActivity != null)
                {
                    if (_waitingActivities.TryGetValue(waitingActivity.Id, out IndexingActivityBase olderWaitingActivity))
                    {
                        SnTrace.IndexQueue.Write($"CIAQ: Attaching new waiting A{waitingActivity.Id} to an older instance.");

                        // if exists, attach the current to existing.
                        olderWaitingActivity.Attach(waitingActivity);
                        // do not load executables because wait-polling cycle is active.
                        return 0;
                    }

                    SnTrace.IndexQueue.Write($"CIAQ: Adding arrived A{waitingActivity.Id} to waiting list.");

                    // add to waiting list
                    _waitingActivities.Add(waitingActivity.Id, waitingActivity);
                }
                // get id array of waiting activities
                waitingActivityIds = _waitingActivities.Keys.ToArray();

                if (SnTrace.IndexQueue.Enabled)
                    SnTrace.IndexQueue.Write($"Waiting set v1: {string.Join(", ", waitingActivityIds)}");
            }

            // load some executable activities and currently finished ones
            var result = DataStore.LoadExecutableIndexingActivitiesAsync(
                _indexingActivityFactory,
                _maxCount,
                _runningTimeoutInSeconds,
                waitingActivityIds, CancellationToken.None).GetAwaiter().GetResult();
            var loadedActivities = result.Activities;
            var finishedActivitiyIds = result.FinishedActivitiyIds;

            if (SnTrace.IndexQueue.Enabled)
                SnTrace.IndexQueue.Write("CIAQ: loaded: {0} ({1}), waiting: {2}, finished: {3}, tasks: {4}",
                    loadedActivities.Length,
                    string.Join(", ", loadedActivities.Select(la => la.Id)),
                    waitingActivityIds.Length, finishedActivitiyIds.Length, _activeTasks);

            // release finished activities
            if (finishedActivitiyIds.Length > 0)
            {
                lock (_waitingActivitiesSync)
                {
                    foreach (var finishedActivitiyId in finishedActivitiyIds)
                    {
                        if (_waitingActivities.TryGetValue(finishedActivitiyId, out IndexingActivityBase finishedActivity))
                        {
                            _waitingActivities.Remove(finishedActivitiyId);
                            finishedActivity.Finish();
                        }
                    }
                }
            }

            if (loadedActivities.Any())
            {
                lock (_waitingActivitiesSync)
                {
                    if (SnTrace.IndexQueue.Enabled)
                        SnTrace.IndexQueue.Write($"Waiting set v2: {string.Join(", ", _waitingActivities.Keys)}");

                    // execute loaded activities
                    foreach (var loadedActivity in loadedActivities)
                    {
                        if (systemStart)
                            loadedActivity.IsUnprocessedActivity = true;

                        // If a loaded activity is the same as the current activity or
                        // the same as any of the already waiting activities, we have to
                        // drop the loaded instance and execute the waiting instance, otherwise
                        // the algorithm would not notice when the activity is finished and
                        // the finish signal is released.
                        IIndexingActivity executableActivity;

                        if (loadedActivity.Id == waitingActivity?.Id)
                        {
                            executableActivity = waitingActivity;
                        }
                        else
                        {
                            if (_waitingActivities.TryGetValue(loadedActivity.Id, out var otherWaitingActivity))
                            {
                                // Found in the waiting list: drop the loaded one and execute the waiting.
                                executableActivity = otherWaitingActivity;

                                SnTrace.IndexQueue.Write($"CIAQ: Loaded A{loadedActivity.Id} found in the waiting list.");
                            }
                            else
                            {
                                // If a loaded activity is not in the waiting list, we have to add it here
                                // so that other threads may find it and be able to attach to it.

                                SnTrace.IndexQueue.Write($"CIAQ: Adding loaded A{loadedActivity.Id} to waiting list.");

                                _waitingActivities.Add(loadedActivity.Id, loadedActivity as IndexingActivityBase);
                                executableActivity = loadedActivity;
                            }
                        }

                        System.Threading.Tasks.Task.Run(() => Execute(executableActivity));
                    }
                }
            }

            // memorize last running time
            _lastExecutionTime = DateTime.UtcNow;

            return loadedActivities.Length;
        }

        /// <summary>
        /// Executes an activity synchronously.
        /// Updates the activity's runningState to "Done" to indicate the end of execution.
        /// Calls the activity's Finish to release the waiting thread.
        /// Removes the activity from the waiting list.
        /// </summary>
        private void Execute(IIndexingActivity activity)
        {
            var act = (IndexingActivityBase)activity;
            using (var op = SnTrace.Index.StartOperation("CIAQ: EXECUTION: A{0}.", act.Id))
            {
                try
                {
#pragma warning disable 420
                    Interlocked.Increment(ref _activeTasks);
#pragma warning restore 420

                    //TODO: [async] refactor this method to be async
                    // execute synchronously
                    using (new Storage.Security.SystemAccount())
                        act.ExecuteIndexingActivityAsync(CancellationToken.None).GetAwaiter().GetResult();

                    // publish the finishing state
                    DataStore.UpdateIndexingActivityRunningStateAsync(act.Id, IndexingActivityRunningState.Done, CancellationToken.None)
                        .GetAwaiter().GetResult();
                }
                catch (Exception e)
                {
                    //TODO: WARNING Do not fill the event log with repetitive messages.
                    SnLog.WriteException(e, $"Indexing activity execution error. Activity: #{act.Id} ({act.ActivityType})");
                    SnTrace.Index.WriteError("CIAQ: A{0} EXECUTION ERROR: {1}", act.Id, e);
                }
                finally
                {
                    // release the waiting thread and remove from the waiting list
                    lock (_waitingActivitiesSync)
                    {
                        act.Finish();
                        _waitingActivities.Remove(act.Id);
                    }

#pragma warning disable 420
                    Interlocked.Decrement(ref _activeTasks);
#pragma warning restore 420
                }
                op.Successful = true;
            }
        }
    }
}
