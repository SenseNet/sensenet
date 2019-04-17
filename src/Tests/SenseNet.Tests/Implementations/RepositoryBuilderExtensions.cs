using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Tools;

namespace SenseNet.Tests.Implementations
{

    public static class RepositoryBuilderExtensions
    {
        public static IRepositoryBuilder UseTestingDataProviderExtension(this IRepositoryBuilder repositoryBuilder, ITestingDataProviderExtension provider)
        {
var backup = DataStore.Enabled;
DataStore.Enabled = false;
            DataProvider.Instance.SetExtension(typeof(ITestingDataProviderExtension), provider); //DB:??
DataStore.Enabled = backup;
            return repositoryBuilder;
        }
    }
}
