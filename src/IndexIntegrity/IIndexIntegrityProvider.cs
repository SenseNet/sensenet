#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using STT = System.Threading.Tasks;
using SenseNet.Search.Indexing.Integrity;

namespace SenseNet.ContentRepository.Search.Indexing.Integrity;

/// <summary>Replaceable source of index integrity reports and targeted repairs.</summary>
public interface IIndexIntegrityProvider
{
    string ProviderId { get; }
    int RepairFormatVersion { get; }
    IndexIntegrityCapabilities GetCapabilities();

    Task<IndexIntegrityCheckResult> CheckAsync(IndexIntegrityWork work, Action<Difference> report,
        CancellationToken cancellationToken);
    STT.Task RepairAsync(IndexIntegrityWork work, IReadOnlyList<IndexIntegritySelection> selection,
        Action<IndexIntegrityRepairItem> report, CancellationToken cancellationToken);
}

public sealed record IndexIntegrityCapabilities(bool CanCheck, bool CanRepair, string SourceId);
public sealed record IndexIntegrityWork(string ScratchDirectory, Action CheckCapacity);
public sealed record IndexIntegrityPhase(DateTimeOffset StartedAtUtc, DateTimeOffset FinishedAtUtc,
    long DurationMs, DateTimeOffset? CapturedAtUtc = null);
public sealed record IndexIntegrityCheckResult(CheckCounts Counts, long DifferentDocuments,
    int DatabaseLastActivityId, long IndexCommitVersion, int IndexLastActivityId, int[] IndexGaps,
    IndexIntegrityPhase DatabaseExport, IndexIntegrityPhase IndexExport, IndexIntegrityPhase Comparison);

public sealed record IndexIntegritySelection(long DifferenceId, DifferenceKind Kind, int? VersionId,
    string? Field, bool RepairableByVersionId);

public sealed record IndexIntegrityRepairItem(long DifferenceId, int? VersionId, string Status);
