using Npgsql;

namespace SnDbMigrator;

/// <summary>
/// Fixes PostgreSQL SERIAL sequences after bulk data migration.
/// When rows are inserted with explicit IDs (via COPY or INSERT with
/// overridden identity), the sequence stays at 1. This class resets
/// each sequence to MAX(id) + 1 so that new inserts get the correct ID.
/// </summary>
public sealed class SequenceFixer
{
    private readonly string _connectionString;

    public SequenceFixer(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <summary>
    /// Fixes the sequence for a single table's SERIAL column.
    /// </summary>
    public async Task<long> FixSequenceAsync(string tableName, string identityColumn,
        CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        // Find the sequence name for this column
        var seqName = await GetSequenceName(conn, tableName, identityColumn, ct);
        if (seqName == null)
            return 0;

        // Get current max ID
        await using var maxCmd = new NpgsqlCommand(
            $"SELECT COALESCE(MAX(\"{identityColumn}\"), 0) FROM \"{tableName}\"", conn);
        var maxId = Convert.ToInt64(await maxCmd.ExecuteScalarAsync(ct));

        if (maxId <= 0)
            return 0;

        // Reset the sequence
        await using var setCmd = new NpgsqlCommand(
            $"SELECT setval('{seqName}', @maxId)", conn);
        setCmd.Parameters.AddWithValue("@maxId", maxId);
        await setCmd.ExecuteScalarAsync(ct);

        return maxId;
    }

    /// <summary>
    /// Fixes sequences for all tables that have identity columns.
    /// Returns a dictionary of table → new sequence value.
    /// </summary>
    public async Task<Dictionary<string, long>> FixAllSequencesAsync(
        IEnumerable<TableDef> tables, CancellationToken ct)
    {
        var results = new Dictionary<string, long>();
        foreach (var table in tables)
        {
            if (table.IdentityColumn == null) continue;

            var newVal = await FixSequenceAsync(table.Name, table.IdentityColumn, ct);
            results[table.Name] = newVal;
        }
        return results;
    }

    private static async Task<string?> GetSequenceName(NpgsqlConnection conn,
        string tableName, string columnName, CancellationToken ct)
    {
        await using var cmd = new NpgsqlCommand(
            "SELECT pg_get_serial_sequence(@table, @column)", conn);
        cmd.Parameters.AddWithValue("@table", tableName);
        cmd.Parameters.AddWithValue("@column", columnName);
        var result = await cmd.ExecuteScalarAsync(ct);
        return result as string;
    }
}
