using System;
using System.Data;
using System.Threading;
using STT=System.Threading.Tasks;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;

// ReSharper disable AccessToDisposedClosure
// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data.MsSqlClient
{
    /// <summary> 
    /// This is an MS SQL implementation of the <see cref="ISharedLockDataProvider"/> interface.
    /// It requires the main data provider to be a <see cref="RelationalDataProviderBase"/>.
    /// </summary>
    public class MsSqlSharedLockDataProvider : ISharedLockDataProvider
    {
        public TimeSpan SharedLockTimeout { get; } = TimeSpan.FromMinutes(30d);

        private readonly RelationalDataProviderBase _mainProvider;

        public MsSqlSharedLockDataProvider(DataProvider mainProvider)
        {
            if (mainProvider == null)
                return;
            if (!(mainProvider is RelationalDataProviderBase relationalDataProviderBase))
                throw new ArgumentException("The mainProvider need to be RelationalDataProviderBase.");
            _mainProvider = relationalDataProviderBase;
        }

        public async STT.Task DeleteAllSharedLocksAsync(CancellationToken cancellationToken)
        {
            using var op = SnTrace.Database.StartOperation("MsSqlSharedLockDataProvider: DeleteAllSharedLocks()");
            using var ctx = _mainProvider.CreateDataContext(cancellationToken);
            await ctx.ExecuteNonQueryAsync("TRUNCATE TABLE [dbo].[SharedLocks]").ConfigureAwait(false);
            op.Successful = true;
        }

        public async STT.Task CreateSharedLockAsync(int contentId, string @lock, CancellationToken cancellationToken)
        {
            var timeLimit = DateTime.UtcNow.AddTicks(-SharedLockTimeout.Ticks);
            const string sql = @"DECLARE @Id INT
DECLARE @Result NVARCHAR(1000)
DELETE FROM [dbo].[SharedLocks] WHERE [ContentId] = @ContentId AND [CreationDate] < @TimeLimit
SELECT @Id = [SharedLockId], @Result = [Lock] FROM [dbo].[SharedLocks] WHERE [ContentId] = @ContentId

IF @Result IS NULL
BEGIN
    INSERT INTO [dbo].[SharedLocks] ( [ContentId], [Lock], [CreationDate] )
        VALUES ( @ContentId, @Lock, GETUTCDATE() );
	SELECT @Result = NULL
END
IF @Result = @Lock
BEGIN
	UPDATE [dbo].[SharedLocks] SET [CreationDate] = GETUTCDATE() WHERE [SharedLockId] = @Id
	SELECT @Result = NULL
END
SELECT @Result
";

            using var op = SnTrace.Database.StartOperation("MsSqlSharedLockDataProvider: " +
                "CreateSharedLock(contentId: {0}, lock: {1})", contentId, @lock);

            using var ctx = _mainProvider.CreateDataContext(cancellationToken);
            var result = await ctx.ExecuteScalarAsync(sql, cmd =>
            {
                cmd.Parameters.AddRange(new[]
                {
                    ctx.CreateParameter("@ContentId", DbType.Int32, contentId),
                    ctx.CreateParameter("@Lock", DbType.String, @lock),
                    ctx.CreateParameter("@TimeLimit", DbType.DateTime2, timeLimit)
                });
            }).ConfigureAwait(false);

            var existingLock = result == DBNull.Value ? null : (string)result;
            if (existingLock != null)
                throw new LockedNodeException(null, $"The node (#{contentId}) is locked by another shared lock.");

            op.Successful = true;
        }

        public async STT.Task<string> RefreshSharedLockAsync(int contentId, string @lock, CancellationToken cancellationToken)
        {
            var timeLimit = DateTime.UtcNow.AddTicks(-SharedLockTimeout.Ticks);
            const string sql = @"DECLARE @Id INT
DECLARE @Result NVARCHAR(1000)
DELETE FROM [dbo].[SharedLocks] WHERE [ContentId] = @ContentId AND [CreationDate] < @TimeLimit
SELECT @Id = [SharedLockId], @Result = [Lock] FROM [dbo].[SharedLocks] WHERE [ContentId] = @ContentId

IF @Result = @Lock
	UPDATE [dbo].[SharedLocks] SET [CreationDate] = GETUTCDATE() WHERE [SharedLockId] = @Id
SELECT @Result
";
            using var op = SnTrace.Database.StartOperation("MsSqlSharedLockDataProvider: " +
                "RefreshSharedLock(contentId: {0}, lock: {1})", contentId, @lock);
            using var ctx = _mainProvider.CreateDataContext(cancellationToken);
            var result = await ctx.ExecuteScalarAsync(sql, cmd =>
            {
                cmd.Parameters.AddRange(new[]
                {
                    ctx.CreateParameter("@ContentId", DbType.Int32, contentId),
                    ctx.CreateParameter("@Lock", DbType.String, @lock),
                    ctx.CreateParameter("@TimeLimit", DbType.DateTime2, timeLimit)
                });
            }).ConfigureAwait(false);

            var existingLock = result == DBNull.Value ? null : (string)result;
            if (existingLock == null)
                throw new SharedLockNotFoundException("Content is unlocked");
            if (existingLock != @lock)
                throw new LockedNodeException(null, $"The node (#{contentId}) is locked by another shared lock.");
            op.Successful = true;

            return existingLock;
        }

        public async STT.Task<string> ModifySharedLockAsync(int contentId, string @lock, string newLock, CancellationToken cancellationToken)
        {
            var timeLimit = DateTime.UtcNow.AddTicks(-SharedLockTimeout.Ticks);
            const string sql = @"DECLARE @Id INT
DECLARE @Result NVARCHAR(1000)
DELETE FROM [dbo].[SharedLocks] WHERE [ContentId] = @ContentId AND [CreationDate] < @TimeLimit
SELECT @Id = [SharedLockId], @Result = [Lock] FROM [dbo].[SharedLocks] WHERE [ContentId] = @ContentId

IF @Result = @OldLock
	UPDATE [dbo].[SharedLocks] SET [Lock] = @NewLock, [CreationDate] = GETUTCDATE() WHERE [SharedLockId] = @Id
SELECT @Result
";
            using var op = SnTrace.Database.StartOperation("MsSqlSharedLockDataProvider: " +
                "ModifySharedLock(contentId: {0}, lock: {1}, newLock: {2})", contentId, @lock, newLock);
            using var ctx = _mainProvider.CreateDataContext(cancellationToken);
            var result = await ctx.ExecuteScalarAsync(sql, cmd =>
            {
                cmd.Parameters.AddRange(new[]
                {
                    ctx.CreateParameter("@ContentId", DbType.Int32, contentId),
                    ctx.CreateParameter("@OldLock", DbType.String, @lock),
                    ctx.CreateParameter("@NewLock", DbType.String, newLock),
                    ctx.CreateParameter("@TimeLimit", DbType.DateTime2, timeLimit)
                });
            }).ConfigureAwait(false);

            var existingLock = result == DBNull.Value ? null : (string)result;
            if (existingLock == null)
                throw new SharedLockNotFoundException("Content is unlocked");
            if (existingLock != @lock)
                throw new LockedNodeException(null, $"The node (#{contentId}) is locked by another shared lock.");
            op.Successful = true;

            return existingLock;
        }

        public async STT.Task<string> GetSharedLockAsync(int contentId, CancellationToken cancellationToken)
        {
            var timeLimit = DateTime.UtcNow.AddTicks(-SharedLockTimeout.Ticks);
            const string sql = @"SELECT [Lock] FROM [dbo].[SharedLocks] WHERE [ContentId] = @ContentId AND [CreationDate] >= @TimeLimit";
            using var op = SnTrace.Database.StartOperation("MsSqlSharedLockDataProvider: " +
                "GetSharedLock(contentId: {0})", contentId);
            using var ctx = _mainProvider.CreateDataContext(cancellationToken);
            var result = await ctx.ExecuteScalarAsync(sql, cmd =>
            {
                cmd.Parameters.AddRange(new []
                {
                    ctx.CreateParameter("@ContentId", DbType.Int32, contentId),
                    ctx.CreateParameter("@TimeLimit", DbType.DateTime2, timeLimit)
                });
            }).ConfigureAwait(false);
            op.Successful = true;
            return result == DBNull.Value ? null : (string)result;
        }

        public async STT.Task<string> DeleteSharedLockAsync(int contentId, string @lock, CancellationToken cancellationToken)
        {
            var timeLimit = DateTime.UtcNow.AddTicks(-SharedLockTimeout.Ticks);
            const string sql = @"DECLARE @Id INT
DECLARE @Result NVARCHAR(1000)
DELETE FROM [dbo].[SharedLocks] WHERE [ContentId] = @ContentId AND [CreationDate] < @TimeLimit
SELECT @Id = [SharedLockId], @Result = [Lock] FROM [dbo].[SharedLocks] WHERE [ContentId] = @ContentId

IF @Result = @Lock
	DELETE FROM [dbo].[SharedLocks] WHERE [SharedLockId] = @Id
SELECT @Result
";
            using var op = SnTrace.Database.StartOperation("MsSqlSharedLockDataProvider: " +
                "DeleteSharedLock(contentId: {0}, lock: {1})", contentId, @lock);
            using var ctx = _mainProvider.CreateDataContext(cancellationToken);
            var result = await ctx.ExecuteScalarAsync(sql, cmd =>
            {
                cmd.Parameters.AddRange(new[]
                {
                    ctx.CreateParameter("@ContentId", DbType.Int32, contentId),
                    ctx.CreateParameter("@Lock", DbType.String, @lock),
                    ctx.CreateParameter("@TimeLimit", DbType.DateTime2, timeLimit)
                });
            }).ConfigureAwait(false);

            var existingLock = result == DBNull.Value ? null : (string)result;
            if (existingLock == null)
                throw new SharedLockNotFoundException("Content is unlocked");
            if (existingLock != @lock)
                throw new LockedNodeException(null, $"The node (#{contentId}) is locked by another shared lock.");
            op.Successful = true;

            return existingLock;
        }

        public async STT.Task CleanupSharedLocksAsync(CancellationToken cancellationToken)
        {
            const string sql = "DELETE FROM [dbo].[SharedLocks] WHERE [CreationDate] < DATEADD(MINUTE, -@TimeoutInMinutes - 30, GETUTCDATE())";

            using var op = SnTrace.Database.StartOperation("MsSqlSharedLockDataProvider: CleanupSharedLocks()");
            using var ctx = _mainProvider.CreateDataContext(cancellationToken);
            await ctx.ExecuteNonQueryAsync(sql, cmd =>
            {
                cmd.Parameters.AddRange(new[]
                {
                    ctx.CreateParameter("@TimeoutInMinutes", DbType.Int32, Convert.ToInt32(SharedLockTimeout.TotalMinutes))
                });
            }).ConfigureAwait(false);
            op.Successful = true;
        }
    }
}
