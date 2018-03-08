using System;
using System.Diagnostics;
using System.Threading;
using SenseNet.Communication.Messaging;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;

namespace SenseNet.ContentRepository.Search.Indexing.Activities
{
    /// <summary>
    /// Represents an indexing activity that can be sent or received through an inter-server communication channel.
    /// Used when the current indexing engine handles local indexes.
    /// </summary>
    [Serializable]
    public abstract class DistributedIndexingActivity : DistributedAction
    {
        /// <summary>
        /// Executes the activity's main action.
        /// </summary>
        /// <param name="onRemote">True if the caller is a message receiver.</param>
        /// <param name="isFromMe">True if the source of the activity is in the current appDomain.</param>
        public override void DoAction(bool onRemote, bool isFromMe)
        {
            if (!IndexManager.Running)
                return;

            if (onRemote && !isFromMe)
            {
                //TODO: Remove unnecessary inheritance steps.
                if (this is IndexingActivityBase indexingActivity)
                {
                    // We can drop activities here because the queue will load these from the database
                    // anyway when it processed all the previous activities.
                    if (DistributedIndexingActivityQueue.IsOverloaded())
                    {
                        SnTrace.Index.Write("IAQ OVERLOAD drop activity FromReceiver A:" + indexingActivity.Id);
                        return;
                    }

                    indexingActivity.FromReceiver = true;

                    DistributedIndexingActivityQueue.ExecuteActivity(indexingActivity);
                }
                else
                {
                    InternalExecuteIndexingActivity();
                }
            }
        }

        // ----------------------------------------------------------------------- 

        [NonSerialized]
        private readonly AutoResetEvent _finishSignal = new AutoResetEvent(false);
        [NonSerialized]
        private bool _finished;
        [NonSerialized]
        private int _waitingThreadId;

        internal void InternalExecuteIndexingActivity()
        {
            try
            {
                var id = !(this is IndexingActivityBase persistentActivity)
                    ? string.Empty
                    : persistentActivity.Id.ToString();

                using (var op = SnTrace.Index.StartOperation("IndexingActivity execution: type:{0} id:{1}", GetType().Name, id))
                {
                    using (new SystemAccount())
                    {
                        ExecuteIndexingActivity();
                    }

                    op.Successful = true;
                }
            }
            finally
            {
                Finish();
            }
        }

        internal abstract void ExecuteIndexingActivity();

        /// <summary>
        /// Waits for a release signal that indicates that this activity has been executed
        /// successfully in the background.
        /// </summary>
        public void WaitForComplete()
        {
            if (_finished)
                return;

            _waitingThreadId = Thread.CurrentThread.ManagedThreadId;

            var indexingActivity = this as IndexingActivityBase;

            SnTrace.IndexQueue.Write("IAQ: A{0} blocks the T{1}", indexingActivity?.Id, _waitingThreadId);

            if (Debugger.IsAttached)
            {
                _finishSignal.WaitOne();
            }
            else
            {
                if (!_finishSignal.WaitOne(Configuration.Indexing.IndexingActivityTimeoutInSeconds * 1000, false))
                {
                    var message = indexingActivity != null
                        ? $"IndexingActivity is timed out. Id: {indexingActivity.Id}, Type: {indexingActivity.ActivityType}. Max task id and exceptions: {DistributedIndexingActivityQueue.GetCurrentCompletionState()}"
                        : "Activity is not finishing on a timely manner";

                    throw new ApplicationException(message);
                }
            }
        }

        /// <summary>
        /// Sets the finish signal to release all threads waiting for this activity to complete.
        /// </summary>
        internal virtual void Finish()
        {
            _finished = true;
            if (_finishSignal != null)
            {
                _finishSignal.Set();
                if (_waitingThreadId > 0)
                    SnTrace.IndexQueue.Write("IAQ: waiting resource released T{0}.", _waitingThreadId);
            }
        }
    }
}
