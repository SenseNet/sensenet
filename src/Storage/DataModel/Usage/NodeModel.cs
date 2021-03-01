using SenseNet.ContentRepository.Storage;

namespace SenseNet.Storage.DataModel.Usage
{
    /// <summary>
    /// Represents a version of a <see cref="Node"/> in the database usage profile.
    /// </summary>
    public class NodeModel
    {
        public int NodeId { get; set; }
        public int VersionId { get; set; }
        public int ParentNodeId { get; set; }
        public int NodeTypeId { get; set; }
        /// <summary>
        /// Version in the <c>V{major}.{minor}.{status}</c> format (e.g. V1.2.D).
        /// </summary>
        public string Version { get; set; }
        /// <summary>
        /// True if the VersionId equals with the id of the last public version.
        /// </summary>
        public bool IsLastPublic { get; set; }
        /// <summary>
        /// True if the VersionId equals with the id of the last version.
        /// </summary>
        public bool IsLastDraft { get; set; }
        public int OwnerId { get; set; }
        /// <summary>
        /// Size of the dynamic properties in bytes.
        /// </summary>
        public long DynamicPropertiesSize { get; set; }
        /// <summary>
        /// Size of the content-list properties in bytes.
        /// </summary>
        public long ContentListPropertiesSize { get; set; }
        /// <summary>
        /// Size of the changed data properties in bytes.
        /// </summary>
        public long ChangedDataSize { get; set; }
        /// <summary>
        /// Size of the precompiled index document in bytes.
        /// </summary>
        public long IndexSize { get; set; }
    }
}
