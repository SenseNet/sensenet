using Microsoft.Extensions.DependencyInjection;
using SenseNet.Diagnostics;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    public static class StatisticsExtensions
    {
        public static IServiceCollection AddStatisticalDataCollector<T>(this IServiceCollection services) where T : class, IStatisticalDataCollector
        {
            return services.AddSingleton<IStatisticalDataCollector, T>();
        }
    }
}