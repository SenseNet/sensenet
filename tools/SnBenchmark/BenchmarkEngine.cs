namespace SnBenchmark;

/// <summary>
/// The benchmark engine: manages worker tasks, ramp-up schedule,
/// and operation mix. Reports every result to the MetricsCollector.
/// </summary>
public sealed class BenchmarkEngine
{
    private readonly BenchmarkOptions _opts;
    private readonly SenseNetClient _client;
    private readonly MetricsCollector _metrics;
    private readonly CancellationTokenSource _cts;

    private int _currentConcurrency;
    private long _contentCounter;
    private readonly List<Task> _workers = new();
    private readonly SemaphoreSlim _workerGate;

    public int CurrentConcurrency => _currentConcurrency;
    public bool IsRunning { get; private set; }

    public BenchmarkEngine(BenchmarkOptions opts, SenseNetClient client,
        MetricsCollector metrics, CancellationTokenSource cts)
    {
        _opts = opts;
        _client = client;
        _metrics = metrics;
        _cts = cts;
        _currentConcurrency = opts.InitialConcurrency;
        _workerGate = new SemaphoreSlim(opts.InitialConcurrency, opts.MaxConcurrency);
    }

    // ── Run ─────────────────────────────────────────────────────────

    public async Task RunAsync()
    {
        IsRunning = true;
        var ct = _cts.Token;

        // Spawn initial workers
        for (var i = 0; i < _opts.InitialConcurrency; i++)
            _workers.Add(Task.Run(() => WorkerLoop(i, ct), ct));

        // Ramp-up scheduler
        var rampUpTask = Task.Run(() => RampUpLoop(ct), ct);

        // Duration limit (if configured)
        if (_opts.TestDurationSeconds > 0)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(_opts.TestDurationSeconds), ct);
                if (!ct.IsCancellationRequested)
                    await _cts.CancelAsync();
            }, ct);
        }

        try
        {
            await rampUpTask;
        }
        catch (OperationCanceledException) { }

        // Wait for all workers to finish
        try
        {
            await Task.WhenAll(_workers);
        }
        catch (OperationCanceledException) { }

        IsRunning = false;
    }

    // ── Ramp-up logic ───────────────────────────────────────────────

    private async Task RampUpLoop(CancellationToken ct)
    {
        var nextWorkerIndex = _opts.InitialConcurrency;

        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(_opts.RampUpStepSeconds), ct);

            if (_currentConcurrency >= _opts.MaxConcurrency) continue;

            var toAdd = Math.Min(
                _opts.RampUpConcurrencyStep,
                _opts.MaxConcurrency - _currentConcurrency);

            for (var i = 0; i < toAdd; i++)
            {
                var idx = nextWorkerIndex++;
                _workers.Add(Task.Run(() => WorkerLoop(idx, ct), ct));
                Interlocked.Increment(ref _currentConcurrency);

                // Release one permit on the semaphore for each new worker
                try { _workerGate.Release(); }
                catch (SemaphoreFullException) { /* at max */ }
            }
        }
    }

    // ── Worker loop ─────────────────────────────────────────────────

    private async Task WorkerLoop(int workerIndex, CancellationToken ct)
    {
        var totalWeight = _opts.CreateWeight + _opts.QueryWeight;

        while (!ct.IsCancellationRequested)
        {
            try
            {
                // Pick operation type based on weight
                var roll = Random.Shared.Next(totalWeight);
                RequestResult result;

                if (roll < _opts.CreateWeight)
                {
                    var seq = Interlocked.Increment(ref _contentCounter);
                    var name = $"bench-{workerIndex:D3}-{seq:D8}-{Guid.NewGuid():N}"[..40];
                    result = await _client.CreateContentAsync(name, workerIndex, ct);
                }
                else
                {
                    result = await _client.QueryContentAsync(workerIndex, ct);
                }

                _metrics.Record(result);

                // Tiny jitter to avoid thundering herd
                await Task.Delay(Random.Shared.Next(5, 50), ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // Swallow individual errors; they're recorded in metrics
            }
        }
    }
}
