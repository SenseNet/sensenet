#nullable enable
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using STT = System.Threading.Tasks;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Search.Indexing.Integrity;

namespace SenseNet.ContentRepository.Search.Indexing.Integrity;

internal sealed class DefaultIndexIntegrityProvider(IDataStore dataStore, IIndexManager indexManager,
    IndexIntegrityCheckService checker, IndexIntegrityRepairService repairer) : IIndexIntegrityProvider
{
    public string ProviderId => "sensenet.mssql.lucene29-local";
    public int RepairFormatVersion => 1;

    public IndexIntegrityCapabilities GetCapabilities()
    {
        if (dataStore.DataProvider is not IIndexIntegrityDataSource dataSource ||
            indexManager.IndexingEngine is not IIndexIntegrityEngine engine || indexManager.IndexingEngine.IndexIsCentralized)
            return new(false, false, string.Empty);

        var source = $"{dataSource.IntegrityDatabaseIdentity}\n{engine.IndexIdentity}";
        var sourceId = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)));
        return new(indexManager.Running, indexManager.Running && indexManager is IndexManager, sourceId);
    }

    public async Task<IndexIntegrityCheckResult> CheckAsync(IndexIntegrityWork work, Action<Difference> report,
        CancellationToken cancellationToken)
    {
        using var account = new SystemAccount();
        return await checker.CheckAsync((IIndexIntegrityDataSource)dataStore.DataProvider,
            (IIndexIntegrityEngine)indexManager.IndexingEngine, work, report, cancellationToken).ConfigureAwait(false);
    }

    public async STT.Task RepairAsync(IndexIntegrityWork work, IReadOnlyList<IndexIntegritySelection> selection,
        Action<IndexIntegrityRepairItem> report, CancellationToken cancellationToken)
    {
        using var account = new SystemAccount();
        await repairer.RepairAsync((IIndexIntegrityDataSource)dataStore.DataProvider,
            (IIndexIntegrityEngine)indexManager.IndexingEngine, (IndexManager)indexManager,
            work, selection, report, cancellationToken).ConfigureAwait(false);
    }
}
