using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;

namespace SenseNet.Services.Core.Diagnostics;

internal sealed class ComponentHealthMonitor : IComponentHealthSource
{
    private readonly IComponentHealthCollector _collector;
    private readonly HealthMonitoringOptions _settings;
    private readonly ComponentHealthOptions _options;
    private readonly TimeProvider _clock;
    private readonly HealthChangeSignal _signal;
    private readonly HealthRepositoryLifecycle _lifecycle;
    private readonly ILogger _logger;
    private ComponentHealthSnapshot _current;
    private string _lastMeasurementFailure;
    private string _lastDetailsFailure;
    public string ComponentName => _collector.ComponentName;
    public ComponentHealthSnapshot Current => Volatile.Read(ref _current);

    internal ComponentHealthMonitor(IComponentHealthCollector collector, HealthMonitoringOptions settings,
        TimeProvider clock, HealthChangeSignal signal, HealthRepositoryLifecycle lifecycle, ILogger logger)
    {
        _collector = collector;
        _settings = settings;
        _options = settings.For(collector.ComponentName);
        _clock = clock;
        _signal = signal;
        _lifecycle = lifecycle;
        _logger = logger;
        _current = new(ComponentName, HealthCheckState.Unknown, HealthColor.Yellow, Reason: "Waiting for the first measurement.");
    }

    private void Publish(ComponentHealthSnapshot snapshot)
    {
        Volatile.Write(ref _current, snapshot);
        _signal.Notify();
    }

    internal async Task RunAsync(CancellationToken stopping)
    {
        if (!_options.Enabled)
        {
            Publish(Current with { State = HealthCheckState.Disabled,
                Color = _options.Required ? HealthColor.Red : HealthColor.Green, Reason = "Health collection is disabled." });
            return;
        }
        try
        {
            while (!_lifecycle.Ready)
                await Task.Delay(TimeSpan.FromMilliseconds(100), _clock, stopping).ConfigureAwait(false);
            await Task.Delay(_settings.InitialJitter * Random.Shared.NextDouble(), _clock, stopping).ConfigureAwait(false);
            while (!stopping.IsCancellationRequested)
            {
                if (_lifecycle.Ready)
                {
                    await MeasureAsync(stopping).ConfigureAwait(false);
                    if (_collector is IComponentHealthDetailsCollector && Current.State != HealthCheckState.NotApplicable &&
                        (!Current.DetailsCheckedAtUtc.HasValue || _clock.GetElapsedTime(Current.DetailsAt) >= _settings.DetailsInterval))
                        await MeasureDetailsAsync(stopping).ConfigureAwait(false);
                }
                var jitter = 1 + (Random.Shared.NextDouble() * 2 - 1) * _settings.JitterRatio;
                await Task.Delay(_options.Interval * jitter, _clock, stopping).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (stopping.IsCancellationRequested) { }
    }

    internal async Task MeasureAsync(CancellationToken stopping)
    {
        await ExecuteAsync(async cancel =>
        {
            var result = await _collector.CollectAsync(cancel).ConfigureAwait(false);
            if (result == null) throw new HealthCollectionException(HealthCheckState.Missing, "The health provider returned no result.");
            if (!Enum.IsDefined(result.Color)) throw new InvalidOperationException("Invalid health color.");
            // Copy while still in the collection operation, before exposing the result to another thread.
            return new HealthResult { Color = result.Color, ResponseTime = result.ResponseTime,
                Reason = HealthDataSanitizer.Text(result.Reason), Method = HealthDataSanitizer.Text(result.Method) };
        }, _options.Timeout, result =>
        {
            var now = _clock.GetUtcNow();
            var snapshot = Current with
            {
                State = HealthCheckState.Available, Color = result.Color,
                ResponseTime = result.ResponseTime, Reason = result.Reason, Method = result.Method,
                CheckedAtUtc = now, LastSuccessAtUtc = result.Color == HealthColor.Green ? now : Current.LastSuccessAtUtc,
                ValidUntilUtc = now + _options.MaxAge, MeasuredAt = _clock.GetTimestamp()
            };
            if (_collector is not IComponentHealthDetailsCollector)
                snapshot = snapshot with { DetailsJson = "{}", DetailsCheckedAtUtc = now,
                    DetailsValidUntilUtc = now + _settings.DetailsMaxAge, DetailsAt = _clock.GetTimestamp() };
            Publish(snapshot);
        }, (state, reason) =>
        {
            var now = _clock.GetUtcNow();
            Publish(Current with { State = state, Color = state == HealthCheckState.NotApplicable ? HealthColor.Green : HealthColor.Red,
                Reason = reason, Method = null, ResponseTime = null, CheckedAtUtc = now,
                ValidUntilUtc = now + _options.MaxAge, MeasuredAt = _clock.GetTimestamp() });
        }, stopping).ConfigureAwait(false);
    }

    private async Task MeasureDetailsAsync(CancellationToken stopping)
    {
        await ExecuteAsync(async cancel =>
        {
            var result = await ((IComponentHealthDetailsCollector)_collector).CollectDetailsAsync(cancel).ConfigureAwait(false);
            return (Json: HealthDataSanitizer.SerializeDetails(result.Data), result.Color, result.Reason);
        }, _settings.DetailsTimeout, result =>
        {
            var now = _clock.GetUtcNow();
            Publish(Current with { DetailsJson = result.Json, DetailsColor = result.Color,
                DetailsError = HealthDataSanitizer.Text(result.Reason), DetailsCheckedAtUtc = now,
                DetailsValidUntilUtc = now + _settings.DetailsMaxAge, DetailsAt = _clock.GetTimestamp() });
        }, (_, reason) => Publish(Current with { DetailsError = reason }), stopping, details: true).ConfigureAwait(false);
    }

    private async Task ExecuteAsync<T>(Func<CancellationToken, Task<T>> collect, TimeSpan timeout,
        Action<T> publish, Action<HealthCheckState, string> fail, CancellationToken stopping, bool details = false)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(stopping);
        var token = cancellation.Token;
        // A bounded, per-component worker also isolates providers that block before returning a Task.
        var operation = Task.Run(() => collect(token), CancellationToken.None);
        try
        {
            var result = await operation.WaitAsync(timeout, _clock, stopping).ConfigureAwait(false);
            stopping.ThrowIfCancellationRequested();
            publish(result);
            if (details) _lastDetailsFailure = null;
            else _lastMeasurementFailure = null;
        }
        catch (TimeoutException)
        {
            fail(HealthCheckState.TimedOut, "Health collection timed out.");
            LogFailure("TimedOut", details);
            await cancellation.CancelAsync().ConfigureAwait(false);
            // Do not release the component slot while the provider is still running.
            await DrainAsync(operation, stopping).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stopping.IsCancellationRequested)
        {
            Observe(operation);
            throw;
        }
        catch (HealthCollectionException exception)
        {
            fail(exception.State, exception.Message);
        }
        catch (Exception exception)
        {
            // Exception text may contain credentials supplied by a provider.
            fail(HealthCheckState.Error, "Health collection failed (" + exception.GetType().Name + ").");
            LogFailure(exception.GetType().Name, details);
        }
    }

    private void LogFailure(string failure, bool details)
    {
        if (failure == (details ? _lastDetailsFailure : _lastMeasurementFailure)) return;
        if (details) _lastDetailsFailure = failure;
        else _lastMeasurementFailure = failure;
        _logger.LogWarning("Health collection failed for {Component} (details: {Details}): {Failure}", ComponentName, details, failure);
    }

    private static async Task DrainAsync(Task operation, CancellationToken stopping)
    {
        try { await operation.WaitAsync(stopping).ConfigureAwait(false); }
        catch (OperationCanceledException) when (stopping.IsCancellationRequested) { Observe(operation); throw; }
        catch { /* Observe a late fault; its result must never replace the timeout. */ }
    }

    private static void Observe(Task operation) => _ = operation.ContinueWith(t => { _ = t.Exception; },
        CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
}

internal sealed class HealthCollectionException : Exception
{
    internal HealthCheckState State { get; }
    internal HealthCollectionException(HealthCheckState state, string message) : base(message) => State = state;
}
