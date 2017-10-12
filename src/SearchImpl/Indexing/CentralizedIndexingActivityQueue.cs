using SenseNet.ContentRepository.Search;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Diagnostics;
using SenseNet.Search.Indexing.Activities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.Search.Indexing
{
    internal static class CentralizedIndexingActivityQueue
    {
        //UNDONE:||||||||||| Polling?

        private static IIndexingActivityFactory _indexingActivityFactory = new IndexingActivityFactory();
        private static readonly int MaxCount = 10;
        private static readonly int RunningTimeoutInSeconds = 60;

        public static void ExecuteActivity(IndexingActivityBase activity, bool waitForComplete)
        {
            //UNDONE:||||||||||| start the iteration in other thread.
            while (true)
            {
                var loadedActivities = DataProvider.Current.StartIndexingActivities(MaxCount, RunningTimeoutInSeconds, _indexingActivityFactory);
                if (loadedActivities.Length == 0)
                    return;

                // The "executeSynchron" will be true if the "waitForComplete" is true and the "activity" is executable.
                // The "activity" is executable if the loadedActivites contains it.
                var executeSynchron = false;
                foreach (var loadedActivity in loadedActivities)
                {
                    if (waitForComplete)
                    {
                        if (loadedActivity.Id == activity.Id)
                        {
                            executeSynchron = true;
                            continue;
                        }
                    }
                    Task.Run(() => Execute(loadedActivity as IndexingActivityBase));
                }

                // execute synchron if loaded "activity" is loaded.
                if (executeSynchron)
                    Execute(activity);
                //UNDONE:||||||||||| what we need to do if the activity is not started? (postponed or another agent allocated it).
            }
        }
        private static void Execute(IndexingActivityBase activity)
        {
            using (var op = SnTrace.Index.StartOperation("CIAQ: A{0} EXECUTION.", activity.Id))
            {
                IndexingActivityHistory.Start(activity.Id);
                try
                {
                    using (new SenseNet.ContentRepository.Storage.Security.SystemAccount())
                        activity.ExecuteIndexingActivity();
                }
                catch (Exception e)
                {
                    SnTrace.Index.WriteError("CIAQ: A{0} EXECUTION ERROR: {1}", activity.Id, e);
                    IndexingActivityHistory.Error(activity.Id, e);
                }
                finally
                {
                    activity.Finish();
                }
                op.Successful = true;
            }
        }

    }
}
