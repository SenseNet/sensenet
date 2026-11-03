using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SenseNet.Services.Core.Tests;

/// <summary>Manual monotonic time including timers; health tests never wait for measurement intervals.</summary>
internal sealed class HealthTestClock : TimeProvider
{
    private readonly object _sync = new();
    private readonly List<ManualTimer> _timers = new();
    private long _ticks;
    public override long TimestampFrequency => TimeSpan.TicksPerSecond;
    public override long GetTimestamp() { lock (_sync) return _ticks; }
    public override DateTimeOffset GetUtcNow() => new DateTimeOffset(2026, 10, 29, 0, 0, 0, TimeSpan.Zero).AddTicks(GetTimestamp());
    internal int TimerCount { get { lock (_sync) return _timers.Count(t => t.Due != long.MaxValue); } }
    public override ITimer CreateTimer(TimerCallback callback, object state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = new ManualTimer(this, callback, state);
        lock (_sync) _timers.Add(timer);
        timer.Change(dueTime, period);
        return timer;
    }
    internal void Advance(TimeSpan delta)
    {
        List<ManualTimer> due;
        lock (_sync)
        {
            _ticks += delta.Ticks;
            due = _timers.Where(t => t.Due <= _ticks).ToList();
            foreach (var timer in due) timer.Due = timer.Period > 0 ? _ticks + timer.Period : long.MaxValue;
        }
        foreach (var timer in due) timer.Fire();
    }
    private sealed class ManualTimer(HealthTestClock clock, TimerCallback callback, object state) : ITimer
    {
        internal long Due = long.MaxValue;
        internal long Period;
        private bool _disposed;
        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            lock (clock._sync)
            {
                if (_disposed) return false;
                Due = dueTime == Timeout.InfiniteTimeSpan ? long.MaxValue : clock._ticks + dueTime.Ticks;
                Period = period.Ticks;
                return true;
            }
        }
        internal void Fire() { if (!_disposed) callback(state); }
        public void Dispose() { lock (clock._sync) { _disposed = true; clock._timers.Remove(this); } }
        public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
    }
}
