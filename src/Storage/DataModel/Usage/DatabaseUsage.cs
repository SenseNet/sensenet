using System;

namespace SenseNet.Storage.DataModel.Usage
{
    public class DatabaseUsage
    {
        public Dimensions Content { get; set; }
        public Dimensions OldVersions { get; set; }
        public Dimensions Preview { get; set; }
        public Dimensions System { get; set; }
        public LogDimensions OperationLog { get; set; }
        public long OrphanedBlobs { get; set; }
        public DateTime Executed { get; set; }
        public TimeSpan ExecutionTime { get; set; }
    }
}
