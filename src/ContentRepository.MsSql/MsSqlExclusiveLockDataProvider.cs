using System;
using System.Data;
using System.Diagnostics;
using System.Threading;
using STT=System.Threading.Tasks;
using SenseNet.Configuration;
using SenseNet.Diagnostics;

// ReSharper disable ConvertToUsingDeclaration

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data.MsSqlClient
{
    /// <summary> 
    /// This is an MS SQL implementation of the <see cref="IExclusiveLockDataProvider"/> interface.
    /// It requires the main data provider to be a <see cref="RelationalDataProviderBase"/>.
    /// </summary>
    public class MsSqlExclusiveLockDataProvider : IExclusiveLockDataProvider
    {
        private const string AcquireScript = @"-- MsSqlExclusiveLockDataProvider.Acquire
-- Ensure existing row for the @Name in a fault-tolerant manner
IF NOT EXISTS (SELECT Id FROM [ExclusiveLocks] (NOLOCK) WHERE [Name] = @Name) BEGIN
    BEGIN TRY INSERT INTO [ExclusiveLocks] ([Name]) VALUES (@Name) END TRY
	BEGIN CATCH /* do nothing */ END CATCH
END
-- lock if unlocked or timed out in an atomic instruction
UPDATE T SET OperationId = @OperationId, TimeLimit = @TimeLimit
	OUTPUT inserted.Id
	FROM (SELECT TOP 1 * FROM [ExclusiveLocks]
			WHERE (OperationId IS NULL OR TimeLimit <= GETUTCDATE())) T
";

        private const string RefreshScript = @"UPDATE ExclusiveLocks SET [TimeLimit] = @TimeLimit WHERE [Name] = @Name";

        private const string ReleaseScript = @"UPDATE ExclusiveLocks SET [OperationId] = NULL WHERE @Name = [Name]";

        private const string IsLockedScript = @"SELECT Id FROM ExclusiveLocks (NOLOCK)
WHERE [Name] = @Name AND [OperationId] IS NOT NULL AND [TimeLimit] > GETUTCDATE()";

        private RelationalDataProviderBase _dataProvider;
        private RelationalDataProviderBase MainProvider =>
            _dataProvider ??= (_dataProvider = (RelationalDataProviderBase)Providers.Instance.DataProvider);

        /// <inheritdoc/>
        public async STT.Task<bool> AcquireAsync(string key, string operationId, DateTime timeLimit,
            CancellationToken cancellationToken)
        {
            using var op = SnTrace.Database.StartOperation("MsSqlExclusiveLockDataProvider: " +
                "Acquire(key: {0}, operationId: {1}, timeLimit: {2:yyyy-MM-dd HH:mm:ss.fffff})", key, operationId, timeLimit);

            using var ctx = MainProvider.CreateDataContext(cancellationToken);
            var result = await ctx.ExecuteScalarAsync(AcquireScript, cmd =>
            {
                cmd.Parameters.AddRange(new[]
                {
                    // ReSharper disable thrice AccessToDisposedClosure
                    ctx.CreateParameter("@Name", DbType.String, key),
                    ctx.CreateParameter("@OperationId", DbType.String, operationId),
                    ctx.CreateParameter("@TimeLimit", DbType.DateTime2, timeLimit)
                });
            }).ConfigureAwait(false);
            SnTrace.Database.Write($"MsSqlExclusiveLockDataProvider: Acquire result: {{0}}", result == null ? "[null]" : "ACQUIRED " + result);
            op.Successful = true;

            return result != DBNull.Value && result != null;
        }

        /// <inheritdoc/>
        public async STT.Task RefreshAsync(string key, string operationId, DateTime newTimeLimit,
            CancellationToken cancellationToken)
        {
            using var op = SnTrace.Database.StartOperation("MsSqlExclusiveLockDataProvider: " +
                "Refresh(key: {0}, operationId: {1}, newTimeLimit: {2:yyyy-MM-dd HH:mm:ss.fffff})",
                key, operationId, newTimeLimit);
            using var ctx = MainProvider.CreateDataContext(cancellationToken);
            await ctx.ExecuteNonQueryAsync(RefreshScript,
                cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        ctx.CreateParameter("@Name", DbType.String, key),
                        ctx.CreateParameter("@TimeLimit", DbType.DateTime2, newTimeLimit)
                    });
                }).ConfigureAwait(false);
            op.Successful = true;
        }

        /// <inheritdoc/>
        public async STT.Task ReleaseAsync(string key, string operationId, CancellationToken cancellationToken)
        {
            using var op = SnTrace.Database.StartOperation("MsSqlExclusiveLockDataProvider: " +
                "Refresh(key: {0}, operationId: {1}", key, operationId);
            using var ctx = MainProvider.CreateDataContext(cancellationToken);
            await ctx.ExecuteNonQueryAsync(ReleaseScript, cmd =>
            {
                cmd.Parameters.AddRange(new[]
                {
                    ctx.CreateParameter("@Name", DbType.String, key)
                });
            }).ConfigureAwait(false);
            op.Successful = true;
        }

        /// <inheritdoc/>
        public async STT.Task<bool> IsLockedAsync(string key, string operationId, CancellationToken cancellationToken)
        {
            using var op = SnTrace.Database.StartOperation("MsSqlExclusiveLockDataProvider: " +
                "IsLocked(key: {0}, operationId: {1}", key, operationId);
            using var ctx = MainProvider.CreateDataContext(cancellationToken);
            var result = await ctx.ExecuteScalarAsync(IsLockedScript,
                cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        ctx.CreateParameter("@Name", DbType.String, key)
                    });
                }).ConfigureAwait(false);

            SnTrace.Database.Write("MsSqlExclusiveLockDataProvider: IsLocked result: {0}",
                result == null || result == DBNull.Value ? "[null]" : "LOCKED");
            op.Successful = true;
            return result != DBNull.Value && result != null;
        }

        /// <inheritdoc/>
        public async STT.Task<bool> IsFeatureAvailable(CancellationToken cancellationToken)
        {
            using var op = SnTrace.Database.StartOperation("MsSqlExclusiveLockDataProvider: IsFeatureAvailable()");
            using var ctx = MainProvider.CreateDataContext(cancellationToken);
            var result = await ctx.ExecuteScalarAsync(
                    "IF OBJECT_ID('ExclusiveLocks', 'U') IS NOT NULL SELECT 1 ELSE SELECT 0")
                .ConfigureAwait(false);
            op.Successful = true;
            return 1 == (result == DBNull.Value ? 0 : (int)result);
        }

        /// <inheritdoc/>
        public async STT.Task ReleaseAllAsync(CancellationToken cancellationToken)
        {
            using var op = SnTrace.Database.StartOperation("MsSqlExclusiveLockDataProvider: ReleaseAll()");
            using var ctx = MainProvider.CreateDataContext(cancellationToken);
            await ctx.ExecuteNonQueryAsync("DELETE FROM ExclusiveLocks").ConfigureAwait(false);
            op.Successful = true;
        }

        /* ====================================================================================== INSTALLATION SCRIPTS */

        public static readonly string DropScript = @"/****** Index [IX_ExclusiveLock_Name_TimeLimit] ******/
DROP INDEX IF EXISTS [IX_ExclusiveLock_Name_TimeLimit] ON [dbo].[ExclusiveLocks]

/****** Index [IX_ExclusiveLock_Name] ******/
DROP INDEX IF EXISTS [IX_ExclusiveLock_Name] ON [dbo].[ExclusiveLocks]

/****** Table [dbo].[ExclusiveLocks] ******/
DROP TABLE IF EXISTS [dbo].[ExclusiveLocks]
";

        public static readonly string CreationScript = @"/****** Table [dbo].[ExclusiveLocks] ******/
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ExclusiveLocks]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[ExclusiveLocks](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](450) NOT NULL,
	[OperationId] [nvarchar](450) NULL,
	[TimeLimit] [datetime2](7) NULL,
 CONSTRAINT [PK_ExclusiveLocks] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
END

/****** Index [IX_ExclusiveLock_Name] ******/
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ExclusiveLocks]') AND name = N'IX_ExclusiveLock_Name')
CREATE UNIQUE NONCLUSTERED INDEX [IX_ExclusiveLock_Name] ON [dbo].[ExclusiveLocks]
(
	[Name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]

/****** Index [IX_ExclusiveLock_Name_TimeLimit] ******/
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ExclusiveLocks]') AND name = N'IX_ExclusiveLock_Name_TimeLimit')
CREATE NONCLUSTERED INDEX [IX_ExclusiveLock_Name_TimeLimit] ON [dbo].[ExclusiveLocks]
(
	[Name] ASC
)
INCLUDE ( 	[TimeLimit]) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
";
    }
}
