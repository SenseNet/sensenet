using System.Collections.Generic;
using System.Data;
using SenseNet.ContentRepository.Storage.Data;

// ReSharper disable once CheckNamespace
namespace SenseNet.Packaging.Steps.Internal
{
    public partial class ReindexBinaries
    {
        private static class DataHandler
        {
            internal static void InstallTables()
            {
                // proc CreateTable
                using (var cmd = DataProvider.CreateDataProcedure(SqlScripts.CreateTables))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.ExecuteNonQuery();
                }
            }
            internal static void StartBackgroundTasks()
            {
                using (var cmd = DataProvider.CreateDataProcedure(SqlScripts.CreateTasks))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.ExecuteNonQuery();
                }
            }

            internal static int[] AssignTasks(int taskCount, int timeoutInMinutes)
            {
                var result = new List<int>();

                // proc AssignTasks(@AssignedTaskCount int, @TimeOutInMinutes int)
                using (var cmd = DataProvider.CreateDataProcedure(SqlScripts.AssignTasks))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.Add(CreateParameter("@AssignedTaskCount", taskCount, DbType.Int32));
                    cmd.Parameters.Add(CreateParameter("@TimeOutInMinutes", timeoutInMinutes, DbType.Int32));

                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                            result.Add(reader.GetInt32(0));
                }

                return result.ToArray();
            }

            internal static void FinishTask(int versionId)
            {
                // proc FinishTask(@VersionId int)
                using (var cmd = DataProvider.CreateDataProcedure(SqlScripts.FinishTask))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.Add(CreateParameter("@VersionId", versionId, DbType.Int32));

                    cmd.ExecuteNonQuery();
                }
            }

            /* ========================================================================================= */

            private static IDbDataParameter CreateParameter(string name, object value, DbType dbType)
            {
                var prm = DataProvider.CreateParameter();
                prm.ParameterName = name;
                prm.DbType = dbType;
                prm.Value = value;
                return prm;
            }

            /* ========================================================================================= */

            public static void CreateTempTask(int versionId, int rank)
            {
                // proc CreateTempTask(@VersionId int, @Rank int)
                using (var cmd = DataProvider.CreateDataProcedure(SqlScripts.CreateTempTask))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.Add(CreateParameter("@VersionId", versionId, DbType.Int32));
                    cmd.Parameters.Add(CreateParameter("@Rank", rank, DbType.Int32));

                    cmd.ExecuteNonQuery();
                }
            }

            public static IEnumerable<int> GetAllNodeIds(int from)
            {
                var result = new List<int>();

                // proc GetAllNodeIds(@From int)
                using (var cmd = DataProvider.CreateDataProcedure(SqlScripts.GetAllNodeIds))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.Add(CreateParameter("@From", from, DbType.Int32));

                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                            result.Add(reader.GetInt32(0));
                }

                return result;
            }
        }

        private static class SqlScripts
        {
            private const string TempTableName = "Maintenance.BinaryReindexingTemp";
            private const string TaskTableName = "Maintenance.BinaryReindexingTasks";

            #region CreateTables
            internal static readonly string CreateTables =
                $@"
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[{TempTableName}]') AND type in (N'U'))
	DROP TABLE [dbo].[{TempTableName}]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[{TempTableName}](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[VersionId] [int] NOT NULL,
	[Rank] [int] NOT NULL,
 CONSTRAINT [PK_{TempTableName}] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_MFRTemp_Rank] ON [dbo].[{TempTableName}]
(
	[Rank] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO

/*------------------------------------*/

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[{TaskTableName}]') AND type in (N'U'))
	DROP TABLE [dbo].[{TaskTableName}]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
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
GO
CREATE NONCLUSTERED INDEX [IX_MFRTask_EndDate] ON [dbo].[{TaskTableName}]
(
	[EndDate] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_MFRTask_StartDate] ON [dbo].[{TaskTableName}]
(
	[StartDate] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_MFRTask_VersionId] ON [dbo].[{TaskTableName}]
(
	[VersionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
";
            #endregion

            #region GetAllNodeIds(@From int)
            internal static readonly string GetAllNodeIds =
                @"SELECT NodeId FROM Nodes WHERE NodeId >= @From ORDER BY NodeId
";
            #endregion

            #region CreateTempTask(@VersionId int, @Rank int)
            internal static readonly string CreateTempTask = $@"NSERT INTO {TempTableName} (VersionId, Rank) VALUES (@VersionId, @Rank)
";
            #endregion

            #region CreateTasks
            internal static readonly string CreateTasks = $@"
INSERT INTO {TaskTableName}
	SELECT VersionId, NULL, NULL FROM {TempTableName} ORDER BY [Rank]

TRUNCATE TABLE {TempTableName}
";
            #endregion

            #region AssignTasks(@AssignedTaskCount int, @TimeOutInMinutes int)
            internal static readonly string AssignTasks =
                $@"UPDATE {TaskTableName} WITH (TABLOCK) SET StartDate = GETUTCDATE()
OUTPUT INSERTED.VersionId, INSERTED.StartDate
WHERE VersionId IN (SELECT TOP (@AssignedTaskCount) VersionId FROM {TaskTableName}
	WHERE EndDate IS NULL AND (StartDate IS NULL OR StartDate < DATEADD(MINUTES, -@TimeOutInMinutes, GETUTCDATE())))
";
            #endregion

            #region FinishTask(@VersionId int)
            internal static readonly string FinishTask =
                $@"UPDATE {TaskTableName} WITH (TABLOCK) SET EndDate = GETUTCDATE()
WHERE VersionId = @VersionId
";
            #endregion

        }
    }
}
