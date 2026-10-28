using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;
using SenseNet.Configuration;
using SenseNet.Diagnostics;
using SenseNet.Tools;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data.PgSqlClient
{
    public class PgSqlExclusiveLockDataProvider : IExclusiveLockDataProvider
    {
        private readonly IRetrier _retrier;
        private DataOptions DataOptions { get; }
        private ConnectionStringOptions ConnectionStrings { get; }

        public PgSqlExclusiveLockDataProvider(IOptions<DataOptions> dataOptions,
            IOptions<ConnectionStringOptions> connectionOptions, IRetrier retrier)
        {
            _retrier = retrier;
            DataOptions = dataOptions?.Value ?? new DataOptions();
            ConnectionStrings = connectionOptions?.Value ?? new ConnectionStringOptions();
        }

        public async Task<bool> AcquireAsync(string key, string operationId,
            DateTime timeLimit, CancellationToken cancellationToken)
        {
            using var op = SnTrace.Database.StartOperation("PgSqlExclusiveLockDataProvider: " +
                "Acquire(key: {0}, operationId: {1})", key, operationId);
            using var ctx = new PgSqlDataContext(ConnectionStrings.Repository, DataOptions, _retrier, cancellationToken);
            var result = await ctx.ExecuteScalarAsync(AcquireScript, cmd =>
            {
                cmd.Parameters.AddRange(new[]
                {
                    ctx.CreateParameter("@Key", DbType.String, 450, key),
                    ctx.CreateParameter("@OperationId", DbType.String, 450, operationId),
                    ctx.CreateParameter("@TimeLimit", DbType.DateTime2, timeLimit),
                    ctx.CreateParameter("@Now", DbType.DateTime2, DateTime.UtcNow),
                });
            }).ConfigureAwait(false);
            op.Successful = true;
            return result != null && Convert.ToBoolean(result);
        }

        public async Task<bool> IsLockedAsync(string key, string operationId,
            CancellationToken cancellationToken)
        {
            using var ctx = new PgSqlDataContext(ConnectionStrings.Repository, DataOptions, _retrier, cancellationToken);
            var result = await ctx.ExecuteScalarAsync(IsLockedScript, cmd =>
            {
                cmd.Parameters.AddRange(new[]
                {
                    ctx.CreateParameter("@Key", DbType.String, 450, key),
                    ctx.CreateParameter("@OperationId", DbType.String, 450, operationId),
                    ctx.CreateParameter("@Now", DbType.DateTime2, DateTime.UtcNow),
                });
            }).ConfigureAwait(false);
            return result != null && Convert.ToBoolean(result);
        }

        public async System.Threading.Tasks.Task RefreshAsync(string key, string operationId, DateTime newTimeLimit,
            CancellationToken cancellationToken)
        {
            using var ctx = new PgSqlDataContext(ConnectionStrings.Repository, DataOptions, _retrier, cancellationToken);
            await ctx.ExecuteNonQueryAsync(RefreshScript, cmd =>
            {
                cmd.Parameters.AddRange(new[]
                {
                    ctx.CreateParameter("@Key", DbType.String, 450, key),
                    ctx.CreateParameter("@OperationId", DbType.String, 450, operationId),
                    ctx.CreateParameter("@TimeLimit", DbType.DateTime2, newTimeLimit),
                });
            }).ConfigureAwait(false);
        }

        public async System.Threading.Tasks.Task ReleaseAsync(string key, string operationId,
            CancellationToken cancellationToken)
        {
            using var ctx = new PgSqlDataContext(ConnectionStrings.Repository, DataOptions, _retrier, cancellationToken);
            await ctx.ExecuteNonQueryAsync(ReleaseScript, cmd =>
            {
                cmd.Parameters.AddRange(new[]
                {
                    ctx.CreateParameter("@Key", DbType.String, 450, key),
                    ctx.CreateParameter("@OperationId", DbType.String, 450, operationId),
                });
            }).ConfigureAwait(false);
        }

        public async System.Threading.Tasks.Task ReleaseAllAsync(CancellationToken cancellationToken)
        {
            using var ctx = new PgSqlDataContext(ConnectionStrings.Repository, DataOptions, _retrier, cancellationToken);
            await ctx.ExecuteNonQueryAsync(@"DELETE FROM ""ExclusiveLocks""").ConfigureAwait(false);
        }

        public async Task<bool> IsFeatureAvailable(CancellationToken cancellationToken)
        {
            try
            {
                using var ctx = new PgSqlDataContext(ConnectionStrings.Repository, DataOptions, _retrier, cancellationToken);
                await ctx.ExecuteScalarAsync(@"SELECT 1 FROM ""ExclusiveLocks"" LIMIT 1").ConfigureAwait(false);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // =============================================================================================== Scripts

        private const string AcquireScript = @"-- PgSqlExclusiveLockDataProvider.Acquire
DELETE FROM ""ExclusiveLocks"" WHERE ""TimeLimit"" < @Now;
INSERT INTO ""ExclusiveLocks"" (""Name"", ""OperationId"", ""TimeLimit"")
VALUES (@Key, @OperationId, @TimeLimit)
ON CONFLICT (""Name"") DO NOTHING
RETURNING TRUE
";

        private const string IsLockedScript = @"-- PgSqlExclusiveLockDataProvider.IsLocked
SELECT EXISTS(
    SELECT 1 FROM ""ExclusiveLocks""
    WHERE ""Name"" = @Key AND ""OperationId"" != @OperationId AND ""TimeLimit"" > @Now
)
";

        private const string RefreshScript = @"-- PgSqlExclusiveLockDataProvider.Refresh
UPDATE ""ExclusiveLocks"" SET ""TimeLimit"" = @TimeLimit
WHERE ""Name"" = @Key AND ""OperationId"" = @OperationId
";

        private const string ReleaseScript = @"-- PgSqlExclusiveLockDataProvider.Release
DELETE FROM ""ExclusiveLocks""
WHERE ""Name"" = @Key AND ""OperationId"" = @OperationId
";

        // =============================================================================================== Installation

        public static readonly string CreationScript = @"-- PgSqlExclusiveLockDataProvider.CreateTable
CREATE TABLE IF NOT EXISTS ""ExclusiveLocks"" (
    ""Id"" SERIAL PRIMARY KEY,
    ""Name"" VARCHAR(450) NOT NULL UNIQUE,
    ""OperationId"" VARCHAR(450) NOT NULL,
    ""TimeLimit"" TIMESTAMP WITHOUT TIME ZONE NOT NULL
);
";
    }
}
