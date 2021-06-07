using System.Threading;
using System.Threading.Tasks;
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
    }
}
