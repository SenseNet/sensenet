using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using SenseNet.Configuration;

namespace SenseNet.ContentRepository.Storage.Security;

/// <summary>Runs an authentication operation under the repository's per-user lock.</summary>
public static class UserAuthenticationScope
{
    private sealed class Scope(int userId, IUserAuthenticationLock provider)
    {
        public readonly int UserId = userId;
        public readonly IUserAuthenticationLock Provider = provider;
        public bool Active = true;
    }

    private static readonly AsyncLocal<Scope> Current = new AsyncLocal<Scope>();

    /// <summary>
    /// Nested saves in the same operation reuse its lock. Do not fork parallel operations inside the callback.
    /// Contention fails closed; callers may retry the entire operation with fresh credentials/data.
    /// </summary>
    public static async Task<T> ExecuteAsync<T>(int userId, Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken)
    {
        if (userId <= 0)
            throw new ArgumentOutOfRangeException(nameof(userId));
        var provider = Providers.Instance.Services.GetRequiredService<IUserAuthenticationLock>();
        var previous = Current.Value;
        if (previous is { Active: true } && previous.UserId == userId && previous.Provider == provider)
            return await action(cancellationToken).ConfigureAwait(false);

        using var bounded = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        bounded.CancelAfter(TimeSpan.FromSeconds(30));
        await using var held = await provider.TryAcquireAsync(userId, bounded.Token).ConfigureAwait(false);
        if (held == null)
            throw new UserAuthenticationLockException();
        var scope = new Scope(userId, provider);
        Current.Value = scope;
        try { return await action(bounded.Token).ConfigureAwait(false); }
        finally
        {
            scope.Active = false;
            Current.Value = previous;
        }
    }
}

/// <summary>The user is already participating in another authentication or password-change operation.</summary>
public sealed class UserAuthenticationLockException : InvalidOperationException
{
    public UserAuthenticationLockException() : base("Another authentication operation is in progress. Retry the operation.") { }
}
