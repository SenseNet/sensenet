using System;
using System.Data;
using SenseNet.Common.Storage.Data;
using SenseNet.ContentRepository.Storage.Security;
// ReSharper disable AccessToDisposedClosure

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data.SqlClient
{
    //UNDONE:DB: Rename to MsSql... or Relational...
    public class SqlSharedLockDataProvider : ISharedLockDataProviderExtension
    {
        public TimeSpan SharedLockTimeout { get; } = TimeSpan.FromMinutes(30d);

        private RelationalDataProviderBase _dataProvider;
        private RelationalDataProviderBase MainProvider => _dataProvider ?? (_dataProvider = (RelationalDataProviderBase)DataStore.DataProvider);

        public void DeleteAllSharedLocks()
        {
            using (var ctx = new SnDataContext(MainProvider))
            {
                //UNDONE: SQLSharedLock: async API + CancellationToken
                ctx.ExecuteNonQueryAsync("TRUNCATE TABLE [dbo].[SharedLocks]").Wait(ctx.CancellationToken);
            }
        }

        public void CreateSharedLock(int contentId, string @lock)
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

            string existingLock;
            using (var ctx = new SnDataContext(MainProvider))
            {
                //UNDONE: SQLSharedLock: async API + CancellationToken
                var result = ctx.ExecuteScalarAsync(sql, cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        ctx.CreateParameter("@ContentId", DbType.Int32, contentId),
                        ctx.CreateParameter("@Lock", DbType.String, @lock),
                        ctx.CreateParameter("@TimeLimit", DbType.DateTime2, timeLimit)
                    });
                }).Result;

                existingLock = result == DBNull.Value ? null : (string)result;
            }

            if (existingLock != null)
                throw new LockedNodeException(null, $"The node (#{contentId}) is locked by another shared lock.");
        }

        public string RefreshSharedLock(int contentId, string @lock)
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
            string existingLock;
            using (var ctx = new SnDataContext(MainProvider))
            {
                //UNDONE: SQLSharedLock: async API + CancellationToken
                var result = ctx.ExecuteScalarAsync(sql, cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        ctx.CreateParameter("@ContentId", DbType.Int32, contentId),
                        ctx.CreateParameter("@Lock", DbType.String, @lock),
                        ctx.CreateParameter("@TimeLimit", DbType.DateTime2, timeLimit)
                    });
                }).Result;

                existingLock = result == DBNull.Value ? null : (string)result;
            }

            if (existingLock == null)
                throw new SharedLockNotFoundException("Content is unlocked");
            if (existingLock != @lock)
                throw new LockedNodeException(null, $"The node (#{contentId}) is locked by another shared lock.");

            return existingLock;
        }

        public string ModifySharedLock(int contentId, string @lock, string newLock)
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
            string existingLock;
            using (var ctx = new SnDataContext(MainProvider))
            {
                //UNDONE: SQLSharedLock: async API + CancellationToken
                var result = ctx.ExecuteScalarAsync(sql, cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        ctx.CreateParameter("@ContentId", DbType.Int32, contentId),
                        ctx.CreateParameter("@OldLock", DbType.String, @lock),
                        ctx.CreateParameter("@NewLock", DbType.String, newLock),
                        ctx.CreateParameter("@TimeLimit", DbType.DateTime2, timeLimit)
                    });
                }).Result;

                existingLock = result == DBNull.Value ? null : (string)result;
            }

            if (existingLock == null)
                throw new SharedLockNotFoundException("Content is unlocked");
            if (existingLock != @lock)
                throw new LockedNodeException(null, $"The node (#{contentId}) is locked by another shared lock.");

            return existingLock;
        }

        public string GetSharedLock(int contentId)
        {
            var timeLimit = DateTime.UtcNow.AddTicks(-SharedLockTimeout.Ticks);
            const string sql = @"SELECT [Lock] FROM [dbo].[SharedLocks] WHERE [ContentId] = @ContentId AND [CreationDate] >= @TimeLimit";
            using (var ctx = new SnDataContext(MainProvider))
            {
                //UNDONE: SQLSharedLock: async API + CancellationToken
                var result = ctx.ExecuteScalarAsync(sql, cmd =>
                {
                    cmd.Parameters.AddRange(new []
                    {
                        ctx.CreateParameter("@ContentId", DbType.Int32, contentId),
                        ctx.CreateParameter("@TimeLimit", DbType.DateTime2, timeLimit)
                    });
                }).Result;
                return result == DBNull.Value ? null : (string)result;
            }
        }

        public string DeleteSharedLock(int contentId, string @lock)
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
            string existingLock;
            using (var ctx = new SnDataContext(MainProvider))
            {
                //UNDONE: SQLSharedLock: async API + CancellationToken
                var result = ctx.ExecuteScalarAsync(sql, cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        ctx.CreateParameter("@ContentId", DbType.Int32, contentId),
                        ctx.CreateParameter("@Lock", DbType.String, @lock),
                        ctx.CreateParameter("@TimeLimit", DbType.DateTime2, timeLimit)
                    });
                }).Result;

                existingLock = result == DBNull.Value ? null : (string)result;
            }

            if (existingLock == null)
                throw new SharedLockNotFoundException("Content is unlocked");
            if (existingLock != @lock)
                throw new LockedNodeException(null, $"The node (#{contentId}) is locked by another shared lock.");

            return existingLock;
        }

        public void CleanupSharedLocks()
        {
            const string sql = "DELETE FROM [dbo].[SharedLocks] WHERE [CreationDate] < DATEADD(MINUTE, -@TimeoutInMinutes - 30, GETUTCDATE())";

            using (var ctx = new SnDataContext(MainProvider))
            {
                //UNDONE: SQLSharedLock: async API + CancellationToken
                var unused = ctx.ExecuteNonQueryAsync(sql, cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        ctx.CreateParameter("@TimeoutInMinutes", DbType.Int32, Convert.ToInt32(SharedLockTimeout.TotalMinutes))
                    });
                }).Result;
            }
        }

        /// <inheritdoc/>
        public void SetSharedLockCreationDate(int nodeId, DateTime value)
        {
            throw new NotImplementedException(); //UNDONE:DB: SQLIMPL SetSharedLockCreationDate
        }
        /// <inheritdoc/>
        public DateTime GetSharedLockCreationDate(int nodeId)
        {
            throw new NotImplementedException(); //UNDONE:DB: SQLIMPL GetSharedLockCreationDate
        }

        /* ============================================================= For tests */

        public DateTime GetCreationDate(int contentId)
        {
            const string sql = "SELECT [CreationDate] FROM [dbo].[SharedLocks] WHERE [ContentId] = @ContentId";

            using (var ctx = new SnDataContext(MainProvider))
            {
                //UNDONE: SQLSharedLock: async API + CancellationToken
                var result = ctx.ExecuteScalarAsync(sql, cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        ctx.CreateParameter("@ContentId", DbType.Int32, contentId)
                    });
                }).Result;
                return result == DBNull.Value ? DateTime.MinValue : (DateTime)result;
            }
        }
        public void SetCreationDate(int contentId, DateTime value)
        {
            const string sql = "UPDATE [dbo].[SharedLocks] SET [CreationDate] = @CreationDate WHERE [ContentId] = @ContentId";

            using (var ctx = new SnDataContext(MainProvider))
            {
                //UNDONE: SQLSharedLock: async API + CancellationToken
                var unused = ctx.ExecuteNonQueryAsync(sql, cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        ctx.CreateParameter("@ContentId", DbType.Int32, contentId),
                        ctx.CreateParameter("@CreationDate", DbType.DateTime2, value)
                    });
                }).Result;
            }
        }
    }
}
