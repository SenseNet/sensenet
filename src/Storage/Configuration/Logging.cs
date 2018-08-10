using System.Diagnostics;
using System.Linq;

// ReSharper disable once CheckNamespace
// ReSharper disable RedundantTypeArgumentsOfMethod
namespace SenseNet.Configuration
{
    public class Logging : SnConfig
    {
        private const string SectionName = "sensenet/logging";

        public static bool PerformanceCountersEnabled { get; internal set; } = GetValue<bool>(SectionName, "PerformanceCountersEnabled");

        public static CounterCreationDataCollection CustomPerformanceCounters { get; } =
            new CounterCreationDataCollection(GetListOrEmpty<string>(SectionName, "CustomPerformanceCounters").Distinct()
                .Select(cn => new CounterCreationData
                {
                    CounterType = PerformanceCounterType.NumberOfItems32,
                    CounterName = cn
                }).ToArray());

        public static bool DownloadCounterEnabled { get; internal set; } = GetValue<bool>(SectionName, "DownloadCounterEnabled");
        public static bool AuditEnabled { get; internal set; } = GetValue<bool>(SectionName, "AuditEnabled", true);
        public static string EventLogName { get; internal set; } = GetValue(SectionName, "EventLogName", "SenseNet");
        public static string EventLogSourceName { get; internal set; } = GetValue(SectionName, "EventLogSourceName", "SenseNetInstrumentation");
    }
}
