using System.Data;
using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Npgsql;
using NpgsqlTypes;

namespace SnDbMigrator;

/// <summary>
/// Migrates a single table from MSSQL to PostgreSQL using streaming reads
/// and PostgreSQL COPY BINARY for maximum throughput. Blob columns are
/// streamed without loading entire values into memory.
/// </summary>
public sealed class TableMigrator
{
    private readonly MigrationOptions _options;
    private readonly Checkpoint _checkpoint;
    private readonly Action<string, long, long> _onProgress; // tableName, rowsDone, totalRows

    // Columns whose MSSQL type differs from PG and need conversion
    private static readonly HashSet<string> BooleanColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "Staging", "IsDeleted", "Hidden", "IsInherited", "LocalOnly", "IsUser"
    };

    // MSSQL [timestamp] / rowversion columns → PG BIGINT
    private static readonly HashSet<string> RowVersionColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "Timestamp"
    };

    // MSSQL UNIQUEIDENTIFIER columns → PG UUID
    private static readonly HashSet<string> GuidColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "RowGuid", "WorkflowInstanceId"
    };

    public TableMigrator(MigrationOptions options, Checkpoint checkpoint,
        Action<string, long, long> onProgress)
    {
        _options = options;
        _checkpoint = checkpoint;
        _onProgress = onProgress;
    }

    /// <summary>
    /// Migrates one table. Returns the number of rows migrated.
    /// </summary>
    public async Task<long> MigrateTableAsync(TableDef table, CancellationToken ct)
    {
        if (_checkpoint.IsTableCompleted(table.Name))
            return _checkpoint.Tables[table.Name].RowsMigrated;

        var totalRows = await GetSourceRowCount(table.Name, ct);
        _onProgress(table.Name, 0, totalRows);

        if (totalRows == 0)
        {
            _checkpoint.MarkTableCompleted(table.Name, 0);
            return 0;
        }

        // Get column metadata from source
        var columns = await GetColumnMetadata(table.Name, ct);

        if (table.IsBlob)
            return await MigrateBlobTableAsync(table, columns, totalRows, ct);
        else
            return await MigrateRegularTableAsync(table, columns, totalRows, ct);
    }

    /// <summary>
    /// Regular (non-blob) table migration using PostgreSQL COPY BINARY for max throughput.
    /// </summary>
    private async Task<long> MigrateRegularTableAsync(
        TableDef table, ColumnMeta[] columns, long totalRows, CancellationToken ct)
    {
        long rowsDone = 0;
        var batchSize = _options.BatchSize;

        // For tables with identity column, we can batch by ID for checkpoint/resume
        if (table.IdentityColumn != null)
        {
            var lastId = _checkpoint.GetLastId(table.Name);
            var maxId = await GetMaxId(table.Name, table.IdentityColumn, ct);

            while (lastId < maxId)
            {
                ct.ThrowIfCancellationRequested();
                var batchEnd = lastId + batchSize;
                var sql = $"SELECT * FROM [{table.Name}] WHERE [{table.IdentityColumn}] > @lastId " +
                          $"AND [{table.IdentityColumn}] <= @batchEnd ORDER BY [{table.IdentityColumn}]";

                await using var srcConn = new SqlConnection(_options.Source.ConnectionString);
                await srcConn.OpenAsync(ct);
                await using var cmd = new SqlCommand(sql, srcConn);
                cmd.Parameters.AddWithValue("@lastId", lastId);
                cmd.Parameters.AddWithValue("@batchEnd", batchEnd);
                cmd.CommandTimeout = 600;

                await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess, ct);

                var batchRows = await CopyBatchToPostgres(table, columns, reader, ct);
                rowsDone += batchRows;
                lastId = batchEnd;

                _checkpoint.MarkTableProgress(table.Name, lastId, rowsDone);
                _onProgress(table.Name, rowsDone, totalRows);
            }
        }
        else
        {
            // Tables without identity column: single pass, no resume
            var sql = $"SELECT * FROM [{table.Name}]";
            await using var srcConn = new SqlConnection(_options.Source.ConnectionString);
            await srcConn.OpenAsync(ct);
            await using var cmd = new SqlCommand(sql, srcConn) { CommandTimeout = 600 };
            await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess, ct);

            rowsDone = await CopyBatchToPostgres(table, columns, reader, ct);
            _onProgress(table.Name, rowsDone, totalRows);
        }

        _checkpoint.MarkTableCompleted(table.Name, rowsDone);
        return rowsDone;
    }

    /// <summary>
    /// Blob table migration uses parameterized INSERTs with streaming to avoid
    /// loading entire BLOBs into memory.
    /// </summary>
    private async Task<long> MigrateBlobTableAsync(
        TableDef table, ColumnMeta[] columns, long totalRows, CancellationToken ct)
    {
        long rowsDone = 0;
        var batchSize = _options.BlobBatchSize; // smaller batches for blob tables
        var blobCols = new HashSet<string>(table.BlobColumns ?? [], StringComparer.OrdinalIgnoreCase);

        if (table.IdentityColumn == null)
            throw new InvalidOperationException($"Blob table '{table.Name}' must have an identity column.");

        var lastId = _checkpoint.GetLastId(table.Name);
        var maxId = await GetMaxId(table.Name, table.IdentityColumn, ct);
        var colNames = columns.Select(c => $"\"{c.Name}\"").ToArray();
        var paramNames = columns.Select((c, i) => $"@p{i}").ToArray();
        var insertSql = $"INSERT INTO \"{table.Name}\" ({string.Join(", ", colNames)}) " +
                        $"VALUES ({string.Join(", ", paramNames)})";

        while (lastId < maxId)
        {
            ct.ThrowIfCancellationRequested();
            var batchEnd = lastId + batchSize;

            var sql = $"SELECT * FROM [{table.Name}] WHERE [{table.IdentityColumn}] > @lastId " +
                      $"AND [{table.IdentityColumn}] <= @batchEnd ORDER BY [{table.IdentityColumn}]";

            await using var srcConn = new SqlConnection(_options.Source.ConnectionString);
            await srcConn.OpenAsync(ct);
            await using var cmd = new SqlCommand(sql, srcConn);
            cmd.Parameters.AddWithValue("@lastId", lastId);
            cmd.Parameters.AddWithValue("@batchEnd", batchEnd);
            cmd.CommandTimeout = 600;
            await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess, ct);

            await using var pgConn = new NpgsqlConnection(_options.Target.ConnectionString);
            await pgConn.OpenAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                await using var insert = new NpgsqlCommand(insertSql, pgConn);
                insert.CommandTimeout = 300;

                for (int i = 0; i < columns.Length; i++)
                {
                    var col = columns[i];
                    if (reader.IsDBNull(i))
                    {
                        insert.Parameters.AddWithValue($"@p{i}", DBNull.Value);
                    }
                    else if (blobCols.Contains(col.Name))
                    {
                        // Stream blob data
                        var blobBytes = (byte[])reader.GetValue(i);
                        insert.Parameters.AddWithValue($"@p{i}", blobBytes);
                    }
                    else
                    {
                        var value = ConvertValue(col, reader, i, table.Name);
                        insert.Parameters.AddWithValue($"@p{i}", value);
                    }
                }

                await insert.ExecuteNonQueryAsync(ct);
                rowsDone++;
            }

            lastId = batchEnd;
            _checkpoint.MarkTableProgress(table.Name, lastId, rowsDone);
            _onProgress(table.Name, rowsDone, totalRows);
        }

        _checkpoint.MarkTableCompleted(table.Name, rowsDone);
        return rowsDone;
    }

    /// <summary>
    /// Writes rows from SqlDataReader to PostgreSQL using COPY BINARY.
    /// Returns the number of rows written.
    /// </summary>
    private async Task<long> CopyBatchToPostgres(
        TableDef table, ColumnMeta[] columns, SqlDataReader reader, CancellationToken ct)
    {
        long rows = 0;
        var colNames = string.Join(", ", columns.Select(c => $"\"{c.Name}\""));
        var copySql = $"COPY \"{table.Name}\" ({colNames}) FROM STDIN (FORMAT BINARY)";

        await using var pgConn = new NpgsqlConnection(_options.Target.ConnectionString);
        await pgConn.OpenAsync(ct);

        await using var writer = await pgConn.BeginBinaryImportAsync(copySql, ct);

        while (await reader.ReadAsync(ct))
        {
            await writer.StartRowAsync(ct);
            for (int i = 0; i < columns.Length; i++)
            {
                var col = columns[i];
                if (reader.IsDBNull(i))
                {
                    await writer.WriteNullAsync(ct);
                }
                else
                {
                    await WriteBinaryValue(writer, col, reader, i, table.Name, ct);
                }
            }
            rows++;
        }

        await writer.CompleteAsync(ct);
        return rows;
    }

    /// <summary>
    /// Writes a single value to the NpgsqlBinaryImporter with proper type mapping.
    /// </summary>
    private async Task WriteBinaryValue(NpgsqlBinaryImporter writer,
        ColumnMeta col, SqlDataReader reader, int ordinal, string tableName,
        CancellationToken ct)
    {
        // MSSQL timestamp (rowversion) → PG BIGINT
        if (RowVersionColumns.Contains(col.Name) && col.SqlType == "timestamp")
        {
            var bytes = (byte[])reader.GetValue(ordinal);
            var longVal = BitConverter.ToInt64(bytes.Reverse().ToArray(), 0);
            await writer.WriteAsync(longVal, NpgsqlDbType.Bigint, ct);
            return;
        }

        // Boolean conversion: bit → BOOLEAN for specific columns
        if (col.SqlType == "bit" && IsBooleanInPostgres(col.Name, tableName))
        {
            var val = reader.GetBoolean(ordinal);
            await writer.WriteAsync(val, NpgsqlDbType.Boolean, ct);
            return;
        }

        // tinyint → SMALLINT
        if (col.SqlType == "tinyint")
        {
            var val = reader.GetByte(ordinal);
            await writer.WriteAsync((short)val, NpgsqlDbType.Smallint, ct);
            return;
        }

        // UNIQUEIDENTIFIER → UUID
        if (col.SqlType == "uniqueidentifier")
        {
            var val = reader.GetGuid(ordinal);
            await writer.WriteAsync(val, NpgsqlDbType.Uuid, ct);
            return;
        }

        // datetime / datetime2 → TIMESTAMP
        if (col.SqlType is "datetime" or "datetime2")
        {
            var val = reader.GetDateTime(ordinal);
            await writer.WriteAsync(val, NpgsqlDbType.Timestamp, ct);
            return;
        }

        // ntext / nvarchar(max) → TEXT, nvarchar(N) → VARCHAR
        if (col.SqlType is "ntext" or "nvarchar" or "varchar" or "nchar")
        {
            var val = reader.GetString(ordinal);
            // CITEXT for Nodes.Path
            if (tableName == "Nodes" && col.Name == "Path")
                await writer.WriteAsync(val, NpgsqlDbType.Citext, ct);
            else
                await writer.WriteAsync(val, NpgsqlDbType.Text, ct);
            return;
        }

        // int → INT
        if (col.SqlType == "int")
        {
            await writer.WriteAsync(reader.GetInt32(ordinal), NpgsqlDbType.Integer, ct);
            return;
        }

        // bigint → BIGINT
        if (col.SqlType == "bigint")
        {
            await writer.WriteAsync(reader.GetInt64(ordinal), NpgsqlDbType.Bigint, ct);
            return;
        }

        // smallint → SMALLINT
        if (col.SqlType == "smallint")
        {
            await writer.WriteAsync(reader.GetInt16(ordinal), NpgsqlDbType.Smallint, ct);
            return;
        }

        // bit → SMALLINT (for legacy sensenet columns that use SMALLINT in PG)
        if (col.SqlType == "bit")
        {
            var val = reader.GetBoolean(ordinal);
            await writer.WriteAsync(val ? (short)1 : (short)0, NpgsqlDbType.Smallint, ct);
            return;
        }

        // varbinary / image → BYTEA (should not hit here for regular tables)
        if (col.SqlType is "varbinary" or "image")
        {
            var val = (byte[])reader.GetValue(ordinal);
            await writer.WriteAsync(val, NpgsqlDbType.Bytea, ct);
            return;
        }

        // Fallback: write as-is and let Npgsql figure it out
        var fallback = reader.GetValue(ordinal);
        await writer.WriteAsync(fallback, ct);
    }

    /// <summary>
    /// Converts a value for parameterized INSERT (blob tables).
    /// </summary>
    private object ConvertValue(ColumnMeta col, SqlDataReader reader, int ordinal, string tableName)
    {
        // MSSQL timestamp (rowversion) → PG BIGINT
        if (RowVersionColumns.Contains(col.Name) && col.SqlType == "timestamp")
        {
            var bytes = (byte[])reader.GetValue(ordinal);
            return BitConverter.ToInt64(bytes.Reverse().ToArray(), 0);
        }

        if (col.SqlType == "bit" && IsBooleanInPostgres(col.Name, tableName))
            return reader.GetBoolean(ordinal);

        if (col.SqlType == "bit")
            return reader.GetBoolean(ordinal) ? (short)1 : (short)0;

        if (col.SqlType == "tinyint")
            return (short)reader.GetByte(ordinal);

        return reader.GetValue(ordinal);
    }

    /// <summary>
    /// Determines whether a bit column should be mapped to BOOLEAN (true) or SMALLINT (false)
    /// in PostgreSQL, based on the actual PG schema.
    /// </summary>
    private static bool IsBooleanInPostgres(string columnName, string tableName)
    {
        // Files: Staging, IsDeleted → BOOLEAN
        if (tableName == "Files" && columnName is "Staging" or "IsDeleted") return true;
        // JournalItems: Hidden → BOOLEAN
        if (tableName == "JournalItems" && columnName == "Hidden") return true;
        // EFEntities: IsInherited → BOOLEAN
        if (tableName == "EFEntities" && columnName == "IsInherited") return true;
        // EFEntries: LocalOnly → BOOLEAN
        if (tableName == "EFEntries" && columnName == "LocalOnly") return true;
        // EFMemberships: IsUser → BOOLEAN
        if (tableName == "EFMemberships" && columnName == "IsUser") return true;
        // Nodes: all bit columns → SMALLINT (legacy)
        return false;
    }

    /// <summary>Truncates a table in PostgreSQL (CASCADE).</summary>
    public async Task TruncateTableAsync(string tableName, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(_options.Target.ConnectionString);
        await conn.OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(
            $"TRUNCATE TABLE \"{tableName}\" CASCADE", conn);
        cmd.CommandTimeout = 120;
        await cmd.ExecuteNonQueryAsync(ct);
    }

    // ── Helpers ─────────────────────────────────────────────────

    private async Task<long> GetSourceRowCount(string tableName, CancellationToken ct)
    {
        await using var conn = new SqlConnection(_options.Source.ConnectionString);
        await conn.OpenAsync(ct);
        await using var cmd = new SqlCommand(
            $"SELECT COUNT_BIG(*) FROM [{tableName}]", conn);
        cmd.CommandTimeout = 120;
        return (long)(await cmd.ExecuteScalarAsync(ct))!;
    }

    private async Task<long> GetMaxId(string tableName, string idCol, CancellationToken ct)
    {
        await using var conn = new SqlConnection(_options.Source.ConnectionString);
        await conn.OpenAsync(ct);
        await using var cmd = new SqlCommand(
            $"SELECT ISNULL(MAX([{idCol}]), 0) FROM [{tableName}]", conn);
        cmd.CommandTimeout = 60;
        return Convert.ToInt64(await cmd.ExecuteScalarAsync(ct));
    }

    /// <summary>Gets column metadata from the source MSSQL table.</summary>
    private async Task<ColumnMeta[]> GetColumnMetadata(string tableName, CancellationToken ct)
    {
        await using var conn = new SqlConnection(_options.Source.ConnectionString);
        await conn.OpenAsync(ct);
        // Use schema query to get column order and types
        await using var cmd = new SqlCommand(
            @"SELECT c.COLUMN_NAME, c.DATA_TYPE, c.CHARACTER_MAXIMUM_LENGTH,
                     c.ORDINAL_POSITION
              FROM INFORMATION_SCHEMA.COLUMNS c
              WHERE c.TABLE_NAME = @table AND c.TABLE_SCHEMA = 'dbo'
              ORDER BY c.ORDINAL_POSITION", conn);
        cmd.Parameters.AddWithValue("@table", tableName);
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        var cols = new List<ColumnMeta>();
        while (await reader.ReadAsync(ct))
        {
            cols.Add(new ColumnMeta(
                reader.GetString(0),          // COLUMN_NAME
                reader.GetString(1),          // DATA_TYPE
                reader.IsDBNull(2) ? null : reader.GetInt32(2) // CHARACTER_MAXIMUM_LENGTH
            ));
        }
        return cols.ToArray();
    }

    /// <summary>Checks if a table exists in the source MSSQL database.</summary>
    public async Task<bool> SourceTableExistsAsync(string tableName, CancellationToken ct)
    {
        await using var conn = new SqlConnection(_options.Source.ConnectionString);
        await conn.OpenAsync(ct);
        await using var cmd = new SqlCommand(
            "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @table AND TABLE_SCHEMA = 'dbo'",
            conn);
        cmd.Parameters.AddWithValue("@table", tableName);
        return (int)(await cmd.ExecuteScalarAsync(ct))! > 0;
    }

    /// <summary>Checks if a table exists in the target PostgreSQL database.</summary>
    public async Task<bool> TargetTableExistsAsync(string tableName, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(_options.Target.ConnectionString);
        await conn.OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(
            "SELECT COUNT(*) FROM information_schema.tables WHERE table_name = @table AND table_schema = 'public'",
            conn);
        cmd.Parameters.AddWithValue("@table", tableName);
        return (long)(await cmd.ExecuteScalarAsync(ct))! > 0;
    }

    /// <summary>Gets the row count from the target PostgreSQL table.</summary>
    public async Task<long> GetTargetRowCount(string tableName, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(_options.Target.ConnectionString);
        await conn.OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(
            $"SELECT COUNT(*) FROM \"{tableName}\"", conn);
        cmd.CommandTimeout = 120;
        return (long)(await cmd.ExecuteScalarAsync(ct))!;
    }
}

/// <summary>Column metadata from INFORMATION_SCHEMA.</summary>
public record ColumnMeta(string Name, string SqlType, int? MaxLength);
