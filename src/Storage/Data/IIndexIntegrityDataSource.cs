using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Search.Indexing.Integrity;

namespace SenseNet.ContentRepository.Storage.Data;

/// <summary>Optional capability of a repository data provider used by the index integrity checker.</summary>
public interface IIndexIntegrityDataSource
{
    string IntegrityDatabaseIdentity { get; }

    Task<int> ExportIntegrityVersionsAsync(IReadOnlySet<int> excludedNodeTypes,
        Action snapshotOpened, Action<DatabaseVersion> write, CancellationToken cancellationToken);

    Task<IReadOnlyList<DatabaseVersion>> ReadIntegrityVersionsAsync(IReadOnlyCollection<int> versionIds,
        IReadOnlySet<int> excludedNodeTypes, CancellationToken cancellationToken);
}
