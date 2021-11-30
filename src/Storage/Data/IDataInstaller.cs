using SenseNet.ContentRepository.Storage.DataModel;
using System.Threading;
using System.Threading.Tasks;

namespace SenseNet.ContentRepository.Storage.Data
{
    public interface IDataInstaller
    {
        Task InstallInitialDataAsync(InitialData data, DataProvider dataProvider, CancellationToken cancel);
    }
}
