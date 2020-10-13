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
        private RelationalDataProviderBase _dataProvider;
        private RelationalDataProviderBase MainProvider => _dataProvider ??= (_dataProvider = (RelationalDataProviderBase)DataStore.DataProvider);

        /// <inheritdoc/>
        public async Task<bool> AcquireAsync(ExclusiveBlockContext context, string key, DateTime timeLimit, CancellationToken cancellationToken)
        {
            Trace.WriteLine($"SnTrace: DATA: START: AcquireAsync {key} #{context.OperationId}");
            using (var ctx = MainProvider.CreateDataContext(cancellationToken))
            {
                var sql = @"-- MsSqlExclusiveLockDataProvider.Acquire
DECLARE @Now DATETIME2 SET @Now = GETUTCDATE()
DECLARE @Id AS INT
SELECT @Id = Id FROM [ExclusiveLocks] (NOLOCK) WHERE [Name] = @Name AND [TimeLimit] > @Now
IF @Id IS NULL BEGIN
    SET TRANSACTION ISOLATION LEVEL SERIALIZABLE
    BEGIN TRANSACTION
        SELECT @Id = Id FROM [ExclusiveLocks] (TABLOCKX) WHERE [Name] = @Name AND [TimeLimit] > @Now
        IF @Id IS NULL BEGIN
            SELECT @Id = Id FROM [ExclusiveLocks] (NOLOCK) WHERE [Name] = @Name --AND [TimeLimit] <= @Now
            IF @Id IS NOT NULL
    			DELETE FROM [ExclusiveLocks] WHERE Id = @Id
            INSERT INTO [ExclusiveLocks] ([Name], [TimeLimit])
	            OUTPUT INSERTED.Id
	            VALUES (@Name, @TimeLimit)
        END
    COMMIT TRANSACTION
END
";
                var result = await ctx.ExecuteScalarAsync(sql, cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        ctx.CreateParameter("@Name", DbType.String, key),
                        ctx.CreateParameter("@TimeLimit", DbType.DateTime2, timeLimit)
                    });
                }).ConfigureAwait(false);
                Trace.WriteLine($"SnTrace: DATA:  END: AcquireAsync: {key} #{context.OperationId} " +
                                $"{(result == null ? "[null]" : "ACQUIRED " + result)}");
                return result != DBNull.Value && result != null;
            }
        }

        /// <inheritdoc/>
        public async Task RefreshAsync(string key, DateTime newTimeLimit, CancellationToken cancellationToken)
        {
            Trace.WriteLine($"SnTrace: DATA: START: RefreshAsync {key} #?");
            using (var ctx = MainProvider.CreateDataContext(cancellationToken))
            {
                await ctx.ExecuteNonQueryAsync(
                    "UPDATE ExclusiveLocks SET [TimeLimit] = @TimeLimit WHERE [Name] = @Name",
                    cmd =>
                    {
                        cmd.Parameters.AddRange(new[]
                        {
                            ctx.CreateParameter("@Name", DbType.String, key),
                            ctx.CreateParameter("@TimeLimit", DbType.DateTime2, newTimeLimit)
                        });
                    }).ConfigureAwait(false);
            }
            Trace.WriteLine($"SnTrace: DATA:  END: RefreshAsync {key} #?");
        }

        /// <inheritdoc/>
        public async Task ReleaseAsync(string key, CancellationToken cancellationToken)
        {
            Trace.WriteLine($"SnTrace: DATA: START: ReleaseAsync {key} #?");
            using (var ctx = MainProvider.CreateDataContext(cancellationToken))
            {
                await ctx.ExecuteNonQueryAsync("DELETE FROM ExclusiveLocks WHERE @Name = [Name]",
                    cmd =>
                    {
                        cmd.Parameters.AddRange(new[]
                        {
                            ctx.CreateParameter("@Name", DbType.String, key)
                        });
                    }).ConfigureAwait(false);
            }
            Trace.WriteLine($"SnTrace: DATA:  END: ReleaseAsync {key} #?");
        }

        /// <inheritdoc/>
        public async Task<bool> IsLockedAsync(string key, CancellationToken cancellationToken)
        {
            Trace.WriteLine($"SnTrace: DATA: START: IsLockedAsync {key} #?");
            using (var ctx = MainProvider.CreateDataContext(cancellationToken))
            {
                var result = await ctx.ExecuteScalarAsync(
                    "SELECT Id FROM ExclusiveLocks (NOLOCK) WHERE @Name = [Name] AND [TimeLimit] > GETUTCDATE()",
                    cmd =>
                    {
                        cmd.Parameters.AddRange(new[]
                        {
                            ctx.CreateParameter("@Name", DbType.String, key)
                        });
                    }).ConfigureAwait(false);

                Trace.WriteLine($"SnTrace: DATA:  END: IsLockedAsync {key} #?: {(result == null || result == DBNull.Value ? "[null]" : "LOCKED")}");
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


        public static readonly string DropScript = @"/****** Object:  Index [IX_ExclusiveLock_Name_TimeLimit] ******/
DROP INDEX IF EXISTS [IX_ExclusiveLock_Name_TimeLimit] ON [dbo].[ExclusiveLocks]
GO
/****** Object:  Table [dbo].[ExclusiveLocks] ******/
DROP TABLE IF EXISTS [dbo].[ExclusiveLocks]
GO
";
        public static readonly string CreationScript = @"/****** Object:  Table [dbo].[ExclusiveLocks] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ExclusiveLocks]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[ExclusiveLocks](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](450) NOT NULL,
	[TimeLimit] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_ExclusiveLocks] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
END
GO
SET ANSI_PADDING ON

GO
/****** Object:  Index [IX_ExclusiveLock_Name_TimeLimit] ******/
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
