using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SenseNet.Diagnostics;

namespace SenseNet.Services.Core.Diagnostics
{
    public class StatisticalDataCollector : IStatisticalDataCollector
    {
        private readonly IStatisticalDataProvider _statDataProvider;

        public StatisticalDataCollector(IStatisticalDataProvider statDataProvider)
        {
            _statDataProvider = statDataProvider;
        }

        public Task RegisterWebTransfer(WebTransferStatInput data, CancellationToken cancel)
        {
            return _statDataProvider.WriteDataAsync(new InputStatisticalDataRecord(data), cancel);
        }
        public Task RegisterWebHook(WebHookStatInput data, CancellationToken cancel)
        {
            return _statDataProvider.WriteDataAsync(new InputStatisticalDataRecord(data), cancel);
        }
        public Task RegisterGeneralData(GeneralStatInput data, CancellationToken cancel)
        {
            return _statDataProvider.WriteDataAsync(new InputStatisticalDataRecord(data), cancel);
        }

        public async Task RegisterGeneralData(string dataType, TimeResolution resolution, object data, CancellationToken cancel)
        {
            var aggregation = new Aggregation
            {
                DataType = dataType,
                Date = DateTime.UtcNow.Truncate(resolution),
                Resolution = resolution,
                Data = Serialize(data)
            };

            await _statDataProvider.WriteAggregationAsync(aggregation, cancel);
        }

        internal static string Serialize(object data)
        {
            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
                JsonSerializer.Create().Serialize(writer, data);
            return sb.ToString();
        }

    }
}
