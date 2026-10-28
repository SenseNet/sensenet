using Microsoft.Extensions.Configuration;
using Spectre.Console;
using SnBenchmark;

// ── Configuration ───────────────────────────────────────────────────

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables("SNBENCH_")
    .AddCommandLine(args)
    .Build();

var opts = new BenchmarkOptions();
config.GetSection("Benchmark").Bind(opts);

// ── Validate ────────────────────────────────────────────────────────

if (string.IsNullOrWhiteSpace(opts.ApiKey))
{
    AnsiConsole.MarkupLine("[bold red]Error:[/] API key is required.");
    AnsiConsole.MarkupLine("[dim]Set it in appsettings.json, via --Benchmark:ApiKey=xxx, or SNBENCH_Benchmark__ApiKey env var.[/]");
    return 1;
}

if (string.IsNullOrWhiteSpace(opts.RepositoryUrl))
{
    AnsiConsole.MarkupLine("[bold red]Error:[/] Repository URL is required.");
    return 1;
}

// ── Banner ──────────────────────────────────────────────────────────

ConsoleUi.PrintBanner(opts);

// ── Ctrl+C handler ──────────────────────────────────────────────────

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    AnsiConsole.MarkupLine("\n[bold yellow]⚠  Stopping benchmark gracefully…[/]");
    cts.Cancel();
};

// ── Pre-flight checks ───────────────────────────────────────────────

using var client = new SenseNetClient(opts);

var alive = await AnsiConsole.Status()
    .Spinner(Spinner.Known.Dots)
    .StartAsync("[cyan]Checking repository connectivity…[/]",
        async _ => await client.IsAliveAsync(cts.Token));

if (!alive)
{
    AnsiConsole.MarkupLine($"[bold red]✗  Cannot reach repository at {Markup.Escape(opts.RepositoryUrl)}[/]");
    AnsiConsole.MarkupLine("[dim]Check the URL, network, and TLS settings.[/]");
    return 2;
}

AnsiConsole.MarkupLine("[bold green]✓  Repository is reachable[/]");

// ── Ensure base folder ──────────────────────────────────────────────

try
{
    await AnsiConsole.Status()
        .Spinner(Spinner.Known.Dots)
        .StartAsync($"[cyan]Ensuring benchmark folder {Markup.Escape(opts.BasePath)}…[/]",
            async _ => await client.EnsureBaseFolderAsync(cts.Token));

    AnsiConsole.MarkupLine($"[bold green]✓  Benchmark folder ready[/]");
}
catch (Exception ex)
{
    AnsiConsole.MarkupLine($"[bold red]✗  Failed to create benchmark folder: {Markup.Escape(ex.Message)}[/]");
    return 3;
}

AnsiConsole.WriteLine();
AnsiConsole.MarkupLine("[bold cyan]🚀 Starting benchmark…[/]");
AnsiConsole.MarkupLine("[dim]Press Ctrl+C to stop at any time.[/]");
AnsiConsole.WriteLine();

// ── Run benchmark ───────────────────────────────────────────────────

var metrics = new MetricsCollector();
var engine = new BenchmarkEngine(opts, client, metrics, cts);

// Start engine in background
var engineTask = Task.Run(() => engine.RunAsync(), cts.Token);

// Run live dashboard (blocks until done)
await ConsoleUi.RunDashboardAsync(engine, metrics, opts, cts.Token);

// Wait for engine to finish
try { await engineTask; }
catch (OperationCanceledException) { }

// ── Final summary ───────────────────────────────────────────────────

var finalSnap = metrics.GetSnapshot(engine.CurrentConcurrency);
ConsoleUi.PrintFinalSummary(finalSnap);

// ── Generate report ─────────────────────────────────────────────────

try
{
    var reportPath = await ReportGenerator.GenerateAsync(opts, metrics, engine.CurrentConcurrency);
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine($"[bold green]📄 Report saved:[/] [link]{Markup.Escape(reportPath)}[/]");
}
catch (Exception ex)
{
    AnsiConsole.MarkupLine($"[bold red]⚠  Failed to generate report: {Markup.Escape(ex.Message)}[/]");
}

AnsiConsole.WriteLine();
AnsiConsole.MarkupLine("[bold cyan]Done. 👋[/]");
return 0;
