using SenseNet.ContentRepository.Storage;

namespace SenseNet.Storage.DataModel.Usage
{
    /// <summary>
    /// Represents a binary property in the database usage profile.
    /// </summary>
    /// <remarks>
    /// The binary property is a linker object between a <see cref="Node"/> and a <c>File</c> representation.
    /// </remarks>
    public class BinaryPropertyModel
    {
        public int VersionId { get; set; }
        public int FileId { get; set; }
    }
}
