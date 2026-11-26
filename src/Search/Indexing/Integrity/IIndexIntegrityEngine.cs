#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;

namespace SenseNet.Search.Indexing.Integrity;

/// <summary>Optional operations on a running index engine.</summary>
public interface IIndexIntegrityEngine
{
    string IndexIdentity { get; }
    IIndexIntegrityReader OpenIntegrityReader(CancellationToken cancellationToken);
    IndexIntegrityProbe ProbeIntegrity(IReadOnlyCollection<int> versionIds, string scratchPath,
        CancellationToken cancellationToken);
    bool TryApplyIntegrityRepairs(long revision, IReadOnlyList<IndexIntegrityUpdate> updates,
        CancellationToken cancellationToken);
}

public interface IIndexIntegrityReader : IDisposable
{
    long CommitVersion { get; }
    IndexingActivityStatus Activity { get; }
    int MaxDoc { get; }
    int ContentDocuments { get; }

    /// <summary>Emits all index rows and structural issues in a single pass over the fixed reader.</summary>
    void Export(string scratchPath, Action<SavedIndexVersion> write, Action<Difference> issue,
        CancellationToken cancellationToken);
}

public sealed record IndexIntegrityProbe(long Revision, IReadOnlyList<SavedIndexVersion> Documents,
    IReadOnlyList<Difference> Issues);

/// <summary>A null document requests deletion of all copies of the indicated version.</summary>
public sealed record IndexIntegrityUpdate(int VersionId, IndexDocument? Document);
