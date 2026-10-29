using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using Microsoft.Extensions.Options;
using SenseNet.Diagnostics;

namespace SenseNet.Services.Core.Diagnostics;

internal sealed class HealthChangeSignal
{
    internal Channel<bool> Channel { get; } = System.Threading.Channels.Channel.CreateBounded<bool>(1);
    internal void Notify() => Channel.Writer.TryWrite(true);
}

internal sealed class HealthRepositoryLifecycle
{
    // Repository.IsRunning can become true before the startup callback has completed.
    private int _state;
    internal bool Ready => Volatile.Read(ref _state) == 1;
    internal bool FailedOrStopping => Volatile.Read(ref _state) == 2;
    internal void SetReady() => Interlocked.CompareExchange(ref _state, 1, 0);
    internal void Stop() => Interlocked.Exchange(ref _state, 2);
}

internal sealed class HealthSnapshotPublisher : IHealthSnapshotStore
{
    private readonly TimeProvider _clock;
    private readonly HealthMonitoringOptions _options;
    private readonly ISenseNetStatus _status;
    private readonly HealthRepositoryLifecycle _lifecycle;
    private readonly long _startedAt;
    private HealthResponseSnapshot _current;
    private long _sequence;
    public HealthResponseSnapshot Current => Volatile.Read(ref _current);

    public HealthSnapshotPublisher(TimeProvider clock, IOptions<HealthMonitoringOptions> options,
        ISenseNetStatus status, HealthRepositoryLifecycle lifecycle)
    {
        _clock = clock;
        _options = options.Value;
        _options.Validate();
        _status = status;
        _lifecycle = lifecycle;
        _startedAt = clock.GetTimestamp();
        Publish(HealthComponents.Names.Select(name => new ComponentHealthSnapshot(name, HealthCheckState.Unknown,
            HealthColor.Yellow, Reason: "Waiting for the first measurement.")));
    }

    internal void Publish(IEnumerable<ComponentHealthSnapshot> input)
    {
        var sequence = Interlocked.Increment(ref _sequence);
        var lifetime = _options.PublisherMaxAge;
        void Limit(TimeSpan remaining)
        {
            if (remaining > TimeSpan.Zero && remaining < lifetime) lifetime = remaining;
        }
        var startupRemaining = _options.StartupGracePeriod - _clock.GetElapsedTime(_startedAt);
        var components = input.Select(original =>
        {
            var component = original;
            var options = _options.For(component.ComponentName);
            if (component.State == HealthCheckState.Unknown)
            {
                Limit(startupRemaining);
                if (startupRemaining <= TimeSpan.Zero)
                    component = component with { Color = HealthColor.Red, State = HealthCheckState.Stale,
                        Reason = "No measurement completed within the startup grace period." };
            }
            else if (component.CheckedAtUtc.HasValue && component.State != HealthCheckState.NotApplicable)
            {
                var remaining = options.MaxAge - _clock.GetElapsedTime(component.MeasuredAt);
                Limit(remaining);
                if (remaining <= TimeSpan.Zero)
                    component = component with { Color = HealthColor.Red, State = HealthCheckState.Stale,
                        Reason = "The last measurement has expired." };
            }
            if (component.State is not (HealthCheckState.Unknown or HealthCheckState.NotApplicable or HealthCheckState.Disabled))
            {
                var remaining = component.DetailsCheckedAtUtc.HasValue
                    ? _options.DetailsMaxAge - _clock.GetElapsedTime(component.DetailsAt) : TimeSpan.Zero;
                Limit(remaining);
                if (remaining <= TimeSpan.Zero || component.DetailsError != null)
                    component = component with
                    {
                        Color = (HealthColor)Math.Max((int)component.Color, (int)HealthColor.Yellow),
                        DetailsError = component.DetailsError ?? "Diagnostic details are not current."
                    };
                component = component with { Color = (HealthColor)Math.Max((int)component.Color, (int)component.DetailsColor) };
            }
            return component;
        }).ToArray();

        // The built-in status source copies all fields under one lock. Custom sources retain their API.
        var status = _status is SenseNetStatus standard ? standard.GetSnapshot() :
            new SenseNetStatusSnapshot(_status?.IsRunning ?? false, _status?.Current ?? "Unavailable", _status?.GetLog() ?? Array.Empty<string>());
        var repositoryColor = _lifecycle.FailedOrStopping || status.Status == SenseNetStatus.Stopped || status.Status == SenseNetStatus.Stopping
            ? HealthColor.Red : _lifecycle.Ready && status.Running ? HealthColor.Green :
                startupRemaining <= TimeSpan.Zero ? HealthColor.Red : HealthColor.Yellow;
        if (repositoryColor == HealthColor.Yellow) Limit(startupRemaining);
        var overall = (HealthColor)Math.Max((int)repositoryColor, components.Max(c => (int)c.Color));
        var repository = new RepositoryHealthSnapshot(overall, components, _lifecycle.Ready && status.Running, status.Status, status.History);
        var response = new HealthResponseSnapshot(repository, _clock, lifetime) { PublicationSequence = sequence };
        // Shutdown may publish concurrently with a finishing periodic publication.
        // A slower, older publication must not replace a newer shutdown snapshot.
        while (true)
        {
            var previous = Volatile.Read(ref _current);
            if (previous != null && previous.PublicationSequence > sequence) return;
            if (Interlocked.CompareExchange(ref _current, response, previous) == previous) return;
        }
    }
}
