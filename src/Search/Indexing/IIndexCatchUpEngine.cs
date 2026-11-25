using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Search.Querying;

namespace SenseNet.Search.Indexing
{
    /// <summary>
    /// An indexing engine that can isolate startup catch-up writes until a final commit.
    /// </summary>
    public interface IIndexCatchUpEngine
    {
        IIndexCatchUpSession BeginCatchUp(CancellationToken cancellationToken);
    }

    /// <summary>
    /// Reads include earlier session writes. Disposing without a successful commit discards the writes.
    /// Commit publishes the documents and their activity status together.
    /// </summary>
    public interface IIndexCatchUpSession : IDisposable
    {
        IReadOnlyList<IndexCatchUpVersion> GetVersions(SnTerm term, CancellationToken cancellationToken);
        Task WriteIndexAsync(IReadOnlyList<SnTerm> deletions, IReadOnlyList<DocumentUpdate> updates,
            CancellationToken cancellationToken);
        Task CommitAsync(IndexingActivityStatus status, CancellationToken cancellationToken);
    }

    public readonly record struct IndexCatchUpVersion(int VersionId, bool IsLastPublic, bool IsLastDraft);
}
