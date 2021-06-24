using Microsoft.Extensions.DependencyInjection;
using SenseNet.Diagnostics;
using SenseNet.Services.Core;
using SenseNet.Services.Core.Diagnostics;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    public static class StatisticsExtensions
    {
        public static IServiceCollection AddStatisticalDataAggregator<T>(this IServiceCollection services)
            where T : class, IStatisticalDataAggregator
        {
            return services
                .AddSingleton<IStatisticalDataAggregator, T>();
        }
        public static IServiceCollection AddStatistics(this IServiceCollection services)
        {
            return services
                .AddDefaultStatisticalDataProvider()
                .AddStatisticalDataCollector<StatisticalDataCollector>()
                .AddTransient<WebTransferRegistrator>()
                .AddSingleton<IStatisticalDataAggregator, WebTransferStatisticalDataAggregator>()
                .AddSingleton<IStatisticalDataAggregator, DatabaseUsageStatisticalDataAggregator>()
                .AddSingleton<IStatisticalDataAggregationController, StatisticalDataAggregationController>();
        }
    }
}
