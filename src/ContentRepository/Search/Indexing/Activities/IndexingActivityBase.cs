﻿using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Diagnostics;

namespace SenseNet.ContentRepository.Search.Indexing.Activities
{
    //UNDONE:!!!! XMLDOC ContentRepository
    [Serializable]
    public abstract class IndexingActivityBase : DistributedIndexingActivity, IIndexingActivity, System.Runtime.Serialization.IDeserializationCallback
    {
        // stored properties
        //UNDONE:!!!! XMLDOC ContentRepository
        public int Id { get; set; }
        //UNDONE:!!!! XMLDOC ContentRepository
        public IndexingActivityType ActivityType { get; set; }
        //UNDONE:!!!! XMLDOC ContentRepository
        public DateTime CreationDate { get; set; }
        //UNDONE:!!!! XMLDOC ContentRepository
        public IndexingActivityRunningState RunningState { get; set; }
        //UNDONE:!!!! XMLDOC ContentRepository
        public DateTime? LockTime { get; set; }
        //UNDONE:!!!! XMLDOC ContentRepository
        public int NodeId { get; set; }
        //UNDONE:!!!! XMLDOC ContentRepository
        public int VersionId { get; set; }
        //UNDONE:!!!! XMLDOC ContentRepository
        public string Path { get; set; }
        //UNDONE:!!!! XMLDOC ContentRepository
        public long? VersionTimestamp { get; set; }

        //UNDONE:!!!! XMLDOC ContentRepository
        public string Extension
        {
            get => GetExtension();
            set => SetExtension(value);
        }

        //UNDONE:!!!! XMLDOC ContentRepository
        protected abstract string GetExtension();
        //UNDONE:!!!! XMLDOC ContentRepository
        protected abstract void SetExtension(string value);

        // not stored properties
        //UNDONE:!!!! XMLDOC ContentRepository
        public IndexDocumentData IndexDocumentData { get; set; }


        [NonSerialized]
        private bool _isUnprocessedActivity;
        //UNDONE:!!!! XMLDOC ContentRepository
        public bool IsUnprocessedActivity
        {
            get => _isUnprocessedActivity;
            set => _isUnprocessedActivity = value;
        }

        [NonSerialized]
        private bool _fromReceiver;
        //UNDONE:!!!! XMLDOC ContentRepository
        public bool FromReceiver
        {
            get => _fromReceiver;
            set => _fromReceiver = value;
        }

        [NonSerialized]
        private bool _fromDatabase;
        //UNDONE:!!!! XMLDOC ContentRepository
        public bool FromDatabase
        {
            get => _fromDatabase;
            set => _fromDatabase = value;
        }

        [NonSerialized]
        private bool _executed;
        internal bool Executed
        {
            get => _executed;
            set => _executed = value;
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
            return IsUnprocessedActivity || IndexManager.Running;
        }

        //UNDONE:!!!! XMLDOC ContentRepository
        protected abstract bool ProtectedExecute();
        
        [NonSerialized]
        private IndexingActivityBase _attachedActivity;

        internal IndexingActivityBase AttachedActivity
        {
            get => _attachedActivity;
            private set => _attachedActivity = value;
        }

        /// <summary>
        /// When an activity gets executed and needs to be finalized, all activity objects that have
        /// the same id need to be finalized too. The Attach methods puts all activities with the
        /// same id to a chain to let the Finish method call the Finish method of each object in the chain.
        /// This method was needed because it is possible that the same activity arrives from different
        /// sources: e.g from messaging, from database or from direct execution.
        /// </summary>
        /// <param name="activity"></param>
        internal void Attach(IndexingActivityBase activity)
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
                SnTrace.IndexQueue.Write("Attached IndexingActivity A{0} finished.", AttachedActivity.Id);
                // finalize attached activities first
                AttachedActivity.Finish();
            }
            base.Finish();
            SnTrace.IndexQueue.Write("IndexingActivity A{0} finished.", this.Id);
        }


        // ================================================= AQ16

        //UNDONE:!!!! XMLDOC ContentRepository
        public IndexingActivityBase()
        {
            _waitingFor = new List<IndexingActivityBase>();
            _waitingForMe = new List<IndexingActivityBase>();
        }

        //UNDONE:!!!! XMLDOC ContentRepository
        public void OnDeserialization(object sender)
        {
            _waitingFor = new List<IndexingActivityBase>();
            _waitingForMe = new List<IndexingActivityBase>();
        }


        [NonSerialized]
        private List<IndexingActivityBase> _waitingFor;
        //UNDONE:!!!! XMLDOC ContentRepository
        public List<IndexingActivityBase> WaitingFor => _waitingFor;

        [NonSerialized]
        private List<IndexingActivityBase> _waitingForMe;
        //UNDONE:!!!! XMLDOC ContentRepository
        public List<IndexingActivityBase> WaitingForMe => _waitingForMe;


        internal void WaitFor(IndexingActivityBase olderActivity)
        {
            // this method must called from thread safe block.
            if (!this.WaitingFor.Any(x => x.Id == olderActivity.Id))
                this.WaitingFor.Add(olderActivity);
            if (!olderActivity.WaitingForMe.Any(x => x.Id == this.Id))
                olderActivity.WaitingForMe.Add(this);
        }

        internal void FinishWaiting(IndexingActivityBase olderActivity)
        {
            // this method must called from thread safe block.
            RemoveDependency(this.WaitingFor, olderActivity);
            RemoveDependency(olderActivity.WaitingForMe, this);
        }
        private static void RemoveDependency(List<IndexingActivityBase> dependencyList, IndexingActivityBase activity)
        {
            // this method must called from thread safe block.
            dependencyList.RemoveAll(x => x.Id == activity.Id);
        }

        internal bool IsInTree(IndexingActivityBase newerActivity)
        {
            return this.Path.StartsWith(newerActivity.Path, StringComparison.OrdinalIgnoreCase);
        }
    }
}