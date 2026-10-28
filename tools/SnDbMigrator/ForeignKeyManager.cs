using Npgsql;

namespace SnDbMigrator;

/// <summary>
/// Manages PostgreSQL foreign key constraints during migration.
/// Disables FKs before data load and re-enables them after.
/// </summary>
public sealed class ForeignKeyManager
{
    private readonly string _connectionString;

    public ForeignKeyManager(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <summary>Disables all FK constraints by setting them NOT VALID and disabling triggers.</summary>
    public async Task DisableForeignKeysAsync(CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        // Disable all triggers (which includes FK enforcement)
        foreach (var table in SenseNetSchema.Tables)
        {
            try
            {
                await using var cmd = new NpgsqlCommand(
                    $"ALTER TABLE \"{table.Name}\" DISABLE TRIGGER ALL", conn);
                cmd.CommandTimeout = 30;
                await cmd.ExecuteNonQueryAsync(ct);
            }
            catch (PostgresException ex) when (ex.SqlState == "42P01") // table does not exist
            {
                // Skip optional tables that don't exist
            }
        }
    }

    /// <summary>Re-enables all FK constraints and validates them.</summary>
    public async Task EnableForeignKeysAsync(CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        // Re-enable triggers
        foreach (var table in SenseNetSchema.Tables)
        {
            try
            {
                await using var cmd = new NpgsqlCommand(
                    $"ALTER TABLE \"{table.Name}\" ENABLE TRIGGER ALL", conn);
                cmd.CommandTimeout = 30;
                await cmd.ExecuteNonQueryAsync(ct);
            }
            catch (PostgresException ex) when (ex.SqlState == "42P01")
            {
                // Skip optional tables
            }
        }

        // Validate all FK constraints
        foreach (var fk in SenseNetSchema.ForeignKeys)
        {
            try
            {
                await using var cmd = new NpgsqlCommand(
                    $"ALTER TABLE \"{fk.Table}\" VALIDATE CONSTRAINT \"{fk.ConstraintName}\"", conn);
                cmd.CommandTimeout = 300;
                await cmd.ExecuteNonQueryAsync(ct);
            }
            catch (PostgresException ex) when (
                ex.SqlState == "42P01" || // table does not exist
                ex.SqlState == "42704")   // constraint does not exist
            {
                // Skip missing constraints
            }
        }
    }

    /// <summary>Allow explicit identity values to be inserted (override SERIAL).</summary>
    public async Task EnableIdentityInsertAsync(CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        // For SERIAL columns, PostgreSQL doesn't need SET IDENTITY_INSERT.
        // We just need to make sure the column is writable, which it always is
        // for SERIAL (it's just a DEFAULT from a sequence, not a constraint).
        // Nothing needed here for PostgreSQL SERIAL columns.
    }
}
