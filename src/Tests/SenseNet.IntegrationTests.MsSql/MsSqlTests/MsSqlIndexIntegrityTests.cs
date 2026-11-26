using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;
using SenseNet.Search.Indexing.Integrity;
using SenseNet.Storage.Data.MsSqlClient;
using SenseNet.Tools;

namespace SenseNet.IntegrationTests.MsSql.MsSqlTests;

[TestClass]
public class MsSqlIndexIntegrityTests
{
    [TestMethod, TestCategory("SqlIntegration")]
    public async Task IntegrityExportKeepsItsSqlSnapshotAndIncludesVersionsWithoutDocuments()
    {
        var connectionString = Environment.GetEnvironmentVariable("SENSENET_INDEX_INTEGRITY_TEST_SQL");
        if (string.IsNullOrWhiteSpace(connectionString))
            Assert.Inconclusive("Set SENSENET_INDEX_INTEGRITY_TEST_SQL to a disposable SQL Server test instance.");

        var databaseName = $"SenseNetIntegrityTest_{Guid.NewGuid():N}";
        var admin = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = "master" };
        var database = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = databaseName, Pooling = false };
        await using var master = new SqlConnection(admin.ConnectionString);
        await master.OpenAsync();
        await ExecuteAsync(master, $"CREATE DATABASE [{databaseName}];");
        try
        {
            await ExecuteAsync(master, $"ALTER DATABASE [{databaseName}] SET ALLOW_SNAPSHOT_ISOLATION ON;");
            await using var writer = new SqlConnection(database.ConnectionString);
            await writer.OpenAsync();
            await ExecuteAsync(writer, """
                CREATE TABLE dbo.Nodes
                (
                    NodeId int PRIMARY KEY, NodeTypeId int NOT NULL, LastMajorVersionId int NULL,
                    LastMinorVersionId int NOT NULL, ModificationDate datetime2 NOT NULL, [Timestamp] rowversion
                );
                CREATE TABLE dbo.Versions
                (
                    VersionId int PRIMARY KEY, NodeId int NOT NULL, ModificationDate datetime2 NOT NULL,
                    IndexDocument nvarchar(max) NULL, [Timestamp] rowversion
                );
                CREATE TABLE dbo.IndexingActivities (IndexingActivityId int IDENTITY PRIMARY KEY);
                INSERT dbo.Nodes (NodeId, NodeTypeId, LastMinorVersionId, ModificationDate)
                    VALUES (21, 10, 42, '2020-01-01T00:00:01'), (22, 11, 43, '2020-01-01T00:00:00');
                INSERT dbo.Versions (VersionId, NodeId, ModificationDate)
                    VALUES (42, 21, '2020-01-01T00:00:02'), (43, 22, '2020-01-01T00:00:03');
                INSERT dbo.IndexingActivities DEFAULT VALUES;
                """);

            var connectionOptions = Options.Create(new ConnectionStringOptions { Repository = database.ConnectionString });
            var installation = Options.Create(new MsSqlDatabaseInstallationOptions());
            IIndexIntegrityDataSource provider = new MsSqlDataProvider(Options.Create(new DataOptions()), connectionOptions,
                installation, new MsSqlDatabaseInstaller(installation, NullLogger<MsSqlDatabaseInstaller>.Instance),
                new MsSqlDataInstaller(connectionOptions, NullLogger<MsSqlDataInstaller>.Instance),
                NullLogger<MsSqlDataProvider>.Instance,
                new DefaultRetrier(Options.Create(new RetrierOptions()), NullLogger<DefaultRetrier>.Instance));
            var rows = new List<DatabaseVersion>();
            var excluded = new HashSet<int> { 11 };
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var activity = await provider.ExportIntegrityVersionsAsync(excluded, () =>
                ExecuteAsync(writer, "UPDATE dbo.Versions SET ModificationDate='2020-01-01T00:00:04' WHERE VersionId=42;")
                    .GetAwaiter().GetResult(), rows.Add, timeout.Token);
            Assert.AreEqual(1, activity);
            CollectionAssert.AreEqual(new[] { 42, 43 }, rows.Select(row => row.VersionId).ToArray());
            Assert.IsTrue(rows[0].IndexingEnabled);
            Assert.IsFalse(rows[1].IndexingEnabled);
            Assert.AreEqual(new DateTime(2020, 1, 1, 0, 0, 2, DateTimeKind.Utc), rows[0].VersionModificationDateUtc);

            var current = (await provider.ReadIntegrityVersionsAsync(new[] { 42 }, excluded, timeout.Token)).Single();
            Assert.AreEqual(new DateTime(2020, 1, 1, 0, 0, 4, DateTimeKind.Utc), current.VersionModificationDateUtc);
            Assert.IsTrue(current.VersionTimestamp > rows[0].VersionTimestamp);
        }
        finally
        {
            await ExecuteAsync(master, $"DROP DATABASE [{databaseName}];");
        }
    }

    private static async Task ExecuteAsync(SqlConnection connection, string sql)
    {
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = 15 };
        await command.ExecuteNonQueryAsync();
    }
}
