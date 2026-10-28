# SnBenchmark — sensenet Repository Stress Test Tool

A multi-threaded benchmark console application that stress-tests a running
[sensenet](https://github.com/SenseNet/sensenet) repository through its OData
REST API. It creates content, runs queries, gradually ramps up concurrency, and
produces a live dashboard with sparkline charts plus a detailed Markdown report
at the end.

![.NET 10](https://img.shields.io/badge/.NET-10.0-blue)
![Spectre.Console](https://img.shields.io/badge/Spectre.Console-0.49-purple)

---

## Features

| Feature | Description |
|---------|-------------|
| **Gradual ramp-up** | Starts with a small number of workers and adds more at configurable intervals until a ceiling is reached. |
| **Configurable workload mix** | Weighted ratio of `Create` vs `Query` operations (default 70 / 30). |
| **Live console dashboard** | Real-time metrics table, load bar, and Unicode sparkline charts (throughput, latency, errors, concurrency) powered by [Spectre.Console](https://spectreconsole.net). |
| **Percentile latencies** | Tracks Avg, P95, P99, and Max for both create and query operations. |
| **Rolling "recent" window** | Last-10-second stats so you can see how performance changes as load increases. |
| **Markdown report** | Auto-generated report with ASCII charts, latency histograms, time-series tables, and error summaries. |
| **Graceful shutdown** | Press <kbd>Ctrl+C</kbd> at any time; the tool stops workers, prints a final summary, and writes the report. |
| **Flexible configuration** | `appsettings.json`, environment variables (`SNBENCH_`), or command-line args — all three are merged. |

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (or later)
- A running sensenet repository with the OData API enabled
- A valid API key with content creation permissions

---

## Quick Start

```bash
cd tools/SnBenchmark

# 1. Edit settings (at minimum set the API key)
#    Or pass it on the command line (see below)
nano appsettings.json

# 2. Run
dotnet run
```

Alternatively, pass settings via the command line:

```bash
dotnet run -- \
  --Benchmark:RepositoryUrl=https://localhost:44362 \
  --Benchmark:ApiKey=YOUR_API_KEY_HERE \
  --Benchmark:MaxConcurrency=128 \
  --Benchmark:TestDurationSeconds=300
```

Or via environment variables (prefix `SNBENCH_`, double underscore for nesting):

```bash
export SNBENCH_Benchmark__ApiKey=YOUR_API_KEY_HERE
export SNBENCH_Benchmark__MaxConcurrency=128
dotnet run
```

---

## Configuration Reference

All settings live under the `"Benchmark"` section in `appsettings.json`.

### Connection

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `RepositoryUrl` | string | `https://localhost:44362` | Base URL of the sensenet repository. |
| `ApiKey` | string | *(required)* | API key sent as the `apikey` header on every request. |
| `SkipTlsValidation` | bool | `true` | Ignore TLS certificate errors (useful for local dev with self-signed certs). |

### Content

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `BasePath` | string | `/Root/Content/Benchmark` | Repository path where benchmark content is created. The folder is auto-created if it doesn't exist. |
| `ContentTypeName` | string | `File` | sensenet content type to create. |

### Concurrency & Ramp-Up

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `InitialConcurrency` | int | `2` | Number of concurrent workers at start. |
| `MaxConcurrency` | int | `64` | Upper ceiling for concurrent workers. |
| `RampUpStepSeconds` | int | `10` | Seconds between each ramp-up step. |
| `RampUpConcurrencyStep` | int | `2` | Number of workers added per ramp-up step. |

> **Example:** With defaults, concurrency goes 2 → 4 → 6 → … → 64, stepping
> every 10 seconds. It takes `(64-2)/2 × 10 = 310s` (~5 min) to reach the
> maximum.

### Duration & Timeout

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `TestDurationSeconds` | int | `120` | Total test duration in seconds. Set to `0` for unlimited (stop manually with Ctrl+C). |
| `RequestTimeoutSeconds` | int | `30` | HTTP timeout per individual request. Requests exceeding this are cancelled and counted as errors. |

### Workload Mix

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `CreateWeight` | int | `70` | Relative weight of content creation operations. |
| `QueryWeight` | int | `30` | Relative weight of OData query operations. |

> The weights don't need to sum to 100 — they are treated as proportions.
> `70 / 30` is the same as `7 / 3`.

### Reporting

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `ReportDirectory` | string | `./reports` | Directory where Markdown reports are saved. Created automatically if missing. |

---

## How It Works

```
┌─────────────┐
│   Program    │  Loads config, validates, runs pre-flight checks
└──────┬──────┘
       │
       ▼
┌─────────────┐     ┌──────────────────┐
│  Benchmark  │────▶│  SenseNetClient  │  HTTP calls to OData API
│   Engine    │     └──────────────────┘
│             │         POST /OData.svc/('BasePath')
│  Workers ×N │         GET  /OData.svc/('BasePath')?$filter=...
└──────┬──────┘
       │ records timing
       ▼
┌─────────────┐     ┌──────────────────┐
│   Metrics   │────▶│    ConsoleUi     │  Live Spectre.Console dashboard
│  Collector  │     └──────────────────┘
│             │
│  (thread-   │     ┌──────────────────┐
│   safe)     │────▶│ ReportGenerator  │  Markdown report with charts
└─────────────┘     └──────────────────┘
```

### Execution Flow

1. **Configuration** — Merges `appsettings.json` + env vars + CLI args.
2. **Validation** — Checks that `ApiKey` and `RepositoryUrl` are set.
3. **Pre-flight** — Pings the repository; ensures the benchmark folder exists.
4. **Engine start** — Spawns `InitialConcurrency` workers. Each worker loops:
   pick a random operation (create or query based on weights), execute it, record
   timing in `MetricsCollector`, repeat.
5. **Ramp-up** — A background timer adds `RampUpConcurrencyStep` workers every
   `RampUpStepSeconds` until `MaxConcurrency` is reached.
6. **Dashboard** — Updates every second with real-time metrics and sparkline
   charts.
7. **Shutdown** — On duration expiry or Ctrl+C: cancels workers, prints a final
   summary table, and writes the Markdown report to `ReportDirectory`.

### Metrics Tracked

- **Throughput:** total creates/min, queries/min, recent req/s
- **Latency:** average, P95, P99, max — separately for creates and queries
- **Rolling window:** last 10 seconds of creates/sec, queries/sec, avg latency,
  error rate
- **Live samples:** up to 120 seconds of per-second snapshots for chart
  rendering
- **Errors:** count + grouped error messages

---

## Live Dashboard

The dashboard uses [Spectre.Console](https://spectreconsole.net) `Live` rendering
and updates every second:

```
╔════════════════════════ sensenet Benchmark ═════════════════════════╗
║ Metric              │ Value       │ Metric              │ Value    ║
║─────────────────────┼─────────────┼──────────────────────┼─────────║
║ ⏱  Elapsed          │ 00:01:23    │ 👥 Concurrency      │ 18 / 64 ║
║ 📝 Creates / min    │ 2,284.7     │ 📊 Total Requests   │ 4,123   ║
║ ...                 │             │ ...                  │         ║
╚════════════════════════════════════════════════════════════════════╝

╭─📈 Throughput (req/s)────────────╮ ╭─🕐 Latency (ms)─────────────────╮
│ ▁▂▃▃▄▅▅▆▆▇▇▇████████████████    │ │ █▇▆▅▅▄▄▃▃▃▃▃▃▃▃▃▃▃▃▃▃▃▃▃▃▃    │
│ now: 42  peak: 48                │ │ now: 189ms  peak: 488ms         │
╰──────────────────────────────────╯ ╰─────────────────────────────────╯
╭─❌ Errors/s─────────────────────╮ ╭─👥 Concurrency─────────────────╮
│ ▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁    │ │ ▁▁▂▂▃▃▄▄▅▅▆▆▇▇████████████    │
│ now: 0  peak: 1                  │ │ now: 18  max: 64               │
╰──────────────────────────────────╯ ╰─────────────────────────────────╯

  🔥 Load: █████████████░░░░░░░░░░░░░░░░░░░ 28%  18/64 workers
```

---

## Report

After each run a Markdown report is written to the `ReportDirectory`:

```
reports/
  benchmark-report-20260228-155235.md
```

The report contains:

- Configuration summary
- Final metrics table
- ASCII throughput and latency time-series charts
- Latency distribution histogram
- Per-second time-series data table
- Error summary

---

## Project Structure

```
tools/SnBenchmark/
├── Program.cs            Entry point, config loading, pre-flight, main loop
├── BenchmarkOptions.cs   Strongly-typed configuration model
├── BenchmarkEngine.cs    Worker management, ramp-up scheduler, operation mix
├── SenseNetClient.cs     OData HTTP client (create, query, health check)
├── MetricsCollector.cs   Thread-safe metrics aggregation & live samples
├── ConsoleUi.cs          Spectre.Console live dashboard with sparkline charts
├── ReportGenerator.cs    Markdown report writer with ASCII charts
├── appsettings.json      Default configuration
├── SnBenchmark.csproj    Project file (.NET 10, Spectre.Console)
└── reports/              Generated reports (git-ignored)
```

---

## Example Scenarios

### Light smoke test (30 seconds, low concurrency)

```bash
dotnet run -- \
  --Benchmark:MaxConcurrency=4 \
  --Benchmark:TestDurationSeconds=30
```

### Aggressive stress test (5 min, 128 workers, fast ramp)

```bash
dotnet run -- \
  --Benchmark:MaxConcurrency=128 \
  --Benchmark:RampUpStepSeconds=5 \
  --Benchmark:RampUpConcurrencyStep=4 \
  --Benchmark:TestDurationSeconds=300
```

### Query-heavy workload

```bash
dotnet run -- \
  --Benchmark:CreateWeight=20 \
  --Benchmark:QueryWeight=80
```

### Unlimited duration (manual stop)

```bash
dotnet run -- --Benchmark:TestDurationSeconds=0
# Press Ctrl+C when done
```

---

## Interpreting Results

| Metric | What to look for |
|--------|------------------|
| **Creates/min** | Higher is better. Watch for plateau or drop as concurrency increases — indicates a bottleneck. |
| **Avg Latency** | Should stay stable as load increases. A steep rise signals saturation. |
| **P95 / P99** | Tail latency. If P99 >> Avg, there's occasional contention (DB locks, GC pauses, etc.). |
| **Error rate** | Should be 0% during the run. `The operation was canceled` errors at shutdown are normal and harmless. |
| **Throughput chart** | Ideally rises linearly with concurrency, then plateaus. A _drop_ means the system is overloaded. |
| **Latency chart** | Inverse of throughput: should stay flat, then rise. A hockey-stick shape = saturation point found. |

### Common Bottlenecks

- **Connection pool exhaustion** — P99 spikes, timeouts appear. Increase pool
  size in the repository's DB config.
- **Thread pool starvation** — Latency climbs across the board. Look for
  sync-over-async calls in the repository.
- **Database lock contention** — Creates slow down while queries stay fast (or
  vice versa). Check DB lock waits.
- **GC pressure** — Periodic latency spikes visible in the chart. Monitor server
  GC metrics.

---

## License

Part of the [sensenet](https://github.com/SenseNet/sensenet) project. See the
repository root [LICENSE](../../LICENSE) for details.
