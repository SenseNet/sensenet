#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Search.Indexing.Integrity;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Search.Indexing;
using SenseNet.Search.Indexing.Integrity;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.ODataTests;

[TestClass]
public class IndexIntegrityCheckServiceTests : ODataTestBase
{
    [TestMethod]
    public Task IndexIntegrity_ChecksCapacityDuringAnExportWithOnlyStructuralIssues() => ODataTestAsync(async () =>
    {
        var scratch = Directory.CreateTempSubdirectory("sensenet-integrity-");
        try
        {
            var engine = new IssuesOnlyIndex();
            var service = new IndexIntegrityCheckService(Providers.Instance.IndexManager, TimeProvider.System);
            var issuesPath = Path.Combine(scratch.FullName, "issues.jsonl");
            var error = await Assert.ThrowsExceptionAsync<IOException>(() => service.CheckAsync(new EmptyDataSource(),
                engine, new(scratch.FullName, () =>
                {
                    if (File.Exists(issuesPath) && new FileInfo(issuesPath).Length > 1024)
                        throw new IOException("Storage limit reached.");
                }), _ => Assert.Fail("The comparison must not start after the storage limit is reached."),
                CancellationToken.None));

            Assert.AreEqual("Storage limit reached.", error.Message);
            Assert.AreEqual(4096, File.ReadLines(issuesPath).Count());
        }
        finally { scratch.Delete(recursive: true); }
    });

    private sealed class EmptyDataSource : IIndexIntegrityDataSource
    {
        public string IntegrityDatabaseIdentity => "check-test";

        public System.Threading.Tasks.Task<int> ExportIntegrityVersionsAsync(IReadOnlySet<int> excludedNodeTypes,
            Action snapshotOpened, Action<DatabaseVersion> write, CancellationToken cancellationToken)
        {
            snapshotOpened();
            return Task.FromResult(0);
        }

        public System.Threading.Tasks.Task<IReadOnlyList<DatabaseVersion>> ReadIntegrityVersionsAsync(
            IReadOnlyCollection<int> versionIds, IReadOnlySet<int> excludedNodeTypes, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class IssuesOnlyIndex : IIndexIntegrityEngine, IIndexIntegrityReader
    {
        public string IndexIdentity => "check-test";
        public long CommitVersion => 1;
        public IndexingActivityStatus Activity => new();
        public int MaxDoc => 5000;
        public int ContentDocuments => MaxDoc;

        public IIndexIntegrityReader OpenIntegrityReader(CancellationToken cancellationToken) => this;

        public void Export(string scratchPath, Action<SavedIndexVersion> write, Action<Difference> issue,
            CancellationToken cancellationToken)
        {
            for (var id = 0; id < MaxDoc; id++)
                issue(new(DifferenceKind.MissingField, DocId: id, Field: "VersionId"));
        }

        public IndexIntegrityProbe ProbeIntegrity(IReadOnlyCollection<int> versionIds, string scratchPath,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public bool TryApplyIntegrityRepairs(long revision, IReadOnlyList<IndexIntegrityUpdate> updates,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public void Dispose() { }
    }
}
