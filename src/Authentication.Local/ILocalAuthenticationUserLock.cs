namespace SenseNet.Authentication.Local;

/// <summary>Exclusive per-user lock shared by every host issuing sessions for a repository.</summary>
public interface ILocalAuthenticationUserLock
{
    /// <returns>A held lock, or null when another operation owns it. Dispose releases it.</returns>
    Task<IAsyncDisposable?> TryAcquireAsync(int userId, CancellationToken cancellationToken);
}
