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
    public class PgSqlSharedLockDataProvider : ISharedLockDataProvider
    {
        private readonly IRetrier _retrier;
        private DataOptions DataOptions { get; }
        private ConnectionStringOptions ConnectionStrings { get; }

        public PgSqlSharedLockDataProvider(IOptions<DataOptions> dataOptions,
            IOptions<ConnectionStringOptions> connectionOptions, IRetrier retrier)
        {
            _retrier = retrier;
            DataOptions = dataOptions?.Value ?? new DataOptions();
            ConnectionStrings = connectionOptions?.Value ?? new ConnectionStringOptions();
        }

        public TimeSpan SharedLockTimeout { get; } = TimeSpan.FromMinutes(30);

        public async System.Threading.Tasks.Task DeleteAllSharedLocksAsync(CancellationToken cancellationToken)
        {
            using var ctx = new PgSqlDataContext(ConnectionStrings.Repository, DataOptions, _retrier, cancellationToken);
            await ctx.ExecuteNonQueryAsync(@"TRUNCATE TABLE ""SharedLocks""").ConfigureAwait(false);
        }

        public async System.Threading.Tasks.Task CreateSharedLockAsync(int contentId, string @lock,
            CancellationToken cancellationToken)
        {
            using var op = SnTrace.Database.StartOperation("PgSqlSharedLockDataProvider: " +
                "CreateSharedLock(contentId: {0})", contentId);

            var timeLimit = DateTime.UtcNow.AddTicks(-SharedLockTimeout.Ticks);

            using var ctx = new PgSqlDataContext(ConnectionStrings.Repository, DataOptions, _retrier, cancellationToken);

            // Delete expired locks first
            await ctx.ExecuteNonQueryAsync(
                @"DELETE FROM ""SharedLocks"" WHERE ""CreationDate"" < @TimeLimit", cmd =>
                {
                    cmd.Parameters.Add(ctx.CreateParameter("@TimeLimit", DbType.DateTime2, timeLimit));
                }).ConfigureAwait(false);

            // Insert new lock
            await ctx.ExecuteNonQueryAsync(
                @"INSERT INTO ""SharedLocks"" (""ContentId"", ""Lock"", ""CreationDate"")
VALUES (@ContentId, @Lock, @Now)
ON CONFLICT (""ContentId"") DO UPDATE SET ""Lock"" = @Lock, ""CreationDate"" = @Now
WHERE ""SharedLocks"".""Lock"" = @Lock", cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        ctx.CreateParameter("@ContentId", DbType.Int32, contentId),
                        ctx.CreateParameter("@Lock", DbType.String, 1000, @lock),
                        ctx.CreateParameter("@Now", DbType.DateTime2, DateTime.UtcNow),
                    });
                }).ConfigureAwait(false);

            op.Successful = true;
        }

        public async Task<string> ModifySharedLockAsync(int contentId, string @lock, string newLock,
            CancellationToken cancellationToken)
        {
            using var ctx = new PgSqlDataContext(ConnectionStrings.Repository, DataOptions, _retrier, cancellationToken);

            var timeLimit = DateTime.UtcNow.AddTicks(-SharedLockTimeout.Ticks);

            // Delete expired
            await ctx.ExecuteNonQueryAsync(
                @"DELETE FROM ""SharedLocks"" WHERE ""CreationDate"" < @TimeLimit", cmd =>
                {
                    cmd.Parameters.Add(ctx.CreateParameter("@TimeLimit", DbType.DateTime2, timeLimit));
                }).ConfigureAwait(false);

            // Get current lock
            var existingLock = await ctx.ExecuteScalarAsync(
                @"SELECT ""Lock"" FROM ""SharedLocks"" WHERE ""ContentId"" = @ContentId", cmd =>
                {
                    cmd.Parameters.Add(ctx.CreateParameter("@ContentId", DbType.Int32, contentId));
                }).ConfigureAwait(false);

            if (existingLock == null || existingLock == DBNull.Value)
                throw new SharedLockNotFoundException("Content is not locked.");

            var currentLock = (string)existingLock;
            if (currentLock != @lock)
                return currentLock;

            await ctx.ExecuteNonQueryAsync(
                @"UPDATE ""SharedLocks"" SET ""Lock"" = @NewLock, ""CreationDate"" = @Now
WHERE ""ContentId"" = @ContentId AND ""Lock"" = @Lock", cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        ctx.CreateParameter("@ContentId", DbType.Int32, contentId),
                        ctx.CreateParameter("@Lock", DbType.String, 1000, @lock),
                        ctx.CreateParameter("@NewLock", DbType.String, 1000, newLock),
                        ctx.CreateParameter("@Now", DbType.DateTime2, DateTime.UtcNow),
                    });
                }).ConfigureAwait(false);

            return newLock;
        }

        public async Task<string> GetSharedLockAsync(int contentId, CancellationToken cancellationToken)
        {
            using var ctx = new PgSqlDataContext(ConnectionStrings.Repository, DataOptions, _retrier, cancellationToken);
            var timeLimit = DateTime.UtcNow.AddTicks(-SharedLockTimeout.Ticks);

            var result = await ctx.ExecuteScalarAsync(
                @"SELECT ""Lock"" FROM ""SharedLocks""
WHERE ""ContentId"" = @ContentId AND ""CreationDate"" >= @TimeLimit", cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        ctx.CreateParameter("@ContentId", DbType.Int32, contentId),
                        ctx.CreateParameter("@TimeLimit", DbType.DateTime2, timeLimit),
                    });
                }).ConfigureAwait(false);

            return result == null || result == DBNull.Value ? null : (string)result;
        }

        public async Task<string> DeleteSharedLockAsync(int contentId, string @lock,
            CancellationToken cancellationToken)
        {
            using var ctx = new PgSqlDataContext(ConnectionStrings.Repository, DataOptions, _retrier, cancellationToken);
            var timeLimit = DateTime.UtcNow.AddTicks(-SharedLockTimeout.Ticks);

            // Delete expired
            await ctx.ExecuteNonQueryAsync(
                @"DELETE FROM ""SharedLocks"" WHERE ""CreationDate"" < @TimeLimit", cmd =>
                {
                    cmd.Parameters.Add(ctx.CreateParameter("@TimeLimit", DbType.DateTime2, timeLimit));
                }).ConfigureAwait(false);

            var existingLock = await ctx.ExecuteScalarAsync(
                @"SELECT ""Lock"" FROM ""SharedLocks"" WHERE ""ContentId"" = @ContentId", cmd =>
                {
                    cmd.Parameters.Add(ctx.CreateParameter("@ContentId", DbType.Int32, contentId));
                }).ConfigureAwait(false);

            if (existingLock == null || existingLock == DBNull.Value)
                throw new SharedLockNotFoundException("Content is not locked.");

            var currentLock = (string)existingLock;
            if (currentLock != @lock)
                return currentLock;

            await ctx.ExecuteNonQueryAsync(
                @"DELETE FROM ""SharedLocks"" WHERE ""ContentId"" = @ContentId AND ""Lock"" = @Lock", cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        ctx.CreateParameter("@ContentId", DbType.Int32, contentId),
                        ctx.CreateParameter("@Lock", DbType.String, 1000, @lock),
                    });
                }).ConfigureAwait(false);

            return @lock;
        }

        public async Task<string> RefreshSharedLockAsync(int contentId, string @lock,
            CancellationToken cancellationToken)
        {
            using var ctx = new PgSqlDataContext(ConnectionStrings.Repository, DataOptions, _retrier, cancellationToken);
            var timeLimit = DateTime.UtcNow.AddTicks(-SharedLockTimeout.Ticks);

            // Delete expired
            await ctx.ExecuteNonQueryAsync(
                @"DELETE FROM ""SharedLocks"" WHERE ""CreationDate"" < @TimeLimit", cmd =>
                {
                    cmd.Parameters.Add(ctx.CreateParameter("@TimeLimit", DbType.DateTime2, timeLimit));
                }).ConfigureAwait(false);

            var existingLock = await ctx.ExecuteScalarAsync(
                @"SELECT ""Lock"" FROM ""SharedLocks"" WHERE ""ContentId"" = @ContentId", cmd =>
                {
                    cmd.Parameters.Add(ctx.CreateParameter("@ContentId", DbType.Int32, contentId));
                }).ConfigureAwait(false);

            if (existingLock == null || existingLock == DBNull.Value)
                throw new SharedLockNotFoundException("Content is not locked.");

            var currentLock = (string)existingLock;
            if (currentLock != @lock)
                return currentLock;

            await ctx.ExecuteNonQueryAsync(
                @"UPDATE ""SharedLocks"" SET ""CreationDate"" = @Now
WHERE ""ContentId"" = @ContentId AND ""Lock"" = @Lock", cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        ctx.CreateParameter("@ContentId", DbType.Int32, contentId),
                        ctx.CreateParameter("@Lock", DbType.String, 1000, @lock),
                        ctx.CreateParameter("@Now", DbType.DateTime2, DateTime.UtcNow),
                    });
                }).ConfigureAwait(false);

            return @lock;
        }

        public async System.Threading.Tasks.Task CleanupSharedLocksAsync(CancellationToken cancellationToken)
        {
            using var ctx = new PgSqlDataContext(ConnectionStrings.Repository, DataOptions, _retrier, cancellationToken);
            var timeLimit = DateTime.UtcNow.AddTicks(-SharedLockTimeout.Ticks);
            await ctx.ExecuteNonQueryAsync(
                @"DELETE FROM ""SharedLocks"" WHERE ""CreationDate"" < @TimeLimit", cmd =>
                {
                    cmd.Parameters.Add(ctx.CreateParameter("@TimeLimit", DbType.DateTime2, timeLimit));
                }).ConfigureAwait(false);
        }
    }
}
