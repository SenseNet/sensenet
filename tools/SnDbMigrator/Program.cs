using Microsoft.Extensions.Configuration;
using Spectre.Console;

namespace SnDbMigrator;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        // ── Banner ─────────────────────────────────────────────
        AnsiConsole.Write(new FigletText("SnDbMigrator")
            .Color(Color.CornflowerBlue));
        AnsiConsole.MarkupLine("[dim]sensenet MSSQL → PostgreSQL database migrator[/]\n");

        // ── Load configuration ─────────────────────────────────
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.local.json", optional: true)
            .AddEnvironmentVariables("SNMIGRATE_")
            .AddCommandLine(args)
            .Build();

        var options = new MigrationOptions();
        config.GetSection("Migration").Bind(options);

        // ── Validate ───────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(options.Source?.ConnectionString))
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Source connection string is not configured.");
            return 1;
        }
        if (string.IsNullOrWhiteSpace(options.Target?.ConnectionString))
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Target connection string is not configured.");
            return 1;
        }
        if (!options.Source.IsMsSql)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Source must be MsSql provider.");
            return 1;
        }
        if (!options.Target.IsPostgreSql)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Target must be PostgreSql provider.");
            return 1;
        }

        // ── Display config ─────────────────────────────────────
        var configTable = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold]Migration Configuration[/]")
            .AddColumn("Setting")
            .AddColumn("Value");

        configTable.AddRow("Source", MaskConnectionString(options.Source.ConnectionString));
        configTable.AddRow("Target", MaskConnectionString(options.Target.ConnectionString));
        configTable.AddRow("Batch Size", options.BatchSize.ToString("N0"));
        configTable.AddRow("Blob Batch Size", options.BlobBatchSize.ToString("N0"));
        configTable.AddRow("Truncate Target", options.TruncateTarget ? "[yellow]Yes[/]" : "No");
        configTable.AddRow("Disable FK", options.DisableForeignKeys ? "Yes" : "No");
        configTable.AddRow("Fix Sequences", options.FixSequences ? "Yes" : "No");
        configTable.AddRow("Verify", options.Verify ? "Yes" : "No");
        configTable.AddRow("Checkpoint File", options.CheckpointFile);

        if (options.SkipTables.Length > 0)
            configTable.AddRow("Skip Tables", string.Join(", ", options.SkipTables));

        AnsiConsole.Write(configTable);
        AnsiConsole.WriteLine();

        // ── Check for existing checkpoint ──────────────────────
        if (File.Exists(options.CheckpointFile))
        {
            var resume = AnsiConsole.Confirm(
                "[yellow]A checkpoint file exists. Resume previous migration?[/]", true);
            if (!resume)
            {
                File.Delete(options.CheckpointFile);
                AnsiConsole.MarkupLine("[dim]Checkpoint cleared.[/]");
            }
            else
            {
                AnsiConsole.MarkupLine("[green]Resuming from checkpoint...[/]");
            }
        }

        // ── Confirm destructive operation ──────────────────────
        if (options.TruncateTarget && !File.Exists(options.CheckpointFile))
        {
            var proceed = AnsiConsole.Confirm(
                "[red]⚠ This will TRUNCATE all target tables. Continue?[/]", false);
            if (!proceed)
            {
                AnsiConsole.MarkupLine("[dim]Aborted.[/]");
                return 0;
            }
        }

        // ── Run migration ──────────────────────────────────────
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
            AnsiConsole.MarkupLine("\n[yellow]Cancellation requested. Finishing current batch...[/]");
        };

        var engine = new MigrationEngine(options);
        MigrationResult result;

        try
        {
            result = await engine.RunAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.MarkupLine("\n[yellow]Migration cancelled. Progress saved to checkpoint.[/]");
            return 2;
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return 1;
        }

        // ── Summary ────────────────────────────────────────────
        AnsiConsole.WriteLine();
        var summaryPanel = new Panel(
            new Rows(
                new Markup($"[bold]Status:[/]      {(result.Success ? "[green]SUCCESS[/]" : "[red]FAILED[/]")}"),
                new Markup($"[bold]Duration:[/]    {result.Elapsed:hh\\:mm\\:ss\\.fff}"),
                new Markup($"[bold]Tables:[/]      {result.TablesProcessed}"),
                new Markup($"[bold]Total Rows:[/]  {result.TotalRows:N0}"),
                new Markup($"[bold]Throughput:[/]  {(result.Elapsed.TotalSeconds > 0 ? (result.TotalRows / result.Elapsed.TotalSeconds).ToString("N0") : "—")} rows/sec"),
                result.Verified
                    ? new Markup("[bold]Verified:[/]    [green]All row counts match ✓[/]")
                    : result.Success
                        ? new Markup("[bold]Verified:[/]    [dim]Not requested[/]")
                        : new Markup("[bold]Verified:[/]    [red]Row count mismatches detected[/]")
            ))
            .Header("[bold]Migration Summary[/]")
            .Border(BoxBorder.Double)
            .Padding(1, 0);

        AnsiConsole.Write(summaryPanel);

        if (result.Errors.Count > 0)
        {
            AnsiConsole.MarkupLine("\n[red]Errors:[/]");
            foreach (var err in result.Errors)
                AnsiConsole.MarkupLine($"  [red]•[/] {err}");
        }

        // ── Per-table breakdown ────────────────────────────────
        if (engine.CompletedTables.Count > 0)
        {
            AnsiConsole.WriteLine();
            var breakdown = new Table()
                .Border(TableBorder.Rounded)
                .Title("[bold]Per-Table Breakdown[/]")
                .AddColumn("Table")
                .AddColumn("Rows", c => c.RightAligned())
                .AddColumn("Time")
                .AddColumn("Rate");

            foreach (var (table, (rows, elapsed)) in engine.CompletedTables.OrderBy(x => x.Key))
            {
                var rate = elapsed.TotalSeconds > 0
                    ? $"{rows / elapsed.TotalSeconds:N0} rows/s"
                    : "—";
                breakdown.AddRow(
                    table,
                    rows.ToString("N0"),
                    elapsed == TimeSpan.Zero ? "[dim]cached[/]" : elapsed.ToString(@"mm\:ss\.fff"),
                    elapsed == TimeSpan.Zero ? "[dim]—[/]" : rate);
            }
            AnsiConsole.Write(breakdown);
        }

        return result.Success ? 0 : 1;
    }

    private static string MaskConnectionString(string cs)
    {
        // Mask password in connection string for display
        var parts = cs.Split(';');
        for (int i = 0; i < parts.Length; i++)
        {
            var kv = parts[i].Split('=', 2);
            if (kv.Length == 2 && kv[0].Trim().Equals("Password", StringComparison.OrdinalIgnoreCase))
                parts[i] = $"{kv[0]}=****";
        }
        return string.Join(";", parts);
    }
}
