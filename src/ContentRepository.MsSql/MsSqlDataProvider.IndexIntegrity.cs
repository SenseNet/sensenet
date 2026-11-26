using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using SenseNet.Search.Indexing.Integrity;

namespace SenseNet.ContentRepository.Storage.Data.MsSqlClient;

public partial class MsSqlDataProvider : IIndexIntegrityDataSource
{
    private const string IntegrityVersionsSql = """
        SELECT V.VersionId, CONVERT(bigint, V.[Timestamp]), N.NodeId, CONVERT(bigint, N.[Timestamp]),
               N.LastMajorVersionId, N.LastMinorVersionId, N.ModificationDate, V.ModificationDate, N.NodeTypeId
        FROM dbo.Versions AS V INNER JOIN dbo.Nodes AS N ON N.NodeId = V.NodeId
        """;

    string IIndexIntegrityDataSource.IntegrityDatabaseIdentity
    {
        get
        {
            var connection = new SqlConnectionStringBuilder(ConnectionStrings.Value.Repository);
            return $"{connection.DataSource}\n{connection.InitialCatalog}\n{connection.AttachDBFilename}";
        }
    }

    async Task<int> IIndexIntegrityDataSource.ExportIntegrityVersionsAsync(IReadOnlySet<int> excludedNodeTypes,
        Action snapshotOpened, Action<DatabaseVersion> write, CancellationToken cancellationToken)
    {
        using var context = CreateDataContext(cancellationToken);
        await using var connection = context.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Snapshot, cancellationToken)
            .ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandTimeout = 0; // The host supplies a time limit and cancels the entire export.
        command.CommandText = IntegrityVersionsSql + "\nORDER BY V.VersionId;";
        await using (var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken)
                         .ConfigureAwait(false))
        {
            snapshotOpened();
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                write(ReadIntegrityVersion(reader, excludedNodeTypes));
        }

        command.CommandText = GetLastIndexingActivityIdScript;
        var lastActivityId = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false));
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return lastActivityId;
    }

    async Task<IReadOnlyList<DatabaseVersion>> IIndexIntegrityDataSource.ReadIntegrityVersionsAsync(
        IReadOnlyCollection<int> versionIds, IReadOnlySet<int> excludedNodeTypes, CancellationToken cancellationToken)
    {
        if (versionIds.Count == 0)
            return Array.Empty<DatabaseVersion>();

        using var context = CreateDataContext(cancellationToken);
        await using var connection = context.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = IntegrityVersionsSql + """

            WHERE V.VersionId IN (SELECT CONVERT(int, [value]) FROM STRING_SPLIT(@VersionIds, ','))
            ORDER BY V.VersionId;
            """;
        command.Parameters.Add(context.CreateParameter("@VersionIds", DbType.String, -1, string.Join(",", versionIds)));
        var result = new List<DatabaseVersion>(versionIds.Count);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken)
            .ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            result.Add(ReadIntegrityVersion(reader, excludedNodeTypes));
        return result;
    }

    private static DatabaseVersion ReadIntegrityVersion(DbDataReader reader, IReadOnlySet<int> excludedNodeTypes) =>
        new(reader.GetInt32(0), reader.GetInt64(1), reader.GetInt32(2), reader.GetInt64(3),
            reader.IsDBNull(4) ? null : reader.GetInt32(4), reader.IsDBNull(5) ? null : reader.GetInt32(5),
            DateTime.SpecifyKind(reader.GetDateTime(6), DateTimeKind.Utc),
            DateTime.SpecifyKind(reader.GetDateTime(7), DateTimeKind.Utc),
            !excludedNodeTypes.Contains(reader.GetInt32(8)));
}
