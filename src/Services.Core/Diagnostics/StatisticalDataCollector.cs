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

        public Task RegisterWebTransfer(WebTransferStatInput data)
        {
            return _statDataProvider.WriteData(new InputStatisticalDataRecord(data));
        }
        public Task RegisterWebHook(WebHookStatInput data)
        {
            return _statDataProvider.WriteData(new InputStatisticalDataRecord(data));
        }
        public Task RegisterGeneralData(GeneralStatInput data)
        {
            return _statDataProvider.WriteData(new InputStatisticalDataRecord(data));
        }
    }
}
