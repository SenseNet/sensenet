#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Search.Indexing.Integrity;
using IoFile = System.IO.File;

namespace SenseNet.ContentRepository.Search.Indexing.Integrity;

internal sealed class IndexIntegrityCheckService(IIndexManager indexManager, TimeProvider clock)
{
    private static readonly JsonSerializerOptions SnapshotJson = new() { IncludeFields = true };

    public async Task<IndexIntegrityCheckResult> CheckAsync(IIndexIntegrityDataSource dataSource,
        IIndexIntegrityEngine engine, IndexIntegrityWork work, Action<Difference> report, CancellationToken cancel)
    {
        var databasePath = Path.Combine(work.ScratchDirectory, "database.bin");
        var indexPath = Path.Combine(work.ScratchDirectory, "index.jsonl");
        var issuesPath = Path.Combine(work.ScratchDirectory, "issues.jsonl");
        var started = clock.GetUtcNow();
        var timestamp = clock.GetTimestamp();
        DateTimeOffset? captured = null;
        var excluded = indexManager.GetNotIndexedNodeTypes().ToHashSet();
        int databaseActivity;
        using (var writer = new BinaryWriter(IoFile.Create(databasePath)))
        {
            var count = 0;
            databaseActivity = await dataSource.ExportIntegrityVersionsAsync(excluded,
                () => captured = clock.GetUtcNow(), version =>
                {
                    WriteDatabaseVersion(writer, version);
                    if (++count % 4096 == 0) work.CheckCapacity();
                }, cancel).ConfigureAwait(false);
        }
        var databasePhase = FinishPhase(started, timestamp, captured);
        work.CheckCapacity();

        started = clock.GetUtcNow();
        timestamp = clock.GetTimestamp();
        long commitVersion;
        int indexActivity, maxDoc;
        int[] gaps;
        using (var reader = engine.OpenIntegrityReader(cancel))
        using (var index = IoFile.CreateText(indexPath))
        using (var issues = IoFile.CreateText(issuesPath))
        {
            captured = clock.GetUtcNow();
            commitVersion = reader.CommitVersion;
            indexActivity = reader.Activity.LastActivityId;
            gaps = reader.Activity.Gaps ?? Array.Empty<int>();
            maxDoc = reader.MaxDoc;
            var count = 0;

            void CheckExportCapacity()
            {
                index.Flush();
                issues.Flush();
                work.CheckCapacity();
            }

            void CountExportedRecord()
            {
                if (++count % 4096 == 0)
                    CheckExportCapacity();
            }

            reader.Export(work.ScratchDirectory, row =>
            {
                index.WriteLine(JsonSerializer.Serialize(row, SnapshotJson));
                CountExportedRecord();
            }, difference =>
            {
                issues.WriteLine(JsonSerializer.Serialize(new ExportedIssue(difference, difference.Index), SnapshotJson));
                CountExportedRecord();
            }, CheckExportCapacity, cancel);
        }
        var indexPhase = FinishPhase(started, timestamp, captured);
        work.CheckCapacity();

        started = clock.GetUtcNow();
        timestamp = clock.GetTimestamp();
        var draftPaths = new BitArray(maxDoc);
        var distinct = new BitArray(maxDoc);
        long differentDocuments = 0;
        using var lookup = new BinaryReader(IoFile.OpenRead(databasePath));
        var reporter = new DifferenceReporter(difference =>
        {
            cancel.ThrowIfCancellationRequested();
            if (difference.DocId is not int docId || Mark(distinct, docId))
                differentDocuments++;
            if (difference.Database == null && difference.VersionId is int versionId)
                difference = difference with { Database = FindDatabaseVersion(lookup, versionId) };
            report(difference);
        });
        foreach (var line in IoFile.ReadLines(issuesPath))
        {
            var issue = JsonSerializer.Deserialize<ExportedIssue>(line, SnapshotJson)!;
            reporter.Report(issue.Difference with { Index = issue.Index });
        }
        var counts = IndexIntegrityComparer.Compare(ReadDatabase(databasePath).Where(row => row.IndexingEnabled),
            IoFile.ReadLines(indexPath).Select(line => JsonSerializer.Deserialize<SavedIndexVersion>(line, SnapshotJson)),
            reporter, id => Mark(draftPaths, id), cancel);
        return new(counts, differentDocuments, databaseActivity, commitVersion, indexActivity, gaps,
            databasePhase, indexPhase, FinishPhase(started, timestamp));
    }

    private IndexIntegrityPhase FinishPhase(DateTimeOffset start, long timestamp, DateTimeOffset? captured = null) =>
        new(start, clock.GetUtcNow(), (long)clock.GetElapsedTime(timestamp).TotalMilliseconds, captured);

    private static bool Mark(BitArray array, int index)
    {
        if (array[index]) return false;
        array[index] = true;
        return true;
    }

    // Fixed-size records allow the structural issues to be enriched by VersionId without loading the SQL export.
    private const int DatabaseRecordSize = 49;

    private static void WriteDatabaseVersion(BinaryWriter writer, DatabaseVersion version)
    {
        writer.Write(version.VersionId);
        writer.Write(version.VersionTimestamp);
        writer.Write(version.NodeId);
        writer.Write(version.NodeTimestamp);
        writer.Write(version.LastMajorVersionId ?? 0);
        writer.Write(version.LastMinorVersionId ?? 0);
        writer.Write(version.NodeModificationDateUtc?.Ticks ?? -1);
        writer.Write(version.VersionModificationDateUtc?.Ticks ?? -1);
        writer.Write(version.IndexingEnabled);
    }

    private static DatabaseVersion ReadDatabaseVersion(BinaryReader reader) =>
        new(reader.ReadInt32(), reader.ReadInt64(), reader.ReadInt32(), reader.ReadInt64(),
            NullId(reader.ReadInt32()), NullId(reader.ReadInt32()), Date(reader.ReadInt64()),
            Date(reader.ReadInt64()), reader.ReadBoolean());

    private static int? NullId(int id) => id == 0 ? null : id;
    private static DateTime? Date(long ticks) => ticks < 0 ? null : new DateTime(ticks, DateTimeKind.Utc);

    private static IEnumerable<DatabaseVersion> ReadDatabase(string path)
    {
        using var reader = new BinaryReader(IoFile.OpenRead(path));
        while (reader.BaseStream.Position < reader.BaseStream.Length)
            yield return ReadDatabaseVersion(reader);
    }

    private static DatabaseVersion? FindDatabaseVersion(BinaryReader reader, int versionId)
    {
        long low = 0, high = reader.BaseStream.Length / DatabaseRecordSize - 1;
        while (low <= high)
        {
            var middle = low + (high - low) / 2;
            reader.BaseStream.Position = middle * DatabaseRecordSize;
            var row = ReadDatabaseVersion(reader);
            if (row.VersionId == versionId) return row;
            if (row.VersionId < versionId) low = middle + 1;
            else high = middle - 1;
        }
        return null;
    }

    private sealed record ExportedIssue(Difference Difference, SavedIndexVersion? Index);
}
