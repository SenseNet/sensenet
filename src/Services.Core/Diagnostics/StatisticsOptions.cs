using SenseNet.Diagnostics;

namespace SenseNet.Services.Core.Diagnostics
{
    public class RetentionSection
    {
        public AggregationRetentionPeriods ApiCalls { get; set; } = new();
        public AggregationRetentionPeriods WebHooks { get; set; } = new();
        public AggregationRetentionPeriods DatabaseUsage { get; set; } = new();
        public AggregationRetentionPeriods General { get; set; } = new();
    }

    public class StatisticsOptions
    {
        public RetentionSection Retention { get; set; } = new();
    }
}
