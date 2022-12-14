﻿using Microsoft.Extensions.DependencyInjection;
using SenseNet.ContentRepository;
using SenseNet.Services.Core.Diagnostics;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    public static class StatisticsExtensions
    {
        public static IServiceCollection AddStatistics(this IServiceCollection services)
        {
            return services
                .AddDefaultStatisticalDataProvider()
                .AddStatisticalDataCollector<StatisticalDataCollector>()
                .AddTransient<WebTransferRegistrator>()
                .AddStatisticalDataAggregator<WebTransferStatisticalDataAggregator>()
                .AddStatisticalDataAggregator<DatabaseUsageStatisticalDataAggregator>()
                .AddSingleton<IStatisticalDataAggregationController, StatisticalDataAggregationController>()
                .AddSingleton<IDatabaseUsageHandler, DatabaseUsageHandler>();
        }
    }
}
