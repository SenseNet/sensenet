#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Search.Indexing.Integrity;
using SenseNet.Search.Indexing.Integrity;

namespace SenseNet.OData.Operations.IndexIntegrity;

internal sealed class IndexIntegrityReportWriter : IDisposable
{
    private readonly StreamWriter _writer;
    private readonly FileStream _stream;
    private readonly Action _checkCapacity;
    private readonly Dictionary<DifferenceKind, long> _counts = new();
    private long _differences;

    public IndexIntegrityReportWriter(string path, Action checkCapacity)
    {
        _stream = File.Create(path);
        _writer = new StreamWriter(_stream, leaveOpen: true);
        _checkCapacity = checkCapacity;
    }

    public void Write(Difference difference)
    {
        var database = difference.Database;
        var index = difference.Index;
        var id = difference.VersionId ?? index?.VersionId;
        var metadata = index?.Metadata;
        _counts[difference.Kind] = _counts.GetValueOrDefault(difference.Kind) + 1;
        WriteLine(new
        {
            type = "difference",
            differenceId = ++_differences,
            difference.Kind,
            versionId = id is > 0 ? id : null,
            nodeId = difference.NodeId ?? database?.NodeId,
            difference.Field,
            expected = FieldValue(difference.Field, difference.Expected),
            actual = FieldValue(difference.Field, difference.Actual),
            repairableByVersionId = id is > 0 && metadata?.AmbiguousVersionId != true &&
                difference.Field != "VersionId",
            nodeModificationDateUtc = database?.NodeModificationDateUtc,
            versionModificationDateUtc = database?.VersionModificationDateUtc,
            sqlNodeTimestamp = Timestamp(database?.NodeTimestamp),
            sqlVersionTimestamp = Timestamp(database?.VersionTimestamp),
            indexNodeTimestamp = metadata is { } im && (im.Present & Fields.NodeTimestamp) != 0
                ? Timestamp(im.NodeTimestamp) : null,
            indexVersionTimestamp = metadata is { } vm && (vm.Present & Fields.VersionTimestamp) != 0
                ? Timestamp(vm.VersionTimestamp) : null,
        });
        if (_differences % 4096 == 0)
            _checkCapacity();
    }

    public JsonElement Finish(IndexIntegrityCheckResult result, IntegrityRun run)
    {
        var summary = JsonSerializer.SerializeToElement(new
        {
            type = "summary",
            schemaVersion = 1,
            status = _differences == 0 ? "consistent" : "differences",
            run.ProviderId,
            run.RepairFormatVersion,
            result.Counts.DatabaseVersions,
            result.Counts.IndexDocuments,
            result.Counts.MatchedDocuments,
            differences = _differences,
            result.DifferentDocuments,
            countsByKind = _counts,
            run.StartedAtUtc,
            run.FinishedAtUtc,
            run.DurationMs,
            phases = new { result.DatabaseExport, result.IndexExport, result.Comparison },
            lastActivity = new
            {
                database = result.DatabaseLastActivityId,
                index = new
                {
                    commitVersion = Timestamp(result.IndexCommitVersion),
                    lastActivityId = result.IndexLastActivityId,
                    gaps = result.IndexGaps
                }
            },
            sourceSnapshotsIndependent = true
        }, IndexIntegrityFileStore.Json);
        WriteLine(summary);
        _writer.Flush();
        _stream.Flush(flushToDisk: true);
        _checkCapacity();
        return summary;
    }

    private void WriteLine<T>(T value) =>
        _writer.WriteLine(JsonSerializer.Serialize(value, IndexIntegrityFileStore.Json));

    private static string? Timestamp(long? value) => value?.ToString(CultureInfo.InvariantCulture);

    private static object? FieldValue(string? field, object? value) =>
        field is "NodeTimestamp" or "VersionTimestamp" ? value switch
        {
            long timestamp => Timestamp(timestamp),
            JsonElement element when element.ValueKind == JsonValueKind.Number && element.TryGetInt64(out var timestamp)
                => Timestamp(timestamp),
            _ => value
        } : value;

    public void Dispose()
    {
        _writer.Dispose();
        _stream.Dispose();
    }

    public static async Task<IReadOnlyList<IndexIntegritySelection>> ReadSelectionAsync(FileStream stream,
        IntegrityRun check, IntegritySelectionRequest request, CancellationToken cancel)
    {
        if (stream.Length != check.ReportLength ||
            Convert.ToHexString(await SHA256.HashDataAsync(stream, cancel).ConfigureAwait(false)) != check.ReportHash)
            throw new InvalidDataException("The integrity report was modified.");
        stream.Position = 0;

        var ids = request.DifferenceIds == null ? new HashSet<long>() : new HashSet<long>(request.DifferenceIds);
        var kinds = request.Kinds == null ? new HashSet<DifferenceKind>() : new HashSet<DifferenceKind>(request.Kinds);
        var selected = new List<IndexIntegritySelection>();
        using var reader = new StreamReader(stream, leaveOpen: true);
        long expectedId = 1;
        while (await reader.ReadLineAsync(cancel).ConfigureAwait(false) is { } line)
        {
            using var record = JsonDocument.Parse(line);
            var row = record.RootElement;
            if (row.GetProperty("type").GetString() == "summary")
            {
                if (row.GetProperty("differences").GetInt64() != expectedId - 1 ||
                    await reader.ReadLineAsync(cancel).ConfigureAwait(false) != null)
                    throw new InvalidDataException("The integrity report summary does not match its records.");
                if (!request.SelectsAll && (selected.Count == 0 || ids.Count > 0 && ids.Count != selected.Count))
                    throw new InvalidIntegritySelectionException();
                return selected;
            }
            var difference = row.Deserialize<IndexIntegritySelection>(IndexIntegrityFileStore.Json)!;
            if (row.GetProperty("type").GetString() != "difference" || difference.DifferenceId != expectedId++)
                throw new InvalidDataException("Invalid integrity difference record.");
            if (request.SelectsAll || ids.Contains(difference.DifferenceId) || kinds.Contains(difference.Kind))
                selected.Add(difference);
        }
        throw new InvalidDataException("The integrity report has no summary.");
    }
}

internal sealed record IntegritySelectionRequest(long[]? DifferenceIds, DifferenceKind[]? Kinds)
{
    internal static IntegritySelectionRequest All { get; } = new(null, null);
    internal bool SelectsAll => DifferenceIds == null && Kinds == null;
}

internal sealed class InvalidIntegritySelectionException : Exception;
