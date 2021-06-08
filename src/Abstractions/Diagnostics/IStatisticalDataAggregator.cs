namespace SenseNet.Diagnostics
{
    public enum TimeResolution { Minute = 0, Hour = 1, Day = 2, Month = 3 }

    public enum TimeWindow { Hour = 0, Day = 1, Month = 2, Year = 3 }

    public interface IStatisticalDataAggregator
    {
        string DataType { get; }
        bool IsEmpty { get; }
        object Data { get; }

        void Aggregate(IStatisticalDataRecord data);
        void Summarize(Aggregation[] aggregations);
    }
}
