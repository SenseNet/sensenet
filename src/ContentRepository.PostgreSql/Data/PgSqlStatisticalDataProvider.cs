using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;
using SenseNet.Configuration;
using SenseNet.Diagnostics;
using SenseNet.Tools;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data.PgSqlClient
{
    public class PgSqlStatisticalDataProvider : IStatisticalDataProvider
    {
        private readonly IRetrier _retrier;
        private DataOptions DataOptions { get; }
        private ConnectionStringOptions ConnectionStrings { get; }

        public PgSqlStatisticalDataProvider(IOptions<DataOptions> dataOptions,
            IOptions<ConnectionStringOptions> connectionOptions, IRetrier retrier)
        {
            _retrier = retrier;
            DataOptions = dataOptions?.Value ?? new DataOptions();
            ConnectionStrings = connectionOptions?.Value ?? new ConnectionStringOptions();
        }

        /* =============================================================================================== Write */

        private static readonly string WriteDataScript = @"-- PgSqlStatisticalDataProvider.WriteData
INSERT INTO ""StatisticalData""
    (""DataType"", ""WrittenTime"", ""CreationTime"", ""Duration"", ""RequestLength"", ""ResponseLength"",
     ""ResponseStatusCode"", ""Url"", ""TargetId"", ""ContentId"", ""EventName"", ""ErrorMessage"", ""GeneralData"")
VALUES (@DataType, @WrittenTime, @CreationTime, @Duration, @RequestLength, @ResponseLength,
     @ResponseStatusCode, @Url, @TargetId, @ContentId, @EventName, @ErrorMessage, @GeneralData)
";

        public async System.Threading.Tasks.Task WriteDataAsync(IStatisticalDataRecord data, CancellationToken cancel)
        {
            using var ctx = new PgSqlDataContext(ConnectionStrings.Repository, DataOptions, _retrier, cancel);
            await ctx.ExecuteNonQueryAsync(WriteDataScript, cmd =>
            {
                cmd.Parameters.AddRange(new[]
                {
                    ctx.CreateParameter("@DataType", DbType.String, 50, data.DataType),
                    ctx.CreateParameter("@WrittenTime", DbType.DateTime2, data.WrittenTime == default ? DateTime.UtcNow : data.WrittenTime),
                    ctx.CreateParameter("@CreationTime", DbType.DateTime2, (object)data.CreationTime ?? DBNull.Value),
                    ctx.CreateParameter("@Duration", DbType.Int64, (object)data.Duration?.Ticks ?? DBNull.Value),
                    ctx.CreateParameter("@RequestLength", DbType.Int64, (object)data.RequestLength ?? DBNull.Value),
                    ctx.CreateParameter("@ResponseLength", DbType.Int64, (object)data.ResponseLength ?? DBNull.Value),
                    ctx.CreateParameter("@ResponseStatusCode", DbType.Int32, (object)data.ResponseStatusCode ?? DBNull.Value),
                    ctx.CreateParameter("@Url", DbType.String, int.MaxValue, (object)data.Url ?? DBNull.Value),
                    ctx.CreateParameter("@TargetId", DbType.Int32, (object)data.TargetId ?? DBNull.Value),
                    ctx.CreateParameter("@ContentId", DbType.Int32, (object)data.ContentId ?? DBNull.Value),
                    ctx.CreateParameter("@EventName", DbType.String, 450, (object)data.EventName ?? DBNull.Value),
                    ctx.CreateParameter("@ErrorMessage", DbType.String, int.MaxValue, (object)data.ErrorMessage ?? DBNull.Value),
                    ctx.CreateParameter("@GeneralData", DbType.String, int.MaxValue, (object)data.GeneralData ?? DBNull.Value),
                });
            }).ConfigureAwait(false);
        }

        /* =============================================================================================== Load */

        private static readonly string LoadUsageListScript = @"-- PgSqlStatisticalDataProvider.LoadUsageList
SELECT ""Id"", ""DataType"", ""WrittenTime"", ""CreationTime"", ""Duration"",
    ""RequestLength"", ""ResponseLength"", ""ResponseStatusCode"", ""Url"",
    ""TargetId"", ""ContentId"", ""EventName"", ""ErrorMessage"", ""GeneralData""
FROM ""StatisticalData""
WHERE ""DataType"" = @DataType AND ""WrittenTime"" < @EndTime
";

        public async Task<IEnumerable<IStatisticalDataRecord>> LoadUsageListAsync(
            string dataType, int[] relatedTargetIds, DateTime endTimeExclusive,
            int count, CancellationToken cancel)
        {
            using var op = SnTrace.Database.StartOperation("PgSqlStatisticalDataProvider: " +
                "LoadUsageList(dataType: {0}, endTimeExclusive: {1:yyyy-MM-dd HH:mm:ss.fffff})",
                dataType, endTimeExclusive);

            var sql = LoadUsageListScript;
            if (relatedTargetIds != null && relatedTargetIds.Length > 0)
                sql += " AND \"TargetId\" IN (" + string.Join(",", relatedTargetIds) + ")";
            sql += " ORDER BY \"WrittenTime\" DESC LIMIT @Count";

            using var ctx = new PgSqlDataContext(ConnectionStrings.Repository, DataOptions, _retrier, cancel);
            var records = new List<IStatisticalDataRecord>();
            await ctx.ExecuteReaderAsync(sql, cmd =>
            {
                cmd.Parameters.AddRange(new[]
                {
                    ctx.CreateParameter("@DataType", DbType.String, 50, dataType),
                    ctx.CreateParameter("@EndTime", DbType.DateTime2, endTimeExclusive),
                    ctx.CreateParameter("@Count", DbType.Int32, count),
                });
            }, async (reader, c) =>
            {
                while (await reader.ReadAsync(c).ConfigureAwait(false))
                    records.Add(GetStatisticalDataRecordFromReader(reader));
                return true;
            }).ConfigureAwait(false);
            op.Successful = true;

            return records;
        }

        /* =============================================================================================== Aggregation */

        private static readonly string LoadAggregatedUsageScript = @"-- PgSqlStatisticalDataProvider.LoadAggregatedUsage
SELECT * FROM ""StatisticalAggregations""
WHERE ""DataType"" = @DataType AND ""Resolution"" = @Resolution AND ""Date"" >= @StartTime AND ""Date"" < @EndTimeExclusive
ORDER BY ""Date""
";

        public async Task<IEnumerable<Aggregation>> LoadAggregatedUsageAsync(
            string dataType, TimeResolution resolution, DateTime startTime, DateTime endTimeExclusive,
            CancellationToken cancel)
        {
            using var op = SnTrace.Database.StartOperation("PgSqlStatisticalDataProvider: " +
                "LoadAggregatedUsage(dataType: {0}, resolution: {1}, startTime: {2:yyyy-MM-dd HH:mm:ss.fffff}, endTimeExclusive: {3:yyyy-MM-dd HH:mm:ss.fffff})",
                dataType, resolution, startTime, endTimeExclusive);

            using var ctx = new PgSqlDataContext(ConnectionStrings.Repository, DataOptions, _retrier, cancel);
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
            }, async (reader, c) =>
            {
                while (await reader.ReadAsync(c).ConfigureAwait(false))
                    aggregations.Add(GetAggregationFromReader(reader));
                return true;
            }).ConfigureAwait(false);
            op.Successful = true;

            return aggregations;
        }

        private static readonly string LoadFirstAggregationTimesByResolutionsScript = @"-- PgSqlStatisticalDataProvider.LoadFirstAggregationTimesByResolutions
SELECT
    (SELECT ""Date"" FROM ""StatisticalAggregations"" WHERE ""DataType"" = @DataType AND ""Resolution"" = 'Minute' ORDER BY ""Date"" LIMIT 1) AS ""Minute"",
    (SELECT ""Date"" FROM ""StatisticalAggregations"" WHERE ""DataType"" = @DataType AND ""Resolution"" = 'Hour' ORDER BY ""Date"" LIMIT 1) AS ""Hour"",
    (SELECT ""Date"" FROM ""StatisticalAggregations"" WHERE ""DataType"" = @DataType AND ""Resolution"" = 'Day' ORDER BY ""Date"" LIMIT 1) AS ""Day"",
    (SELECT ""Date"" FROM ""StatisticalAggregations"" WHERE ""DataType"" = @DataType AND ""Resolution"" = 'Month' ORDER BY ""Date"" LIMIT 1) AS ""Month""
";

        public async Task<DateTime?[]> LoadFirstAggregationTimesByResolutionsAsync(string dataType,
            CancellationToken cancel)
        {
            using var op = SnTrace.Database.StartOperation("PgSqlStatisticalDataProvider: " +
                "LoadFirstAggregationTimesByResolutions(dataType: {0})", dataType);

            using var ctx = new PgSqlDataContext(ConnectionStrings.Repository, DataOptions, _retrier, cancel);
            var result = new DateTime?[4];
            await ctx.ExecuteReaderAsync(LoadFirstAggregationTimesByResolutionsScript, cmd =>
            {
                cmd.Parameters.AddRange(new[]
                {
                    ctx.CreateParameter("@DataType", DbType.String, dataType),
                });
            }, async (reader, c) =>
            {
                if (await reader.ReadAsync(c).ConfigureAwait(false))
                {
                    result[0] = reader.IsDBNull(reader.GetOrdinal("Minute")) ? null : reader.GetDateTime(reader.GetOrdinal("Minute"));
                    result[1] = reader.IsDBNull(reader.GetOrdinal("Hour")) ? null : reader.GetDateTime(reader.GetOrdinal("Hour"));
                    result[2] = reader.IsDBNull(reader.GetOrdinal("Day")) ? null : reader.GetDateTime(reader.GetOrdinal("Day"));
                    result[3] = reader.IsDBNull(reader.GetOrdinal("Month")) ? null : reader.GetDateTime(reader.GetOrdinal("Month"));
                }
                return true;
            }).ConfigureAwait(false);
            op.Successful = true;

            return result;
        }

        private static readonly string LoadLastAggregationTimesByResolutionsScript = @"-- PgSqlStatisticalDataProvider.LoadLastAggregationTimesByResolutions
SELECT
    (SELECT ""Date"" FROM ""StatisticalAggregations"" WHERE ""Resolution"" = 'Minute' ORDER BY ""Date"" DESC LIMIT 1) AS ""Minute"",
    (SELECT ""Date"" FROM ""StatisticalAggregations"" WHERE ""Resolution"" = 'Hour' ORDER BY ""Date"" DESC LIMIT 1) AS ""Hour"",
    (SELECT ""Date"" FROM ""StatisticalAggregations"" WHERE ""Resolution"" = 'Day' ORDER BY ""Date"" DESC LIMIT 1) AS ""Day"",
    (SELECT ""Date"" FROM ""StatisticalAggregations"" WHERE ""Resolution"" = 'Month' ORDER BY ""Date"" DESC LIMIT 1) AS ""Month""
";

        public async Task<DateTime?[]> LoadLastAggregationTimesByResolutionsAsync(CancellationToken cancel)
        {
            using var op = SnTrace.Database.StartOperation("PgSqlStatisticalDataProvider: " +
                "LoadLastAggregationTimesByResolutions()");

            using var ctx = new PgSqlDataContext(ConnectionStrings.Repository, DataOptions, _retrier, cancel);
            var result = new DateTime?[4];
            await ctx.ExecuteReaderAsync(LoadLastAggregationTimesByResolutionsScript, async (reader, c) =>
            {
                if (await reader.ReadAsync(c).ConfigureAwait(false))
                {
                    result[0] = reader.IsDBNull(reader.GetOrdinal("Minute")) ? null : reader.GetDateTime(reader.GetOrdinal("Minute"));
                    result[1] = reader.IsDBNull(reader.GetOrdinal("Hour")) ? null : reader.GetDateTime(reader.GetOrdinal("Hour"));
                    result[2] = reader.IsDBNull(reader.GetOrdinal("Day")) ? null : reader.GetDateTime(reader.GetOrdinal("Day"));
                    result[3] = reader.IsDBNull(reader.GetOrdinal("Month")) ? null : reader.GetDateTime(reader.GetOrdinal("Month"));
                }
                return true;
            }).ConfigureAwait(false);
            op.Successful = true;

            return result;
        }

        /* =============================================================================================== EnumerateData */

        private static readonly string EnumerateDataScript = @"-- PgSqlStatisticalDataProvider.EnumerateData
SELECT * FROM ""StatisticalData""
WHERE ""DataType"" = @DataType AND ""CreationTime"" >= @StartTime AND ""CreationTime"" < @EndTimeExclusive
ORDER BY ""CreationTime""
";

        public async System.Threading.Tasks.Task EnumerateDataAsync(string dataType, DateTime startTime, DateTime endTimeExclusive,
            Action<IStatisticalDataRecord> aggregatorCallback, CancellationToken cancel)
        {
            using var op = SnTrace.Database.StartOperation("PgSqlStatisticalDataProvider: " +
                "EnumerateData(dataType: {0}, startTime: {1:yyyy-MM-dd HH:mm:ss.fffff}, endTimeExclusive: {2:yyyy-MM-dd HH:mm:ss.fffff})",
                dataType, startTime, endTimeExclusive);

            using var ctx = new PgSqlDataContext(ConnectionStrings.Repository, DataOptions, _retrier, cancel);
            await ctx.ExecuteReaderAsync(EnumerateDataScript, cmd =>
            {
                cmd.Parameters.AddRange(new[]
                {
                    ctx.CreateParameter("@DataType", DbType.String, dataType),
                    ctx.CreateParameter("@StartTime", DbType.DateTime2, startTime),
                    ctx.CreateParameter("@EndTimeExclusive", DbType.DateTime2, endTimeExclusive),
                });
            }, async (reader, c) =>
            {
                while (await reader.ReadAsync(c).ConfigureAwait(false))
                {
                    c.ThrowIfCancellationRequested();
                    var item = GetStatisticalDataRecordFromReader(reader);
                    aggregatorCallback(item);
                }
                return true;
            }).ConfigureAwait(false);
            op.Successful = true;
        }

        /* =============================================================================================== WriteAggregation */

        private static readonly string WriteAggregationScript = @"-- PgSqlStatisticalDataProvider.WriteAggregation
INSERT INTO ""StatisticalAggregations"" (""DataType"", ""Date"", ""Resolution"", ""Data"")
VALUES (@DataType, @Date, @Resolution, @Data)
ON CONFLICT (""DataType"", ""Date"", ""Resolution"") DO UPDATE SET ""Data"" = EXCLUDED.""Data""
";

        public async System.Threading.Tasks.Task WriteAggregationAsync(Aggregation aggregation, CancellationToken cancel)
        {
            using var op = SnTrace.Database.StartOperation("PgSqlStatisticalDataProvider: " +
                "WriteAggregation: DataType: {0}, Resolution: {1}, Date: {2:yyyy-MM-dd HH:mm:ss.fffff})",
                aggregation.DataType, aggregation.Resolution, aggregation.Date);

            using var ctx = new PgSqlDataContext(ConnectionStrings.Repository, DataOptions, _retrier, cancel);
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

        /* =============================================================================================== Cleanup */

        private static readonly string CleanupRecordsScript = @"-- PgSqlStatisticalDataProvider.CleanupRecords
DELETE FROM ""StatisticalData"" WHERE ""DataType"" = @DataType AND ""CreationTime"" < @RetentionTime
";

        public async System.Threading.Tasks.Task CleanupRecordsAsync(string dataType, DateTime retentionTime, CancellationToken cancel)
        {
            using var op = SnTrace.Database.StartOperation("PgSqlStatisticalDataProvider: " +
                "CleanupRecords(dataType: {0}, retentionTime: {1:yyyy-MM-dd HH:mm:ss.fffff})",
                dataType, retentionTime);

            using var ctx = new PgSqlDataContext(ConnectionStrings.Repository, DataOptions, _retrier, cancel);
            await ctx.ExecuteNonQueryAsync(CleanupRecordsScript, cmd =>
            {
                cmd.Parameters.AddRange(new[]
                {
                    ctx.CreateParameter("@DataType", DbType.String, 50, dataType),
                    ctx.CreateParameter("@RetentionTime", DbType.DateTime2, retentionTime),
                });
            }).ConfigureAwait(false);
            op.Successful = true;
        }

        private static readonly string CleanupAggregationsScript = @"-- PgSqlStatisticalDataProvider.CleanupAggregations
DELETE FROM ""StatisticalAggregations"" WHERE ""DataType"" = @DataType AND ""Resolution"" = @Resolution AND ""Date"" < @RetentionTime
";

        public async System.Threading.Tasks.Task CleanupAggregationsAsync(string dataType, TimeResolution resolution,
            DateTime retentionTime, CancellationToken cancel)
        {
            using var op = SnTrace.Database.StartOperation("PgSqlStatisticalDataProvider: " +
                "CleanupAggregations(dataType: {0}, resolution: {1}, retentionTime: {2:yyyy-MM-dd HH:mm:ss.fffff})",
                dataType, resolution, retentionTime);

            using var ctx = new PgSqlDataContext(ConnectionStrings.Repository, DataOptions, _retrier, cancel);
            await ctx.ExecuteNonQueryAsync(CleanupAggregationsScript, cmd =>
            {
                cmd.Parameters.AddRange(new[]
                {
                    ctx.CreateParameter("@DataType", DbType.String, 50, dataType),
                    ctx.CreateParameter("@Resolution", DbType.String, resolution.ToString()),
                    ctx.CreateParameter("@RetentionTime", DbType.DateTime2, retentionTime),
                });
            }).ConfigureAwait(false);
            op.Successful = true;
        }

        /* =============================================================================================== Helpers */

        private IStatisticalDataRecord GetStatisticalDataRecordFromReader(DbDataReader reader)
        {
            var durationIndex = reader.GetOrdinal("Duration");
            return new StatisticalDataRecord
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                DataType = reader.GetString(reader.GetOrdinal("DataType")),
                WrittenTime = reader.IsDBNull(reader.GetOrdinal("WrittenTime"))
                    ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("WrittenTime")),
                CreationTime = reader.IsDBNull(reader.GetOrdinal("CreationTime"))
                    ? null : reader.GetDateTime(reader.GetOrdinal("CreationTime")),
                Duration = reader.IsDBNull(durationIndex)
                    ? null : TimeSpan.FromTicks(reader.GetInt64(durationIndex)),
                RequestLength = reader.IsDBNull(reader.GetOrdinal("RequestLength"))
                    ? null : reader.GetInt64(reader.GetOrdinal("RequestLength")),
                ResponseLength = reader.IsDBNull(reader.GetOrdinal("ResponseLength"))
                    ? null : reader.GetInt64(reader.GetOrdinal("ResponseLength")),
                ResponseStatusCode = reader.IsDBNull(reader.GetOrdinal("ResponseStatusCode"))
                    ? null : reader.GetInt32(reader.GetOrdinal("ResponseStatusCode")),
                Url = reader.IsDBNull(reader.GetOrdinal("Url"))
                    ? null : reader.GetString(reader.GetOrdinal("Url")),
                TargetId = reader.IsDBNull(reader.GetOrdinal("TargetId"))
                    ? null : reader.GetInt32(reader.GetOrdinal("TargetId")),
                ContentId = reader.IsDBNull(reader.GetOrdinal("ContentId"))
                    ? null : reader.GetInt32(reader.GetOrdinal("ContentId")),
                EventName = reader.IsDBNull(reader.GetOrdinal("EventName"))
                    ? null : reader.GetString(reader.GetOrdinal("EventName")),
                ErrorMessage = reader.IsDBNull(reader.GetOrdinal("ErrorMessage"))
                    ? null : reader.GetString(reader.GetOrdinal("ErrorMessage")),
                GeneralData = reader.IsDBNull(reader.GetOrdinal("GeneralData"))
                    ? null : reader.GetString(reader.GetOrdinal("GeneralData")),
            };
        }

        private Aggregation GetAggregationFromReader(DbDataReader reader)
        {
            return new Aggregation
            {
                DataType = reader.GetString(reader.GetOrdinal("DataType")),
                Date = reader.GetDateTime(reader.GetOrdinal("Date")),
                Resolution = (TimeResolution)Enum.Parse(typeof(TimeResolution),
                    reader.GetString(reader.GetOrdinal("Resolution"))),
                Data = reader.IsDBNull(reader.GetOrdinal("Data"))
                    ? null : reader.GetString(reader.GetOrdinal("Data")),
            };
        }

        // =============================================================================================== Installation

        public static readonly string CreationScript = @"-- PgSqlStatisticalDataProvider.CreateTables
CREATE TABLE IF NOT EXISTS ""StatisticalData"" (
    ""Id"" SERIAL PRIMARY KEY,
    ""DataType"" VARCHAR(50) NOT NULL,
    ""CreationTime"" TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    ""WrittenTime"" TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    ""Duration"" BIGINT NULL,
    ""RequestLength"" BIGINT NULL,
    ""ResponseLength"" BIGINT NULL,
    ""ResponseStatusCode"" INT NULL,
    ""Url"" VARCHAR(1000) NULL,
    ""TargetId"" INT NULL,
    ""ContentId"" INT NULL,
    ""EventName"" VARCHAR(50) NULL,
    ""ErrorMessage"" VARCHAR(500) NULL,
    ""GeneralData"" TEXT NULL
);

CREATE INDEX IF NOT EXISTS ""IX_StatisticalData_DataType_CreationTime""
    ON ""StatisticalData"" (""DataType"", ""CreationTime"");

CREATE TABLE IF NOT EXISTS ""StatisticalAggregations"" (
    ""DataType"" VARCHAR(50) NOT NULL,
    ""Date"" TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    ""Resolution"" VARCHAR(10) NOT NULL,
    ""Data"" TEXT NULL,
    CONSTRAINT ""PK_StatisticalAggregations"" PRIMARY KEY (""DataType"", ""Date"", ""Resolution"")
);
";
    }
}
