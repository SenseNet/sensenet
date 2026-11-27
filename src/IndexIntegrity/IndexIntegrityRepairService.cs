#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using STT = System.Threading.Tasks;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Search;
using SenseNet.Search.Indexing.Integrity;

namespace SenseNet.ContentRepository.Search.Indexing.Integrity;

internal sealed class IndexIntegrityRepairService(IDataStore dataStore)
{
    private const int BatchSize = 128;

    public async STT.Task RepairAsync(IIndexIntegrityDataSource source, IIndexIntegrityEngine engine, IndexManager indexManager,
        IndexIntegrityWork work, IReadOnlyList<IndexIntegritySelection> selection,
        Action<IndexIntegrityRepairItem> report, CancellationToken cancel)
    {
        var targets = selection.Where(item => item.RepairableByVersionId && item.VersionId is > 0)
            .GroupBy(item => item.VersionId!.Value);
        foreach (var item in selection.Where(item => !item.RepairableByVersionId || item.VersionId is not > 0))
            report(new(item.DifferenceId, item.VersionId, "manual_review_required"));

        foreach (var batch in targets.Chunk(BatchSize))
        {
            var result = await RepairBatchAsync(source, engine, indexManager, work, batch, cancel)
                .ConfigureAwait(false);
            foreach (var item in result)
                report(item);
        }
    }

    private async Task<IReadOnlyList<IndexIntegrityRepairItem>> RepairBatchAsync(IIndexIntegrityDataSource source,
        IIndexIntegrityEngine engine, IndexManager indexManager, IndexIntegrityWork work,
        IGrouping<int, IndexIntegritySelection>[] batch, CancellationToken cancel)
    {
        var ids = batch.Select(group => group.Key).ToArray();
        for (var attempt = 0; attempt < 3; attempt++)
        {
            cancel.ThrowIfCancellationRequested();
            work.CheckCapacity();
            var probe = engine.ProbeIntegrity(ids, work.ScratchDirectory, cancel);
            work.CheckCapacity();
            var results = await TryApplyAsync(source, engine, indexManager, batch, ids, probe, cancel)
                .ConfigureAwait(false);
            if (results != null)
                return results;

            if (attempt < 2)
                await STT.Task.Delay(TimeSpan.FromMilliseconds(100), cancel).ConfigureAwait(false);
        }
        return batch.SelectMany(group => group.Select(item =>
            new IndexIntegrityRepairItem(item.DifferenceId, item.VersionId, "retryable_conflict"))).ToArray();
    }

    private async Task<IReadOnlyList<IndexIntegrityRepairItem>?> TryApplyAsync(IIndexIntegrityDataSource source,
        IIndexIntegrityEngine engine, IndexManager indexManager, IGrouping<int, IndexIntegritySelection>[] batch,
        int[] ids, IndexIntegrityProbe probe, CancellationToken cancel)
    {
        var expected = (await source.ReadIntegrityVersionsAsync(ids,
            indexManager.GetNotIndexedNodeTypes().ToHashSet(), cancel).ConfigureAwait(false))
            .Where(row => row.IndexingEnabled).ToDictionary(row => row.VersionId);
        var indexed = probe.Documents.ToLookup(row => row.VersionId);
        var issues = probe.Issues.ToLookup(issue => issue.VersionId);
        var result = new List<IndexIntegrityRepairItem>();
        var approved = new Dictionary<int, IndexIntegritySelection[]>();
        var updates = new List<IndexIntegrityUpdate>();
        foreach (var group in batch)
        {
            var documents = indexed[group.Key].ToArray();
            if (documents.Any(row => row.Metadata.AmbiguousVersionId))
            {
                result.AddRange(group.Select(item =>
                    new IndexIntegrityRepairItem(item.DifferenceId, item.VersionId, "manual_review_required")));
                continue;
            }

            var differences = issues[group.Key].ToList();
            IndexIntegrityComparer.Compare(expected.TryGetValue(group.Key, out var stored) ? new[] { stored } :
                    Array.Empty<DatabaseVersion>(), documents, new DifferenceReporter(differences.Add), _ => true, cancel);
            foreach (var item in group.Where(item => !Matches(differences, item)))
                result.Add(new(item.DifferenceId, item.VersionId,
                    differences.Count == 0 ? "already_resolved" : "changed"));
            var selected = group.Where(item => Matches(differences, item)).ToArray();
            if (selected.Length == 0)
                continue;
            approved.Add(group.Key, selected);
            if (!expected.ContainsKey(group.Key))
                updates.Add(new(group.Key, null));
        }

        var loadIds = approved.Keys.Where(expected.ContainsKey).ToArray();
        if (loadIds.Length > 0)
        {
            var storedDocuments = (await dataStore.LoadIndexDocumentsAsync(loadIds, cancel).ConfigureAwait(false))
                .ToDictionary(row => row.VersionId);
            foreach (var id in loadIds)
            {
                var sourceVersion = expected[id];
                if (!storedDocuments.TryGetValue(id, out var row) || row.NodeId != sourceVersion.NodeId ||
                    row.VersionTimestamp != sourceVersion.VersionTimestamp || row.NodeTimestamp != sourceVersion.NodeTimestamp)
                    continue;
                try
                {
                    var document = indexManager.CompleteIndexDocument(row);
                    if (document == null ||
                        !document.Fields.TryGetValue(IndexFieldName.VersionId, out var versionField) ||
                        versionField.Type != IndexValueType.Int || versionField.IntegerValue != id ||
                        !document.Fields.TryGetValue(IndexFieldName.NodeId, out var nodeField) ||
                        nodeField.Type != IndexValueType.Int || nodeField.IntegerValue != row.NodeId)
                        continue;
                    updates.Add(new(id, document));
                }
                catch (Exception e) when (e is FormatException or ArgumentException or InvalidOperationException
                                             or System.IO.InvalidDataException or Newtonsoft.Json.JsonException
                                             or SerializationException)
                {
                    // Other selected versions can be repaired when this source document is malformed.
                }
            }
        }

        if (!engine.TryApplyIntegrityRepairs(probe.Revision, updates, cancel))
            return null;
        var applied = updates.Select(update => update.VersionId).ToHashSet();
        foreach (var (version, items) in approved)
        foreach (var item in items)
            result.Add(new(item.DifferenceId, version, applied.Contains(version) ? "repaired" : "invalid_source_document"));
        return result;
    }

    private static bool Matches(List<Difference> current, IndexIntegritySelection selected) =>
        current.Any(difference => difference.Kind == selected.Kind &&
            (difference.VersionId ?? difference.Index?.VersionId) == selected.VersionId &&
            (selected.Field == null || difference.Field == selected.Field));
}
