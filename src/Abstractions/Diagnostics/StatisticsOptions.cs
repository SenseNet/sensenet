using SenseNet.Tools.Configuration;

namespace SenseNet.Diagnostics
{
    public class RetentionSection
    {
        public AggregationRetentionPeriods ApiCalls { get; set; } = new AggregationRetentionPeriods();
        public AggregationRetentionPeriods WebHooks { get; set; } = new AggregationRetentionPeriods();
        public AggregationRetentionPeriods DatabaseUsage { get; set; } = new AggregationRetentionPeriods();
        public AggregationRetentionPeriods General { get; set; } = new AggregationRetentionPeriods();
    }

    [OptionsClass(sectionName: "sensenet:statistics")]
    public class StatisticsOptions
    {
        public bool Enabled { get; set; } = true;
        public RetentionSection Retention { get; set; } = new RetentionSection();
    }
}
