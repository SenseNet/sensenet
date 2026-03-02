using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Npgsql;
using Spectre.Console;

namespace SnDbMigrator;

/// <summary>
/// Orchestrates the full migration pipeline:
/// 1. Pre-flight connectivity checks
/// 2. Disable FK constraints & triggers
/// 3. Truncate target tables (if configured)
/// 4. Migrate each table in dependency order
/// 5. Fix SERIAL sequences
/// 6. Re-enable FK constraints & triggers
/// 7. Verify row counts
/// </summary>
public sealed class MigrationEngine
{
    private readonly MigrationOptions _options;
    private readonly Checkpoint _checkpoint;
    private readonly TableMigrator _tableMigrator;
    private readonly SequenceFixer _sequenceFixer;
    private readonly ForeignKeyManager _fkManager;

    public MigrationEngine(MigrationOptions options)
    {
        _options = options;
        _checkpoint = Checkpoint.Load(options.CheckpointFile);
        _tableMigrator = new TableMigrator(options, _checkpoint, OnTableProgress);
        _sequenceFixer = new SequenceFixer(options.Target.ConnectionString);
        _fkManager = new ForeignKeyManager(options.Target.ConnectionString);
    }

    /// <summary>Current table being migrated (for UI).</summary>
    public string CurrentTable { get; private set; } = "";
    public long CurrentTableRows { get; private set; }
    public long CurrentTableTotal { get; private set; }
    public Dictionary<string, (long rows, TimeSpan elapsed)> CompletedTables { get; } = new();

    public event Action? ProgressChanged;

    private void OnTableProgress(string table, long done, long total)
    {
        CurrentTable = table;
        CurrentTableRows = done;
        CurrentTableTotal = total;
        ProgressChanged?.Invoke();
    }

    /// <summary>
    /// Runs the full migration.
    /// </summary>
    public async Task<MigrationResult> RunAsync(CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var result = new MigrationResult();

        // ── 1. Pre-flight checks ───────────────────────────────
        AnsiConsole.MarkupLine("[blue]▸[/] Testing source (MSSQL) connection...");
        if (!await TestSourceConnection(ct))
        {
            result.Success = false;
            result.Error = "Cannot connect to source MSSQL database.";
            return result;
        }
        AnsiConsole.MarkupLine("[green]  ✓[/] Source connected.");

        AnsiConsole.MarkupLine("[blue]▸[/] Testing target (PostgreSQL) connection...");
        if (!await TestTargetConnection(ct))
        {
            result.Success = false;
            result.Error = "Cannot connect to target PostgreSQL database.";
            return result;
        }
        AnsiConsole.MarkupLine("[green]  ✓[/] Target connected.");

        // ── 2. Determine tables to migrate ─────────────────────
        var tablesToMigrate = new List<TableDef>();
        foreach (var table in SenseNetSchema.Tables)
        {
            if (_options.SkipTables.Contains(table.Name, StringComparer.OrdinalIgnoreCase))
            {
                AnsiConsole.MarkupLine($"[yellow]  ⊘[/] Skipping [bold]{table.Name}[/] (configured)");
                continue;
            }

            var srcExists = await _tableMigrator.SourceTableExistsAsync(table.Name, ct);
            if (!srcExists)
            {
                if (table.Optional)
                {
                    AnsiConsole.MarkupLine($"[dim]  ○[/] {table.Name} (not in source, optional)");
                    continue;
                }
                AnsiConsole.MarkupLine($"[yellow]  ⚠[/] {table.Name} not found in source!");
                continue;
            }

            var tgtExists = await _tableMigrator.TargetTableExistsAsync(table.Name, ct);
            if (!tgtExists)
            {
                AnsiConsole.MarkupLine($"[yellow]  ⚠[/] {table.Name} not found in target!");
                continue;
            }

            tablesToMigrate.Add(table);
        }

        AnsiConsole.MarkupLine($"\n[blue]▸[/] {tablesToMigrate.Count} tables to migrate.\n");

        // ── 3. Disable FK constraints ──────────────────────────
        if (_options.DisableForeignKeys)
        {
            AnsiConsole.MarkupLine("[blue]▸[/] Disabling foreign key constraints...");
            await _fkManager.DisableForeignKeysAsync(ct);
            AnsiConsole.MarkupLine("[green]  ✓[/] FK constraints disabled.");
        }

        // ── 4. Truncate target tables (reverse order) ──────────
        if (_options.TruncateTarget && _checkpoint.Tables.Count == 0)
        {
            AnsiConsole.MarkupLine("[blue]▸[/] Truncating target tables...");
            foreach (var table in tablesToMigrate.AsEnumerable().Reverse())
            {
                try
                {
                    await _tableMigrator.TruncateTableAsync(table.Name, ct);
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[yellow]  ⚠[/] Truncate {table.Name}: {ex.Message}");
                }
            }
            AnsiConsole.MarkupLine("[green]  ✓[/] Target tables truncated.");
        }

        // ── 5. Migrate tables ──────────────────────────────────
        AnsiConsole.WriteLine();

        await AnsiConsole.Progress()
            .AutoClear(false)
            .HideCompleted(false)
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new SpinnerColumn(),
                new RemainingTimeColumn())
            .StartAsync(async ctx =>
            {
                foreach (var table in tablesToMigrate)
                {
                    ct.ThrowIfCancellationRequested();

                    if (_checkpoint.IsTableCompleted(table.Name))
                    {
                        var tc = _checkpoint.Tables[table.Name];
                        var task = ctx.AddTask($"[green]✓[/] {table.Name}", maxValue: tc.RowsMigrated);
                        task.Value = tc.RowsMigrated;
                        CompletedTables[table.Name] = (tc.RowsMigrated, TimeSpan.Zero);
                        continue;
                    }

                    var progressTask = ctx.AddTask($"  {table.Name}", maxValue: 100);
                    var tableSw = Stopwatch.StartNew();

                    // Wire up progress
                    void UpdateProgress()
                    {
                        if (CurrentTable == table.Name && CurrentTableTotal > 0)
                        {
                            progressTask.MaxValue = CurrentTableTotal;
                            progressTask.Value = CurrentTableRows;
                        }
                    }
                    ProgressChanged += UpdateProgress;

                    try
                    {
                        var rows = await _tableMigrator.MigrateTableAsync(table, ct);
                        tableSw.Stop();
                        CompletedTables[table.Name] = (rows, tableSw.Elapsed);
                        result.TotalRows += rows;

                        progressTask.Description = $"[green]✓[/] {table.Name}";
                        progressTask.MaxValue = Math.Max(rows, 1);
                        progressTask.Value = Math.Max(rows, 1);
                    }
                    catch (Exception ex)
                    {
                        progressTask.Description = $"[red]✗[/] {table.Name}: {ex.Message}";
                        result.Errors.Add($"{table.Name}: {ex.Message}");
                    }
                    finally
                    {
                        ProgressChanged -= UpdateProgress;
                    }
                }
            });

        // ── 6. Fix sequences ───────────────────────────────────
        if (_options.FixSequences)
        {
            AnsiConsole.MarkupLine("\n[blue]▸[/] Fixing SERIAL sequences...");
            var seqResults = await _sequenceFixer.FixAllSequencesAsync(tablesToMigrate, ct);
            foreach (var (table, maxId) in seqResults.Where(x => x.Value > 0))
            {
                AnsiConsole.MarkupLine($"[dim]    {table}: sequence → {maxId}[/]");
            }
            AnsiConsole.MarkupLine("[green]  ✓[/] Sequences fixed.");
        }

        // ── 7. Re-enable FK constraints ────────────────────────
        if (_options.DisableForeignKeys)
        {
            AnsiConsole.MarkupLine("[blue]▸[/] Re-enabling foreign key constraints...");
            await _fkManager.EnableForeignKeysAsync(ct);
            AnsiConsole.MarkupLine("[green]  ✓[/] FK constraints enabled and validated.");
        }

        // ── 8. Verify row counts ───────────────────────────────
        if (_options.Verify)
        {
            AnsiConsole.MarkupLine("\n[blue]▸[/] Verifying row counts...");
            var verifyTable = new Table()
                .Border(TableBorder.Rounded)
                .AddColumn("Table")
                .AddColumn("Source", c => c.RightAligned())
                .AddColumn("Target", c => c.RightAligned())
                .AddColumn("Status");

            bool allMatch = true;
            foreach (var table in tablesToMigrate)
            {
                var srcCount = await GetSourceRowCount(table.Name, ct);
                var tgtCount = await _tableMigrator.GetTargetRowCount(table.Name, ct);
                var match = srcCount == tgtCount;
                if (!match) allMatch = false;

                verifyTable.AddRow(
                    table.Name,
                    srcCount.ToString("N0"),
                    tgtCount.ToString("N0"),
                    match ? "[green]✓[/]" : "[red]✗ MISMATCH[/]");
            }
            AnsiConsole.Write(verifyTable);
            result.Verified = allMatch;
        }

        // ── Done ───────────────────────────────────────────────
        sw.Stop();
        result.Success = result.Errors.Count == 0;
        result.Elapsed = sw.Elapsed;
        result.TablesProcessed = CompletedTables.Count;

        _checkpoint.MarkMigrationCompleted();
        if (result.Success)
            _checkpoint.Delete(); // Clean up checkpoint on success

        return result;
    }

    private async Task<bool> TestSourceConnection(CancellationToken ct)
    {
        try
        {
            await using var conn = new SqlConnection(_options.Source.ConnectionString);
            await conn.OpenAsync(ct);
            return true;
        }
        catch { return false; }
    }

    private async Task<bool> TestTargetConnection(CancellationToken ct)
    {
        try
        {
            await using var conn = new NpgsqlConnection(_options.Target.ConnectionString);
            await conn.OpenAsync(ct);
            return true;
        }
        catch { return false; }
    }

    private async Task<long> GetSourceRowCount(string tableName, CancellationToken ct)
    {
        await using var conn = new SqlConnection(_options.Source.ConnectionString);
        await conn.OpenAsync(ct);
        await using var cmd = new SqlCommand($"SELECT COUNT_BIG(*) FROM [{tableName}]", conn);
        cmd.CommandTimeout = 120;
        return (long)(await cmd.ExecuteScalarAsync(ct))!;
    }
}

public sealed class MigrationResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public TimeSpan Elapsed { get; set; }
    public long TotalRows { get; set; }
    public int TablesProcessed { get; set; }
    public bool Verified { get; set; }
    public List<string> Errors { get; } = [];
}
