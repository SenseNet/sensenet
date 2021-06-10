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
                WebHookId = data.WebHookId,
                ContentId = data.ContentId,
                EventName = data.EventName,
                ErrorMessage = data.ErrorMessage,
                GeneralData = data.GeneralData
            });

            return System.Threading.Tasks.Task.CompletedTask;
        }

        public STT.Task CleanupAsync(DateTime timeMax, CancellationToken cancel)
        {
            throw new NotImplementedException(); //UNDONE:<?Stat: Implement CleanupAsync
        }

        public STT.Task LoadUsageListAsync(string dataType, DateTime startTime, TimeResolution resolution, CancellationToken cancel)
        {
            throw new NotImplementedException(); //UNDONE:<?Stat: Implement LoadUsageListAsync
        }

        public STT.Task<IEnumerable<Aggregation>> LoadAggregatedUsageAsync(string dataType, TimeResolution resolution,
            DateTime startTime, DateTime endTimeExclusive, CancellationToken cancel)
        {
            throw new NotImplementedException(); //UNDONE:<?Stat: Implement LoadAggregatedUsageAsync
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
            throw new NotImplementedException(); //UNDONE:<?Stat: Implement WriteAggregation
        }
    }
}
