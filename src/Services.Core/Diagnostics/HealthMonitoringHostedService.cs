using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SenseNet.Diagnostics;

namespace SenseNet.Services.Core.Diagnostics;

internal sealed class HealthMonitoringHostedService : IHostedService, IDisposable
{
    private readonly ComponentHealthMonitor[] _monitors;
    private readonly HealthSnapshotPublisher _publisher;
    private readonly HealthAccessEvaluator _access;
    private readonly HealthChangeSignal _signal;
    private readonly TimeProvider _clock;
    private readonly HealthMonitoringOptions _options;
    private readonly ILogger<HealthMonitoringHostedService> _logger;
    private readonly SenseNetStatus _status;
    private readonly HealthRepositoryLifecycle _lifecycle;
    private readonly CancellationTokenSource _stopping = new();
    private Task _workers;
    private int _disposed;

    public HealthMonitoringHostedService(IEnumerable<IComponentHealthCollector> collectors,
        HealthSnapshotPublisher publisher, HealthAccessEvaluator access, HealthChangeSignal signal,
        HealthRepositoryLifecycle lifecycle, ISenseNetStatus status, TimeProvider clock,
        IOptions<HealthMonitoringOptions> options, ILogger<HealthMonitoringHostedService> logger)
    {
        _publisher = publisher; _access = access; _signal = signal; _clock = clock;
        _options = options.Value; _logger = logger; _status = status as SenseNetStatus; _lifecycle = lifecycle;
        var sources = collectors.ToArray();
        _options.Validate(HealthComponents.ValidateNames(sources.Select(c => c.ComponentName)));
        _monitors = sources.Select(c => new ComponentHealthMonitor(c, _options, clock, signal, lifecycle, logger)).ToArray();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_status != null) _status.Changed += OnStatusChanged;
        _publisher.Publish(_monitors.Select(m => m.Current));
        var token = _stopping.Token;
        // Start once per host, never on demand from a request.
        _workers = Task.WhenAll(_monitors.Select(m => Task.Run(() => m.RunAsync(token), CancellationToken.None))
            .Append(Task.Run(() => PublishAsync(token), CancellationToken.None))
            .Append(Task.Run(() => RefreshAccessAsync(token), CancellationToken.None)));
        return Task.CompletedTask;
    }

    private void OnStatusChanged(object sender, EventArgs args) => _signal.Notify();

    private async Task PublishAsync(CancellationToken stopping)
    {
        var failed = false;
        while (!stopping.IsCancellationRequested)
        {
            try { _publisher.Publish(_monitors.Select(m => m.Current)); failed = false; }
            catch (Exception)
            {
                // Keep the previous publication; its built-in expiry protects readers.
                if (!failed) _logger.LogWarning("Health snapshot publication failed.");
                failed = true;
            }
            using var timer = new CancellationTokenSource(_options.PublishInterval, _clock);
            using var wait = CancellationTokenSource.CreateLinkedTokenSource(timer.Token, stopping);
            try { await _signal.Channel.Reader.ReadAsync(wait.Token).ConfigureAwait(false); }
            catch (OperationCanceledException) { }
            while (_signal.Channel.Reader.TryRead(out _)) { }
        }
    }

    private async Task RefreshAccessAsync(CancellationToken stopping)
    {
        try
        {
            while (!stopping.IsCancellationRequested)
            {
                try { _access.RefreshDeveloperGroup(); }
                catch { /* No resolved group means public visibility only for developers. */ }
                await Task.Delay(_options.PublishInterval, _clock, stopping).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (stopping.IsCancellationRequested) { }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _lifecycle.Stop();
        _publisher.Publish(_monitors.Select(m => m.Current));
        if (_status != null) _status.Changed -= OnStatusChanged;
        // CancelAsync does not execute arbitrary provider callbacks on the host's stopping thread.
        var cancel = _stopping.CancelAsync();
        if (_workers == null) return;
        try { await Task.WhenAll(_workers, cancel).WaitAsync(_options.ShutdownTimeout, _clock, cancellationToken).ConfigureAwait(false); }
        catch (OperationCanceledException) { }
        catch (TimeoutException) { _logger.LogWarning("Health monitoring shutdown timed out."); }
    }
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        if (_status != null) _status.Changed -= OnStatusChanged;
        _ = _stopping.CancelAsync();
        // Workers may still be draining a non-cooperative provider. Dispose only after they finish.
        if (_workers == null || _workers.IsCompleted) _stopping.Dispose();
        else _ = _workers.ContinueWith(_ => _stopping.Dispose(), TaskScheduler.Default);
    }
}
