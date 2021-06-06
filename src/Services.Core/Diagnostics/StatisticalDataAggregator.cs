using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SenseNet.Diagnostics;

namespace SenseNet.Services.Core.Diagnostics
{
    public class WebHookStatisticalDataAggregator : IStatisticalDataAggregator
    {
        public static readonly string DataType = "WebHook";

        internal class WebHookAggregation
        {
            public int CallCount { get; set; }
            public int[] StatusCounts { get; set; } = new int[5];
            public long RequestLengths { get; set; }
            public long ResponseLengths { get; set; }
        }

        private readonly IStatisticalDataProvider _statDataProvider;
        private WebHookAggregation _aggregation;

        public WebHookStatisticalDataAggregator(IStatisticalDataProvider statDataProvider)
        {
            _statDataProvider = statDataProvider;
        }

        public async Task AggregateAsync(DateTime startTime, TimeResolution resolution, CancellationToken cancel)
        {
            var start = startTime.Truncate(resolution);
            var end = startTime.Next(resolution);
            _aggregation = new WebHookAggregation();

            await _statDataProvider.EnumerateDataAsync("WebHook", start, end, resolution, Aggregate, cancel);

            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
                JsonSerializer.Create().Serialize(writer, _aggregation);

            var aggregation = new Aggregation
            {
                DataType = DataType,
                Date = start,
                Resolution = resolution,
                Data = sb.ToString()
            };

            await _statDataProvider.WriteAggregationAsync(aggregation, cancel);
        }

        private void Aggregate(IStatisticalDataRecord record)
        {
            _aggregation.CallCount++;
            _aggregation.RequestLengths += record.RequestLength ?? 0;
            _aggregation.ResponseLengths += record.ResponseLength ?? 0;
            var leadDigit = (record.ResponseStatusCode ?? 0) / 100 - 1;
            if (leadDigit is >= 0 and < 5)
                _aggregation.StatusCounts[leadDigit]++;
        }
    }
}
