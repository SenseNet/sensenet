using Spectre.Console;
using Spectre.Console.Rendering;

namespace SnBenchmark;

/// <summary>
/// Live dashboard rendered with Spectre.Console.
/// Updates every second while the benchmark runs.
/// Includes live sparkline charts for throughput, latency, errors, and concurrency.
/// </summary>
public static class ConsoleUi
{
    private const int ChartWidth = 46;
    private const int ChartHeight = 7;

    public static async Task RunDashboardAsync(
        BenchmarkEngine engine, MetricsCollector metrics, BenchmarkOptions opts,
        CancellationToken ct)
    {
        IRenderable display = new Text("Starting...");

        await AnsiConsole.Live(display)
            .AutoClear(true)
            .Overflow(VerticalOverflow.Ellipsis)
            .StartAsync(async ctx =>
            {
                while (!ct.IsCancellationRequested && engine.IsRunning)
                {
                    metrics.RecordLiveSample(engine.CurrentConcurrency);
                    var snap = metrics.GetSnapshot(engine.CurrentConcurrency);
                    var samples = metrics.GetLiveSamples();
                    display = BuildFullDashboard(snap, samples, opts);
                    ctx.UpdateTarget(display);

                    try { await Task.Delay(1000, ct); }
                    catch (OperationCanceledException) { break; }
                }

                // Final render
                metrics.RecordLiveSample(engine.CurrentConcurrency);
                var finalSnap = metrics.GetSnapshot(engine.CurrentConcurrency);
                var finalSamples = metrics.GetLiveSamples();
                display = BuildFullDashboard(finalSnap, finalSamples, opts);
                ctx.UpdateTarget(display);
            });
    }

    /// <summary>
    /// Builds the entire dashboard as a vertical stack of renderables:
    /// metrics table on top, chart panels below.
    /// </summary>
    private static IRenderable BuildFullDashboard(MetricsSnapshot snap, LiveSample[] samples,
        BenchmarkOptions opts)
    {
        var parts = new List<IRenderable>();

        // ── Metrics table
        parts.Add(BuildMetricsTable(snap, opts));

        // ── Charts section
        if (samples.Length > 1)
        {
            parts.Add(new Text(""));
            parts.Add(BuildChartsSection(samples, opts));
        }

        // ── Errors
        if (snap.ErrorMessages.Count > 0)
        {
            parts.Add(new Text(""));
            parts.Add(BuildErrorsPanel(snap));
        }

        // ── Load bar
        parts.Add(new Text(""));
        parts.Add(BuildLoadBar(snap, opts));

        return new Rows(parts);
    }

    // ── Metrics table ───────────────────────────────────────────────

    private static Table BuildMetricsTable(MetricsSnapshot snap, BenchmarkOptions opts)
    {
        var t = new Table()
            .Border(TableBorder.Double)
            .BorderColor(Color.Cyan1)
            .Title("[bold cyan]╔══ sensenet Benchmark ══╗[/]")
            .Caption($"[dim]Press Ctrl+C to stop  │  Target: {opts.RepositoryUrl}[/]")
            .AddColumn(new TableColumn("[bold]Metric[/]").Width(24))
            .AddColumn(new TableColumn("[bold]Value[/]").Width(18))
            .AddColumn(new TableColumn("[bold]Metric[/]").Width(24))
            .AddColumn(new TableColumn("[bold]Value[/]").Width(18));

        t.AddRow(
            Lbl("⏱  Elapsed"), Val($"{snap.Elapsed:hh\\:mm\\:ss}"),
            Lbl("👥 Concurrency"), ValHighlight($"{snap.CurrentConcurrency} / {opts.MaxConcurrency}"));

        t.AddRow(Rule("Throughput"), Empty(), Rule("Totals"), Empty());

        t.AddRow(
            Lbl("📝 Creates / min"), ValGood($"{snap.CreatesPerMinute:F1}"),
            Lbl("📊 Total Requests"), Val($"{snap.TotalRequests:N0}"));
        t.AddRow(
            Lbl("🔍 Queries / min"), ValGood($"{snap.QueriesPerMinute:F1}"),
            Lbl("✅ Successful"), ValGood($"{snap.TotalSuccessful:N0}"));
        t.AddRow(
            Lbl("⚡ Recent req/s"), ValHighlight($"{snap.RecentRequestsPerSec:F1}"),
            Lbl("❌ Failed"), snap.TotalFailed > 0 ? ValBad($"{snap.TotalFailed:N0}") : Val("0"));

        t.AddRow(Rule("Latency (ms)"), Empty(), Rule("Recent (10s)"), Empty());

        t.AddRow(
            Lbl("📝 Avg Create"), LatencyColor(snap.AvgCreateMs),
            Lbl("⚡ Create / sec"), ValHighlight($"{snap.RecentCreatesPerSec:F1}"));
        t.AddRow(
            Lbl("🔍 Avg Query"), LatencyColor(snap.AvgQueryMs),
            Lbl("⚡ Query / sec"), ValHighlight($"{snap.RecentQueriesPerSec:F1}"));
        t.AddRow(
            Lbl("📝 P95 Create"), LatencyColor(snap.P95CreateMs),
            Lbl("📝 Recent Avg Create"), LatencyColor(snap.RecentAvgCreateMs));
        t.AddRow(
            Lbl("🔍 P95 Query"), LatencyColor(snap.P95QueryMs),
            Lbl("🔍 Recent Avg Query"), LatencyColor(snap.RecentAvgQueryMs));
        t.AddRow(
            Lbl("📝 P99 Create"), LatencyColor(snap.P99CreateMs),
            Lbl("🔥 Recent Error %"),
            snap.RecentErrorRate > 5 ? ValBad($"{snap.RecentErrorRate:F1}%") : ValGood($"{snap.RecentErrorRate:F1}%"));
        t.AddRow(
            Lbl("📝 Max Create"), LatencyColor(snap.MaxCreateMs),
            Lbl("🔍 Max Query"), LatencyColor(snap.MaxQueryMs));

        return t;
    }

    // ── Charts section ──────────────────────────────────────────────

    private static IRenderable BuildChartsSection(LiveSample[] samples, BenchmarkOptions opts)
    {
        var tMax = samples.Max(s => s.RequestsPerSec);
        var tCur = samples.Last().RequestsPerSec;
        var lMax = samples.Max(s => s.AvgLatencyMs);
        var lCur = samples.Last().AvgLatencyMs;
        var eMax = samples.Max(s => s.ErrorCount);
        var eCur = samples.Last().ErrorCount;
        var cCur = samples.Last().Concurrency;

        // ── Row 1: Throughput + Latency
        var throughputPanel = new Panel(
                new Rows(
                    BuildSparkChart(samples.Select(s => (double)s.RequestsPerSec).ToArray(), "green", "darkgreen"),
                    new Markup($" [dim]now:[/] [bold green]{tCur}[/]  [dim]peak:[/] [bold green]{tMax}[/]")))
            .Header("[bold green]📈 Throughput (req/s)[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Green)
            .Expand();

        var latencyPanel = new Panel(
                new Rows(
                    BuildSparkChart(samples.Select(s => s.AvgLatencyMs).ToArray(), "yellow", "orange1"),
                    new Markup($" [dim]now:[/] [bold yellow]{lCur:F0}ms[/]  [dim]peak:[/] [bold yellow]{lMax:F0}ms[/]")))
            .Header("[bold yellow]🕐 Latency (ms)[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Yellow)
            .Expand();

        // ── Row 2: Errors + Concurrency
        var errorPanel = new Panel(
                new Rows(
                    BuildSparkChart(samples.Select(s => (double)s.ErrorCount).ToArray(), "red", "darkred"),
                    new Markup($" [dim]now:[/] [bold red]{eCur}[/]  [dim]peak:[/] [bold red]{eMax}[/]")))
            .Header("[bold red]❌ Errors/s[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Red)
            .Expand();

        var concurrencyPanel = new Panel(
                new Rows(
                    BuildSparkChart(samples.Select(s => (double)s.Concurrency).ToArray(), "magenta1", "purple"),
                    new Markup($" [dim]now:[/] [bold magenta1]{cCur}[/]  [dim]max:[/] [bold magenta1]{opts.MaxConcurrency}[/]")))
            .Header("[bold magenta1]👥 Concurrency[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Magenta1)
            .Expand();

        var chartsGrid = new Grid()
            .AddColumn(new GridColumn().NoWrap())
            .AddColumn(new GridColumn().NoWrap());

        chartsGrid.AddRow(throughputPanel, latencyPanel);
        chartsGrid.AddRow(errorPanel, concurrencyPanel);

        return chartsGrid;
    }

    // ── Errors panel ────────────────────────────────────────────────

    private static Panel BuildErrorsPanel(MetricsSnapshot snap)
    {
        var rows = new List<IRenderable>();
        foreach (var (msg, count) in snap.ErrorMessages)
        {
            rows.Add(new Markup($"  [red]{Markup.Escape(msg)}[/] [bold red]×{count}[/]"));
        }

        return new Panel(new Rows(rows))
            .Header("[bold red]⚠ Error Details[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Red);
    }

    // ── Load bar ────────────────────────────────────────────────────

    private static IRenderable BuildLoadBar(MetricsSnapshot snap, BenchmarkOptions opts)
    {
        var pct = (double)snap.CurrentConcurrency / opts.MaxConcurrency * 100;
        var barLen = 40;
        var filled = (int)(pct / 100 * barLen);
        var bar = new string('█', filled) + new string('░', barLen - filled);
        var barColor = pct < 50 ? "green" : pct < 80 ? "yellow" : "red";

        return new Markup($"  [bold]🔥 Load:[/] [{barColor}]{bar}[/] [{barColor} bold]{pct:F0}%[/]  [{barColor}]{snap.CurrentConcurrency}/{opts.MaxConcurrency} workers[/]");
    }

    // ── Spark chart builder ─────────────────────────────────────────

    private static Markup BuildSparkChart(double[] data, string mainColor, string dimColor)
    {
        if (data.Length == 0)
            return new Markup("[dim]waiting for data…[/]");

        var resampled = Resample(data, ChartWidth);
        var maxVal = resampled.Max();
        if (maxVal < 0.001) maxVal = 1;

        var blocks = new[] { ' ', '▁', '▂', '▃', '▄', '▅', '▆', '▇', '█' };
        var lines = new string[ChartHeight];

        for (var row = ChartHeight - 1; row >= 0; row--)
        {
            var chars = new char[resampled.Length];
            for (var col = 0; col < resampled.Length; col++)
            {
                var normalized = resampled[col] / maxVal;
                var totalLevels = ChartHeight * 8;
                var level = (int)(normalized * totalLevels);
                var rowBase = row * 8;
                var inRow = level - rowBase;

                chars[col] = inRow <= 0 ? ' ' : inRow >= 8 ? '█' : blocks[inRow];
            }
            lines[ChartHeight - 1 - row] = new string(chars);
        }

        var sb = new System.Text.StringBuilder();
        for (var i = 0; i < lines.Length; i++)
        {
            var escaped = Markup.Escape(lines[i]);
            var color = i < 3 ? mainColor : dimColor;
            sb.Append($"[{color}]{escaped}[/]");
            if (i < lines.Length - 1) sb.AppendLine();
        }

        return new Markup(sb.ToString());
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private static double[] Resample(double[] data, int targetLen)
    {
        if (data.Length == 0) return new double[targetLen];
        if (data.Length <= targetLen)
        {
            var padded = new double[targetLen];
            var offset = targetLen - data.Length;
            Array.Copy(data, 0, padded, offset, data.Length);
            return padded;
        }

        var result = new double[targetLen];
        var ratio = (double)data.Length / targetLen;
        for (var i = 0; i < targetLen; i++)
        {
            var start = (int)(i * ratio);
            var end = (int)((i + 1) * ratio);
            if (end > data.Length) end = data.Length;
            if (start >= end) { result[i] = data[start]; continue; }
            result[i] = 0;
            for (var j = start; j < end; j++) result[i] += data[j];
            result[i] /= (end - start);
        }
        return result;
    }

    private static Markup Lbl(string t) => new($"[bold]{Markup.Escape(t)}[/]");
    private static Markup Val(string t) => new($"[white]{Markup.Escape(t)}[/]");
    private static Markup ValGood(string t) => new($"[bold green]{Markup.Escape(t)}[/]");
    private static Markup ValBad(string t) => new($"[bold red]{Markup.Escape(t)}[/]");
    private static Markup ValHighlight(string t) => new($"[bold yellow]{Markup.Escape(t)}[/]");
    private static IRenderable Empty() => new Text("");
    private static IRenderable Rule(string label) =>
        new Spectre.Console.Rule($"[dim]{Markup.Escape(label)}[/]").RuleStyle("grey");

    private static Markup LatencyColor(double ms) => ms switch
    {
        0 => new("[dim]—[/]"),
        < 100 => new($"[bold green]{ms:F0} ms[/]"),
        < 500 => new($"[bold yellow]{ms:F0} ms[/]"),
        < 2000 => new($"[bold orange1]{ms:F0} ms[/]"),
        _ => new($"[bold red]{ms:F0} ms[/]")
    };

    // ── Banner ──────────────────────────────────────────────────────

    public static void PrintBanner(BenchmarkOptions opts)
    {
        AnsiConsole.Write(new FigletText("SnBenchmark")
            .Color(Color.Cyan1).Centered());

        AnsiConsole.Write(new Spectre.Console.Rule("[cyan]sensenet Repository Stress Test Tool[/]")
            .RuleStyle("cyan").DoubleBorder());

        var configPanel = new Panel(
            new Rows(
                new Markup($"[bold]Repository:[/]      [cyan]{Markup.Escape(opts.RepositoryUrl)}[/]"),
                new Markup($"[bold]Base path:[/]       [cyan]{Markup.Escape(opts.BasePath)}[/]"),
                new Markup($"[bold]Content type:[/]    [cyan]{Markup.Escape(opts.ContentTypeName)}[/]"),
                new Markup($"[bold]Concurrency:[/]     [yellow]{opts.InitialConcurrency}[/] → [yellow]{opts.MaxConcurrency}[/]  (step: [yellow]+{opts.RampUpConcurrencyStep}[/] every [yellow]{opts.RampUpStepSeconds}s[/])"),
                new Markup($"[bold]Duration:[/]        [yellow]{(opts.TestDurationSeconds > 0 ? $"{opts.TestDurationSeconds}s" : "unlimited (Ctrl+C)")}[/]"),
                new Markup($"[bold]Mix:[/]             Create [yellow]{opts.CreateWeight}%[/] / Query [yellow]{opts.QueryWeight}%[/]"),
                new Markup($"[bold]Timeout:[/]         [yellow]{opts.RequestTimeoutSeconds}s[/] per request")
            ))
            .Header("[bold cyan]⚙  Configuration[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Grey);

        AnsiConsole.Write(configPanel);
        AnsiConsole.WriteLine();
    }

    // ── Final summary ───────────────────────────────────────────────

    public static void PrintFinalSummary(MetricsSnapshot snap)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Spectre.Console.Rule("[bold green]✔  Benchmark Complete[/]")
            .RuleStyle("green").DoubleBorder());

        var st = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Green)
            .AddColumn("[bold]Metric[/]")
            .AddColumn("[bold]Value[/]");

        st.AddRow("Duration", $"{snap.Elapsed:hh\\:mm\\:ss}");
        st.AddRow("Peak Concurrency", $"{snap.CurrentConcurrency}");
        st.AddRow("Total Requests", $"{snap.TotalRequests:N0}");
        st.AddRow("Successful", $"[green]{snap.TotalSuccessful:N0}[/]");
        st.AddRow("Failed", snap.TotalFailed > 0 ? $"[red]{snap.TotalFailed:N0}[/]" : "[green]0[/]");
        st.AddRow("Creates / min", $"{snap.CreatesPerMinute:F1}");
        st.AddRow("Queries / min", $"{snap.QueriesPerMinute:F1}");
        st.AddRow("Avg Create Latency", $"{snap.AvgCreateMs:F0} ms");
        st.AddRow("Avg Query Latency", $"{snap.AvgQueryMs:F0} ms");
        st.AddRow("P95 Create Latency", $"{snap.P95CreateMs:F0} ms");
        st.AddRow("P99 Create Latency", $"{snap.P99CreateMs:F0} ms");
        st.AddRow("Max Create Latency", $"{snap.MaxCreateMs:F0} ms");

        if (snap.ErrorMessages.Count > 0)
        {
            st.AddEmptyRow();
            foreach (var (msg, count) in snap.ErrorMessages)
                st.AddRow($"[red]Error[/]", $"[red]{Markup.Escape(msg)} (×{count})[/]");
        }

        AnsiConsole.Write(st);
    }
}
