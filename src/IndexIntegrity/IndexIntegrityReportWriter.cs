#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualBasic.FileIO;
using SenseNet.ContentRepository.Search.Indexing.Integrity;
using SenseNet.Search.Indexing.Integrity;

namespace SenseNet.OData.Operations.IndexIntegrity;

internal sealed class IndexIntegrityReportWriter : IDisposable
{
    private const int CsvSchemaVersion = 3;
    private static readonly string[] Columns =
    [
        "differenceId", "kind", "versionId", "nodeId", "field", "expected", "actual", "repairableByVersionId",
        "nodeModificationDateUtc", "versionModificationDateUtc", "sqlNodeTimestamp", "sqlVersionTimestamp",
        "indexNodeTimestamp", "indexVersionTimestamp"
    ];
    private static readonly char[] CharactersRequiringQuotes = [',', '"', '\r', '\n'];

    private readonly StreamWriter _writer;
    private readonly FileStream _stream;
    private readonly Action _checkCapacity;
    private readonly Dictionary<DifferenceKind, long> _counts = new();
    private long _differences;

    public IndexIntegrityReportWriter(string path, Action checkCapacity)
    {
        _stream = File.Create(path);
        _writer = new StreamWriter(_stream, leaveOpen: true) { NewLine = "\r\n" };
        _writer.WriteLine(string.Join(',', Columns));
        _checkCapacity = checkCapacity;
    }

    public void Write(Difference difference)
    {
        var database = difference.Database;
        var index = difference.Index;
        var id = difference.VersionId ?? index?.VersionId;
        var metadata = index?.Metadata;
        _counts[difference.Kind] = _counts.GetValueOrDefault(difference.Kind) + 1;
        WriteRow(
            ++_differences,
            difference.Kind,
            id is > 0 ? id : null,
            difference.NodeId ?? database?.NodeId,
            difference.Field,
            difference.Expected,
            difference.Actual,
            id is > 0 && metadata?.AmbiguousVersionId != true &&
                difference.Field != "VersionId",
            database?.NodeModificationDateUtc,
            database?.VersionModificationDateUtc,
            database?.NodeTimestamp,
            database?.VersionTimestamp,
            metadata is { } im && (im.Present & Fields.NodeTimestamp) != 0 ? im.NodeTimestamp : (long?)null,
            metadata is { } vm && (vm.Present & Fields.VersionTimestamp) != 0 ? vm.VersionTimestamp : (long?)null);
        if (_differences % 4096 == 0)
            _checkCapacity();
    }

    public JsonElement Finish(IndexIntegrityCheckResult result, IntegrityRun run, string summaryPath)
    {
        var summary = JsonSerializer.SerializeToElement(new
        {
            schemaVersion = CsvSchemaVersion,
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
                    commitVersion = result.IndexCommitVersion.ToString(CultureInfo.InvariantCulture),
                    lastActivityId = result.IndexLastActivityId,
                    gaps = result.IndexGaps
                }
            },
            sourceSnapshotsIndependent = true
        }, IndexIntegrityFileStore.Json);
        _writer.Flush();
        _stream.Flush(flushToDisk: true);
        using (var summaryStream = File.Create(summaryPath))
        {
            JsonSerializer.Serialize(summaryStream, summary, IndexIntegrityFileStore.Json);
            summaryStream.Flush(flushToDisk: true);
        }
        _checkCapacity();
        return summary;
    }

    private void WriteRow(params object?[] values)
    {
        for (var i = 0; i < values.Length; i++)
        {
            if (i > 0)
                _writer.Write(',');
            var cell = FormatCell(values[i]);
            if (cell == null)
                continue;
            if (cell.IndexOfAny(CharactersRequiringQuotes) < 0)
            {
                _writer.Write(cell);
                continue;
            }
            _writer.Write('"');
            _writer.Write(cell.Replace("\"", "\"\""));
            _writer.Write('"');
        }
        _writer.WriteLine();
    }

    private static string? FormatCell(object? value) => value switch
    {
        null => null,
        string text => text,
        bool flag => flag ? "true" : "false",
        DateTime date => date.ToString("O", CultureInfo.InvariantCulture),
        JsonElement { ValueKind: JsonValueKind.String } text => text.GetString(),
        JsonElement { ValueKind: JsonValueKind.Null or JsonValueKind.Undefined } => null,
        JsonElement element => element.GetRawText(),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => JsonSerializer.Serialize(value, IndexIntegrityFileStore.Json)
    };

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
        var summary = check.Summary ?? throw new InvalidDataException("The integrity report has no summary.");
        var differenceCount = summary.GetProperty("differences").GetInt64();
        var schemaVersion = summary.GetProperty("schemaVersion").GetInt32();
        if (schemaVersion is not (1 or 2 or CsvSchemaVersion))
            throw new InvalidDataException("Unsupported integrity report schema version.");
        using var csv = schemaVersion == CsvSchemaVersion ? new TextFieldParser(reader) { TrimWhiteSpace = false } : null;
        if (csv != null)
        {
            csv.SetDelimiters(",");
            if (ReadCsvFields(csv) is not { } header || !header.SequenceEqual(Columns))
                throw new InvalidDataException("Invalid integrity report CSV header.");
        }
        var hasInlineSummary = schemaVersion == 1;
        for (long expectedId = 1; expectedId <= differenceCount; expectedId++)
        {
            cancel.ThrowIfCancellationRequested();
            var difference = csv != null ? ReadCsvDifference(csv)
                : await ReadJsonDifferenceAsync(reader, hasInlineSummary, cancel).ConfigureAwait(false);
            if (difference.DifferenceId != expectedId)
                throw new InvalidDataException("Invalid integrity difference record.");
            if (request.SelectsAll || ids.Contains(difference.DifferenceId) || kinds.Contains(difference.Kind))
                selected.Add(difference);
        }
        if (hasInlineSummary)
        {
            var line = await reader.ReadLineAsync(cancel).ConfigureAwait(false)
                ?? throw new InvalidDataException("The integrity report has no summary.");
            using var record = JsonDocument.Parse(line);
            var row = record.RootElement;
            if (row.GetProperty("type").GetString() != "summary" || row.GetProperty("differences").GetInt64() != differenceCount)
                throw new InvalidDataException("The integrity report summary does not match its records.");
        }
        if (csv != null ? !csv.EndOfData : await reader.ReadLineAsync(cancel).ConfigureAwait(false) != null)
            throw new InvalidDataException("The integrity report contains unexpected records.");
        if (!request.SelectsAll && (selected.Count == 0 || ids.Count > 0 && ids.Count != selected.Count))
            throw new InvalidIntegritySelectionException();
        return selected;
    }

    private static IndexIntegritySelection ReadCsvDifference(TextFieldParser csv)
    {
        var row = ReadCsvFields(csv)
            ?? throw new InvalidDataException("The integrity report has fewer differences than its summary.");
        if (row.Length != Columns.Length ||
            !long.TryParse(row[0], NumberStyles.None, CultureInfo.InvariantCulture, out var differenceId) ||
            !Enum.TryParse<DifferenceKind>(row[1], out var kind) || Enum.GetName(kind) != row[1] ||
            !bool.TryParse(row[7], out var repairable))
            throw new InvalidDataException("Invalid integrity difference record.");

        int? versionId = null;
        if (row[2].Length != 0)
        {
            if (!int.TryParse(row[2], NumberStyles.None, CultureInfo.InvariantCulture, out var id) || id <= 0)
                throw new InvalidDataException("Invalid integrity difference version ID.");
            versionId = id;
        }
        return new(differenceId, kind, versionId, row[4].Length == 0 ? null : row[4], repairable);
    }

    private static string[]? ReadCsvFields(TextFieldParser csv)
    {
        try { return csv.ReadFields(); }
        catch (MalformedLineException error)
        {
            throw new InvalidDataException("Invalid integrity report CSV record.", error);
        }
    }

    private static async Task<IndexIntegritySelection> ReadJsonDifferenceAsync(StreamReader reader,
        bool hasInlineSummary, CancellationToken cancel)
    {
        var line = await reader.ReadLineAsync(cancel).ConfigureAwait(false)
            ?? throw new InvalidDataException("The integrity report has fewer differences than its summary.");
        using var record = JsonDocument.Parse(line);
        var row = record.RootElement;
        var difference = row.Deserialize<IndexIntegritySelection>(IndexIntegrityFileStore.Json);
        if (difference == null || hasInlineSummary && row.GetProperty("type").GetString() != "difference")
            throw new InvalidDataException("Invalid integrity difference record.");
        return difference;
    }
}

internal sealed record IntegritySelectionRequest(long[]? DifferenceIds, DifferenceKind[]? Kinds)
{
    internal static IntegritySelectionRequest All { get; } = new(null, null);
    internal bool SelectsAll => DifferenceIds == null && Kinds == null;
}

internal sealed class InvalidIntegritySelectionException : Exception;
