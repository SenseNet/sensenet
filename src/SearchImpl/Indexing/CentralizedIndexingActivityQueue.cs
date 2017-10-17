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
        //UNDONE:||||||||||| Need to check periodically RunningState of the activities in the waiting list.
        //UNDONE:||||||||||| Use async int the whole class.
        //UNDONE:||||||||||| Executing unprocessed activitied are not treated by CentralizedIndexingActivityQueue.

        private static readonly object _waitingActivitiesSync = new object();
        private static readonly Dictionary<int, IndexingActivityBase> _waitingActivities = new Dictionary<int, IndexingActivityBase>();
        private static bool _executing;

        private static IIndexingActivityFactory _indexingActivityFactory = new IndexingActivityFactory();
        private static readonly int MaxCount = 10;
        private static readonly int RunningTimeoutInSeconds = 60;

        public static void Startup(TextWriter consoleOut)
        {
            using (var op = SnTrace.Index.StartOperation("CIAQ: STARTUP."))
            {
                while (true)
                {
                    var loadedActivities = DataProvider.Current.StartIndexingActivities(_indexingActivityFactory, MaxCount, RunningTimeoutInSeconds);
                    if (loadedActivities.Length == 0)
                        break;

                    SnTrace.IndexQueue.Write("CIAQ startup: loaded: {0}", loadedActivities.Length);
                    foreach (var loadedActivity in loadedActivities)
                    {
                        loadedActivity.IsUnprocessedActivity = true;
                        Execute(loadedActivity);
                    }
                }
                op.Successful = true;
            }
        }

        /// <summary>
        /// Entry point of the centralized indexing activity organizer (manager, dealer, handler or anybody who doing something).
        /// Memorizes the activity in a waiting list and executes some available activities in asynchron way.
        /// </summary>
        public static void ExecuteActivity(IndexingActivityBase activity)
        {
            lock (_waitingActivitiesSync)
                _waitingActivities.Add(activity.Id, activity);
            ExecuteActivities(activity);
        }

        /// <summary>
        /// Checks the RunningState of the activities in the waiting list.
        /// If the state of an activity is done releases the activity and removes it from the waiting list.
        /// Loads some available activities and executes them in asynchron way.
        /// </summary>
        private static void Polling()
        {
            //UNDONE:||||||||||| Check running state of the waiting activities and call the ExecuteActivities().
            //UNDONE:||||||||||| :() Do not check activities that are just running (need to watch active Tasks).
            //UNDONE:||||||||||| :() Which intervals should we call the polling method?
            //UNDONE:||||||||||| :() Polling timer need to be started in the system start sequence?
            //UNDONE:||||||||||| :() Is the polling a SnMaintenance process? (It is a provider dependent task).
        }

        /// <summary>
        /// Loads some available activities and executes them in asynchron way.
        /// </summary>
        private static void ExecuteActivities(IIndexingActivity waitingActivity)
        {
            // disable polling
            _executing = true;

            // load and execute some activity
            var loadedActivities = DataProvider.Current.StartIndexingActivities(_indexingActivityFactory, MaxCount, RunningTimeoutInSeconds);
            SnTrace.IndexQueue.Write("CIAQ: loaded: {0}, waiting: {1}", loadedActivities.Length, _waitingActivities.Count);
            foreach (var loadedActivity in loadedActivities)
                Task.Run(() => Execute(loadedActivity.Id == waitingActivity.Id ? waitingActivity : loadedActivity));

            // enable polling
            _executing = false;
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
                    using (new SenseNet.ContentRepository.Storage.Security.SystemAccount())
                        act.ExecuteIndexingActivity();
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
                    act.Finish();
                    lock (_waitingActivitiesSync)
                        _waitingActivities.Remove(act.Id);
                }
                op.Successful = true;
            }
        }
    }
}
