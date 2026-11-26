#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SenseNet.ContentRepository.Search.Indexing.Integrity;

namespace SenseNet.OData.Operations.IndexIntegrity;

internal sealed class IndexIntegrityWorker(IndexIntegrityFileStore files, IIndexIntegrityProvider provider,
    IndexIntegrityOptions options, TimeProvider clock, ILogger<IndexIntegrityWorker> logger) : BackgroundService
{
    private readonly Channel<Job> _queue = Channel.CreateUnbounded<Job>(new UnboundedChannelOptions
    {
        SingleReader = true
    });

    public void Enqueue(IntegrityRun run, IntegritySelectionRequest? selection)
    {
        if (!_queue.Writer.TryWrite(new(run, selection)))
            throw new InvalidOperationException("The integrity worker is unavailable.");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        files.Recover();
        try
        {
            await foreach (var job in _queue.Reader.ReadAllAsync(stoppingToken).ConfigureAwait(false))
                await ExecuteJobAsync(job, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        finally { files.Recover(); }
    }

    private async Task ExecuteJobAsync(Job job, CancellationToken stoppingToken)
    {
        var run = job.Run;
        var timestamp = clock.GetTimestamp();
        run.StartedAtUtc = clock.GetUtcNow();
        run.Status = "running";
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        timeout.CancelAfter(options.Timeout);
        try
        {
            files.Save(run);
            var capabilities = provider.GetCapabilities();
            if (run.ProviderId != provider.ProviderId || run.RepairFormatVersion != provider.RepairFormatVersion ||
                run.SourceId != capabilities.SourceId || (run.IsRepair ? !capabilities.CanRepair : !capabilities.CanCheck))
                throw new InvalidDataException("The integrity provider or source changed before the run started.");
            if (job.Selection == null)
                files.CleanPrevious(run);
            var scratch = files.ScratchPath(run.CheckId);
            Directory.CreateDirectory(scratch);
            var work = new IndexIntegrityWork(scratch, () => files.CheckCapacity(run.CheckId));
            work.CheckCapacity();
            logger.LogInformation("Index integrity run {RunId} started by user {UserId}. Repair: {IsRepair}",
                run.Id, run.UserId, run.IsRepair);
            if (job.Selection == null)
                await CheckAsync(run, work, timestamp, timeout.Token).ConfigureAwait(false);
            else
                await RepairAsync(run, work, job.Selection, timeout.Token).ConfigureAwait(false);
            run.Status = "completed";
        }
        catch (InvalidIntegritySelectionException)
        {
            run.Status = "failed";
            run.Error = "invalid_selection";
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
        {
            run.Status = stoppingToken.IsCancellationRequested ? "interrupted" : "failed";
            run.Error = stoppingToken.IsCancellationRequested ? "interrupted" : "timeout";
        }
        catch (Exception error)
        {
            run.Status = "failed";
            run.Error = error is InvalidDataException ? "invalid_data" : "run_failed";
            logger.LogError(error, "Index integrity run {RunId} failed.", run.Id);
        }
        finally
        {
            run.FinishedAtUtc ??= clock.GetUtcNow();
            run.DurationMs ??= (long)clock.GetElapsedTime(timestamp).TotalMilliseconds;
            files.DeleteScratch(run.CheckId);
            try { files.Save(run); }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            {
                logger.LogError(error, "Unable to save the final state of index integrity run {RunId}.", run.Id);
            }
            logger.LogInformation("Index integrity run {RunId} ended with {RunStatus}.", run.Id, run.Status);
        }
    }

    private async Task CheckAsync(IntegrityRun run, IndexIntegrityWork work, long timestamp, CancellationToken cancel)
    {
        var path = files.ReportPath(run.CheckId);
        var partial = Path.Combine(work.ScratchDirectory, "comparison.partial");
        using (var writer = new IndexIntegrityReportWriter(partial, work.CheckCapacity))
        {
            var result = await provider.CheckAsync(work, writer.Write, cancel).ConfigureAwait(false);
            cancel.ThrowIfCancellationRequested();
            run.FinishedAtUtc = clock.GetUtcNow();
            run.DurationMs = (long)clock.GetElapsedTime(timestamp).TotalMilliseconds;
            run.Summary = writer.Finish(result, run);
        }
        await using (var stream = File.OpenRead(partial))
        {
            run.ReportLength = stream.Length;
            run.ReportHash = Convert.ToHexString(await SHA256.HashDataAsync(stream, cancel).ConfigureAwait(false));
        }
        File.Move(partial, path);
    }

    private async Task RepairAsync(IntegrityRun run, IndexIntegrityWork work,
        IntegritySelectionRequest selection, CancellationToken cancel)
    {
        var check = files.Read(run.CheckId, run.CheckId) ?? throw new InvalidDataException("Missing check state.");
        using var report = files.OpenReport(run.CheckId) ?? throw new InvalidDataException("Missing integrity report.");
        var selected = await IndexIntegrityReportWriter.ReadSelectionAsync(report.Stream, check, selection, cancel)
            .ConfigureAwait(false);
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        var repairedVersions = new HashSet<int>();
        using var results = File.CreateText(files.RepairPath(run));
        run.Summary = SummarizeRepair(selected.Count, repairedVersions, counts);
        files.Save(run);
        try
        {
            await provider.RepairAsync(work, selected, item =>
            {
                counts[item.Status] = counts.GetValueOrDefault(item.Status) + 1;
                if (item.Status == "repaired" && item.VersionId is int versionId)
                    repairedVersions.Add(versionId);
                results.WriteLine(JsonSerializer.Serialize(item, IndexIntegrityFileStore.Json));
                results.Flush();
                if ((counts[item.Status] & 255) == 0)
                    work.CheckCapacity();
            }, cancel).ConfigureAwait(false);
        }
        finally
        {
            run.Summary = SummarizeRepair(selected.Count, repairedVersions, counts);
        }
    }

    internal static JsonElement SummarizeRepair(int selected, HashSet<int> repairedVersions,
        Dictionary<string, int> counts) => JsonSerializer.SerializeToElement(new
    {
        selectedDifferences = selected,
        repairedVersions = repairedVersions.Count,
        countsByStatus = counts
    }, IndexIntegrityFileStore.Json);

    private sealed record Job(IntegrityRun Run, IntegritySelectionRequest? Selection);
}
