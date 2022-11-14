using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;
using SenseNet.Diagnostics;

namespace SenseNet.Storage.Data.MsSqlClient
{
    public class MsSqlStatisticalDataProvider : IStatisticalDataProvider
    {
        protected DataOptions DataOptions { get; }
        private string ConnectionString { get; }

        public MsSqlStatisticalDataProvider(IOptions<DataOptions> options, IOptions<ConnectionStringOptions> connectionOptions)
        {
            DataOptions = options?.Value ?? new DataOptions();

            if (connectionOptions == null)
                throw new ArgumentNullException(nameof(connectionOptions));
            ConnectionString = connectionOptions.Value.Repository;
            if (ConnectionString == null)
                throw new ArgumentException($"The {connectionOptions.Value.Repository} cannot be null");
        }

        private static readonly string WriteDataScript = @"-- MsSqlStatisticalDataProvider.WriteData
INSERT INTO StatisticalData
    (DataType, CreationTime, WrittenTime, Duration, RequestLength, ResponseLength, ResponseStatusCode, [Url],
	 TargetId, ContentId, EventName, ErrorMessage, GeneralData)
    VALUES
    (@DataType, @CreationTime, @WrittenTime, @Duration, @RequestLength, @ResponseLength, @ResponseStatusCode, @Url,
	 @TargetId, @ContentId, @EventName, @ErrorMessage, @GeneralData)
";
        public async Task WriteDataAsync(IStatisticalDataRecord data, CancellationToken cancellation)
        {
            using var op = SnTrace.Database.StartOperation("MsSqlStatisticalDataProvider: " +
                "WriteData: DataType: {0}", data.DataType);
            using var ctx = new MsSqlDataContext(ConnectionString, DataOptions, cancellation);
            await ctx.ExecuteNonQueryAsync(WriteDataScript, cmd =>
            {
                var now = DateTime.UtcNow;
                cmd.Parameters.AddRange(new[]
                {
                    ctx.CreateParameter("@DataType", SqlDbType.NVarChar, 50,  data.DataType),
                    ctx.CreateParameter("@WrittenTime", SqlDbType.DateTime2, now),
                    ctx.CreateParameter("@CreationTime", SqlDbType.DateTime2, data.CreationTime ?? now),
                    ctx.CreateParameter("@Duration", SqlDbType.BigInt, (object)data.Duration?.Ticks ?? DBNull.Value),
                    ctx.CreateParameter("@RequestLength", SqlDbType.BigInt, (object)data.RequestLength ?? DBNull.Value),
                    ctx.CreateParameter("@ResponseLength", SqlDbType.BigInt, (object)data.ResponseLength ?? DBNull.Value),
                    ctx.CreateParameter("@ResponseStatusCode", SqlDbType.Int, (object)data.ResponseStatusCode ?? DBNull.Value),
                    ctx.CreateParameter("@Url", SqlDbType.NVarChar, 1000, (object)data.Url ?? DBNull.Value),
                    ctx.CreateParameter("@TargetId", SqlDbType.Int, (object)data.TargetId ?? DBNull.Value),
                    ctx.CreateParameter("@ContentId", SqlDbType.Int, (object)data.ContentId ?? DBNull.Value),
                    ctx.CreateParameter("@EventName", SqlDbType.NVarChar, 50, (object)data.EventName ?? DBNull.Value),
                    ctx.CreateParameter("@ErrorMessage", SqlDbType.NVarChar, 500, (object)data.ErrorMessage ?? DBNull.Value),
                    ctx.CreateParameter("@GeneralData", SqlDbType.NVarChar, (object)data.GeneralData ?? DBNull.Value),
                });
            }).ConfigureAwait(false);
            op.Successful = true;
        }

        private static readonly string LoadUsageListScript = @"-- MsSqlStatisticalDataProvider.LoadUsageList
SELECT TOP(@Take) * FROM StatisticalData
WHERE DataType = @DataType AND CreationTime < @EndTimeExclusive
ORDER BY CreationTime DESC
";
        private static readonly string LoadUsageListByTargetIdsScript = @"-- MsSqlStatisticalDataProvider.LoadUsageListByTargetIds
SELECT TOP(@Take) * FROM StatisticalData
WHERE DataType = @DataType AND CreationTime < @EndTimeExclusive AND TargetId IN ({0})
ORDER BY CreationTime DESC
";
        public async Task<IEnumerable<IStatisticalDataRecord>> LoadUsageListAsync(string dataType, int[] relatedTargetIds, DateTime endTimeExclusive, int count, CancellationToken cancellation)
        {
            using var op = SnTrace.Database.StartOperation("MsSqlStatisticalDataProvider: " +
                "LoadUsageList(dataType: {0}, relatedTargetIds: {1}, endTimeExclusive: {2:yyyy-MM-dd HH:mm:ss.fffff}, count: {3})",
                dataType, SnTraceTools.ConvertToString(relatedTargetIds), endTimeExclusive, count);

            var sql = relatedTargetIds == null || relatedTargetIds.Length == 0
                ? LoadUsageListScript
                : string.Format(LoadUsageListByTargetIdsScript,
                    string.Join(", ", relatedTargetIds.Select(x => x.ToString())));
            using var ctx = new MsSqlDataContext(ConnectionString, DataOptions, cancellation);
            var records = new List<IStatisticalDataRecord>();
            await ctx.ExecuteReaderAsync(sql, cmd =>
            {
                cmd.Parameters.AddRange(new[]
                {
                    ctx.CreateParameter("@Take", DbType.Int32, count),
                    ctx.CreateParameter("@DataType", DbType.String, dataType),
                    ctx.CreateParameter("@EndTimeExclusive", DbType.DateTime2, endTimeExclusive),
                });
            }, async (reader, cancel) =>
            {
                while (await reader.ReadAsync(cancel))
                    records.Add(GetStatisticalDataRecordFromReader(reader));
                return true;
            }).ConfigureAwait(false);
            op.Successful = true;

            return records;
        }

        private static readonly string LoadAggregatedUsageScript = @"-- MsSqlStatisticalDataProvider.LoadAggregatedUsage
SELECT * FROM StatisticalAggregations
WHERE DataType = @DataType AND Resolution = @Resolution AND [Date] >= @StartTime AND [Date] < @EndTimeExclusive
ORDER BY Date
";
        public async Task<IEnumerable<Aggregation>> LoadAggregatedUsageAsync(string dataType, TimeResolution resolution, DateTime startTime, DateTime endTimeExclusive,
            CancellationToken cancellation)
        {
            using var op = SnTrace.Database.StartOperation("MsSqlStatisticalDataProvider: " +
                "LoadAggregatedUsage(dataType: {0}, resolution: {1}, startTime: {2:yyyy-MM-dd HH:mm:ss.fffff}, endTimeExclusive: {3:yyyy-MM-dd HH:mm:ss.fffff})",
                dataType, resolution, startTime, endTimeExclusive);

            using var ctx = new MsSqlDataContext(ConnectionString, DataOptions, cancellation);
            var aggregations = new List<Aggregation>();
            await ctx.ExecuteReaderAsync(LoadAggregatedUsageScript, cmd =>
            {
                cmd.Parameters.AddRange(new[]
                {
                    ctx.CreateParameter("@DataType", DbType.String, dataType),
                    ctx.CreateParameter("@Resolution", DbType.String, resolution.ToString()),
                    ctx.CreateParameter("@StartTime", DbType.DateTime2, startTime),
                    ctx.CreateParameter("@EndTimeExclusive", DbType.DateTime2, endTimeExclusive),
                });
            }, async (reader, cancel) => {
                while (await reader.ReadAsync(cancel))
                    aggregations.Add(GetAggregationFromReader(reader));
                return true;
            }).ConfigureAwait(false);
            op.Successful = true;

            return aggregations;
        }

        private static readonly string LoadFirstAggregationTimesByResolutionsScript = @"-- MsSqlStatisticalDataProvider.LoadFirstAggregationTimesByResolutions
DECLARE @Minute datetime2
DECLARE @Hour datetime2
DECLARE @Day datetime2
DECLARE @Month datetime2
SELECT TOP 1 @Minute = [Date] FROM StatisticalAggregations WHERE DataType = @DataType AND Resolution = 'Minute' ORDER BY Date
SELECT TOP 1 @Hour = [Date] FROM StatisticalAggregations WHERE DataType = @DataType AND Resolution = 'Hour' ORDER BY Date
SELECT TOP 1 @Day = [Date] FROM StatisticalAggregations WHERE DataType = @DataType AND Resolution = 'Day' ORDER BY Date
SELECT TOP 1 @Month = [Date] FROM StatisticalAggregations WHERE DataType = @DataType AND Resolution = 'Month' ORDER BY Date
SELECT  @Minute [Minute], @Hour [Hour], @Day [Day], @Month [Month]
";
        public async Task<DateTime?[]> LoadFirstAggregationTimesByResolutionsAsync(string dataType, CancellationToken cancellation)
        {
            using var op = SnTrace.Database.StartOperation("MsSqlStatisticalDataProvider: " +
                "LoadFirstAggregationTimesByResolutions(dataType: {0})", dataType);

            using var ctx = new MsSqlDataContext(ConnectionString, DataOptions, cancellation);
            var result = new DateTime?[4];
            await ctx.ExecuteReaderAsync(LoadFirstAggregationTimesByResolutionsScript, cmd =>
            {
                cmd.Parameters.AddRange(new[]
                {
                    ctx.CreateParameter("@DataType", DbType.String, dataType),
                });
            }, async (reader, cancel) => {
                if (await reader.ReadAsync(cancel))
                {
                    result[0] = reader.GetDateTimeUtcOrNull("Minute");
                    result[1] = reader.GetDateTimeUtcOrNull("Hour");
                    result[2] = reader.GetDateTimeUtcOrNull("Day");
                    result[3] = reader.GetDateTimeUtcOrNull("Month");
                }
                return true;
            }).ConfigureAwait(false);
            op.Successful = true;

            return result.ToArray();
        }

        private static readonly string LoadLastAggregationTimesByResolutionsScript = @"-- MsSqlStatisticalDataProvider.LoadFirstAggregationTimesByResolutions
DECLARE @Minute datetime2
DECLARE @Hour datetime2
DECLARE @Day datetime2
DECLARE @Month datetime2
SELECT TOP 1 @Minute = [Date] FROM StatisticalAggregations WHERE Resolution = 'Minute' ORDER BY Date DESC
SELECT TOP 1 @Hour = [Date] FROM StatisticalAggregations WHERE Resolution = 'Hour' ORDER BY Date DESC
SELECT TOP 1 @Day = [Date] FROM StatisticalAggregations WHERE Resolution = 'Day' ORDER BY Date DESC
SELECT TOP 1 @Month = [Date] FROM StatisticalAggregations WHERE Resolution = 'Month' ORDER BY Date DESC
SELECT  @Minute [Minute], @Hour [Hour], @Day [Day], @Month [Month]
";
        public async Task<DateTime?[]> LoadLastAggregationTimesByResolutionsAsync(CancellationToken cancel)
        {
            using var op = SnTrace.Database.StartOperation("MsSqlStatisticalDataProvider: " +
                "LoadLastAggregationTimesByResolutions()");

            using var ctx = new MsSqlDataContext(ConnectionString, DataOptions, cancel);
            var result = new DateTime?[4];
            await ctx.ExecuteReaderAsync(LoadLastAggregationTimesByResolutionsScript, async (reader, cancel) => {
                if (await reader.ReadAsync(cancel))
                {
                    result[0] = reader.GetDateTimeUtcOrNull("Minute");
                    result[1] = reader.GetDateTimeUtcOrNull("Hour");
                    result[2] = reader.GetDateTimeUtcOrNull("Day");
                    result[3] = reader.GetDateTimeUtcOrNull("Month");
                }
                return true;
            }).ConfigureAwait(false);
            op.Successful = true;

            return result.ToArray();
        }

        private static readonly string EnumerateDataScript = @"-- MsSqlStatisticalDataProvider.EnumerateData
SELECT * FROM StatisticalData
WHERE DataType = @DataType AND CreationTime >= @StartTime AND CreationTime < @EndTimeExclusive
ORDER BY CreationTime
";
        public async Task EnumerateDataAsync(string dataType, DateTime startTime, DateTime endTimeExclusive,
            Action<IStatisticalDataRecord> aggregatorCallback,
            CancellationToken cancellation)
        {
            using var op = SnTrace.Database.StartOperation("MsSqlStatisticalDataProvider: " +
                "EnumerateData(dataType: {0}, startTime: {1:yyyy-MM-dd HH:mm:ss.fffff}, endTimeExclusive: {2:yyyy-MM-dd HH:mm:ss.fffff})",
                dataType, startTime, endTimeExclusive);
            using var ctx = new MsSqlDataContext(ConnectionString, DataOptions, cancellation);
            await ctx.ExecuteReaderAsync(EnumerateDataScript, cmd =>
            {
                cmd.Parameters.AddRange(new[]
                {
                    ctx.CreateParameter("@DataType", DbType.String, dataType),
                    ctx.CreateParameter("@StartTime", DbType.DateTime2, startTime),
                    ctx.CreateParameter("@EndTimeExclusive", DbType.DateTime2, endTimeExclusive),
                });
            }, async (reader, cancel) =>
            {
                while (await reader.ReadAsync(cancel))
                {
                    cancel.ThrowIfCancellationRequested();
                    var item = GetStatisticalDataRecordFromReader(reader);
                    aggregatorCallback(item);
                }

                return true;
            }).ConfigureAwait(false);
            op.Successful = true;
        }

        private static readonly string WriteAggregationScript = @"-- MsSqlStatisticalDataProvider.WriteAggregation
-- Special solution of upsert (the insert is much more likely than update)
BEGIN TRY
	INSERT INTO [StatisticalAggregations] ([DataType], [Date], [Resolution], [Data]) VALUES (@DataType, @Date, @Resolution, @Data)
END TRY
BEGIN CATCH
	UPDATE [StatisticalAggregations] SET [Data] = @Data WHERE DataType = @DataType AND Resolution = @Resolution AND [Date] = @Date
END CATCH
";
        public async Task WriteAggregationAsync(Aggregation aggregation, CancellationToken cancellation)
        {
            using var op = SnTrace.Database.StartOperation("MsSqlStatisticalDataProvider: " +
                "WriteAggregation: DataType: {0}, Resolution: {1}, Date: {2:yyyy-MM-dd HH:mm:ss.fffff})",
                aggregation.DataType, aggregation.Resolution, aggregation.Date);

            using var ctx = new MsSqlDataContext(ConnectionString, DataOptions, cancellation);
            await ctx.ExecuteNonQueryAsync(WriteAggregationScript, cmd =>
            {
                cmd.Parameters.AddRange(new[]
                {
                    ctx.CreateParameter("@DataType", DbType.String, aggregation.DataType),
                    ctx.CreateParameter("@Resolution", DbType.String, aggregation.Resolution.ToString()),
                    ctx.CreateParameter("@Date", DbType.DateTime2, aggregation.Date),
                    ctx.CreateParameter("@Data", DbType.String, (object)aggregation.Data ?? DBNull.Value),
                });
            }).ConfigureAwait(false);
            op.Successful = true;
        }

        private static readonly string CleanupRecordsScript = @"-- MsSqlStatisticalDataProvider.CleanupRecords
DELETE FROM StatisticalData WHERE DataType = @DataType AND CreationTime < @RetentionTime
";
        public async Task CleanupRecordsAsync(string dataType, DateTime retentionTime, CancellationToken cancellation)
        {
            using var op = SnTrace.Database.StartOperation("MsSqlStatisticalDataProvider: " +
                "CleanupRecords(dataType: {0}, retentionTime: {1:yyyy-MM-dd HH:mm:ss.fffff})",
                dataType, retentionTime);
            using var ctx = new MsSqlDataContext(ConnectionString, DataOptions, cancellation);
            await ctx.ExecuteNonQueryAsync(CleanupRecordsScript, cmd =>
            {
                cmd.Parameters.AddRange(new[]
                {
                    ctx.CreateParameter("@DataType", SqlDbType.NVarChar, 50,  dataType),
                    ctx.CreateParameter("@RetentionTime", SqlDbType.DateTime2, retentionTime),
                });
            }).ConfigureAwait(false);
            op.Successful = true;
        }

        private static readonly string CleanupAggregationsScript = @"-- MsSqlStatisticalDataProvider.CleanupAggregations
DELETE FROM StatisticalAggregations WHERE DataType = @DataType AND Resolution = @Resolution AND [Date] < @RetentionTime
";
        public async Task CleanupAggregationsAsync(string dataType, TimeResolution resolution, DateTime retentionTime,
            CancellationToken cancellation)
        {
            using var op = SnTrace.Database.StartOperation("MsSqlStatisticalDataProvider: " +
                "CleanupAggregations(dataType: {0}, resolution: {1}, retentionTime: {2:yyyy-MM-dd HH:mm:ss.fffff})", 
                dataType, resolution, retentionTime);
            using var ctx = new MsSqlDataContext(ConnectionString, DataOptions, cancellation);
            await ctx.ExecuteNonQueryAsync(CleanupAggregationsScript, cmd =>
            {
                cmd.Parameters.AddRange(new[]
                {
                    ctx.CreateParameter("@DataType", DbType.String,  dataType),
                    ctx.CreateParameter("@Resolution", DbType.AnsiString,  resolution.ToString()),
                    ctx.CreateParameter("@RetentionTime", SqlDbType.DateTime2, retentionTime),
                });
            }).ConfigureAwait(false);
            op.Successful = true;
        }

        /* ====================================================================================================== */

        private IStatisticalDataRecord GetStatisticalDataRecordFromReader(DbDataReader reader)
        {
            var durationIndex = reader.GetOrdinal("Duration");
            return new StatisticalDataRecord
            {
                Id = reader.GetSafeInt32(reader.GetOrdinal("Id")),
                DataType = reader.GetSafeString(reader.GetOrdinal("DataType")),
                WrittenTime = reader.GetDateTimeUtc(reader.GetOrdinal("WrittenTime")),
                CreationTime = reader.GetDateTimeUtc(reader.GetOrdinal("CreationTime")),
                Duration = reader.IsDBNull(durationIndex) ? (TimeSpan?)null : TimeSpan.FromTicks(reader.GetInt64(durationIndex)),
                RequestLength = reader.GetLongOrNull("RequestLength"),
                ResponseLength = reader.GetLongOrNull("ResponseLength"),
                ResponseStatusCode = reader.GetIntOrNull("ResponseStatusCode"),
                Url = reader.GetStringOrNull("Url"),
                TargetId = reader.GetIntOrNull("TargetId"),
                ContentId = reader.GetIntOrNull("ContentId"),
                EventName = reader.GetStringOrNull("EventName"),
                ErrorMessage = reader.GetStringOrNull("ErrorMessage"),
                GeneralData = reader.GetStringOrNull("GeneralData"),
            };
        }

        private Aggregation GetAggregationFromReader(DbDataReader reader)
        {
            return new Aggregation
            {
                DataType = reader.GetString(reader.GetOrdinal("DataType")),
                Date = reader.GetDateTimeUtc(reader.GetOrdinal("Date")),
                Resolution = (TimeResolution) Enum.Parse(typeof(TimeResolution),
                    reader.GetString(reader.GetOrdinal("Resolution"))),
                Data = reader.GetStringOrNull("Data"),
            };
        }

        /* ====================================================================================== INSTALLATION SCRIPTS */

        public static readonly string DropScript = @"/****** Index [IX_StatisticalData_DataType_CreationTime] ******/
DROP INDEX IF EXISTS [IX_StatisticalData_DataType_CreationTime] ON [dbo].[StatisticalData]

/****** Table [dbo].[StatisticalData] ******/
DROP TABLE IF EXISTS [dbo].[StatisticalData]

/****** Table [dbo].[StatisticalAggregations] ******/
DROP TABLE IF EXISTS [dbo].[StatisticalAggregations]
";

        public static readonly string CreationScript = @"/****** Table [dbo].[StatisticalData] ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[StatisticalData]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[StatisticalData](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[DataType] [nvarchar](50) NOT NULL,
	[CreationTime] [datetime2](7) NOT NULL,
	[WrittenTime] [datetime2](7) NOT NULL,
	[Duration] [bigint] NULL,
	[RequestLength] [bigint] NULL,
	[ResponseLength] [bigint] NULL,
	[ResponseStatusCode] [int] NULL,
	[Url] [nvarchar](1000) NULL,
	[TargetId] [int] NULL,
	[ContentId] [int] NULL,
	[EventName] [nvarchar](50) NULL,
	[ErrorMessage] [nvarchar](500) NULL,
	[GeneralData] [nvarchar](max) NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END

/****** Object:  Index [IX_StatisticalData_DataType_CreationTime]    Script Date: 2021. 06. 22. 2:45:01 ******/
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[StatisticalData]') AND name = N'IX_StatisticalData_DataType_CreationTime')
CREATE NONCLUSTERED INDEX [IX_StatisticalData_DataType_CreationTime] ON [dbo].[StatisticalData]
(
	[DataType] ASC,
	[CreationTime] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]

/****** Table [dbo].[StatisticalAggregations] ******/
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[StatisticalAggregations]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[StatisticalAggregations](
	[DataType] [nvarchar](50) NOT NULL,
	[Date] [datetime2](7) NOT NULL,
	[Resolution] [varchar](10) NOT NULL,
	[Data] [nvarchar](max) NULL,
 CONSTRAINT [PK_StatisticalAggregations] PRIMARY KEY CLUSTERED 
(
	[DataType] ASC,
	[Date] ASC,
	[Resolution] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END
";

    }
}
