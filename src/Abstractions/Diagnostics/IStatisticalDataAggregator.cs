using System;

namespace SenseNet.Diagnostics
{
    public enum TimeResolution { Minute = 0, Hour = 1, Day = 2, Month = 3 }

    public enum TimeWindow { Hour = 0, Day = 1, Month = 2, Year = 3 }

    public class AggregationRetentionPeriods
    {
        public int Momentary { get; set; } = 3; // 60 * 3 seconds
        public int Minutely { get; set; } = 3; // 60 * 3 minutes
        public int Hourly { get; set; } = 3; // 24 * 3 hours
        public int Daily { get; set; } = 3; // 31 * 3 days
        public int Monthly { get; set; } = 3; // 12 * 3 months
    }

    public interface IStatisticalDataAggregator
    {
        string DataType { get; }
        bool IsEmpty { get; }
        object Data { get; }
        AggregationRetentionPeriods RetentionPeriods { get; }

        void Aggregate(IStatisticalDataRecord data);
        void Summarize(Aggregation[] aggregations);
    }
}
