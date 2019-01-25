using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Tools;

namespace SenseNet.Tests.Implementations
{

    public static class RepositoryBuilderExtensions
    {
        public static IRepositoryBuilder UseTestingDataProviderExtension(this IRepositoryBuilder repositoryBuilder, ITestingDataProviderExtension provider)
        {
            DataProvider.Instance.SetExtension(typeof(ITestingDataProviderExtension), provider);
            return repositoryBuilder;
        }
    }
}
