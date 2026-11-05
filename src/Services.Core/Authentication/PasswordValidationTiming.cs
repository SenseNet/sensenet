using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace SenseNet.Services.Core.Authentication
{
    /// <summary>Pads a credential attempt to a randomly chosen total duration, including user lookup.</summary>
    public sealed class PasswordValidationTiming
    {
        private readonly TimeProvider _clock;

        public PasswordValidationTiming(TimeProvider clock) => _clock = clock;

        /// <summary>
        /// Start before resolving the user. Dispose asynchronously after releasing authentication locks,
        /// and before returning a result or propagating an error to the response writer.
        /// </summary>
        public IAsyncDisposable Begin(CancellationToken cancellationToken)
        {
            var started = _clock.GetTimestamp();
            var target = TimeSpan.FromMilliseconds(RandomNumberGenerator.GetInt32(900, 1101));
            return new Attempt(_clock, started, target, cancellationToken);
        }

        private sealed class Attempt(TimeProvider clock, long started, TimeSpan target,
            CancellationToken cancellationToken) : IAsyncDisposable
        {
            public async ValueTask DisposeAsync()
            {
                var remaining = target - clock.GetElapsedTime(started);
                if (remaining > TimeSpan.Zero)
                    await Task.Delay(remaining, clock, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
