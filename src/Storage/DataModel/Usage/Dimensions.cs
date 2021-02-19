namespace SenseNet.Storage.DataModel.Usage
{
    /// <summary>
    /// Represents aggregated counters.
    /// </summary>
    public class Dimensions
    {
        /// <summary>
        /// Count of rows.
        /// </summary>
        public int Count { get; set; }
        /// <summary>
        /// Size of blobs in bytes.
        /// </summary>
        public long Blob { get; set; }
        /// <summary>
        /// Size of metadata in bytes.
        /// </summary>
        /// <remarks>
        /// Summary of the <c>DynamicPropertiesSize</c>, <c>ContentListPropertiesSize</c> and <c>ChangedDataSize</c>
        /// of the related <see cref="NodeModel"/>s.
        /// </remarks>
        public long Metadata { get; set; }
        /// <summary>
        /// Size of long text properties in bytes.
        /// </summary>
        public long Text { get; set; }
        /// <summary>
        /// Size of the precompiled index documents in bytes.
        /// </summary>
        public long Index { get; set; }

        /// <summary>
        /// Combines the given <see cref="Dimensions"/> and returns a new one. 
        /// </summary>
        public static Dimensions Sum(params Dimensions[] items)
        {
            var d = new Dimensions();
            foreach (var item in items)
            {
                d.Count += item.Count;
                d.Blob += item.Blob;
                d.Metadata += item.Metadata;
                d.Text += item.Text;
                d.Index += item.Index;
            }
            return d;
        }

        /// <summary>
        /// Returns a copy of this object.
        /// </summary>
        /// <returns></returns>
        public Dimensions Clone()
        {
            return new()
            {
                Count = Count,
                Blob = Blob,
                Metadata = Metadata,
                Text = Text,
                Index = Index
            };
        }
    }
    /// <summary>
    /// Represents aggregated counters.
    /// </summary>
    public class LogDimensions
    {
        /// <summary>
        /// Count of rows.
        /// </summary>
        public int Count { get; set; }
        /// <summary>
        /// Size of metadata in bytes.
        /// </summary>
        /// <remarks>
        /// Summary of the <c>DynamicPropertiesSize</c>, <c>ContentListPropertiesSize</c> and <c>ChangedDataSize</c>
        /// of the related <see cref="NodeModel"/>s.
        /// </remarks>
        public long Metadata { get; set; }
        /// <summary>
        /// Size of long text properties in bytes.
        /// </summary>
        public long Text { get; set; }

        /// <summary>
        /// Returns a copy of this object.
        /// </summary>
        /// <returns></returns>
        public LogDimensions Clone()
        {
            return new()
            {
                Count = Count,
                Metadata = Metadata,
                Text = Text,
            };
        }
    }
}
