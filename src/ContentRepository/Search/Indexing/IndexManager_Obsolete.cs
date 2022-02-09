using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Search.Indexing.Activities;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Search.Indexing;
using SenseNet.Search.Querying;
using STT = System.Threading.Tasks;

namespace SenseNet.ContentRepository.Search.Indexing
{
    //UNDONE:<?xxx: Delete IndexManager and rename IndexManager_INSTANCE to IndexManager if all references rewritten in the ecosystem
    public static class IndexManager // alias LuceneManager
    {
        private static IndexManager_INSTANCE IndexManagerImplementation => (IndexManager_INSTANCE)Providers.Instance.IndexManager;

        [Obsolete("Use Providers.Instance.IndexManager instead.", true)]
        internal static DistributedIndexingActivityQueue DistributedIndexingActivityQueue
            => IndexManagerImplementation.DistributedIndexingActivityQueue;

        /* ==================================================================== Managing index */

        [Obsolete("Use Providers.Instance.IndexManager instead.", true)]
        public static IIndexingEngine IndexingEngine => Providers.Instance.SearchManager.SearchEngine.IndexingEngine;

        [Obsolete("Use Providers.Instance.IndexManager instead.", true)]
        public static bool Running => IndexManagerImplementation.Running;

        [Obsolete("Use Providers.Instance.IndexManager instead.", true)]
        public static int[] GetNotIndexedNodeTypes()
            => IndexManagerImplementation.GetNotIndexedNodeTypes();

        [Obsolete("Use Providers.Instance.IndexManager instead.", true)]
        public static STT.Task StartAsync(TextWriter consoleOut, CancellationToken cancellationToken)
            => IndexManagerImplementation.StartAsync(consoleOut, cancellationToken);

        [Obsolete("Use Providers.Instance.IndexManager instead.", true)]
        public static void ShutDown()
            => IndexManagerImplementation.ShutDown();

        [Obsolete("Use Providers.Instance.IndexManager instead.", true)]
        public static STT.Task ClearIndexAsync(CancellationToken cancellationToken)
            => IndexManagerImplementation.ClearIndexAsync(cancellationToken);

        /* ========================================================================================== Activity */

        [Obsolete("Use Providers.Instance.IndexManager instead.", true)]
        public static STT.Task RegisterActivityAsync(IndexingActivityBase activity, CancellationToken cancellationToken)
            => IndexManagerImplementation.RegisterActivityAsync(activity, cancellationToken);

        [Obsolete("Use Providers.Instance.IndexManager instead.", true)]
        public static STT.Task ExecuteActivityAsync(IndexingActivityBase activity, CancellationToken cancellationToken)
            => IndexManagerImplementation.ExecuteActivityAsync(activity, cancellationToken);

        [Obsolete("Use Providers.Instance.IndexManager instead.", true)]
        public static int GetLastStoredIndexingActivityId()
            => IndexManagerImplementation.GetLastStoredIndexingActivityId();

        [Obsolete("Use Providers.Instance.IndexManager instead.", true)]
        internal static STT.Task DeleteAllIndexingActivitiesAsync(CancellationToken cancellationToken)
            => IndexManagerImplementation.DeleteAllIndexingActivitiesAsync(cancellationToken);

        [Obsolete("Use Providers.Instance.IndexManager instead.", true)]
        public static IndexingActivityStatus GetCurrentIndexingActivityStatus()
            => IndexManagerImplementation.GetCurrentIndexingActivityStatus();

        [Obsolete("Use Providers.Instance.IndexManager instead.", true)]
        public static STT.Task DeleteRestorePointsAsync(CancellationToken cancellationToken)
            => IndexManagerImplementation.DeleteRestorePointsAsync(cancellationToken);

        [Obsolete("Use Providers.Instance.IndexManager instead.", true)]
        public static STT.Task<IndexingActivityStatus> LoadCurrentIndexingActivityStatusAsync(CancellationToken cancellationToken)
            => IndexManagerImplementation.LoadCurrentIndexingActivityStatusAsync(cancellationToken);

        [Obsolete("Use Providers.Instance.IndexManager instead.", true)]
        public static STT.Task<IndexingActivityStatusRestoreResult> RestoreIndexingActivityStatusAsync(IndexingActivityStatus status, CancellationToken cancellationToken)
            => IndexManagerImplementation.RestoreIndexingActivityStatusAsync(status, cancellationToken);

        /*========================================================================================== Commit */

        [Obsolete("Use Providers.Instance.IndexManager instead.", true)]
        internal static void ActivityFinished(int activityId, bool executingUnprocessedActivities)
            => IndexManagerImplementation.ActivityFinished(activityId, executingUnprocessedActivities);

        [Obsolete("Use Providers.Instance.IndexManager instead.", true)]
        internal static void ActivityFinished(int activityId)
            => IndexManagerImplementation.ActivityFinished(activityId);

        [Obsolete("Use Providers.Instance.IndexManager instead.", true)]
        internal static STT.Task CommitAsync(CancellationToken cancellationToken)
            => IndexManagerImplementation.CommitAsync(cancellationToken);

        /* ==================================================================== Document operations */

        [Obsolete("Use Providers.Instance.IndexManager instead.", true)]
        internal static STT.Task AddDocumentsAsync(IEnumerable<IndexDocument> documents, CancellationToken cancellationToken)
            => IndexManagerImplementation.AddDocumentsAsync(documents, cancellationToken);

        [Obsolete("Use Providers.Instance.IndexManager instead.", true)]
        internal static STT.Task<bool> AddDocumentAsync(IndexDocument document, VersioningInfo versioning, CancellationToken cancellationToken)
            => IndexManagerImplementation.AddDocumentAsync(document, versioning, cancellationToken);

        [Obsolete("Use Providers.Instance.IndexManager instead.", true)]
        internal static STT.Task<bool> UpdateDocumentAsync(IndexDocument document, VersioningInfo versioning, CancellationToken cancellationToken)
            => IndexManagerImplementation.UpdateDocumentAsync(document, versioning, cancellationToken);

        [Obsolete("Use Providers.Instance.IndexManager instead.", true)]
        internal static STT.Task<bool> DeleteDocumentsAsync(IEnumerable<SnTerm> deleteTerms, VersioningInfo versioning, CancellationToken cancellationToken)
            => IndexManagerImplementation.DeleteDocumentsAsync(deleteTerms, versioning, cancellationToken);

        internal static STT.Task<bool> AddTreeAsync(string treeRoot, int activityId, bool executingUnprocessedActivities, CancellationToken cancellationToken)
            => IndexManagerImplementation.AddTreeAsync(treeRoot, activityId, executingUnprocessedActivities, cancellationToken);

        /* ==================================================================== IndexDocument management */

        [Obsolete("Use Providers.Instance.IndexManager instead.", true)]
        internal static IPerFieldIndexingInfo NameFieldIndexingInfo => IndexManagerImplementation.NameFieldIndexingInfo;
        [Obsolete("Use Providers.Instance.IndexManager instead.", true)]
        internal static IPerFieldIndexingInfo PathFieldIndexingInfo => IndexManagerImplementation.PathFieldIndexingInfo;
        [Obsolete("Use Providers.Instance.IndexManager instead.", true)]
        internal static IPerFieldIndexingInfo InTreeFieldIndexingInfo => IndexManagerImplementation.InTreeFieldIndexingInfo;
        [Obsolete("Use Providers.Instance.IndexManager instead.", true)]
        internal static IPerFieldIndexingInfo InFolderFieldIndexingInfo => IndexManagerImplementation.InFolderFieldIndexingInfo;
        [Obsolete("Use Providers.Instance.IndexManager instead.", true)]
        internal static IndexDocument LoadIndexDocumentByVersionId(int versionId) => IndexManagerImplementation.LoadIndexDocumentByVersionId(versionId);
        [Obsolete("Use Providers.Instance.IndexManager instead.", true)]
        internal static IEnumerable<IndexDocument> LoadIndexDocumentsByVersionId(int[] versionIds) => IndexManagerImplementation.LoadIndexDocumentsByVersionId(versionIds);
        [Obsolete("Use Providers.Instance.IndexManager instead.", true)]
        internal static IndexDocument CompleteIndexDocument(IndexDocumentData docData) => IndexManagerImplementation.CompleteIndexDocument(docData);
    }
}
