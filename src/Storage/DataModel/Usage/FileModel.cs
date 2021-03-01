using SenseNet.ContentRepository.Storage;

namespace SenseNet.Storage.DataModel.Usage
{
    /// <summary>
    /// Represents a blob in the database usage profile.
    /// </summary>
    /// <remarks>
    /// It is connected to a <see cref="Node"/> through a binary property.
    /// If the connection is broken, the file is deletable (orphaned).
    /// </remarks>
    public class FileModel
    {
        public int FileId { get; set; }
        /// <summary>
        /// Size of the stream.
        /// </summary>
        public long Size { get; set; }
        /// <summary>
        /// Size of the stream if it is stored in the built in table otherwise 0.
        /// </summary>
        public long StreamSize { get; set; }
    }
}
