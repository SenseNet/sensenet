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
            ConnectionString = (connectionOptions?.Value ?? new ConnectionStringOptions()).ConnectionString;
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
            using (var ctx = new MsSqlDataContext(ConnectionString, DataOptions, CancellationToken.None))
            {
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
            }
        }

        private static readonly string LoadUsageListScript = @"-- MsSqlStatisticalDataProvider.LoadUsageList
SELECT TOP(@Take) * FROM StatisticalData
WHERE DataType = @DataType AND CreationTime < @EndTimeExclusive
ORDER BY CreationTime DESC
";
        private static readonly string LoadUsageListByTargetIdsScript = @"-- MsSqlStatisticalDataProvider.LoadUsageListByTargetIds
SELECT TOP(@Take) * FROM StatisticalData
WHERE DataType = @DataType AND CreationTime < @EndTimeExclusive
    AND TargetId IN ({0})
ORDER BY CreationTime DESC
";
        public async Task<IEnumerable<IStatisticalDataRecord>> LoadUsageListAsync(string dataType, int[] relatedTargetIds, DateTime endTimeExclusive, int count, CancellationToken cancellation)
        {
            string sql;
            if (relatedTargetIds == null || relatedTargetIds.Length == 0)
            {
                sql = LoadUsageListScript;
            }
            else
            {
                var ids = string.Join(", ", relatedTargetIds.Select(x => x.ToString()));
                sql = string.Format(LoadUsageListByTargetIdsScript, ids);
            }

            var records = new List<IStatisticalDataRecord>();
            using (var ctx = new MsSqlDataContext(ConnectionString, DataOptions, CancellationToken.None))
            {
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
            }

            return records;
        }

        private static readonly string LoadAggregatedUsageScript = @"-- MsSqlStatisticalDataProvider.LoadAggregatedUsage
SELECT * FROM StatisticalAggregations
WHERE DataType = @DataType AND Resolution = @Resolution AND Date >= @StartTime AND Date < @EndTimeExclusive
--ORDER BY @Date
";
        public async Task<IEnumerable<Aggregation>> LoadAggregatedUsageAsync(string dataType, TimeResolution resolution, DateTime startTime, DateTime endTimeExclusive,
            CancellationToken cancellation)
        {
            var aggregations = new List<Aggregation>();
            using (var ctx = new MsSqlDataContext(ConnectionString, DataOptions, CancellationToken.None))
            {
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
            }
            return aggregations;
        }

        private static readonly string LoadFirstAggregationTimesByResolutionsScript = @"-- MsSqlStatisticalDataProvider.LoadFirstAggregationTimesByResolutions
SELECT TOP 1 * FROM StatisticalAggregations WHERE DataType = @DataType AND Resolution = 'Minute'
UNION ALL
SELECT TOP 1 * FROM StatisticalAggregations WHERE DataType = @DataType AND Resolution = 'Hour'
UNION ALL
SELECT TOP 1 * FROM StatisticalAggregations WHERE DataType = @DataType AND Resolution = 'Day'
UNION ALL
SELECT TOP 1 * FROM StatisticalAggregations WHERE DataType = @DataType AND Resolution = 'Month'
";
        public async Task<DateTime?[]> LoadFirstAggregationTimesByResolutionsAsync(string dataType, CancellationToken cancellation)
        {
            var result = new List<DateTime?>();
            using (var ctx = new MsSqlDataContext(ConnectionString, DataOptions, CancellationToken.None))
            {
                await ctx.ExecuteReaderAsync(LoadFirstAggregationTimesByResolutionsScript, async (reader, cancel) => {
                    while (await reader.ReadAsync(cancel))
                        result.Add(reader.GetDateTimeUtcOrNull("Date"));
                    return true;
                }).ConfigureAwait(false);
            }
            return result.ToArray();
        }

        public Task EnumerateDataAsync(string dataType, DateTime startTime, DateTime endTimeExclusive, Action<IStatisticalDataRecord> aggregatorCallback,
            CancellationToken cancellation)
        {
            //UNDONE:<?Stat: IMPLEMENT MsSqlStatisticalDataProvider.EnumerateDataAsync
            throw new NotImplementedException();
        }

        private static readonly string WriteAggregationScript = @"-- MsSqlStatisticalDataProvider.WriteAggregation
-- Special solution of upsert (the insert is much more likely than update)
BEGIN TRY
	INSERT INTO [StatisticalAggregations] ([DataType], [Date], [Resolution], [Data]) VALUES (@DataType, @Date, @Resolution, @Data)
END TRY
BEGIN CATCH
	UPDATE [StatisticalAggregations] SET [Data] = @Data WHERE DataType = @DataType AND Resolution = @Resolution AND Date = @Date
END CATCH
";
        public async Task WriteAggregationAsync(Aggregation aggregation, CancellationToken cancellation)
        {
            using (var ctx = new MsSqlDataContext(ConnectionString, DataOptions, CancellationToken.None))
            {
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
            }
        }

        public Task CleanupRecordsAsync(string dataType, DateTime retentionTime, CancellationToken cancellation)
        {
            //UNDONE:<?Stat: IMPLEMENT MsSqlStatisticalDataProvider.CleanupRecordsAsync
            throw new NotImplementedException();
        }

        public Task CleanupAggregationsAsync(string dataType, TimeResolution resolution, DateTime retentionTime,
            CancellationToken cancellation)
        {
            //UNDONE:<?Stat: IMPLEMENT MsSqlStatisticalDataProvider.CleanupAggregationsAsync
            throw new NotImplementedException();
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
    }
}
