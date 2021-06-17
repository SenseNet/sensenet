using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using SenseNet.Diagnostics;
using STT = System.Threading.Tasks;

namespace SenseNet.ContentRepository.InMemory
{
    public class InMemoryStatisticalDataProvider : IStatisticalDataProvider
    {
        private int _lastId;
        public int GetNextId()
        {
            return Interlocked.Increment(ref _lastId);
        }

        private List<StatisticalDataRecord> Storage { get; } = new List<StatisticalDataRecord>();
        private List<Aggregation> Aggregations { get; } = new List<Aggregation>();

        public STT.Task WriteDataAsync(IStatisticalDataRecord data, CancellationToken cancel)
        {
            var now = DateTime.UtcNow;

            Storage.Add(new StatisticalDataRecord
            {
                Id = GetNextId(),
                DataType = data.DataType,
                WrittenTime = now,
                CreationTime = data.CreationTime ?? now,
                Duration = data.Duration,
                RequestLength = data.RequestLength,
                ResponseLength = data.ResponseLength,
                ResponseStatusCode = data.ResponseStatusCode,
                Url = data.Url,
                TargetId = data.TargetId,
                ContentId = data.ContentId,
                EventName = data.EventName,
                ErrorMessage = data.ErrorMessage,
                GeneralData = data.GeneralData
            });

            return System.Threading.Tasks.Task.CompletedTask;
        }

        public STT.Task<IEnumerable<IStatisticalDataRecord>> LoadUsageListAsync(string dataType, int[] relatedTargetIds, DateTime endTimeExclusive, int count, CancellationToken cancel)
        {
            IStatisticalDataRecord[] result;
            if (relatedTargetIds == null || relatedTargetIds.Length == 0)
            {
                result = Storage
                    .Where(r => r.DataType == dataType && r.CreationTime < endTimeExclusive)
                    .OrderByDescending(r => r.CreationTime)
                    .Take(count)
                    .Select(CloneRecord)
                    .ToArray();
            }
            else
            {
                result = Storage
                    .Where(r => r.DataType == dataType && r.CreationTime < endTimeExclusive
                                && r.TargetId.HasValue && relatedTargetIds.Contains(r.TargetId.Value))
                    .OrderByDescending(r => r.CreationTime)
                    .Take(count)
                    .Select(CloneRecord)
                    .ToArray();
            }

            return STT.Task.FromResult((IEnumerable<IStatisticalDataRecord>)result);
        }

        public STT.Task<IEnumerable<Aggregation>> LoadAggregatedUsageAsync(string dataType, TimeResolution resolution,
            DateTime startTime, DateTime endTimeExclusive, CancellationToken cancel)
        {
            var result = Aggregations.Where(x =>
                x.DataType == dataType &&
                x.Resolution == resolution &&
                x.Date >= startTime &&
                x.Date < endTimeExclusive)
                .Select(CloneAggregation)
                .ToArray();
            return STT.Task.FromResult((IEnumerable<Aggregation>)result);
        }

        public STT.Task<DateTime?[]> LoadFirstAggregationTimesByResolutionsAsync(string dataType, CancellationToken httpContextRequestAborted)
        {
            var result = new DateTime?[4];
            for (var resolution = TimeResolution.Minute; resolution <= TimeResolution.Month; resolution++)
            {
                result[(int)resolution] = Aggregations
                    .FirstOrDefault(x => x.DataType == dataType && x.Resolution == resolution)?.Date;
            }

            return STT.Task.FromResult(result);
        }

        public STT.Task EnumerateDataAsync(string dataType, DateTime startTime, DateTime endTimeExclusive,
            Action<IStatisticalDataRecord> aggregatorCallback, CancellationToken cancel)
        {
            var relatedItems = Storage.Where(
                x => x.DataType == dataType && x.CreationTime >= startTime && x.CreationTime < endTimeExclusive);

            foreach (var item in relatedItems)
            {
                cancel.ThrowIfCancellationRequested();
                aggregatorCallback(item);
            }

            return STT.Task.CompletedTask;
        }

        public STT.Task WriteAggregationAsync(Aggregation aggregation, CancellationToken cancel)
        {
            var existing = Aggregations.FirstOrDefault(x =>
                x.DataType == aggregation.DataType && x.Date == aggregation.Date &&
                x.Resolution == aggregation.Resolution);
            if (existing == null)
                Aggregations.Add(CloneAggregation(aggregation));
            else
                existing.Data = aggregation.Data;
            return STT.Task.CompletedTask;
        }

        public STT.Task CleanupRecordsAsync(string dataType, DateTime retentionTime, CancellationToken cancel)
        {
            var toDelete = Storage.Where(x => x.DataType == dataType && x.CreationTime < retentionTime).ToArray();
            foreach (var item in toDelete)
                Storage.Remove(item);
            return STT.Task.CompletedTask;
        }

        public STT.Task CleanupAggregationsAsync(string dataType, TimeResolution resolution, DateTime retentionTime,
            CancellationToken cancel)
        {
            var toDelete = Aggregations
                .Where(x => x.DataType == dataType && x.Resolution == resolution && x.Date < retentionTime).ToArray();
            foreach (var item in toDelete)
                Aggregations.Remove(item);
            return STT.Task.CompletedTask;
        }

        private IStatisticalDataRecord CloneRecord(IStatisticalDataRecord record)
        {
            return new StatisticalDataRecord
            {
                Id = record.Id,
                DataType = record.DataType,
                WrittenTime = record.WrittenTime,
                CreationTime = record.CreationTime,
                Duration = record.Duration,
                RequestLength = record.RequestLength,
                ResponseLength = record.ResponseLength,
                ResponseStatusCode = record.ResponseStatusCode,
                Url = record.Url,
                TargetId = record.TargetId,
                ContentId = record.ContentId,
                EventName = record.EventName,
                ErrorMessage = record.ErrorMessage,
                GeneralData = record.GeneralData
            };
        }
        private Aggregation CloneAggregation(Aggregation aggregation)
        {
            return new Aggregation
            {
                DataType = aggregation.DataType,
                Date = aggregation.Date,
                Resolution = aggregation.Resolution,
                Data = aggregation.Data
            };
        }
    }
}
