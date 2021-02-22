namespace SenseNet.Storage.DataModel.Usage
{
    /// <summary>
    /// Represents a long text property in the database usage profile.
    /// </summary>
    public class LongTextModel
    {
        public int VersionId { get; set; }
        /// <summary>
        /// Size of the text value in bytes.
        /// </summary>
        public long Size { get; set; }
    }
}
