using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Store;
using SenseNet.Search.Indexing;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Diagnostics;
using SenseNet.ContentRepository.Storage;
using System.Threading;

namespace SenseNet.Search.Indexing.Activities
{
    [Serializable]
    public abstract class LuceneIndexingActivity : DistributedLuceneActivity, IIndexingActivity, System.Runtime.Serialization.IDeserializationCallback
    {
        // stored properties
        public int Id { get; set; }
        public IndexingActivityType ActivityType { get; set; }
        public DateTime CreationDate { get; set; }
        public int NodeId { get; set; }
        public int VersionId { get; set; }
        public Nullable<bool> SingleVersion { get; set; }
        public bool? MoveOrRename { get; set; }
        public bool? IsLastDraftValue { get; set; }
        public string Path { get; set; }
        public long? VersionTimestamp { get; set; }

        public string Extension
        {
            get { return GetExtension(); }
            set { SetExtension(value); }
        }

        protected abstract string GetExtension();
        protected abstract void SetExtension(string value);

        // not stored properties
        public IndexDocumentData IndexDocumentData { get; set; }


        [NonSerialized]
        private bool _isUnprocessedActivity;
        public bool IsUnprocessedActivity
        {
            get { return _isUnprocessedActivity; }
            set { _isUnprocessedActivity = value; }
        }

        [NonSerialized]
        private bool _fromReceiver;
        public bool FromReceiver
        {
            get { return _fromReceiver; }
            set { _fromReceiver = value; }
        }

        [NonSerialized]
        private bool _fromDatabase;
        public bool FromDatabase
        {
            get { return _fromDatabase; }
            set { _fromDatabase = value; }
        }

        [NonSerialized]
        private bool _executed;
        internal bool Executed
        {
            get { return _executed; }
            set { _executed = value; }
        }


        internal override void ExecuteIndexingActivity()
        {
            // if not running or paused, skip execution except executing unprocessed activities
            if (!IsExecutable())
            {
                SnTrace.Index.Write($"LM: {this} skipped. ActivityId:{Id}, ExecutingUnprocessedActivities:{IsUnprocessedActivity}");
                return;
            }
            SnTrace.Index.Write($"LM: {this}. ActivityId:{Id}, ExecutingUnprocessedActivities:{IsUnprocessedActivity}");

            if (ProtectedExecute())
                _executed = true;

            // ActivityFinished must be called after executing an activity, even if index is not changed
            if (IsExecutable())
                IndexManager.ActivityFinished(this.Id, this.IsUnprocessedActivity);
        }
        private bool IsExecutable()
        {
            // if not running or paused, skip execution except executing unprocessed activities
            return IsUnprocessedActivity || (IndexManager.Running && !IndexManager.Paused);
        }


        protected abstract bool ProtectedExecute();


        [NonSerialized]
        private LuceneIndexingActivity _attachedActivity;
        internal LuceneIndexingActivity AttachedActivity { get { return _attachedActivity; } private set { _attachedActivity = value; } }
        /// <summary>
        /// When an activity gets executed and needs to be finalized, all activity objects that have
        /// the same id need to be finalized too. The Attach methods puts all activities with the
        /// same id to a chain to let the Finish method call the Finish method of each object in the chain.
        /// This method was needed because it is possible that the same activity arrives from different
        /// sources: e.g from messaging, from database or from direct execution.
        /// </summary>
        /// <param name="activity"></param>
        internal void Attach(LuceneIndexingActivity activity)
        {
            if (AttachedActivity != null)
                AttachedActivity.Attach(activity);
            else
                AttachedActivity = activity;
        }

        /// <summary>
        /// Finish the full activity chain (see the Attach method for details).
        /// </summary>
        internal override void Finish()
        {
            if (AttachedActivity != null)
            {
                SnTrace.IndexQueue.Write("IAQ: attached A{0} finished.", AttachedActivity.Id);
                // finalize attached activities first
                AttachedActivity.Finish();
            }
            base.Finish();
            SnTrace.IndexQueue.Write("IAQ: A{0} finished.", this.Id);
        }


        // ================================================= AQ16

        public LuceneIndexingActivity()
        {
            _waitingFor = new List<LuceneIndexingActivity>();
            _waitingForMe = new List<LuceneIndexingActivity>();
        }

        public void OnDeserialization(object sender)
        {
            _waitingFor = new List<LuceneIndexingActivity>();
            _waitingForMe = new List<LuceneIndexingActivity>();
        }


        [NonSerialized]
        private List<LuceneIndexingActivity> _waitingFor = new List<LuceneIndexingActivity>();
        public List<LuceneIndexingActivity> WaitingFor { get { return _waitingFor; } }

        [NonSerialized]
        private List<LuceneIndexingActivity> _waitingForMe;
        public List<LuceneIndexingActivity> WaitingForMe { get { return _waitingForMe; } }

        internal void WaitFor(LuceneIndexingActivity olderActivity)
        {
            // this method must called from thread safe block.
            if (!this.WaitingFor.Any(x => x.Id == olderActivity.Id))
                this.WaitingFor.Add(olderActivity);
            if (!olderActivity.WaitingForMe.Any(x => x.Id == this.Id))
                olderActivity.WaitingForMe.Add(this);
        }

        internal void FinishWaiting(LuceneIndexingActivity olderActivity)
        {
            // this method must called from thread safe block.
            RemoveDependency(this.WaitingFor, olderActivity);
            RemoveDependency(olderActivity.WaitingForMe, this);
        }
        private static void RemoveDependency(List<LuceneIndexingActivity> dependencyList, LuceneIndexingActivity activity)
        {
            // this method must called from thread safe block.
            dependencyList.RemoveAll(x => x.Id == activity.Id);
        }

        internal bool IsInTree(LuceneIndexingActivity newerActivity)
        {
            return this.Path.StartsWith(newerActivity.Path, StringComparison.OrdinalIgnoreCase);
        }

    }
}