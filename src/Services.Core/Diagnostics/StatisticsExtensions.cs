using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using SenseNet.Diagnostics;

namespace SenseNet.Services.Core.Diagnostics
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
                .AddSingleton<IStatisticalDataAggregator, WebTransferStatisticalDataAggregator>()
                .AddSingleton<IStatisticalDataAggregator, DatabaseUsageStatisticalDataAggregator>()
                .AddSingleton<IStatisticalDataAggregationController, StatisticalDataAggregationController>();
        }
    }
}
