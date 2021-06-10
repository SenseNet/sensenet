namespace SenseNet.Services.Core.Diagnostics
{
    public class RetentionSection
    {
        public AggregationRetention ApiCalls { get; set; } = new();
        public AggregationRetention WebHooks { get; set; } = new();
        public AggregationRetention DatabaseUsage { get; set; } = new();
        public AggregationRetention General { get; set; } = new();
    }

    public class AggregationRetention
    {
        public int Momentary { get; set; } = 60 * 3;
        public int Minutely { get; set; } = 60 * 3;
        public int Hourly { get; set; } = 24 * 3;
        public int Daily { get; set; } = 31 * 3;
        public int Monthly { get; set; } = 12 * 3;
    }

    public class StatisticsOptions
    {
        public RetentionSection Retention { get; set; } = new();
    }
}
