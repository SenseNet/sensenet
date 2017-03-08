using System.IO;
using SenseNet.ContentRepository.Storage.Data;

namespace SenseNet.ContentRepository.Storage
{
    /// <summary>
    /// Encapsulates identifiers and raw values for a binary property.
    /// </summary>
	public class BinaryDataValue
	{
        /// <summary>
        /// Binary property id.
        /// </summary>
        public int Id { get; set; }

        private int _fileId;
        /// <summary>
        /// File row id in the metadata database.
        /// </summary>
        public int FileId
        {
            get { return _fileId; }
            set
            {
                _fileId = value;
                SetStreamId(value);
            }
        }

        /// <summary>
        /// Size of the full binary stream.
        /// </summary>
        public long Size { get; set; }
        /// <summary>
        /// Binary file name.
        /// </summary>
        public BinaryFileName FileName { get; set; }
        /// <summary>
        /// Binary content type (MIME type).
        /// </summary>
        public string ContentType { get; set; }
        /// <summary>
        /// Binary checksum (currently not used and empty).
        /// </summary>
        public string Checksum { get; set; }
        /// <summary>
        /// Binary data stream.
        /// </summary>
        public Stream Stream { get; set; }
        /// <summary>
        /// Database timestamp of the binary row creation.
        /// </summary>
        public long Timestamp { get; set; }

        /// <summary>
        /// Name of the blob provider that was stores the binary data.
        /// </summary>
        public string BlobProviderName { get; set; }
        /// <summary>
        /// Provider-specific blob metadata.
        /// </summary>
        public string BlobProviderData { get; set; }

        /// <summary>
        /// Gets a value indicating whether this binary data object is empty or not.
        /// </summary>
        public bool IsEmpty
		{
			get
			{
                if (Id > 0) return false;
                if (FileId > 0) return false;
                if (Size >= 0) return false;
				if (!string.IsNullOrEmpty(FileName)) return false;
				if (!string.IsNullOrEmpty(ContentType)) return false;
				return Stream == null;
			}
		}

        private void SetStreamId(int fileId)
        {
            var repoStream = Stream as RepositoryStream;
            if (repoStream == null)
                return;
            repoStream.FileId = fileId;
        }
    }
}