namespace SnBenchmark;

/// <summary>
/// Benchmark configuration bound from appsettings.json "Benchmark" section.
/// All parameters can be overridden via command-line arguments (--Benchmark:Key=Value).
/// </summary>
public sealed class BenchmarkOptions
{
    // ── Connection ──────────────────────────────────────────────────
    public string RepositoryUrl { get; set; } = "https://localhost:44362";
    public string ApiKey { get; set; } = string.Empty;
    public bool SkipTlsValidation { get; set; } = true;

    // ── Content ─────────────────────────────────────────────────────
    public string BasePath { get; set; } = "/Root/Content/Benchmark";
    public string ContentTypeName { get; set; } = "File";

    // ── Concurrency & ramp-up ───────────────────────────────────────
    /// <summary>Starting number of concurrent workers.</summary>
    public int InitialConcurrency { get; set; } = 2;

    /// <summary>Maximum concurrent workers – the ceiling.</summary>
    public int MaxConcurrency { get; set; } = 64;

    /// <summary>Seconds between each ramp-up step.</summary>
    public int RampUpStepSeconds { get; set; } = 10;

    /// <summary>How many workers to add per ramp-up step.</summary>
    public int RampUpConcurrencyStep { get; set; } = 2;

    // ── Duration & timeout ──────────────────────────────────────────
    /// <summary>Total benchmark duration in seconds (0 = unlimited, stop with Ctrl+C).</summary>
    public int TestDurationSeconds { get; set; } = 120;

    /// <summary>Per-request HTTP timeout in seconds.</summary>
    public int RequestTimeoutSeconds { get; set; } = 30;

    // ── Workload mix ────────────────────────────────────────────────
    /// <summary>Relative weight of CREATE operations (vs Query).</summary>
    public int CreateWeight { get; set; } = 70;

    /// <summary>Relative weight of QUERY operations (vs Create).</summary>
    public int QueryWeight { get; set; } = 30;

    // ── Reporting ───────────────────────────────────────────────────
    public string ReportDirectory { get; set; } = "./reports";
}
