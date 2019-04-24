using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Tools;

namespace SenseNet.Tests.Implementations
{

    public static class RepositoryBuilderExtensions
    {
        public static IRepositoryBuilder UseTestingDataProviderExtension(this IRepositoryBuilder repositoryBuilder, ITestingDataProviderExtension provider)
        {
            if(DataStore.Enabled)
                DataStore.DataProvider.SetExtension(typeof(ITestingDataProviderExtension), provider);
            else
                DataProvider.Instance.SetExtension(typeof(ITestingDataProviderExtension), provider); //DB:??
            return repositoryBuilder;
        }
    }
}
