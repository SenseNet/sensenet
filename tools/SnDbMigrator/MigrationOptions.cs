namespace SnDbMigrator;

/// <summary>
/// Migration configuration bound from appsettings.json "Migration" section.
/// </summary>
public sealed class MigrationOptions
{
    public DbEndpoint Source { get; set; } = new();
    public DbEndpoint Target { get; set; } = new();

    /// <summary>Rows per batch for non-blob tables.</summary>
    public int BatchSize { get; set; } = 5000;

    /// <summary>Rows per batch for blob tables (Files, EFMessages).</summary>
    public int BlobBatchSize { get; set; } = 50;

    /// <summary>Path to checkpoint file for resume support.</summary>
    public string CheckpointFile { get; set; } = "./migration-checkpoint.json";

    /// <summary>Tables to skip (e.g. LogEntries, StatisticalData).</summary>
    public string[] SkipTables { get; set; } = [];

    /// <summary>Truncate target tables before migration.</summary>
    public bool TruncateTarget { get; set; } = true;

    /// <summary>Disable FK constraints during migration for performance.</summary>
    public bool DisableForeignKeys { get; set; } = true;

    /// <summary>Fix PostgreSQL sequences after migration.</summary>
    public bool FixSequences { get; set; } = true;

    /// <summary>Verify row counts after migration.</summary>
    public bool Verify { get; set; } = true;
}

public sealed class DbEndpoint
{
    public string Provider { get; set; } = "";
    public string ConnectionString { get; set; } = "";

    public bool IsMsSql => Provider.Equals("MsSql", StringComparison.OrdinalIgnoreCase);
    public bool IsPostgreSql => Provider.Equals("PostgreSql", StringComparison.OrdinalIgnoreCase);
}
