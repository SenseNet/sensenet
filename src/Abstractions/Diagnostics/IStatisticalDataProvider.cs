using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace SenseNet.Diagnostics
{
    [DebuggerDisplay("{ToString()}")]
    public class Aggregation
    {
        public string DataType { get; set; }
        public DateTime Date { get; set; }
        public TimeResolution Resolution { get; set; }

        public string Data { get; set; }

        public override string ToString()
        {
            return $"{Date:yyyy-MM-dd HH:mm:ss} {Resolution}";
        }
    }

    public interface IStatisticalDataProvider
    {
        Task WriteDataAsync(IStatisticalDataRecord data, CancellationToken cancel);

        Task<IEnumerable<IStatisticalDataRecord>> LoadUsageListAsync(string dataType, int[] relatedTargetIds, 
            DateTime endTimeExclusive, int count, CancellationToken cancel);

        Task<IEnumerable<Aggregation>> LoadAggregatedUsageAsync(string dataType, TimeResolution resolution,
            DateTime startTime, DateTime endTimeExclusive, CancellationToken cancel);

        Task<DateTime?[]> LoadFirstAggregationTimesByResolutionsAsync(string dataType, CancellationToken cancel);
        Task<DateTime?[]> LoadLastAggregationTimesByResolutionsAsync(CancellationToken cancel);

        Task EnumerateDataAsync(string dataType, DateTime startTime, DateTime endTimeExclusive,
            Action<IStatisticalDataRecord> aggregatorCallback, CancellationToken cancel);

        Task WriteAggregationAsync(Aggregation aggregation, CancellationToken cancel);
        Task CleanupRecordsAsync(string dataType, DateTime retentionTime, CancellationToken cancel);
        Task CleanupAggregationsAsync(string dataType, TimeResolution resolution, DateTime retentionTime,
            CancellationToken cancel);
    }

    public class NullStatisticalDataProvider : IStatisticalDataProvider
    {
        public Task WriteDataAsync(IStatisticalDataRecord data, CancellationToken cancel)
        {
            return Task.CompletedTask;
        }

        public Task<IEnumerable<IStatisticalDataRecord>> LoadUsageListAsync(string dataType, int[] relatedTargetIds,
            DateTime endTimeExclusive, int count, CancellationToken cancel)
        {
            return Task.FromResult((IEnumerable<IStatisticalDataRecord>)Array.Empty<IStatisticalDataRecord>());
        }

        public Task<IEnumerable<Aggregation>> LoadAggregatedUsageAsync(string dataType, TimeResolution resolution,
            DateTime startTime, DateTime endTimeExclusive, CancellationToken cancel)
        {
            return Task.FromResult((IEnumerable<Aggregation>)Array.Empty<Aggregation>());
        }

        public Task<DateTime?[]> LoadFirstAggregationTimesByResolutionsAsync(string dataType, CancellationToken cancel)
        {
            return Task.FromResult(Array.Empty<DateTime?>());
        }
        public Task<DateTime?[]> LoadLastAggregationTimesByResolutionsAsync(CancellationToken cancel)
        {
            return Task.FromResult(Array.Empty<DateTime?>());
        }

        public Task EnumerateDataAsync(string dataType, DateTime startTime, DateTime endTimeExclusive, Action<IStatisticalDataRecord> aggregatorCallback,
            CancellationToken cancel)
        {
            return Task.CompletedTask;
        }

        public Task WriteAggregationAsync(Aggregation aggregation, CancellationToken cancel)
        {
            return Task.CompletedTask;
        }

        public Task CleanupRecordsAsync(string dataType, DateTime retentionTime, CancellationToken cancel)
        {
            return Task.CompletedTask;
        }

        public Task CleanupAggregationsAsync(string dataType, TimeResolution resolution, DateTime retentionTime,
            CancellationToken cancel)
        {
            return Task.CompletedTask;
        }
    }
}
