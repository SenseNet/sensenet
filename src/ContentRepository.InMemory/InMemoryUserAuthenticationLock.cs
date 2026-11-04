#nullable enable
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.ContentRepository.Storage.Data;

/// <summary>Per-user locks for the process-local in-memory repository.</summary>
public sealed class InMemoryUserAuthenticationLock : IUserAuthenticationLock
{
    private readonly ConcurrentDictionary<int, SemaphoreSlim> _gates = new();

    public async Task<IAsyncDisposable?> TryAcquireAsync(int userId, CancellationToken cancellationToken)
    {
        if (userId <= 0) throw new ArgumentOutOfRangeException(nameof(userId));
        var gate = _gates.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1));
        return await gate.WaitAsync(0, cancellationToken).ConfigureAwait(false) ? new Lease(gate) : null;
    }

    private sealed class Lease(SemaphoreSlim gate) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            Interlocked.Exchange(ref gate, null)?.Release();
            return ValueTask.CompletedTask;
        }
    }
}
