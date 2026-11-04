#nullable enable
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Configuration;

namespace SenseNet.ContentRepository.Storage.Data.MsSqlClient;

/// <summary>Database-scoped SQL application locks, independent of the legacy ExclusiveLocks table.</summary>
public class MsSqlUserAuthenticationLock(IOptions<ConnectionStringOptions> connectionStrings)
    : IUserAuthenticationLock
{
    public async Task<IAsyncDisposable?> TryAcquireAsync(int userId, CancellationToken cancellationToken)
    {
        if (userId <= 0) throw new ArgumentOutOfRangeException(nameof(userId));
        // A physical disconnect releases the session-owned lock even if an operation fails.
        var connectionString = new SqlConnectionStringBuilder(connectionStrings.Value.Repository)
        {
            Pooling = false,
            ConnectTimeout = 10
        };
        var connection = new SqlConnection(connectionString.ConnectionString);
        try
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            using var command = connection.CreateCommand();
            command.CommandTimeout = 10;
            command.CommandText = @"
DECLARE @result int;
EXEC @result = sys.sp_getapplock @Resource = @resource, @LockMode = 'Exclusive',
    @LockOwner = 'Session', @LockTimeout = 0, @DbPrincipal = 'public';
SELECT @result;";
            command.Parameters.Add("@resource", SqlDbType.NVarChar, 255).Value = "sensenet-local-auth-user:" + userId;
            var result = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false));
            if (result >= 0) return connection;
            await connection.DisposeAsync().ConfigureAwait(false);
            return null;
        }
        catch
        {
            await connection.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }
}

