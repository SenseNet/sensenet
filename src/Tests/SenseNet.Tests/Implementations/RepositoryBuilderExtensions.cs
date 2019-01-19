using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Tools;

namespace SenseNet.Tests.Implementations
{

    public static class RepositoryBuilderExtensions
    {
        public static IRepositoryBuilder UseTestingDataProvider(this IRepositoryBuilder repositoryBuilder, ITestingDataProvider provider)
        {
            DataProvider.Instance().SetProvider(typeof(ITestingDataProvider), provider);
            return repositoryBuilder;
        }
    }
}
