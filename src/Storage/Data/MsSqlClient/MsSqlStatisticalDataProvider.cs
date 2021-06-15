using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Diagnostics;

namespace SenseNet.Storage.Data.MsSqlClient
{
    public class MsSqlStatisticalDataProvider : IStatisticalDataProvider
    {
        public Task WriteDataAsync(IStatisticalDataRecord data, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<IStatisticalDataRecord>> LoadUsageListAsync(string dataType, DateTime endTimeExclusive, int count, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Aggregation>> LoadAggregatedUsageAsync(string dataType, TimeResolution resolution, DateTime startTime, DateTime endTimeExclusive,
            CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public Task<DateTime?[]> LoadFirstAggregationTimesByResolutionsAsync(string dataType, CancellationToken httpContextRequestAborted)
        {
            throw new NotImplementedException();
        }

        public Task EnumerateDataAsync(string dataType, DateTime startTime, DateTime endTimeExclusive, Action<IStatisticalDataRecord> aggregatorCallback,
            CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public Task WriteAggregationAsync(Aggregation aggregation, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public Task CleanupRecordsAsync(string dataType, DateTime retentionTime, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public Task CleanupAggregationsAsync(string dataType, TimeResolution resolution, DateTime retentionTime,
            CancellationToken cancel)
        {
            throw new NotImplementedException();
        }
    }
}
