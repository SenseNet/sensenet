using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using STT=System.Threading.Tasks;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Diagnostics;
// ReSharper disable ArrangeThisQualifier

namespace SenseNet.ContentRepository.Search.Indexing.Activities
{
    /// <summary>
    /// Defines a base class of the indexing activities for storage layer.
    /// </summary>
    [Serializable]
    public abstract class IndexingActivityBase : DistributedIndexingActivity, IIndexingActivity, IDeserializationCallback
    {
        /* ====================================================== stored properties */

        /// <inheritdoc cref="IIndexingActivity.Id"/>
        public int Id { get; set; }
        /// <inheritdoc cref="IIndexingActivity.ActivityType"/>
        public IndexingActivityType ActivityType { get; set; }
        /// <inheritdoc cref="IIndexingActivity.CreationDate"/>
        public DateTime CreationDate { get; set; }
        /// <inheritdoc cref="IIndexingActivity.RunningState"/>
        public IndexingActivityRunningState RunningState { get; set; }
        /// <inheritdoc cref="IIndexingActivity.LockTime"/>
        public DateTime? LockTime { get; set; }
        /// <inheritdoc cref="IIndexingActivity.NodeId"/>
        public int NodeId { get; set; }
        /// <inheritdoc cref="IIndexingActivity.VersionId"/>
        public int VersionId { get; set; }
        /// <inheritdoc cref="IIndexingActivity.Path"/>
        public string Path { get; set; }
        /// <inheritdoc cref="IIndexingActivity.VersionTimestamp"/>
        public long? VersionTimestamp { get; set; }

        /// <inheritdoc cref="IIndexingActivity.Extension"/>
        public string Extension
        {
            get => GetExtension();
            set => SetExtension(value);
        }

        /// <summary>
        /// Returns with the loaded extension data.
        /// The data is not interpreted, serialized string.
        /// </summary>
        /// <returns></returns>
        protected abstract string GetExtension();
        /// <summary>
        /// Sets an extension data to save.
        /// The data is not interpreted, serialized string.
        /// </summary>
        protected abstract void SetExtension(string value);

        /* ====================================================== not stored properties */

        /// <summary>
        /// Gets or sets the <see cref="IndexDocumentData"/> which the activity applies.
        /// </summary>
        public IndexDocumentData IndexDocumentData { get; set; }


        [NonSerialized]
        private bool _isUnprocessedActivity;
        /// <inheritdoc cref="IIndexingActivity.IsUnprocessedActivity"/>
        public bool IsUnprocessedActivity
        {
            get => _isUnprocessedActivity;
            set => _isUnprocessedActivity = value;
        }

        [NonSerialized]
        private bool _fromReceiver;
        /// <summary>
        /// Gets or sets a value that is true if the activity is received from a messaging channel.
        /// </summary>
        public bool FromReceiver
        {
            get => _fromReceiver;
            set => _fromReceiver = value;
        }

        [NonSerialized]
        private bool _fromDatabase;
        /// <summary>
        /// Gets or sets a value that is true if the activity is loaded from the database.
        /// </summary>
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


        internal override async STT.Task ExecuteIndexingActivityAsync(CancellationToken cancellationToken)
        {
            // if not running or paused, skip execution except executing unprocessed activities
            if (!IsExecutable())
            {
                SnTrace.Index.Write($"LM: {this} skipped. ActivityId:{Id}, ExecutingUnprocessedActivities:{IsUnprocessedActivity}");
                return;
            }
            SnTrace.Index.Write($"LM: {this}. ActivityId:{Id}, ExecutingUnprocessedActivities:{IsUnprocessedActivity}");

            bool successful;
            try
            {
                successful = await ProtectedExecuteAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (SerializationException)
            {
                if (IsUnprocessedActivity && this is DocumentIndexingActivity docActivity)
                {
                    SetNotIndexableContent(docActivity);
                    successful = true;
                }
                else
                {
                    throw;
                }
            }

            if(successful)
                _executed = true;

            // ActivityFinished must be called after executing an activity, even if index is not changed
            if (IsExecutable())
                IndexManager.ActivityFinished(Id, IsUnprocessedActivity);
        }
        private bool IsExecutable()
        {
            // if not running or paused, skip execution except executing unprocessed activities
            return IsUnprocessedActivity || IndexManager.Running;
        }

        /// <summary>
        /// Defines the customizable method to reach the activity's main goal.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        protected abstract STT.Task<bool> ProtectedExecuteAsync(CancellationToken cancellationToken);
        
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
            SnTrace.IndexQueue.Write("IndexingActivity A{0} finished.", Id);
        }


        // ================================================= AQ16

        /// <summary>
        /// Initializes a new IndexingActivityBase instance.
        /// </summary>
        protected IndexingActivityBase()
        {
            _waitingFor = new List<IndexingActivityBase>();
            _waitingForMe = new List<IndexingActivityBase>();
        }

        /// <summary>
        /// Constructor for deserialization.
        /// </summary>
        public void OnDeserialization(object sender)
        {
            _waitingFor = new List<IndexingActivityBase>();
            _waitingForMe = new List<IndexingActivityBase>();
        }


        [NonSerialized]
        private List<IndexingActivityBase> _waitingFor;
        /// <summary>
        /// Gets the indexing activities that block this instance.
        /// Used when the current indexing engine uses local index.
        /// </summary>
        public List<IndexingActivityBase> WaitingFor => _waitingFor;

        [NonSerialized]
        private List<IndexingActivityBase> _waitingForMe;
        /// <summary>
        /// Gets the indexing activities that are blocked by this instance.
        /// Used when the current indexing engine uses local index.
        /// </summary>
        public List<IndexingActivityBase> WaitingForMe => _waitingForMe;


        internal void WaitFor(IndexingActivityBase olderActivity)
        {
            // this method must called from thread safe block.
            if (this.WaitingFor.All(x => x.Id != olderActivity.Id))
                this.WaitingFor.Add(olderActivity);
            if (olderActivity.WaitingForMe.All(x => x.Id != this.Id))
                olderActivity.WaitingForMe.Add(this);
        }

        internal void FinishWaiting(IndexingActivityBase olderActivity)
        {
            // this method must called from thread safe block.
            RemoveDependency(WaitingFor, olderActivity);
            RemoveDependency(olderActivity.WaitingForMe, this);
        }
        private static void RemoveDependency(List<IndexingActivityBase> dependencyList, IndexingActivityBase activity)
        {
            // this method must called from thread safe block.
            dependencyList.RemoveAll(x => x.Id == activity.Id);
        }

        internal bool IsInTree(IndexingActivityBase newerActivity)
        {
            return Path.StartsWith(newerActivity.Path, StringComparison.OrdinalIgnoreCase);
        }

        /*  ========================================================= Not serializable collection support */

        // (key: version id, value: content id).
        internal static readonly Dictionary<int, int> NotIndexableContentCollection = new Dictionary<int, int>();
        private static readonly object NotIndexableContentCollectionSync = new object();
        private static void SetNotIndexableContent(DocumentIndexingActivity activity)
        {
            lock (NotIndexableContentCollectionSync)
                NotIndexableContentCollection[activity.VersionId] = activity.NodeId;
        }
    }
}