using System;
using System.Data;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
// ReSharper disable ConvertToUsingDeclaration

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data.MsSqlClient
{
    /// <summary> 
    /// This is an MS SQL implementation of the <see cref="IExclusiveLockDataProviderExtension"/> interface.
    /// It requires the main data provider to be a <see cref="RelationalDataProviderBase"/>.
    /// </summary>
    public class MsSqlExclusiveLockDataProvider : IExclusiveLockDataProviderExtension
    {
        private string _acquireScript = @"-- MsSqlExclusiveLockDataProvider.Acquire
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

        private string _refreshScript = @"UPDATE ExclusiveLocks SET [TimeLimit] = @TimeLimit WHERE [Name] = @Name";

        private string _releaseScript = @"UPDATE ExclusiveLocks SET [OperationId] = NULL WHERE @Name = [Name]";

        private string _isLockedScript = @"SELECT Id FROM ExclusiveLocks (NOLOCK)
WHERE [Name] = @Name AND [OperationId] IS NOT NULL AND [TimeLimit] > GETUTCDATE()";

        private RelationalDataProviderBase _dataProvider;
        private RelationalDataProviderBase MainProvider =>
            _dataProvider ??= (_dataProvider = (RelationalDataProviderBase)DataStore.DataProvider);

        /// <inheritdoc/>
        public async Task<bool> AcquireAsync(string key, string operationId, DateTime timeLimit,
            CancellationToken cancellationToken)
        {
            Trace.WriteLine($"SnTrace: DATA: START: AcquireAsync {key} #{operationId}");
            using (var ctx = MainProvider.CreateDataContext(cancellationToken))
            {
                var result = await ctx.ExecuteScalarAsync(_acquireScript, cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        ctx.CreateParameter("@Name", DbType.String, key),
                        ctx.CreateParameter("@OperationId", DbType.String, operationId),
                        ctx.CreateParameter("@TimeLimit", DbType.DateTime2, timeLimit)
                    });
                }).ConfigureAwait(false);
                Trace.WriteLine($"SnTrace: DATA:  END: AcquireAsync: {key} #{operationId} " +
                                $"{(result == null ? "[null]" : "ACQUIRED " + result)}");
                return result != DBNull.Value && result != null;
            }
        }

        /// <inheritdoc/>
        public async Task RefreshAsync(string key, string operationId, DateTime newTimeLimit,
            CancellationToken cancellationToken)
        {
            Trace.WriteLine($"SnTrace: DATA: START: RefreshAsync {key} #{operationId}");
            using (var ctx = MainProvider.CreateDataContext(cancellationToken))
            {
                await ctx.ExecuteNonQueryAsync(_refreshScript,
                    cmd =>
                    {
                        cmd.Parameters.AddRange(new[]
                        {
                            ctx.CreateParameter("@Name", DbType.String, key),
                            ctx.CreateParameter("@TimeLimit", DbType.DateTime2, newTimeLimit)
                        });
                    }).ConfigureAwait(false);
            }
            Trace.WriteLine($"SnTrace: DATA:  END: RefreshAsync {key} #{operationId}");
        }

        /// <inheritdoc/>
        public async Task ReleaseAsync(string key, string operationId, CancellationToken cancellationToken)
        {
            Trace.WriteLine($"SnTrace: DATA: START: ReleaseAsync {key} #{operationId}");
            using (var ctx = MainProvider.CreateDataContext(cancellationToken))
            {
                await ctx.ExecuteNonQueryAsync(_releaseScript,
                    cmd =>
                    {
                        cmd.Parameters.AddRange(new[]
                        {
                            ctx.CreateParameter("@Name", DbType.String, key)
                        });
                    }).ConfigureAwait(false);
            }
            Trace.WriteLine($"SnTrace: DATA:  END: ReleaseAsync {key} #{operationId}");
        }

        /// <inheritdoc/>
        public async Task<bool> IsLockedAsync(string key, string operationId, CancellationToken cancellationToken)
        {
            Trace.WriteLine($"SnTrace: DATA: START: IsLockedAsync {key} #{operationId}");
            using (var ctx = MainProvider.CreateDataContext(cancellationToken))
            {
                var result = await ctx.ExecuteScalarAsync(_isLockedScript,
                    cmd =>
                    {
                        cmd.Parameters.AddRange(new[]
                        {
                            ctx.CreateParameter("@Name", DbType.String, key)
                        });
                    }).ConfigureAwait(false);

                Trace.WriteLine($"SnTrace: DATA:  END: IsLockedAsync {key} #{operationId}: {(result == null || result == DBNull.Value ? "[null]" : "LOCKED")}");
                return result != DBNull.Value && result != null;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> IsFeatureAvailable(CancellationToken cancellationToken)
        {
            Trace.WriteLine($"SnTrace: DATA: START: IsFeatureAvailable");
            using (var ctx = MainProvider.CreateDataContext(cancellationToken))
            {
                var result = await ctx.ExecuteScalarAsync(
                    "IF OBJECT_ID('ExclusiveLocks', 'U') IS NOT NULL SELECT 1 ELSE SELECT 0")
                    .ConfigureAwait(false);

                Trace.WriteLine($"SnTrace: DATA:  END: IsFeatureAvailable");
                return 1 == (result == DBNull.Value ? 0 : (int)result);
            }
        }

        /// <inheritdoc/>
        public async Task ReleaseAllAsync(CancellationToken cancellationToken)
        {
            using (var ctx = MainProvider.CreateDataContext(cancellationToken))
            {
                await ctx.ExecuteNonQueryAsync("DELETE FROM ExclusiveLocks").ConfigureAwait(false);
            }
        }

        /* ====================================================================================== INSTALLATION SCRIPTS */

        public static readonly string DropScript = @"/****** Index [IX_ExclusiveLock_Name_TimeLimit] ******/
DROP INDEX IF EXISTS [IX_ExclusiveLock_Name_TimeLimit] ON [dbo].[ExclusiveLocks]
GO
/****** Index [IX_ExclusiveLock_Name] ******/
DROP INDEX IF EXISTS [IX_ExclusiveLock_Name] ON [dbo].[ExclusiveLocks]
GO
/****** Table [dbo].[ExclusiveLocks] ******/
DROP TABLE IF EXISTS [dbo].[ExclusiveLocks]
GO
";

        public static readonly string CreationScript = @"/****** Table [dbo].[ExclusiveLocks] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
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
GO
SET ANSI_PADDING ON

GO
/****** Index [IX_ExclusiveLock_Name] ******/
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ExclusiveLocks]') AND name = N'IX_ExclusiveLock_Name')
CREATE UNIQUE NONCLUSTERED INDEX [IX_ExclusiveLock_Name] ON [dbo].[ExclusiveLocks]
(
	[Name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
SET ANSI_PADDING ON

GO
/****** Index [IX_ExclusiveLock_Name_TimeLimit] ******/
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ExclusiveLocks]') AND name = N'IX_ExclusiveLock_Name_TimeLimit')
CREATE NONCLUSTERED INDEX [IX_ExclusiveLock_Name_TimeLimit] ON [dbo].[ExclusiveLocks]
(
	[Name] ASC
)
INCLUDE ( 	[TimeLimit]) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
";
    }
}
