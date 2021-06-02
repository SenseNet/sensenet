using System;
using System.Collections.Generic;
using System.Threading;
using SenseNet.Diagnostics;

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

        public System.Threading.Tasks.Task WriteData(IStatisticalDataRecord data)
        {
            Storage.Add(new StatisticalDataRecord
            {
                Id = GetNextId(),
                DataType = data.DataType,
                WrittenTime = DateTime.UtcNow,
                RequestTime = data.RequestTime,
                ResponseTime = data.ResponseTime,
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
    }
}
