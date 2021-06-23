namespace SenseNet.Diagnostics
{
    public class RetentionSection
    {
        public AggregationRetentionPeriods ApiCalls { get; set; } = new AggregationRetentionPeriods();
        public AggregationRetentionPeriods WebHooks { get; set; } = new AggregationRetentionPeriods();
        public AggregationRetentionPeriods DatabaseUsage { get; set; } = new AggregationRetentionPeriods();
        public AggregationRetentionPeriods General { get; set; } = new AggregationRetentionPeriods();
    }

    public class StatisticsOptions
    {
        public RetentionSection Retention { get; set; } = new RetentionSection();
    }
}
