using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using SenseNet.ContentRepository.Search.Indexing.Activities;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Diagnostics;
using SenseNet.Search;
using SenseNet.Search.Indexing;
using SenseNet.Search.Querying;
using STT = System.Threading.Tasks;

namespace SenseNet.ContentRepository.Search.Indexing
{
    internal sealed class ExecutionFrameIndexCatchUpProvider : IIndexCatchUpProvider
    {
        private const int BatchSize = 200;
        private const int MaxDocumentWorkers = 8;
        private readonly IDataStore _dataStore;

        public ExecutionFrameIndexCatchUpProvider(IDataStore dataStore)
        {
            _dataStore = dataStore;
        }

        public bool BufferLiveActivities => true;

        public async STT.Task CatchUpAsync(IndexManager indexManager, IndexingActivityStatus status,
            TextWriter consoleOut, CancellationToken cancellationToken)
        {
            if (indexManager.IndexingEngine is not IIndexCatchUpEngine engine)
                throw new NotSupportedException("Execution frame catch-up requires an index engine with atomic catch-up support.");

            var gaps = status.Gaps ?? Array.Empty<int>();
            if (status.LastActivityId < 0 || gaps.Any(id => id <= 0 || id > status.LastActivityId))
                throw new InvalidOperationException("Invalid indexing activity status for execution frame catch-up.");

            var dataProvider = _dataStore.DataProvider;
            var upperId = await dataProvider.GetLastIndexingActivityIdAsync(cancellationToken).ConfigureAwait(false);
            if (upperId < status.LastActivityId)
                throw new InvalidOperationException("The index activity checkpoint is ahead of the activity log.");

            var lowerId = gaps.Length == 0 ? (long)status.LastActivityId + 1 : gaps.Min();
            var queue = indexManager.DistributedIndexingActivityQueue;
            if (lowerId > upperId)
            {
                queue.InitializeFromCatchUp(status);
                return;
            }

            using var op = SnTrace.Index.StartOperation("Execution frame catch-up from A{0} through A{1}.", lowerId, upperId);
            consoleOut?.WriteLine("Executing indexing activity frames. {0}-{1}", lowerId, upperId);
            using var session = engine.BeginCatchUp(cancellationToken);
            var excludedNodeTypes = indexManager.GetNotIndexedNodeTypes().ToHashSet();
            var activities = new List<IndexingActivityMetadata>(BatchSize);
            var scheduledVersions = Array.Empty<int>();

            for (var nextId = lowerId; nextId <= upperId;)
            {
                var count = (int)Math.Min(BatchSize, upperId - nextId + 1);
                var page = await LoadVerifiedPageAsync(dataProvider, (int)nextId, count, cancellationToken)
                    .ConfigureAwait(false);

                for (var i = 0; i < page.Length; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var activity = page[i];
                    if (activity.ActivityType == IndexingActivityType.RemoveTree)
                    {
                        await WriteActivityBatchAsync(activities, scheduledVersions, indexManager, session,
                            excludedNodeTypes, cancellationToken).ConfigureAwait(false);
                        activities.Clear();

                        scheduledVersions = await CloseFrameAsync(activity.Path,
                            page.Skip(i + 1).TakeWhile(item => item.ActivityType != IndexingActivityType.RemoveTree),
                            indexManager, session, excludedNodeTypes, cancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    activities.Add(activity);
                    if (activities.Count == BatchSize)
                    {
                        await WriteActivityBatchAsync(activities, scheduledVersions, indexManager, session,
                            excludedNodeTypes, cancellationToken).ConfigureAwait(false);
                        activities.Clear();
                        scheduledVersions = Array.Empty<int>();
                    }
                }

                nextId += count;
            }

            await WriteActivityBatchAsync(activities, scheduledVersions, indexManager, session, excludedNodeTypes, cancellationToken)
                .ConfigureAwait(false);

            var completed = new IndexingActivityStatus { LastActivityId = upperId };
            await session.CommitAsync(completed, cancellationToken).ConfigureAwait(false);
            queue.InitializeFromCatchUp(completed);
            op.Successful = true;
        }

        private static async STT.Task<IndexingActivityMetadata[]> LoadVerifiedPageAsync(DataProvider dataProvider,
            int fromId, int count, CancellationToken cancellationToken)
        {
            var toId = fromId + (count - 1);
            for (var attempt = 0; ; attempt++)
            {
                var page = await dataProvider.LoadIndexingActivityMetadataAsync(fromId, toId, count, cancellationToken)
                    .ConfigureAwait(false);
                var index = 0;
                while (index < page.Length && index < count && page[index].Id == fromId + index)
                    index++;
                if (index == count && page.Length == count)
                    return page;

                // An identity hole and a lost activity look the same without additional log evidence.
                if (attempt == 2)
                    throw new InvalidOperationException($"Incomplete indexing activity history in range {fromId}-{toId}.");
                await STT.Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }
        }

        private async STT.Task WriteActivityBatchAsync(List<IndexingActivityMetadata> activities, int[] scheduledVersions,
            IndexManager indexManager,
            IIndexCatchUpSession session, HashSet<int> excludedNodeTypes, CancellationToken cancellationToken)
        {
            if (activities.Count == 0 && scheduledVersions.Length == 0)
                return;

            var versions = scheduledVersions.ToHashSet();
            var rebuildNodes = new HashSet<int>();
            var documentNodes = new HashSet<int>();
            var trees = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var activity in activities)
            {
                switch (activity.ActivityType)
                {
                    case IndexingActivityType.AddDocument:
                    case IndexingActivityType.UpdateDocument:
                        if (activity.VersionId <= 0)
                            throw new InvalidOperationException($"Missing version ID in indexing activity {activity.Id}.");
                        versions.Add(activity.VersionId);
                        documentNodes.Add(activity.NodeId);
                        var deleted = VersioningInfo.Deserialize(activity.Extension)?.Delete;
                        if (deleted != null)
                            versions.UnionWith(deleted);
                        break;
                    case IndexingActivityType.AddTree:
                        trees.Add(activity.Path);
                        break;
                    case IndexingActivityType.Rebuild:
                        rebuildNodes.Add(activity.NodeId);
                        break;
                    case IndexingActivityType.Restore:
                        break;
                    default:
                        throw new NotSupportedException($"Unknown catch-up activity type: {activity.ActivityType}.");
                }
            }

            foreach (var path in trees)
            {
                versions.UnionWith(await _dataStore.DataProvider.LoadIndexingSubtreeVersionIdsAsync(
                    NormalizePath(path), cancellationToken).ConfigureAwait(false));
            }

            var nodeIds = documentNodes.Union(rebuildNodes).Where(id => id > 0).ToArray();
            if (nodeIds.Length != 0)
            {
                var heads = (await _dataStore.DataProvider.LoadNodeHeadsAsync(nodeIds, cancellationToken)
                    .ConfigureAwait(false)).ToDictionary(head => head.Id);
                foreach (var nodeId in nodeIds)
                {
                    var indexed = session.GetVersions(new SnTerm(IndexFieldName.NodeId, nodeId), cancellationToken);
                    heads.TryGetValue(nodeId, out var head);
                    if (rebuildNodes.Contains(nodeId))
                    {
                        if (head != null)
                            versions.UnionWith(head.Versions.Select(version => version.VersionId));
                        versions.UnionWith(indexed.Select(version => version.VersionId));
                    }
                    else
                    {
                        AddChangedVersionFlags(versions, head, indexed);
                    }
                }
            }

            await WriteVersionsAsync(versions, indexManager, session, excludedNodeTypes, cancellationToken)
                .ConfigureAwait(false);
        }

        private static void AddChangedVersionFlags(HashSet<int> versions, NodeHead head,
            IReadOnlyList<IndexCatchUpVersion> indexed)
        {
            var lastDraft = head?.LastMinorVersionId ?? 0;
            var lastPublic = head?.LastMajorVersionId ?? 0;
            if (lastPublic > 0 && !head.Versions.Any(v =>
                    v.VersionId == lastPublic && v.VersionNumber.Status == VersionStatus.Approved))
                lastPublic = 0;

            if (lastDraft > 0 && !indexed.Any(v => v.VersionId == lastDraft && v.IsLastDraft))
                versions.Add(lastDraft);
            if (lastPublic > 0 && !indexed.Any(v => v.VersionId == lastPublic && v.IsLastPublic))
                versions.Add(lastPublic);

            foreach (var version in indexed)
            {
                if (version.IsLastDraft != (version.VersionId == lastDraft) ||
                    version.IsLastPublic != (version.VersionId == lastPublic))
                    versions.Add(version.VersionId);
            }
        }

        private async STT.Task<int[]> CloseFrameAsync(string path, IEnumerable<IndexingActivityMetadata> nextFrame,
            IndexManager indexManager, IIndexCatchUpSession session, HashSet<int> excludedNodeTypes,
            CancellationToken cancellationToken)
        {
            path = NormalizePath(path);
            var current = await _dataStore.DataProvider.LoadIndexingSubtreeVersionIdsAsync(path, cancellationToken)
                .ConfigureAwait(false);
            var versions = current.ToHashSet();
            var terms = new[] { new SnTerm(IndexFieldName.InTree, path), new SnTerm(IndexFieldName.Path, path) };
            foreach (var term in terms)
                versions.UnionWith(session.GetVersions(term, cancellationToken).Select(version => version.VersionId));

            if (versions.Count == 0)
                return Array.Empty<int>();

            var rewrites = await PlanTreeRewritesAsync(versions, nextFrame, excludedNodeTypes, cancellationToken)
                .ConfigureAwait(false);
            if (rewrites != null)
            {
                await session.WriteIndexAsync(terms, Array.Empty<DocumentUpdate>(), cancellationToken).ConfigureAwait(false);
                return rewrites;
            }

            await WriteVersionsAsync(versions, indexManager, session, excludedNodeTypes, cancellationToken)
                .ConfigureAwait(false);
            return Array.Empty<int>();
        }

        private async STT.Task<int[]> PlanTreeRewritesAsync(HashSet<int> versions,
            IEnumerable<IndexingActivityMetadata> nextFrame, HashSet<int> excludedNodeTypes,
            CancellationToken cancellationToken)
        {
            // Look ahead only within the verified page. A wider or ambiguous scope is reconciled by version.
            var following = nextFrame.ToArray();
            var directVersions = following
                .Where(item => item.ActivityType is IndexingActivityType.AddDocument or IndexingActivityType.UpdateDocument)
                .Select(item => item.VersionId).ToHashSet();
            var targetPaths = following.Where(item => item.ActivityType == IndexingActivityType.AddTree)
                .Select(item => NormalizePath(item.Path)).Distinct().ToArray();
            if (directVersions.Count == 0 && targetPaths.Length == 0)
                return null;

            var rewrites = new List<int>();
            foreach (var ids in versions.Chunk(BatchSize))
            {
                var documents = await _dataStore.LoadIndexDocumentsAsync(ids, cancellationToken).ConfigureAwait(false);
                foreach (var document in documents)
                {
                    if (excludedNodeTypes.Contains(document.NodeTypeId))
                        continue;

                    if (!directVersions.Contains(document.VersionId) &&
                        !targetPaths.Any(path => RepositoryPath.IsInTree(document.Path, path)))
                        return null;

                    rewrites.Add(document.VersionId);
                }
            }
            return rewrites.ToArray();
        }

        private static string NormalizePath(string path)
        {
            if (RepositoryPath.IsValidPath(path) != RepositoryPath.PathResult.Correct)
                throw new InvalidOperationException($"Invalid tree path in indexing activity: '{path}'.");
            return path.ToLowerInvariant();
        }

        private async STT.Task WriteVersionsAsync(IEnumerable<int> versions, IndexManager indexManager,
            IIndexCatchUpSession session, HashSet<int> excludedNodeTypes, CancellationToken cancellationToken)
        {
            var parallelOptions = new STT.ParallelOptions
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = Math.Min(MaxDocumentWorkers, Environment.ProcessorCount)
            };

            foreach (var ids in versions.Where(id => id > 0).Chunk(BatchSize))
            {
                var documents = (await _dataStore.LoadIndexDocumentsAsync(ids, cancellationToken).ConfigureAwait(false))
                    .ToDictionary(document => document.VersionId);
                var updates = new DocumentUpdate[ids.Length];
                var deletions = new SnTerm[ids.Length];

                STT.Parallel.For(0, ids.Length, parallelOptions, i =>
                {
                    var id = ids[i];
                    var term = new SnTerm(IndexFieldName.VersionId, id);
                    if (!documents.TryGetValue(id, out var data) || excludedNodeTypes.Contains(data.NodeTypeId))
                    {
                        deletions[i] = term;
                        return;
                    }

                    if (data.IndexDocument is NotIndexedIndexDocument)
                    {
                        deletions[i] = term;
                        return;
                    }

                    var document = indexManager.CompleteIndexDocument(data);
                    if (document == null || document.VersionId != id)
                        throw new InvalidOperationException($"Unusable index document for version {id}.");
                    updates[i] = new DocumentUpdate { UpdateTerm = term, Document = document };
                });

                await session.WriteIndexAsync(deletions.Where(term => term != null).ToArray(),
                    updates.Where(update => update != null).ToArray(), cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
