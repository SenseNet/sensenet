using System.Collections.Concurrent;

namespace SnBenchmark;

/// <summary>
/// Thread-safe metrics collector. Every worker pushes results here;
/// the UI reads aggregated snapshots.
/// </summary>
public sealed class MetricsCollector
{
    private readonly ConcurrentBag<RequestResult> _results = new();
    private long _totalRequests;
    private long _totalSuccessful;
    private long _totalFailed;
    private long _totalCreates;
    private long _totalQueries;
    private readonly DateTime _startTime = DateTime.UtcNow;

    // ── Rolling history for live charts (1 sample per second) ───────
    private readonly ConcurrentQueue<LiveSample> _liveSamples = new();
    private const int MaxLiveSamples = 120; // 2 minutes of history

    /// <summary>
    /// Called once per second from the dashboard loop to record a live sample.
    /// </summary>
    public void RecordLiveSample(int concurrency)
    {
        var now = DateTime.UtcNow;
        var window = now.AddSeconds(-1);
        var results = _results.ToArray();
        var recent = results.Where(r => r.Timestamp > window).ToArray();

        _liveSamples.Enqueue(new LiveSample
        {
            Timestamp = now,
            RequestsPerSec = recent.Length,
            AvgLatencyMs = recent.Length > 0 ? recent.Average(r => r.ElapsedMs) : 0,
            ErrorCount = recent.Count(r => !r.Success),
            Concurrency = concurrency,
            CreatesPerSec = recent.Count(r => r.Operation == OperationType.Create),
            QueriesPerSec = recent.Count(r => r.Operation == OperationType.Query),
        });

        while (_liveSamples.Count > MaxLiveSamples)
            _liveSamples.TryDequeue(out _);
    }

    public LiveSample[] GetLiveSamples() => _liveSamples.ToArray();

    // ── Record ──────────────────────────────────────────────────────

    public void Record(RequestResult result)
    {
        _results.Add(result);
        Interlocked.Increment(ref _totalRequests);

        if (result.Success)
            Interlocked.Increment(ref _totalSuccessful);
        else
            Interlocked.Increment(ref _totalFailed);

        if (result.Operation == OperationType.Create)
            Interlocked.Increment(ref _totalCreates);
        else
            Interlocked.Increment(ref _totalQueries);
    }

    // ── Snapshots ───────────────────────────────────────────────────

    public MetricsSnapshot GetSnapshot(int currentConcurrency)
    {
        var elapsed = DateTime.UtcNow - _startTime;
        var results = _results.ToArray();

        var createResults = results.Where(r => r.Operation == OperationType.Create).ToArray();
        var queryResults = results.Where(r => r.Operation == OperationType.Query).ToArray();

        var recentWindow = DateTime.UtcNow.AddSeconds(-10);
        var recentResults = results.Where(r => r.Timestamp > recentWindow).ToArray();
        var recentCreates = recentResults.Where(r => r.Operation == OperationType.Create).ToArray();
        var recentQueries = recentResults.Where(r => r.Operation == OperationType.Query).ToArray();

        return new MetricsSnapshot
        {
            Elapsed = elapsed,
            TotalRequests = Interlocked.Read(ref _totalRequests),
            TotalSuccessful = Interlocked.Read(ref _totalSuccessful),
            TotalFailed = Interlocked.Read(ref _totalFailed),
            TotalCreates = Interlocked.Read(ref _totalCreates),
            TotalQueries = Interlocked.Read(ref _totalQueries),
            CurrentConcurrency = currentConcurrency,

            AvgCreateMs = createResults.Length > 0
                ? createResults.Average(r => r.ElapsedMs) : 0,
            AvgQueryMs = queryResults.Length > 0
                ? queryResults.Average(r => r.ElapsedMs) : 0,
            P95CreateMs = Percentile(createResults, 0.95),
            P95QueryMs = Percentile(queryResults, 0.95),
            P99CreateMs = Percentile(createResults, 0.99),
            P99QueryMs = Percentile(queryResults, 0.99),
            MaxCreateMs = createResults.Length > 0
                ? createResults.Max(r => r.ElapsedMs) : 0,
            MaxQueryMs = queryResults.Length > 0
                ? queryResults.Max(r => r.ElapsedMs) : 0,

            CreatesPerMinute = elapsed.TotalMinutes > 0
                ? Interlocked.Read(ref _totalCreates) / elapsed.TotalMinutes : 0,
            QueriesPerMinute = elapsed.TotalMinutes > 0
                ? Interlocked.Read(ref _totalQueries) / elapsed.TotalMinutes : 0,

            RecentRequestsPerSec = recentResults.Length / 10.0,
            RecentCreatesPerSec = recentCreates.Length / 10.0,
            RecentQueriesPerSec = recentQueries.Length / 10.0,
            RecentAvgCreateMs = recentCreates.Length > 0
                ? recentCreates.Average(r => r.ElapsedMs) : 0,
            RecentAvgQueryMs = recentQueries.Length > 0
                ? recentQueries.Average(r => r.ElapsedMs) : 0,
            RecentErrorRate = recentResults.Length > 0
                ? (double)recentResults.Count(r => !r.Success) / recentResults.Length * 100 : 0,

            ErrorMessages = results
                .Where(r => !r.Success && r.ErrorMessage != null)
                .GroupBy(r => TruncateError(r.ErrorMessage!))
                .OrderByDescending(g => g.Count())
                .Take(5)
                .ToDictionary(g => g.Key, g => g.Count()),
        };
    }

    public IReadOnlyList<RequestResult> GetAllResults() => _results.ToArray();

    // ── Time-series for report ──────────────────────────────────────

    public IReadOnlyList<TimeSlice> GetTimeSlices(int intervalSeconds = 5)
    {
        var results = _results.ToArray();
        if (results.Length == 0) return [];

        var minTime = results.Min(r => r.Timestamp);
        var maxTime = results.Max(r => r.Timestamp);
        var slices = new List<TimeSlice>();

        for (var t = minTime; t < maxTime; t = t.AddSeconds(intervalSeconds))
        {
            var windowEnd = t.AddSeconds(intervalSeconds);
            var window = results.Where(r => r.Timestamp >= t && r.Timestamp < windowEnd).ToArray();

            slices.Add(new TimeSlice
            {
                OffsetSeconds = (t - minTime).TotalSeconds,
                RequestCount = window.Length,
                SuccessCount = window.Count(r => r.Success),
                FailCount = window.Count(r => !r.Success),
                AvgLatencyMs = window.Length > 0 ? window.Average(r => r.ElapsedMs) : 0,
                CreateCount = window.Count(r => r.Operation == OperationType.Create),
                QueryCount = window.Count(r => r.Operation == OperationType.Query),
            });
        }

        return slices;
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private static double Percentile(RequestResult[] sorted, double p)
    {
        if (sorted.Length == 0) return 0;
        var ordered = sorted.OrderBy(r => r.ElapsedMs).ToArray();
        var index = (int)Math.Ceiling(p * ordered.Length) - 1;
        return ordered[Math.Max(0, index)].ElapsedMs;
    }

    private static string TruncateError(string msg) =>
        msg.Length > 80 ? msg[..80] + "…" : msg;
}

// ── Snapshot DTO ────────────────────────────────────────────────────

public sealed class MetricsSnapshot
{
    public TimeSpan Elapsed { get; init; }
    public long TotalRequests { get; init; }
    public long TotalSuccessful { get; init; }
    public long TotalFailed { get; init; }
    public long TotalCreates { get; init; }
    public long TotalQueries { get; init; }
    public int CurrentConcurrency { get; init; }

    public double AvgCreateMs { get; init; }
    public double AvgQueryMs { get; init; }
    public double P95CreateMs { get; init; }
    public double P95QueryMs { get; init; }
    public double P99CreateMs { get; init; }
    public double P99QueryMs { get; init; }
    public double MaxCreateMs { get; init; }
    public double MaxQueryMs { get; init; }

    public double CreatesPerMinute { get; init; }
    public double QueriesPerMinute { get; init; }

    public double RecentRequestsPerSec { get; init; }
    public double RecentCreatesPerSec { get; init; }
    public double RecentQueriesPerSec { get; init; }
    public double RecentAvgCreateMs { get; init; }
    public double RecentAvgQueryMs { get; init; }
    public double RecentErrorRate { get; init; }

    public Dictionary<string, int> ErrorMessages { get; init; } = new();
}

public sealed class TimeSlice
{
    public double OffsetSeconds { get; init; }
    public int RequestCount { get; init; }
    public int SuccessCount { get; init; }
    public int FailCount { get; init; }
    public double AvgLatencyMs { get; init; }
    public int CreateCount { get; init; }
    public int QueryCount { get; init; }
}

public sealed class LiveSample
{
    public DateTime Timestamp { get; init; }
    public int RequestsPerSec { get; init; }
    public double AvgLatencyMs { get; init; }
    public int ErrorCount { get; init; }
    public int Concurrency { get; init; }
    public int CreatesPerSec { get; init; }
    public int QueriesPerSec { get; init; }
}
