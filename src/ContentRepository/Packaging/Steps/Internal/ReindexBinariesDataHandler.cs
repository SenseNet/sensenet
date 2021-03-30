﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;

// ReSharper disable once CheckNamespace
namespace SenseNet.Packaging.Steps.Internal
{
    public partial class ReindexBinaries
    {
        //TODO: [DIREF] get options from DI through constructor
        private static class DataHandler
        {
            internal static void InstallTables(CancellationToken cancellationToken)
            {
                using (var ctx = new MsSqlDataContext(ConnectionStrings.ConnectionString, DataOptions.GetLegacyConfiguration(), cancellationToken))
                    ctx.ExecuteNonQueryAsync(SqlScripts.CreateTables).GetAwaiter().GetResult();
            }
            internal static void StartBackgroundTasks(CancellationToken cancellationToken)
            {
                using (var ctx = new MsSqlDataContext(ConnectionStrings.ConnectionString, DataOptions.GetLegacyConfiguration(), cancellationToken))
                    ctx.ExecuteNonQueryAsync(SqlScripts.CreateTasks).GetAwaiter().GetResult();
            }

            internal class AssignedTaskResult
            {
                public int[] VersionIds;
                public int RemainingTaskCount;
            }
            internal static AssignedTaskResult AssignTasks(int taskCount, int timeoutInMinutes, CancellationToken cancellationToken)
            {
                var result = new List<int>();
                int remainingTasks = 0;
                using (var ctx = new MsSqlDataContext(ConnectionStrings.ConnectionString, DataOptions.GetLegacyConfiguration(), cancellationToken))
                {
                    ctx.ExecuteReaderAsync(SqlScripts.AssignTasks, cmd =>
                    {
                        cmd.Parameters.Add("@AssignedTaskCount", SqlDbType.Int, taskCount);
                        cmd.Parameters.Add("@TimeOutInMinutes", SqlDbType.Int, timeoutInMinutes);
                    }, async (reader, cancel) =>
                    {
                        while (await reader.ReadAsync(cancel).ConfigureAwait(false))
                            result.Add(reader.GetInt32(0));
                        await reader.NextResultAsync(cancel).ConfigureAwait(false);

                        await reader.ReadAsync(cancel).ConfigureAwait(false);
                        remainingTasks = reader.GetInt32(0);

                        return Task.FromResult(0);
                    }).GetAwaiter().GetResult();
                }

                return new AssignedTaskResult {VersionIds = result.ToArray(), RemainingTaskCount = remainingTasks};
            }

            internal static void FinishTask(int versionId, CancellationToken cancellationToken)
            {
                using (var ctx = new MsSqlDataContext(ConnectionStrings.ConnectionString, DataOptions.GetLegacyConfiguration(), cancellationToken))
                    ctx.ExecuteNonQueryAsync(SqlScripts.FinishTask, cmd =>
                    {
                        cmd.Parameters.Add("@VersionId", SqlDbType.Int, versionId);
                    }).GetAwaiter().GetResult();
            }

            /* ========================================================================================= */

            public static void CreateTempTask(int versionId, int rank, CancellationToken cancellationToken)
            {
                using (var ctx = new MsSqlDataContext(ConnectionStrings.ConnectionString, DataOptions.GetLegacyConfiguration(), cancellationToken))
                    ctx.ExecuteNonQueryAsync(SqlScripts.FinishTask, cmd =>
                    {
                        cmd.Parameters.Add("@VersionId", SqlDbType.Int, versionId);
                        cmd.Parameters.Add("@Rank", SqlDbType.Int, rank);
                    }).GetAwaiter().GetResult();
            }

            public static List<int> GetAllNodeIds(CancellationToken cancellationToken)
            {
                using (var ctx = new MsSqlDataContext(ConnectionStrings.ConnectionString, DataOptions.GetLegacyConfiguration(), cancellationToken))
                {
                    return ctx.ExecuteReaderAsync(SqlScripts.GetAllNodeIds, (reader, cancel) =>
                    {
                        var result = new List<int>();
                        while (reader.Read())
                            result.Add(reader.GetInt32(0));
                        return Task.FromResult(result);
                    }).GetAwaiter().GetResult();
                }
            }

            public static void DropTables(CancellationToken cancellationToken)
            {
                using (var ctx = new MsSqlDataContext(ConnectionStrings.ConnectionString, DataOptions.GetLegacyConfiguration(), cancellationToken))
                    ctx.ExecuteNonQueryAsync(SqlScripts.DropTables).GetAwaiter().GetResult();
            }

            public static bool CheckFeature(CancellationToken cancellationToken)
            {
                try
                {
                    using (var ctx = new MsSqlDataContext(ConnectionStrings.ConnectionString, DataOptions.GetLegacyConfiguration(), cancellationToken))
                    {
                        var result = ctx.ExecuteScalarAsync(SqlScripts.CheckFeature).GetAwaiter().GetResult();
                        return Convert.ToInt32(result) != 0;
                    }
                }
                catch (AggregateException ae)
                {
                    if (ae.InnerException is NotSupportedException)
                        return false;
                    throw;
                }
                catch (NotSupportedException)
                {
                    return false;
                }
            }

            public static DateTime LoadTimeLimit(CancellationToken cancellationToken)
            {
                using (var ctx = new MsSqlDataContext(ConnectionStrings.ConnectionString, DataOptions.GetLegacyConfiguration(), cancellationToken))
                {
                    var result = ctx.ExecuteScalarAsync(SqlScripts.SelectTimeLimit).GetAwaiter().GetResult();
                    var timeLimit = Convert.ToDateTime(result).ToUniversalTime();
                    Tracer.Write("UTC timelimit: " + timeLimit.ToString("yyyy-MM-dd HH:mm:ss"));
                    return timeLimit;
                }
            }
        }

        private static class SqlScripts
        {
            private const string TempTableName = "Maintenance.BinaryReindexingTemp";
            private const string TaskTableName = "Maintenance.BinaryReindexingTasks";

            #region CreateTables
            internal static readonly string CreateTables =
                $@"/****** Object:  Table [dbo].[{TaskTableName}] ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[{TaskTableName}]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[{TaskTableName}](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[VersionId] [int] NOT NULL,
	[StartDate] [datetime] NULL,
	[EndDate] [datetime] NULL,
 CONSTRAINT [PK_{TaskTableName}] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
END
/****** Object:  Table [dbo].[{TempTableName}] ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[{TempTableName}]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[{TempTableName}](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[VersionId] [int] NOT NULL,
	[Rank] [int] NOT NULL,
 CONSTRAINT [PK_{TempTableName}] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
END
/****** Object:  Index [IX_MBRTask_EndDate] ******/
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[{TaskTableName}]') AND name = N'IX_MBRTask_EndDate')
CREATE NONCLUSTERED INDEX [IX_MBRTask_EndDate] ON [dbo].[{TaskTableName}]
(
	[EndDate] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
/****** Object:  Index [IX_MBRTask_StartDate] ******/
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[{TaskTableName}]') AND name = N'IX_MBRTask_StartDate')
CREATE NONCLUSTERED INDEX [IX_MBRTask_StartDate] ON [dbo].[{TaskTableName}]
(
	[StartDate] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
/****** Object:  Index [IX_MBRTask_VersionId] ******/
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[{TaskTableName}]') AND name = N'IX_MBRTask_VersionId')
CREATE NONCLUSTERED INDEX [IX_MBRTask_VersionId] ON [dbo].[{TaskTableName}]
(
	[VersionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
/****** Object:  Index [IX_MBRTemp_Rank] ******/
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[{TempTableName}]') AND name = N'IX_MBRTemp_Rank')
CREATE NONCLUSTERED INDEX [IX_MBRTemp_Rank] ON [dbo].[{TempTableName}]
(
	[Rank] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
";
            #endregion

            #region GetAllNodeIds(@From int)
            internal static readonly string GetAllNodeIds =
                @"SELECT NodeId FROM Nodes
";
            #endregion

            #region CreateTempTask(@VersionId int, @Rank int)
            internal static readonly string CreateTempTask = $@"INSERT INTO [{TempTableName}] (VersionId, Rank) VALUES (@VersionId, @Rank)
";
            #endregion

            #region CreateTasks
            internal static readonly string CreateTasks = $@"
INSERT INTO [{TaskTableName}]
	SELECT VersionId, NULL, NULL FROM [{TempTableName}] ORDER BY [Rank]

TRUNCATE TABLE [{TempTableName}]
";
            #endregion

            #region AssignTasks(@AssignedTaskCount int, @TimeOutInMinutes int)
            internal static readonly string AssignTasks =
                $@"UPDATE [{TaskTableName}] WITH (TABLOCK) SET StartDate = GETUTCDATE()
OUTPUT INSERTED.VersionId, INSERTED.StartDate
WHERE VersionId IN (SELECT TOP (@AssignedTaskCount) VersionId FROM [{TaskTableName}]
	WHERE EndDate IS NULL AND (StartDate IS NULL OR StartDate < DATEADD(MINUTE, -@TimeOutInMinutes, GETUTCDATE())))

SELECT COUNT(0) FROM [{TaskTableName}] WHERE EndDate IS NULL
";
            #endregion

            #region FinishTask(@VersionId int)
            internal static readonly string FinishTask =
                $@"UPDATE [{TaskTableName}] SET EndDate = GETUTCDATE()
WHERE VersionId = @VersionId
";
            #endregion

            #region DropTables
            internal static readonly string DropTables = $@"/****** Object:  Table [dbo].[{TempTableName}] ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[{TempTableName}]') AND type in (N'U'))
	DROP TABLE [dbo].[Maintenance.BinaryReindexingTemp]
/****** Object:  Table [dbo].[Maintenance.BinaryReindexingTasks] ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[{TaskTableName}]') AND type in (N'U'))
	DROP TABLE [dbo].[{TaskTableName}]
";
            #endregion

            #region CheckFeature
            internal static readonly string CheckFeature = $@"IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[{TaskTableName}]') AND type in (N'U'))
	SELECT 1
ELSE
	SELECT 0
";
            #endregion

            #region SelectTimeLimit
            internal static readonly string SelectTimeLimit = $@"SELECT create_date FROM sys.tables WHERE name='{TaskTableName}'
";
            #endregion
        }
    }
}
