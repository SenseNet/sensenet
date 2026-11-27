using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Search.Indexing.Integrity;

namespace SenseNet.ContentRepository.Storage.Data;

public abstract partial class RelationalDataProviderBase
{
    /// <inheritdoc cref="IIndexIntegrityDataSource.ExportIntegrityVersionsAsync"/>
    public async Task<int> ExportIntegrityVersionsAsync(IReadOnlySet<int> excludedNodeTypes,
        Action snapshotOpened, Action<DatabaseVersion> write, CancellationToken cancellationToken)
    {
        var script = ExportIntegrityVersionsScript;
        using var context = CreateDataContext(cancellationToken);
        await using var connection = context.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync(IntegritySnapshotIsolationLevel, cancellationToken)
            .ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandTimeout = 0; // The host supplies a time limit and cancels the entire export.
        command.CommandText = script;
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

    /// <summary>Isolation level that gives the export a consistent read snapshot.</summary>
    protected virtual IsolationLevel IntegritySnapshotIsolationLevel => IsolationLevel.Snapshot;

    /// <summary>Script that returns the integrity projection in VersionId order.</summary>
    protected virtual string ExportIntegrityVersionsScript =>
        throw new NotSupportedException("This data provider does not support exporting index integrity data.");

    /// <inheritdoc cref="IIndexIntegrityDataSource.ReadIntegrityVersionsAsync"/>
    public async Task<IReadOnlyList<DatabaseVersion>> ReadIntegrityVersionsAsync(
        IReadOnlyCollection<int> versionIds, IReadOnlySet<int> excludedNodeTypes, CancellationToken cancellationToken)
    {
        if (versionIds.Count == 0)
            return Array.Empty<DatabaseVersion>();

        var script = ReadIntegrityVersionsScript;
        using var context = CreateDataContext(cancellationToken);
        await using var connection = context.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = script;
        command.Parameters.Add(context.CreateParameter("@VersionIds", DbType.String, string.Join(",", versionIds)));
        var result = new List<DatabaseVersion>(versionIds.Count);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken)
            .ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            result.Add(ReadIntegrityVersion(reader, excludedNodeTypes));
        return result;
    }

    /// <summary>Script that returns the same projection for the comma-separated @VersionIds parameter.</summary>
    protected virtual string ReadIntegrityVersionsScript =>
        throw new NotSupportedException("This data provider does not support reading index integrity data by version ID.");

    private static DatabaseVersion ReadIntegrityVersion(DbDataReader reader, IReadOnlySet<int> excludedNodeTypes) =>
        new(reader.GetInt32(0), reader.GetInt64(1), reader.GetInt32(2), reader.GetInt64(3),
            reader.IsDBNull(4) ? null : reader.GetInt32(4), reader.IsDBNull(5) ? null : reader.GetInt32(5),
            DateTime.SpecifyKind(reader.GetDateTime(6), DateTimeKind.Utc),
            DateTime.SpecifyKind(reader.GetDateTime(7), DateTimeKind.Utc),
            !excludedNodeTypes.Contains(reader.GetInt32(8)));
}
