using SenseNet.Configuration;
using SenseNet.Tests.Core.Implementations;
using SenseNet.Tools;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    public static class TestingDataProviderExtension
    {
        public static IRepositoryBuilder UseTestingDataProvider(this IRepositoryBuilder repositoryBuilder, ITestingDataProvider provider)
        {
            Providers.Instance.SetProvider(typeof(ITestingDataProvider), provider);
            return repositoryBuilder;
        }
    }
}
