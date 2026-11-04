using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Options;

namespace SenseNet.Services.Core.Authentication
{
    /// <summary>
    /// Reserves password checks atomically in a sliding window. Register as a singleton.
    /// State is local to the application and is lost on restart.
    /// </summary>
    public sealed class PasswordValidationLimiter
    {
        private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);
        private readonly Dictionary<int, Queue<long>> _attempts = new();
        private readonly object _lock = new();
        private readonly TimeProvider _clock;
        private readonly int _limit;
        private long _lastCleanup;

        public PasswordValidationLimiter(TimeProvider clock, IOptions<PasswordValidationOptions> options)
        {
            _clock = clock;
            _limit = options.Value.AttemptsPerMinutePerAccount;
            if (_limit <= 0)
                throw new ArgumentOutOfRangeException(nameof(options), "The password attempt limit must be positive.");
            _lastCleanup = clock.GetTimestamp();
        }

        /// <summary>
        /// Consumes one attempt for a resolved repository user, or throws without extending the window.
        /// Successful password checks also consume an attempt.
        /// </summary>
        public void CheckAttempt(int userId)
        {
            lock (_lock)
            {
                var now = _clock.GetTimestamp();
                // Only resolved users allocate state. Reclaim inactive accounts periodically.
                if (_clock.GetElapsedTime(_lastCleanup, now) >= Window)
                {
                    foreach (var id in _attempts.Where(pair =>
                                 _clock.GetElapsedTime(pair.Value.Last(), now) >= Window)
                                 .Select(pair => pair.Key).ToArray())
                        _attempts.Remove(id);
                    _lastCleanup = now;
                }

                if (!_attempts.TryGetValue(userId, out var attempts))
                    _attempts[userId] = attempts = new Queue<long>();

                while (attempts.Count > 0 && _clock.GetElapsedTime(attempts.Peek(), now) >= Window)
                    attempts.Dequeue();

                if (attempts.Count >= _limit)
                {
                    var remaining = Window - _clock.GetElapsedTime(attempts.Peek(), now);
                    throw new PasswordValidationThrottledException((int)Math.Ceiling(remaining.TotalSeconds));
                }

                attempts.Enqueue(now);
            }
        }
    }
}
