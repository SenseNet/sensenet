﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using SenseNet.ContentRepository.Search.Indexing.Activities;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Diagnostics;

namespace SenseNet.ContentRepository.Search.Indexing
{
    internal static class CentralizedIndexingActivityQueue
    {
        private static readonly int MaxCount = 10;
        private static readonly int RunningTimeoutInSeconds = Configuration.Indexing.IndexingActivityTimeoutInSeconds;
        private static readonly int LockRefreshPeriodInMilliseconds = RunningTimeoutInSeconds * 3000 / 4;
        private static readonly int HearthBeatMilliseconds = 1000;

        private static readonly TimeSpan WaitingPollingPeriod = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan HealthCheckPeriod = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan DeleteFinishedPeriod = TimeSpan.FromMinutes(23);
        private const int ActiveTaskLimit = 43;

        private static System.Timers.Timer _timer;
        private static DateTime _lastExecutionTime;
        private static DateTime _lastLockRefreshTime;
        private static DateTime _lastDeleteFinishedTime;
        private static volatile int _activeTasks;
        private static int _pollingBlockerCounter;

        private static readonly object WaitingActivitiesSync = new object();
        private static readonly Dictionary<int, IndexingActivityBase> WaitingActivities = new Dictionary<int, IndexingActivityBase>();

        public static void Startup(TextWriter consoleOut)
        {
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
                        Thread.Sleep(HearthBeatMilliseconds);

                    // finish execution cycle if everything is done.
                    if (_activeTasks == 0 && loadedCount == 0)
                        break;

                    // wait a bit in case of too many active tasks
                    while (_activeTasks > ActiveTaskLimit)
                        Thread.Sleep(HearthBeatMilliseconds);
                }

                // every period starts now
                _lastLockRefreshTime = DateTime.UtcNow;
                _lastExecutionTime = DateTime.UtcNow;
                _lastDeleteFinishedTime = DateTime.UtcNow;

                _timer = new System.Timers.Timer(HearthBeatMilliseconds);
                _timer.Elapsed += Timer_Elapsed;
                _timer.Disposed += Timer_Disposed;
                // awakening (this is the judgement day)
                _timer.Enabled = true;

                var msg = $"CIAQ: polling timer started. Heartbeat: {HearthBeatMilliseconds} milliseconds";
                SnTrace.IndexQueue.Write(msg);
                consoleOut?.WriteLine(msg);

                op.Successful = true;
            }
        }
        internal static void ShutDown()
        {
            SnTrace.IndexQueue.Write("Shutting down CentralizedIndexingActivityQueue.");
            _timer.Enabled = false;
            _timer.Dispose();
        }

        private static void Timer_Disposed(object sender, EventArgs e)
        {
            _timer.Elapsed -= Timer_Elapsed;
            _timer.Disposed -= Timer_Disposed;
        }
        private static void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Polling();
        }

        /// <summary>
        /// Entry point of the centralized indexing activity queue.
        /// Memorizes the activity in a waiting list and executes some available activities in asynchron way.
        /// </summary>
        public static void ExecuteActivity(IndexingActivityBase activity)
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

        private static void Polling()
        {
            RefreshLocks();

            DeleteFinishedActivitiesOccasionally();

            if (!IsPollingEnabled())
                return;

            int waitingListLength;
            lock (WaitingActivitiesSync)
                waitingListLength = WaitingActivities.Count;
            var timeLimit = waitingListLength > 0 ? WaitingPollingPeriod : HealthCheckPeriod;

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

        private static void RefreshLocks()
        {
            if (_lastLockRefreshTime.AddMilliseconds(LockRefreshPeriodInMilliseconds) > DateTime.UtcNow)
                return;

            _lastLockRefreshTime = DateTime.UtcNow;

            int[] waitingIds;
            lock (WaitingActivitiesSync)
                waitingIds = WaitingActivities.Keys.ToArray();

            if (waitingIds.Length == 0)
                return;

            SnTrace.IndexQueue.Write($"CIAQ: Refreshing indexing activity locks: {string.Join(", ", waitingIds)}");

            DataProvider.Current.RefreshIndexingActivityLockTime(waitingIds);
        }
        private static void DeleteFinishedActivitiesOccasionally()
        {
            if (DateTime.UtcNow - _lastDeleteFinishedTime >= DeleteFinishedPeriod)
            {
                DataProvider.Current.DeleteFinishedIndexingActivities();
                _lastDeleteFinishedTime = DateTime.UtcNow;
            }
        }

        private static void EnablePolling()
        {
            _pollingBlockerCounter--;
        }
        private static void DisablePolling()
        {
            _pollingBlockerCounter++;
        }
        private static bool IsPollingEnabled()
        {
            return _pollingBlockerCounter == 0;
        }

        /// <summary>
        /// Loads some executable activities and queries the state of the waiting activities in one database request.
        /// Releases the finished activities and executes the loaded ones in asynchron way.
        /// </summary>
        private static int ExecuteActivities(IndexingActivityBase waitingActivity, bool systemStart)
        {
            int[] waitingActivityIds;
            lock (WaitingActivitiesSync)
            {
                if (waitingActivity != null)
                {
                    if (WaitingActivities.TryGetValue(waitingActivity.Id, out IndexingActivityBase olderWaitingActivity))
                    {
                        // if exists, attach the current to existing.
                        olderWaitingActivity.Attach(waitingActivity);
                        // do not load executables because wait-polling cycle is active.
                        return 0;
                    }
                    // add to waiting list
                    WaitingActivities.Add(waitingActivity.Id, waitingActivity);
                }
                // get id array of waiting activities
                waitingActivityIds = WaitingActivities.Keys.ToArray();
            }

            // load some executable activities and currently finished ones
            var loadedActivities = DataProvider.Current.LoadExecutableIndexingActivities(
                IndexingActivityFactory.Instance,
                MaxCount,
                RunningTimeoutInSeconds,
                waitingActivityIds,
                out var finishedActivitiyIds);

            SnTrace.IndexQueue.Write("CIAQ: loaded: {0}, waiting: {1}, finished: {2}, tasks: {3}", loadedActivities.Length, waitingActivityIds.Length, finishedActivitiyIds.Length, _activeTasks);

            // release finished activities
            if (finishedActivitiyIds.Length > 0)
            {
                lock (WaitingActivitiesSync)
                {
                    foreach (var finishedActivitiyId in finishedActivitiyIds)
                    {
                        if (WaitingActivities.TryGetValue(finishedActivitiyId, out IndexingActivityBase finishedActivity))
                        {
                            WaitingActivities.Remove(finishedActivitiyId);
                            finishedActivity.Finish();
                        }
                    }
                }
            }

            // execute loaded activities
            foreach (var loadedActivity in loadedActivities)
            {
                if (systemStart)
                    loadedActivity.IsUnprocessedActivity = true;

                if (waitingActivity == null)
                    // polling branch
                    System.Threading.Tasks.Task.Run(() => Execute(loadedActivity));
                else
                    // UI branch: pay attention to executing waiting instance, because that is needed to be released (loaded one can be dropped).
                    System.Threading.Tasks.Task.Run(() => Execute(loadedActivity.Id == waitingActivity.Id ? waitingActivity : loadedActivity));
            }

            // memorize last running time
            _lastExecutionTime = DateTime.UtcNow;

            return loadedActivities.Length;
        }

        /// <summary>
        /// Executes an activity with synchron way with its encapsulated implementation.
        /// Updates the activity's runningState to "Done" to indicate the end of execution.
        /// Calls the activity's Finish to release the waiting thread.
        /// Removes the activity from the waiting list.
        /// </summary>
        private static void Execute(IIndexingActivity activity)
        {
            var act = (IndexingActivityBase)activity;
            using (var op = SnTrace.Index.StartOperation("CIAQ: A{0} EXECUTION.", act.Id))
            {
                try
                {
                    _activeTasks++;

                    // execute synchronously
                    using (new Storage.Security.SystemAccount())
                        act.ExecuteIndexingActivity();

                    // publish the finishing state
                    DataProvider.Current.UpdateIndexingActivityRunningState(act.Id, IndexingActivityRunningState.Done);
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
                    lock (WaitingActivitiesSync)
                    {
                        act.Finish();
                        WaitingActivities.Remove(act.Id);
                    }
                    _activeTasks--;
                }
                op.Successful = true;
            }
        }
    }
}
