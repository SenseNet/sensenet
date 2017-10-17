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
using System.Threading.Tasks;

namespace SenseNet.Search.Indexing
{
    internal static class CentralizedIndexingActivityQueue
    {
        //UNDONE:||||||||||| Polling timer is not implemented.
        //UNDONE:||||||||||| Use async int the whole class.
        //UNDONE:||||||||||| REFRESH STARTTIME OF THE RUNNING ACTIVITIES

        private static readonly object _waitingActivitiesSync = new object();
        private static readonly Dictionary<int, IndexingActivityBase> _waitingActivities = new Dictionary<int, IndexingActivityBase>();
        private static volatile int _activeTasks;

        private static IIndexingActivityFactory _indexingActivityFactory = new IndexingActivityFactory();
        private static readonly int MaxCount = 10;
        private static readonly int RunningTimeoutInSeconds = 60;

        public static void Startup(TextWriter consoleOut)
        {
            using (var op = SnTrace.Index.StartOperation("CIAQ: STARTUP."))
            {
                // executing unprocessed activities in system start sequence.
                while (true)
                {
                    var loadedActivities = DataProvider.Current.LoadExecutableIndexingActivities(_indexingActivityFactory, MaxCount, RunningTimeoutInSeconds);
                    if (loadedActivities.Length == 0)
                        break;

                    SnTrace.IndexQueue.Write("CIAQ startup: loaded: {0}", loadedActivities.Length);
                    foreach (var loadedActivity in loadedActivities)
                    {
                        loadedActivity.IsUnprocessedActivity = true;
                        Execute(loadedActivity);
                    }
                }

                //UNDONE:||||||||||| Polling timer need to be started in this method

                op.Successful = true;
            }
        }

        /// <summary>
        /// Entry point of the centralized indexing activity queue.
        /// Memorizes the activity in a waiting list and executes some available activities in asynchron way.
        /// </summary>
        public static void ExecuteActivity(IndexingActivityBase activity)
        {
            //UNDONE:||||||||||||| disable polling

            ExecuteActivities(activity);

            //UNDONE:||||||||||||| enable polling
        }

        private static void Polling()
        {
            //UNDONE:||||||||||| Check running state of the waiting activities and call the ExecuteActivities().
            //UNDONE:||||||||||| :() Do not check activities that are just running (need to watch active Tasks).
            //UNDONE:||||||||||| :() Which intervals should we call the polling method?


            //UNDONE:||||||||||||| skip if polling disabled
        }

        /// <summary>
        /// Loads some executable activities and queries the state of the waiting activities in one database request.
        /// Releases the finished activities and executes the loaded ones in asynchron way.
        /// </summary>
        private static void ExecuteActivities(IndexingActivityBase waitingActivity = null)
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
                        return;
                    }
                    // add to waiting list
                    _waitingActivities.Add(waitingActivity.Id, waitingActivity);
                }
                // get id array of waiting activities
                waitingActivityIds = _waitingActivities.Keys.ToArray();
            }

            // load some executable activities and currently finished ones
            var loadedActivities = DataProvider.Current.LoadExecutableIndexingActivities(
                _indexingActivityFactory,
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
                if (waitingActivity == null)
                    // polling branch
                    Task.Run(() => Execute(loadedActivity));
                else
                    // UI branch: pay attention to executing waiting instance, because that is needed to be released (loaded one can be dropped).
                    Task.Run(() => Execute(loadedActivity.Id == waitingActivity.Id ? waitingActivity : loadedActivity));
            }
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
