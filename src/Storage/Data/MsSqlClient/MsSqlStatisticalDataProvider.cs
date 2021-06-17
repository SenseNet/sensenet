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
            //UNDONE:<?Stat: IMPLEMENT MsSqlStatisticalDataProvider.WriteDataAsync
            throw new NotImplementedException();
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
