using SenseNet.ContentRepository.Search;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Diagnostics;
using SenseNet.Search.Indexing.Activities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SenseNet.Search.Indexing
{
    //UNDONE:||||||||||| Use async keyword in the whole class.
    internal static class CentralizedIndexingActivityQueue
    {
        private static readonly int MaxCount = 10;
        private static readonly int RunningTimeoutInSeconds = Configuration.Indexing.LuceneActivityTimeoutInSeconds;
        private static readonly int LockRefreshPeriodInMilliseconds = RunningTimeoutInSeconds * 3000 / 4;
        private static readonly int HearthBeatMilliseconds = 1000;

        private static readonly TimeSpan _waitingPollingPeriod = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan _healthCheckPeriod = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan _deleteFinishedPeriod = TimeSpan.FromMinutes(23);
        private static readonly int _activeTaskLimit = 43;

        private static System.Timers.Timer _timer;
        private static DateTime _lastExecutionTime;
        private static DateTime _lastLockRefreshTime;
        private static DateTime _lastDeleteFinishedTime;
        private static volatile int _activeTasks;
        private static int _pollingBlockerCounter;

        private static readonly object _waitingActivitiesSync = new object();
        private static readonly Dictionary<int, IndexingActivityBase> _waitingActivities = new Dictionary<int, IndexingActivityBase>();

        public static void Startup(TextWriter consoleOut)
        {
            using (var op = SnTrace.Index.StartOperation("CIAQ: STARTUP."))
            {
                var loadedCount = 0;
                // executing unprocessed activities in system start sequence.
                while (true)
                {
                    // execute one charge in async way
                    if (_activeTasks < _activeTaskLimit)
                        loadedCount = ExecuteActivities(null, true);

                    if (loadedCount > 0)
                        Thread.Sleep(HearthBeatMilliseconds);

                    // finish execution cycle if everything is done.
                    if (_activeTasks == 0 && loadedCount == 0)
                        break;

                    // wait a bit in case of too many active tasks
                    while (_activeTasks > _activeTaskLimit)
                        Thread.Sleep(HearthBeatMilliseconds);
                }

                // every period starts now
                _lastLockRefreshTime = DateTime.UtcNow;
                _lastExecutionTime = DateTime.UtcNow;
                _lastDeleteFinishedTime = DateTime.UtcNow;

                _timer = new System.Timers.Timer(HearthBeatMilliseconds);
                _timer.Elapsed += new System.Timers.ElapsedEventHandler(Timer_Elapsed);
                _timer.Disposed += new EventHandler(Timer_Disposed);
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
            _timer.Elapsed -= new System.Timers.ElapsedEventHandler(Timer_Elapsed);
            _timer.Disposed -= new EventHandler(Timer_Disposed);
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
            lock (_waitingActivitiesSync)
                waitingListLength = _waitingActivities.Count;
            var timeLimit = waitingListLength > 0 ? _waitingPollingPeriod : _healthCheckPeriod;

            if ((DateTime.UtcNow - _lastExecutionTime) > timeLimit && _activeTasks < _activeTaskLimit)
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
            if (DateTime.UtcNow.AddMilliseconds(LockRefreshPeriodInMilliseconds) > _lastLockRefreshTime)
                return;
            _lastLockRefreshTime = DateTime.UtcNow;

            int[] waitingIds;
            lock (_waitingActivitiesSync)
                waitingIds = _waitingActivities.Keys.ToArray();

            if (waitingIds.Length == 0)
                return;

            DataProvider.Current.RefreshIndexingActivityLockTime(waitingIds);
        }
        private static void DeleteFinishedActivitiesOccasionally()
        {
            if (DateTime.UtcNow - _lastDeleteFinishedTime >= _deleteFinishedPeriod)
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
            lock (_waitingActivitiesSync)
            {
                if (waitingActivity != null)
                {
                    if (_waitingActivities.TryGetValue(waitingActivity.Id, out IndexingActivityBase olderWaitingActivity))
                    {
                        // if exists, attach the current to existing.
                        olderWaitingActivity.Attach(waitingActivity);
                        // do not load executables because wait-polling cycle is active.
                        return 0;
                    }
                    // add to waiting list
                    _waitingActivities.Add(waitingActivity.Id, waitingActivity);
                }
                // get id array of waiting activities
                waitingActivityIds = _waitingActivities.Keys.ToArray();
            }

            // load some executable activities and currently finished ones
            var loadedActivities = DataProvider.Current.LoadExecutableIndexingActivities(
                IndexingActivityFactory.Instance,
                MaxCount,
                RunningTimeoutInSeconds,
                waitingActivityIds,
                out int[] finishedActivitiyIds);
            SnTrace.IndexQueue.Write("CIAQ: loaded: {0}, waiting: {1}, finished: {2}, tasks: {3}", loadedActivities.Length, _waitingActivities.Count, finishedActivitiyIds.Length, _activeTasks);

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

            // execute loaded activities
            foreach (var loadedActivity in loadedActivities)
            {
                if (systemStart)
                    loadedActivity.IsUnprocessedActivity = true;

                if (waitingActivity == null)
                    // polling branch
                    Task.Run(() => Execute(loadedActivity));
                else
                    // UI branch: pay attention to executing waiting instance, because that is needed to be released (loaded one can be dropped).
                    Task.Run(() => Execute(loadedActivity.Id == waitingActivity.Id ? waitingActivity : loadedActivity));
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
                    using (new SenseNet.ContentRepository.Storage.Security.SystemAccount())
                        act.ExecuteIndexingActivity();

                    // publish the finishing state
                    DataProvider.Current.UpdateIndexingActivityRunningState(act.Id, IndexingActivityRunningState.Done);
                }
                catch (Exception e)
                {
                    //UNDONE:||||||||||| Error logging is not implemented. WARNING Do not fill the event log with repetitive messages.
                    //UNDONE:||||||||||| :() Wait and retry? The client may not be able to handle this situation
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
                    _activeTasks--;
                }
                op.Successful = true;
            }
        }
    }
}
