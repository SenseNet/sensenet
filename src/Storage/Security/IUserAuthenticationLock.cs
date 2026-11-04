#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SenseNet.ContentRepository.Storage.Security;

/// <summary>Serializes credential changes and local authentication across all repository hosts.</summary>
public interface IUserAuthenticationLock
{
    Task<IAsyncDisposable?> TryAcquireAsync(int userId, CancellationToken cancellationToken);
}
