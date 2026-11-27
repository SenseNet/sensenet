#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using SenseNet.ContentRepository.Search.Indexing.Integrity;

namespace SenseNet.OData.Operations.IndexIntegrity;

internal sealed record IntegrityRun(Guid CheckId, Guid Id, string SourceId, string ProviderId, int RepairFormatVersion,
    int UserId, string? IdempotencyKey, string Fingerprint, DateTimeOffset QueuedAtUtc)
{
    public string Status { get; set; } = "queued";
    public DateTimeOffset? StartedAtUtc { get; set; }
    public DateTimeOffset? FinishedAtUtc { get; set; }
    public long? DurationMs { get; set; }
    public string? Error { get; set; }
    public string? ReportHash { get; set; }
    public long? ReportLength { get; set; }
    public JsonElement? Summary { get; set; }
    public bool IsRepair => CheckId != Id;
    public bool Active => Status is "queued" or "running";
}

internal sealed class IndexIntegrityFileStore(IWebHostEnvironment environment, IndexIntegrityOptions options,
    TimeProvider clock, ILogger<IndexIntegrityFileStore> logger)
{
    internal const string ReportFileName = "comparison.jsonl";

    internal static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
    };

    private readonly string _root = Path.Combine(environment.ContentRootPath, "App_Data", "index-integrity-check");
    private readonly object _sync = new();
    private readonly Dictionary<Guid, int> _readers = new();
    private readonly HashSet<Guid> _deferred = new();

    public string ScratchPath(Guid checkId) => Path.Combine(RunPath(checkId), "scratch");
    public string ReportPath(Guid checkId) => Path.Combine(RunPath(checkId), ReportFileName);
    public string SummaryPath(Guid checkId) => Path.Combine(RunPath(checkId), "summary.json");
    public string RepairPath(IntegrityRun repair) => Path.Combine(RunPath(repair.CheckId), "repairs", $"{repair.Id:D}.jsonl");
    private string RunPath(Guid id) => Path.Combine(_root, id.ToString("D"));
    private string StatePath(Guid checkId, Guid id) => checkId == id
        ? Path.Combine(RunPath(checkId), "manifest.json")
        : Path.Combine(RunPath(checkId), "repairs", $"{id:D}.json");

    public IntegrityRun? Read(Guid checkId, Guid id)
    {
        lock (_sync)
            return ReadState(StatePath(checkId, id));
    }

    public (IntegrityRun Check, IntegrityRun[] Repairs)[] ListRuns()
    {
        lock (_sync)
        {
            return AllStates().GroupBy(run => run.CheckId)
                .Select(runs => (Check: runs.First(run => !run.IsRepair), Repairs: runs.Where(run => run.IsRepair)
                    .OrderByDescending(run => run.QueuedAtUtc)
                    .ThenBy(run => run.Id)
                    .ToArray()))
                .OrderByDescending(run => run.Check.QueuedAtUtc)
                .ThenBy(run => run.Check.Id)
                .ToArray();
        }
    }

    public void Save(IntegrityRun run)
    {
        lock (_sync)
        {
            var path = StatePath(run.CheckId, run.Id);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var temp = path + ".partial";
            using (var stream = File.Create(temp))
            {
                JsonSerializer.Serialize(stream, run, Json);
                stream.Flush(flushToDisk: true);
            }
            File.Move(temp, path, overwrite: true);
        }
    }

    public (IntegrityRun? Run, string? Error, bool Created) Reserve(IntegrityRun request)
    {
        lock (_sync)
        {
            var states = AllStates().Where(run => run.SourceId == request.SourceId).ToArray();
            if (request.IdempotencyKey != null)
            {
                var previous = states.FirstOrDefault(run => run.UserId == request.UserId &&
                    run.IdempotencyKey == request.IdempotencyKey);
                if (previous != null)
                    return previous.Fingerprint == request.Fingerprint
                        ? (previous, null, false)
                        : (null, "idempotency_conflict", false);
            }
            if (states.Any(run => run.Active))
                return (null, "index_busy", false);
            if (request.IsRepair)
            {
                var check = states.FirstOrDefault(run => run.Id == request.CheckId && !run.IsRepair);
                if (check == null)
                    return (null, "check_not_found", false);
                if (check.Status != "completed")
                    return (null, "report_not_ready", false);
                if (check.ProviderId != request.ProviderId || check.RepairFormatVersion != request.RepairFormatVersion)
                    return (null, "incompatible_provider", false);
            }
            Save(request);
            return (request, null, true);
        }
    }

    public ReportLease? OpenReport(Guid checkId)
    {
        lock (_sync)
        {
            if (Read(checkId, checkId)?.Status != "completed")
                return null;

            var path = ReportPath(checkId);
            if (!File.Exists(path))
            {
                path = Path.Combine(RunPath(checkId), $"comparison-{checkId:D}.jsonl");
                if (!File.Exists(path))
                    return null;
            }
            var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
                64 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
            _readers[checkId] = _readers.GetValueOrDefault(checkId) + 1;
            return new(stream, () => ReleaseReport(checkId));
        }
    }

    public void Recover()
    {
        lock (_sync)
        {
            foreach (var run in AllStates().Where(run => run.Active).ToArray())
            {
                run.Status = "interrupted";
                if (run.IsRepair)
                    RecoverRepairSummary(run);
                run.FinishedAtUtc = clock.GetUtcNow();
                if (run.StartedAtUtc is { } started)
                    run.DurationMs = Math.Max(0, (long)(run.FinishedAtUtc.Value - started).TotalMilliseconds);
                Save(run);
            }
            foreach (var run in AllStates().Where(run => !run.IsRepair).ToArray())
            {
                DeleteScratch(run.CheckId);
                if (options.KeepPreviousChecks && IsExpired(run))
                    TryDelete(run.CheckId);
            }
        }
    }

    private void RecoverRepairSummary(IntegrityRun run)
    {
        var path = RepairPath(run);
        if (!File.Exists(path) || run.Summary is not { } summary ||
            !summary.TryGetProperty("selectedDifferences", out var selected))
            return;
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        var repaired = new HashSet<int>();
        foreach (var line in File.ReadLines(path))
        {
            try
            {
                if (JsonSerializer.Deserialize<IndexIntegrityRepairItem>(line, Json) is { } result)
                {
                    counts[result.Status] = counts.GetValueOrDefault(result.Status) + 1;
                    if (result.Status == "repaired" && result.VersionId is int versionId)
                        repaired.Add(versionId);
                }
            }
            catch (JsonException)
            {
                break; // An interrupted write can leave the last line incomplete.
            }
        }
        run.Summary = IndexIntegrityWorker.SummarizeRepair(selected.GetInt32(), repaired, counts);
    }

    public void CleanPreviousChecks(IntegrityRun current)
    {
        lock (_sync)
        {
            foreach (var run in AllStates().Where(run => !run.IsRepair && run.CheckId != current.CheckId &&
                run.SourceId == current.SourceId && !run.Active).ToArray())
            {
                if (!options.KeepPreviousChecks || IsExpired(run))
                    TryDelete(run.CheckId);
            }
        }
    }

    public void CheckCapacity(Guid checkId)
    {
        lock (_sync)
        {
            if (Size(RunPath(checkId)) > options.MaxRunBytes || Size(_root) > options.MaxTotalBytes ||
                new DriveInfo(Path.GetPathRoot(_root)!).AvailableFreeSpace < options.MinimumFreeBytes)
                throw new IOException("The index integrity storage limit was reached.");
        }
    }

    public void DeleteScratch(Guid checkId)
    {
        var path = ScratchPath(checkId);
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(error, "Unable to clean index integrity scratch files for {CheckId}.", checkId);
        }
    }

    private void ReleaseReport(Guid checkId)
    {
        lock (_sync)
        {
            if (--_readers[checkId] != 0)
                return;
            _readers.Remove(checkId);
            if (_deferred.Remove(checkId))
                TryDelete(checkId);
        }
    }

    private void TryDelete(Guid checkId)
    {
        if (_readers.ContainsKey(checkId) || AllStates(checkId).Any(run => run.Active))
        {
            _deferred.Add(checkId);
            return;
        }
        try
        {
            var path = RunPath(checkId);
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
            _deferred.Remove(checkId);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(error, "Unable to delete index integrity check {CheckId}.", checkId);
        }
    }

    private bool IsExpired(IntegrityRun run) =>
        run.FinishedAtUtc is { } finished && clock.GetUtcNow() - finished >= options.Retention;

    private IEnumerable<IntegrityRun> AllStates()
    {
        if (!Directory.Exists(_root))
            yield break;
        foreach (var directory in Directory.EnumerateDirectories(_root))
        {
            if (!Guid.TryParseExact(Path.GetFileName(directory), "D", out var id) ||
                (File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0)
                continue;
            foreach (var run in AllStates(id))
                yield return run;
        }
    }

    private IEnumerable<IntegrityRun> AllStates(Guid id)
    {
        var state = ReadState(StatePath(id, id));
        if (state == null || state.CheckId != id || state.Id != id)
            yield break;
        yield return state;
        var repairs = Path.Combine(RunPath(id), "repairs");
        if (!Directory.Exists(repairs))
            yield break;
        foreach (var path in Directory.EnumerateFiles(repairs, "*.json"))
        {
            if (Guid.TryParseExact(Path.GetFileNameWithoutExtension(path), "D", out var repairId) &&
                ReadState(path) is { } repair && repair.CheckId == id && repair.Id == repairId && repair.IsRepair)
                yield return repair;
        }
    }

    private IntegrityRun? ReadState(string path)
    {
        if (!File.Exists(path))
            return null;
        try
        {
            using var stream = File.OpenRead(path);
            return JsonSerializer.Deserialize<IntegrityRun>(stream, Json);
        }
        catch (JsonException error)
        {
            logger.LogWarning(error, "An index integrity state file is invalid.");
            return null;
        }
    }

    private static long Size(string path) => Directory.Exists(path)
        ? Directory.EnumerateFiles(path, "*", new EnumerationOptions { RecurseSubdirectories = true,
            AttributesToSkip = FileAttributes.ReparsePoint }).Sum(file => new FileInfo(file).Length)
        : 0;

    internal sealed class ReportLease(FileStream stream, Action release) : IDisposable
    {
        private Action? _release = release;
        public FileStream Stream { get; } = stream;
        public void Dispose()
        {
            var callback = Interlocked.Exchange(ref _release, null);
            if (callback == null)
                return;
            try { Stream.Dispose(); }
            finally { callback(); }
        }
    }
}
