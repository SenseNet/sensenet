using System.Globalization;
using System.Text;

namespace SnBenchmark;

/// <summary>
/// Generates a Markdown benchmark report with all metrics, time-series
/// data, configuration, and error summary.
/// </summary>
public static class ReportGenerator
{
    public static async Task<string> GenerateAsync(
        BenchmarkOptions opts, MetricsCollector metrics, int peakConcurrency)
    {
        var snap = metrics.GetSnapshot(peakConcurrency);
        var slices = metrics.GetTimeSlices(5);
        var allResults = metrics.GetAllResults();
        var timestamp = DateTime.Now;
        var fileName = $"benchmark-report-{timestamp:yyyyMMdd-HHmmss}.md";

        var dir = Path.GetFullPath(opts.ReportDirectory);
        Directory.CreateDirectory(dir);
        var filePath = Path.Combine(dir, fileName);

        var sb = new StringBuilder();

        // ── Header
        sb.AppendLine("# sensenet Benchmark Report");
        sb.AppendLine();
        sb.AppendLine($"**Generated:** {timestamp:yyyy-MM-dd HH:mm:ss}  ");
        sb.AppendLine($"**Repository:** `{opts.RepositoryUrl}`  ");
        sb.AppendLine($"**Duration:** {snap.Elapsed:hh\\:mm\\:ss}  ");
        sb.AppendLine();

        // ── Configuration
        sb.AppendLine("## ⚙ Configuration");
        sb.AppendLine();
        sb.AppendLine("| Parameter | Value |");
        sb.AppendLine("|-----------|-------|");
        sb.AppendLine($"| Base Path | `{opts.BasePath}` |");
        sb.AppendLine($"| Content Type | `{opts.ContentTypeName}` |");
        sb.AppendLine($"| Initial Concurrency | {opts.InitialConcurrency} |");
        sb.AppendLine($"| Max Concurrency | {opts.MaxConcurrency} |");
        sb.AppendLine($"| Ramp-Up Step | +{opts.RampUpConcurrencyStep} every {opts.RampUpStepSeconds}s |");
        sb.AppendLine($"| Test Duration | {(opts.TestDurationSeconds > 0 ? $"{opts.TestDurationSeconds}s" : "unlimited")} |");
        sb.AppendLine($"| Request Timeout | {opts.RequestTimeoutSeconds}s |");
        sb.AppendLine($"| Create/Query Mix | {opts.CreateWeight}% / {opts.QueryWeight}% |");
        sb.AppendLine();

        // ── Summary
        sb.AppendLine("## 📊 Summary");
        sb.AppendLine();
        sb.AppendLine("| Metric | Value |");
        sb.AppendLine("|--------|-------|");
        sb.AppendLine($"| Total Requests | {snap.TotalRequests:N0} |");
        sb.AppendLine($"| Successful | {snap.TotalSuccessful:N0} |");
        sb.AppendLine($"| Failed | {snap.TotalFailed:N0} |");
        sb.AppendLine($"| Success Rate | {(snap.TotalRequests > 0 ? (double)snap.TotalSuccessful / snap.TotalRequests * 100 : 0):F1}% |");
        sb.AppendLine($"| Peak Concurrency | {peakConcurrency} |");
        sb.AppendLine($"| Total Creates | {snap.TotalCreates:N0} |");
        sb.AppendLine($"| Total Queries | {snap.TotalQueries:N0} |");
        sb.AppendLine();

        // ── Throughput
        sb.AppendLine("## ⚡ Throughput");
        sb.AppendLine();
        sb.AppendLine("| Metric | Value |");
        sb.AppendLine("|--------|-------|");
        sb.AppendLine($"| Creates / minute | {snap.CreatesPerMinute:F1} |");
        sb.AppendLine($"| Queries / minute | {snap.QueriesPerMinute:F1} |");
        sb.AppendLine($"| Total req / minute | {(snap.TotalRequests / Math.Max(snap.Elapsed.TotalMinutes, 0.01)):F1} |");
        sb.AppendLine();

        // ── Latency
        sb.AppendLine("## 🕐 Latency (ms)");
        sb.AppendLine();
        sb.AppendLine("| Metric | Create | Query |");
        sb.AppendLine("|--------|--------|-------|");
        sb.AppendLine($"| Average | {snap.AvgCreateMs:F0} | {snap.AvgQueryMs:F0} |");
        sb.AppendLine($"| P95 | {snap.P95CreateMs:F0} | {snap.P95QueryMs:F0} |");
        sb.AppendLine($"| P99 | {snap.P99CreateMs:F0} | {snap.P99QueryMs:F0} |");
        sb.AppendLine($"| Max | {snap.MaxCreateMs:F0} | {snap.MaxQueryMs:F0} |");
        sb.AppendLine();

        // ── Time series
        if (slices.Count > 0)
        {
            sb.AppendLine("## 📈 Time Series (5-second intervals)");
            sb.AppendLine();
            sb.AppendLine("| Time (s) | Requests | Success | Failed | Avg Latency (ms) | Creates | Queries |");
            sb.AppendLine("|----------|----------|---------|--------|-------------------|---------|---------|");
            foreach (var s in slices)
            {
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "| {0:F0} | {1} | {2} | {3} | {4:F0} | {5} | {6} |",
                    s.OffsetSeconds, s.RequestCount, s.SuccessCount,
                    s.FailCount, s.AvgLatencyMs, s.CreateCount, s.QueryCount));
            }
            sb.AppendLine();

            // ASCII chart: throughput over time
            sb.AppendLine("### Throughput Over Time");
            sb.AppendLine();
            sb.AppendLine("```");
            AppendAsciiChart(sb, slices);
            sb.AppendLine("```");
            sb.AppendLine();

            // ASCII chart: latency over time
            sb.AppendLine("### Latency Over Time");
            sb.AppendLine();
            sb.AppendLine("```");
            AppendLatencyChart(sb, slices);
            sb.AppendLine("```");
            sb.AppendLine();
        }

        // ── Latency distribution
        sb.AppendLine("## 📊 Latency Distribution");
        sb.AppendLine();
        AppendLatencyHistogram(sb, allResults);
        sb.AppendLine();

        // ── Errors
        if (snap.ErrorMessages.Count > 0)
        {
            sb.AppendLine("## ❌ Errors");
            sb.AppendLine();
            sb.AppendLine("| Error | Count |");
            sb.AppendLine("|-------|-------|");
            foreach (var (msg, count) in snap.ErrorMessages)
                sb.AppendLine($"| {EscapeMd(msg)} | {count} |");
            sb.AppendLine();
        }

        // ── Footer
        sb.AppendLine("---");
        sb.AppendLine($"*Report generated by SnBenchmark v1.0 at {timestamp:O}*");

        await File.WriteAllTextAsync(filePath, sb.ToString());
        return filePath;
    }

    // ── ASCII charts ────────────────────────────────────────────────

    private static void AppendAsciiChart(StringBuilder sb, IReadOnlyList<TimeSlice> slices)
    {
        const int height = 15;
        const int width = 60;

        var maxVal = slices.Max(s => s.RequestCount);
        if (maxVal == 0) maxVal = 1;

        // Resample to width
        var data = Resample(slices.Select(s => (double)s.RequestCount).ToArray(), width);

        sb.AppendLine($"  Requests per 5s interval (max: {maxVal})");
        for (var row = height; row >= 1; row--)
        {
            var threshold = (double)row / height * maxVal;
            sb.Append(row == height ? $"{maxVal,5} │" : row == 1 ? $"{"0",5} │" : "      │");

            foreach (var val in data)
            {
                sb.Append(val >= threshold ? '█' : ' ');
            }
            sb.AppendLine();
        }
        sb.Append("      └");
        sb.AppendLine(new string('─', width));

        var totalSecs = slices.Last().OffsetSeconds;
        sb.AppendLine($"       0s{new string(' ', width / 2 - 5)}{totalSecs / 2:F0}s{new string(' ', width / 2 - 5)}{totalSecs:F0}s");
    }

    private static void AppendLatencyChart(StringBuilder sb, IReadOnlyList<TimeSlice> slices)
    {
        const int height = 12;
        const int width = 60;

        var maxVal = slices.Max(s => s.AvgLatencyMs);
        if (maxVal < 1) maxVal = 1;

        var data = Resample(slices.Select(s => s.AvgLatencyMs).ToArray(), width);

        sb.AppendLine($"  Avg latency ms (max: {maxVal:F0})");
        for (var row = height; row >= 1; row--)
        {
            var threshold = (double)row / height * maxVal;
            sb.Append(row == height ? $"{maxVal,7:F0} │" : row == 1 ? $"{"0",7} │" : "        │");

            foreach (var val in data)
            {
                sb.Append(val >= threshold ? '▓' : ' ');
            }
            sb.AppendLine();
        }
        sb.Append("        └");
        sb.AppendLine(new string('─', width));
    }

    private static void AppendLatencyHistogram(StringBuilder sb, IReadOnlyList<RequestResult> results)
    {
        var buckets = new (string Label, double Min, double Max)[]
        {
            ("< 50ms", 0, 50),
            ("50-100ms", 50, 100),
            ("100-200ms", 100, 200),
            ("200-500ms", 200, 500),
            ("500ms-1s", 500, 1000),
            ("1-2s", 1000, 2000),
            ("2-5s", 2000, 5000),
            ("5-10s", 5000, 10000),
            ("> 10s", 10000, double.MaxValue),
        };

        sb.AppendLine("```");
        var maxCount = 0;
        var bucketCounts = buckets.Select(b =>
        {
            var count = results.Count(r => r.ElapsedMs >= b.Min && r.ElapsedMs < b.Max);
            if (count > maxCount) maxCount = count;
            return (b.Label, Count: count);
        }).ToArray();

        if (maxCount == 0) maxCount = 1;
        const int barWidth = 40;

        foreach (var (label, count) in bucketCounts)
        {
            var width = (int)((double)count / maxCount * barWidth);
            var bar = new string('█', width) + new string('░', barWidth - width);
            sb.AppendLine($"  {label,10} │{bar}│ {count:N0}");
        }
        sb.AppendLine("```");
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private static double[] Resample(double[] data, int targetLen)
    {
        if (data.Length == 0) return new double[targetLen];
        if (data.Length <= targetLen) return data;

        var result = new double[targetLen];
        var ratio = (double)data.Length / targetLen;

        for (var i = 0; i < targetLen; i++)
        {
            var start = (int)(i * ratio);
            var end = (int)((i + 1) * ratio);
            if (end > data.Length) end = data.Length;
            if (start >= end) { result[i] = data[start]; continue; }

            result[i] = 0;
            for (var j = start; j < end; j++)
                result[i] += data[j];
            result[i] /= (end - start);
        }
        return result;
    }

    private static string EscapeMd(string text) =>
        text.Replace("|", "\\|").Replace("\n", " ");
}
