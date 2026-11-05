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
        private readonly Dictionary<int, LinkedList<long>> _attempts = new();
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
        /// Reserves one attempt for a resolved repository user, or throws without extending the window.
        /// Complete successful validation to release only this reservation; failures expire with the window.
        /// </summary>
        public Attempt CheckAttempt(int userId)
        {
            lock (_lock)
            {
                var now = _clock.GetTimestamp();
                // Only resolved users allocate state. Reclaim inactive accounts periodically.
                if (_clock.GetElapsedTime(_lastCleanup, now) >= Window)
                {
                    foreach (var id in _attempts.Where(pair =>
                                 _clock.GetElapsedTime(pair.Value.Last.Value, now) >= Window)
                                 .Select(pair => pair.Key).ToArray())
                        _attempts.Remove(id);
                    _lastCleanup = now;
                }

                if (!_attempts.TryGetValue(userId, out var attempts))
                    _attempts[userId] = attempts = new LinkedList<long>();

                while (attempts.Count > 0 && _clock.GetElapsedTime(attempts.First.Value, now) >= Window)
                    attempts.RemoveFirst();

                if (attempts.Count >= _limit)
                {
                    var remaining = Window - _clock.GetElapsedTime(attempts.First.Value, now);
                    throw new PasswordValidationThrottledException((int)Math.Ceiling(remaining.TotalSeconds));
                }

                return new Attempt(this, userId, attempts.AddLast(now));
            }
        }

        private void CompleteSuccessfully(int userId, LinkedListNode<long> reservation)
        {
            lock (_lock)
            {
                // An expired or already completed reservation must not remove a newer attempt.
                if (!_attempts.TryGetValue(userId, out var attempts) || reservation.List != attempts)
                    return;
                attempts.Remove(reservation);
                if (attempts.Count == 0)
                    _attempts.Remove(userId);
            }
        }

        public sealed class Attempt
        {
            private readonly PasswordValidationLimiter _limiter;
            private readonly int _userId;
            private readonly LinkedListNode<long> _reservation;

            internal Attempt(PasswordValidationLimiter limiter, int userId, LinkedListNode<long> reservation)
            {
                _limiter = limiter;
                _userId = userId;
                _reservation = reservation;
            }

            /// <summary>Releases this attempt after all required credential checks succeed. Safe to call repeatedly.</summary>
            public void CompleteSuccessfully() => _limiter.CompleteSuccessfully(_userId, _reservation);
        }
    }
}
