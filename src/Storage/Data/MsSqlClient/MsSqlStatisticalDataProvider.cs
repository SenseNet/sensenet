using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SenseNet.Configuration;
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

        private readonly string WriteDataScript = @"-- MsSqlStatisticalDataProvider.WriteData
INSERT INTO StatisticalData
    (DataType, CreationTime, WrittenTime, Duration, RequestLength, ResponseLength, ResponseStatusCode, [Url],
	 TargetId, ContentId, EventName, ErrorMessage, GeneralData)
    VALUES
    (@DataType, @CreationTime, @WrittenTime, @Duration, @RequestLength, @ResponseLength, @ResponseStatusCode, @Url,
	 @TargetId, @ContentId, @EventName, @ErrorMessage, @GeneralData)
";
        public async Task WriteDataAsync(IStatisticalDataRecord data, CancellationToken cancel)
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

        public Task<IEnumerable<IStatisticalDataRecord>> LoadUsageListAsync(string dataType, int[] relatedTargetIds, DateTime endTimeExclusive, int count, CancellationToken cancel)
        {
            //UNDONE:<?Stat: IMPLEMENT MsSqlStatisticalDataProvider.LoadUsageListAsync
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Aggregation>> LoadAggregatedUsageAsync(string dataType, TimeResolution resolution, DateTime startTime, DateTime endTimeExclusive,
            CancellationToken cancel)
        {
            //UNDONE:<?Stat: IMPLEMENT MsSqlStatisticalDataProvider.LoadAggregatedUsageAsync
            throw new NotImplementedException();
        }

        public Task<DateTime?[]> LoadFirstAggregationTimesByResolutionsAsync(string dataType, CancellationToken httpContextRequestAborted)
        {
            //UNDONE:<?Stat: IMPLEMENT MsSqlStatisticalDataProvider.LoadFirstAggregationTimesByResolutionsAsync
            throw new NotImplementedException();
        }

        public Task EnumerateDataAsync(string dataType, DateTime startTime, DateTime endTimeExclusive, Action<IStatisticalDataRecord> aggregatorCallback,
            CancellationToken cancel)
        {
            //UNDONE:<?Stat: IMPLEMENT MsSqlStatisticalDataProvider.EnumerateDataAsync
            throw new NotImplementedException();
        }

        public Task WriteAggregationAsync(Aggregation aggregation, CancellationToken cancel)
        {
            //UNDONE:<?Stat: IMPLEMENT MsSqlStatisticalDataProvider.WriteAggregationAsync
            throw new NotImplementedException();
        }

        public Task CleanupRecordsAsync(string dataType, DateTime retentionTime, CancellationToken cancel)
        {
            //UNDONE:<?Stat: IMPLEMENT MsSqlStatisticalDataProvider.CleanupRecordsAsync
            throw new NotImplementedException();
        }

        public Task CleanupAggregationsAsync(string dataType, TimeResolution resolution, DateTime retentionTime,
            CancellationToken cancel)
        {
            //UNDONE:<?Stat: IMPLEMENT MsSqlStatisticalDataProvider.CleanupAggregationsAsync
            throw new NotImplementedException();
        }
    }
}
