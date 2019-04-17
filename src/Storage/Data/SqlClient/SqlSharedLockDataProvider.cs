using System;
using System.Data;
using SenseNet.ContentRepository.Storage.Security;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data.SqlClient
{
    public class SqlSharedLockDataProvider : ISharedLockDataProviderExtension
    {
        public TimeSpan SharedLockTimeout { get; } = TimeSpan.FromMinutes(30d);

        private DataProvider _mainProvider; //DB:ok but rewrite in the SqlSharedLockDataProvider2
        public DataProvider MainProvider => _mainProvider ?? (_mainProvider = DataProvider.Instance); //DB:ok but rewrite in the SqlSharedLockDataProvider2

        public void DeleteAllSharedLocks()
        {
            using (var proc = MainProvider.CreateDataProcedure("TRUNCATE TABLE [dbo].[SharedLocks]"))
            {
                proc.CommandType = CommandType.Text;
                proc.ExecuteNonQuery();
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
            using (var proc = MainProvider.CreateDataProcedure(sql)
                .AddParameter("@ContentId", contentId)
                .AddParameter("@Lock", @lock)
                .AddParameter("@TimeLimit", timeLimit))
            {
                proc.CommandType = CommandType.Text;
                var result = proc.ExecuteScalar();
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
            using (var proc = MainProvider.CreateDataProcedure(sql)
                .AddParameter("@ContentId", contentId)
                .AddParameter("@Lock", @lock)
                .AddParameter("@TimeLimit", timeLimit))
            {
                proc.CommandType = CommandType.Text;
                var result = proc.ExecuteScalar();
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
            using (var proc = MainProvider.CreateDataProcedure(sql)
                .AddParameter("@ContentId", contentId)
                .AddParameter("@OldLock", @lock)
                .AddParameter("@NewLock", newLock)
                .AddParameter("@TimeLimit", timeLimit))
            {
                proc.CommandType = CommandType.Text;
                var result = proc.ExecuteScalar();
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

            string existingLock;
            using (var proc = MainProvider.CreateDataProcedure(sql)
                .AddParameter("@ContentId", contentId)
                .AddParameter("@TimeLimit", timeLimit))
            {
                proc.CommandType = CommandType.Text;
                var result = proc.ExecuteScalar();
                existingLock = result == DBNull.Value ? null : (string)result;
            }

            return existingLock;
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
            using (var proc = MainProvider.CreateDataProcedure(sql)
                .AddParameter("@ContentId", contentId)
                .AddParameter("@Lock", @lock)
                .AddParameter("@TimeLimit", timeLimit))
            {
                proc.CommandType = CommandType.Text;
                var result = proc.ExecuteScalar();
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
            using (var proc = MainProvider.CreateDataProcedure(sql)
                .AddParameter("@TimeoutInMinutes", Convert.ToInt32(SharedLockTimeout.TotalMinutes)))
            {
                proc.CommandType = CommandType.Text;
                proc.ExecuteNonQuery();
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
            using (var proc = MainProvider.CreateDataProcedure(sql)
                .AddParameter("@ContentId", contentId))
            {
                proc.CommandType = CommandType.Text;
                var result = proc.ExecuteScalar();
                return result == DBNull.Value ? DateTime.MinValue : (DateTime) result;
            }
        }
        public void SetCreationDate(int contentId, DateTime value)
        {
            const string sql = "UPDATE [dbo].[SharedLocks] SET [CreationDate] = @CreationDate WHERE [ContentId] = @ContentId";
            using (var proc = MainProvider.CreateDataProcedure(sql)
                .AddParameter("@ContentId", contentId)
                .AddParameter("@CreationDate", value))
            {
                proc.CommandType = CommandType.Text;
                proc.ExecuteNonQuery();
            }
        }
    }
}
