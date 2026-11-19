using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Diagnostics;
using SenseNet.Search.Indexing;
using STT = System.Threading.Tasks;

namespace SenseNet.ContentRepository.Search.Indexing
{
    internal sealed class LegacyIndexCatchUpProvider : IIndexCatchUpProvider
    {
        private readonly IDataStore _dataStore;
        private readonly IIndexingActivityFactory _activityFactory;

        public LegacyIndexCatchUpProvider(IDataStore dataStore, IIndexingActivityFactory activityFactory)
        {
            _dataStore = dataStore;
            _activityFactory = activityFactory;
        }

        public async STT.Task CatchUpAsync(IndexManager indexManager, IndexingActivityStatus status,
            TextWriter consoleOut, CancellationToken cancellationToken)
        {
            var lastDatabaseId = await _dataStore.GetLastIndexingActivityIdAsync(cancellationToken).ConfigureAwait(false);
            var lastExecutedId = status.LastActivityId;
            var gaps = status.Gaps ?? Array.Empty<int>();

            using var op = SnTrace.Index.StartOperation(
                "IAQ: InitializeFromIndex. LastIndexedActivityId: {0}, LastDatabaseId: {1}, TotalUnprocessed: {2}",
                lastExecutedId, lastDatabaseId, lastDatabaseId - lastExecutedId + gaps.Length);

            IEnumerable<IIndexingActivity> activities = Enumerable.Empty<IIndexingActivity>();
            if (gaps.Length > 0)
                activities = new DistributedIndexingActivityQueue.IndexingActivityLoader(gaps, true, _activityFactory);
            if (lastExecutedId < lastDatabaseId)
                activities = activities.Concat(new DistributedIndexingActivityQueue.IndexingActivityLoader(
                    lastExecutedId + 1, lastDatabaseId, true, _activityFactory));

            indexManager.DistributedIndexingActivityQueue.ReplayUnprocessedActivities(
                lastDatabaseId, lastExecutedId, gaps, activities, consoleOut);
            op.Successful = true;
        }
    }
}
