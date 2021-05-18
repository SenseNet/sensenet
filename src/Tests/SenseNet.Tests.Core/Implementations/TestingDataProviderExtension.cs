using SenseNet.Configuration;
using SenseNet.Tests.Core.Implementations;
using SenseNet.Tools;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    public static class TestingDataProviderExtension
    {
        public static IRepositoryBuilder UseTestingDataProviderExtension(this IRepositoryBuilder repositoryBuilder, ITestingDataProviderExtension provider)
        {
            Providers.Instance.DataProvider.SetExtension(typeof(ITestingDataProviderExtension), provider);
            return repositoryBuilder;
        }
    }
}
