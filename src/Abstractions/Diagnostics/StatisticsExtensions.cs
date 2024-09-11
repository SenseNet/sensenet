﻿using System;
using Microsoft.Extensions.DependencyInjection;
using SenseNet.Diagnostics;

namespace SenseNet.Diagnostics
{
    public static class StatisticsExtensions
    {
        public static DateTime Truncate(this DateTime d, TimeResolution resolution)
        {
            switch (resolution)
            {
                case TimeResolution.Minute: return new DateTime(d.Year, d.Month, d.Day, d.Hour, d.Minute, 0, DateTimeKind.Utc);
                case TimeResolution.Hour: return new DateTime(d.Year, d.Month, d.Day, d.Hour, 0, 0, DateTimeKind.Utc);
                case TimeResolution.Day: return new DateTime(d.Year, d.Month, d.Day, 0, 0, 0, DateTimeKind.Utc);
                case TimeResolution.Month: return new DateTime(d.Year, d.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                default:
                    throw new ArgumentOutOfRangeException(nameof(resolution), resolution, null);
            }
        }
        public static DateTime Truncate(this DateTime d, TimeWindow timeWindow)
        {
            switch (timeWindow)
            {
                case TimeWindow.Hour: return new DateTime(d.Year, d.Month, d.Day, d.Hour, 0, 0, DateTimeKind.Utc);
                case TimeWindow.Day: return new DateTime(d.Year, d.Month, d.Day, 0, 0, 0, DateTimeKind.Utc);
                case TimeWindow.Month: return new DateTime(d.Year, d.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                case TimeWindow.Year: return new DateTime(d.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                default:
                    throw new ArgumentOutOfRangeException(nameof(timeWindow), timeWindow, null);
            }
        }
        public static DateTime Next(this DateTime d, TimeResolution timeWindow)
        {
            switch (timeWindow)
            {
                case TimeResolution.Minute: return new DateTime(d.Year, d.Month, d.Day, d.Hour, d.Minute, 0).AddMinutes(1);
                case TimeResolution.Hour: return new DateTime(d.Year, d.Month, d.Day, d.Hour, 0, 0).AddHours(1);
                case TimeResolution.Day: return new DateTime(d.Year, d.Month, d.Day, 0, 0, 0).AddDays(1);
                case TimeResolution.Month: return new DateTime(d.Year, d.Month, 1, 0, 0, 0).AddMonths(1);
                default:
                    throw new ArgumentOutOfRangeException(nameof(timeWindow), timeWindow, null);
            }
        }
        public static DateTime Next(this DateTime d, TimeWindow timeWindow)
        {
            switch (timeWindow)
            {
                case TimeWindow.Hour: return new DateTime(d.Year, d.Month, d.Day, d.Hour, 0, 0).AddHours(1);
                case TimeWindow.Day: return new DateTime(d.Year, d.Month, d.Day, 0, 0, 0).AddDays(1);
                case TimeWindow.Month: return new DateTime(d.Year, d.Month, 1, 0, 0, 0).AddMonths(1);
                case TimeWindow.Year: return new DateTime(d.Year, 1, 1, 0, 0, 0).AddYears(1);
                default:
                    throw new ArgumentOutOfRangeException(nameof(timeWindow), timeWindow, null);
            }
        }
        public static DateTime AddPeriods(this DateTime d, int periods, TimeResolution resolution)
        {
            switch (resolution)
            {
                case TimeResolution.Minute: return d.AddMinutes(periods);
                case TimeResolution.Hour: return d.AddHours(periods);
                case TimeResolution.Day: return d.AddDays(periods);
                case TimeResolution.Month: return d.AddMonths(periods);
                default: throw new ArgumentOutOfRangeException(nameof(resolution), resolution, null);
            }
        }

    }
}
// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    public static class StatisticsExtensions
    {
        public static IServiceCollection AddDefaultStatisticalDataCollector(this IServiceCollection services)
        {
            return services.AddStatisticalDataCollector<NullStatisticalDataCollector>();
        }
        public static IServiceCollection AddStatisticalDataCollector<T>(this IServiceCollection services) where T : class, IStatisticalDataCollector
        {
            return services.AddSingleton<IStatisticalDataCollector, T>();
        }
        public static IServiceCollection AddStatisticalDataAggregator<T>(this IServiceCollection services) where T : class, IStatisticalDataAggregator
        {
            return services.AddTransient<IStatisticalDataAggregator, T>();
        }

        public static IServiceCollection AddDefaultStatisticalDataProvider(this IServiceCollection services)
        {
            return services.AddStatisticalDataProvider<NullStatisticalDataProvider>();
        }
        public static IServiceCollection AddStatisticalDataProvider<T>(this IServiceCollection services) where T : class, IStatisticalDataProvider
        {
            return services.AddSingleton<IStatisticalDataProvider, T>();
        }
    }
}