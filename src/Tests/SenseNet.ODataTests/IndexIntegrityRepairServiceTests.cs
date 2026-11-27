#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository.InMemory;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Search.Indexing.Integrity;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Search.Indexing.Integrity;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.ODataTests;

[TestClass]
public class IndexIntegrityRepairServiceTests : ODataTestBase
{
    [TestMethod]
    public Task IndexIntegrity_RepairContinuesAfterInvalidSourceDocuments() => ODataTestAsync(async () =>
    {
        var source = new RepairDataProvider(
        [
            Row(41, 21, "["),
            Row(42, 22, """[{"Name":"VersionId","Type":"String","Value":"42"},{"Name":"Id","Type":"Int","Value":22}]"""),
            Row(43, 23, """[{"Name":"VersionId","Type":"Int","Value":43},{"Name":"Id","Type":"String","Value":"23"}]"""),
            Row(44, 24, """[{"Name":"VersionId","Type":"Int","Value":44},{"Name":"Id","Type":"Int","Value":24}]""")
        ]);
        var dataStore = new DataStore(source, NullLogger<DataStore>.Instance);
        var engine = new RepairEngine();
        var results = new List<IndexIntegrityRepairItem>();
        var selection = Enumerable.Range(41, 4)
            .Select((id, index) => new IndexIntegritySelection(index + 1, DifferenceKind.NotInIndex, id, null, true))
            .ToArray();

        await new IndexIntegrityRepairService(dataStore).RepairAsync(source, engine,
            (IndexManager)Providers.Instance.IndexManager, new(string.Empty, () => { }), selection, results.Add,
            CancellationToken.None);

        CollectionAssert.AreEquivalent(new[]
        {
            new IndexIntegrityRepairItem(1, 41, "invalid_source_document"),
            new IndexIntegrityRepairItem(2, 42, "invalid_source_document"),
            new IndexIntegrityRepairItem(3, 43, "invalid_source_document"),
            new IndexIntegrityRepairItem(4, 44, "repaired")
        }, results);
        var update = engine.Updates.Single();
        Assert.AreEqual(44, update.VersionId);
        Assert.AreEqual(44, update.Document?.VersionId);
    });

    private static IndexDocumentData Row(int versionId, int nodeId, string document) => new(null, document)
    {
        VersionId = versionId,
        NodeId = nodeId,
        Path = $"/Root/Contents/{nodeId}",
        VersionTimestamp = versionId,
        NodeTimestamp = nodeId
    };

    private sealed class RepairDataProvider(IndexDocumentData[] rows) : InMemoryDataProvider, IIndexIntegrityDataSource
    {
        public string IntegrityDatabaseIdentity => "repair-test";

        public System.Threading.Tasks.Task<int> ExportIntegrityVersionsAsync(IReadOnlySet<int> excludedNodeTypes,
            Action snapshotOpened, Action<DatabaseVersion> write, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public System.Threading.Tasks.Task<IReadOnlyList<DatabaseVersion>> ReadIntegrityVersionsAsync(
            IReadOnlyCollection<int> versionIds, IReadOnlySet<int> excludedNodeTypes, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<DatabaseVersion>>(rows.Where(row => versionIds.Contains(row.VersionId))
                .Select(row => new DatabaseVersion(row.VersionId, row.VersionTimestamp, row.NodeId,
                    row.NodeTimestamp, null, null)).ToArray());

        public override System.Threading.Tasks.Task<IEnumerable<IndexDocumentData>> LoadIndexDocumentsAsync(
            IEnumerable<int> versionIds, CancellationToken cancellationToken) =>
            Task.FromResult(rows.Where(row => versionIds.Contains(row.VersionId)));
    }

    private sealed class RepairEngine : IIndexIntegrityEngine
    {
        public string IndexIdentity => "repair-test";
        public IReadOnlyList<IndexIntegrityUpdate> Updates { get; private set; } = [];

        public IIndexIntegrityReader OpenIntegrityReader(CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public IndexIntegrityProbe ProbeIntegrity(IReadOnlyCollection<int> versionIds, string scratchPath,
            CancellationToken cancellationToken) => new(0, [], []);

        public bool TryApplyIntegrityRepairs(long revision, IReadOnlyList<IndexIntegrityUpdate> updates,
            CancellationToken cancellationToken)
        {
            Updates = updates.ToArray();
            return true;
        }
    }
}
