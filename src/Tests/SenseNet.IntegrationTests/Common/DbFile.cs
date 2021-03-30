using System;
using System.Collections.Generic;
using System.Text;

namespace SenseNet.IntegrationTests.Common
{
    /// <summary>
    /// Represents a platform independent File object in a database.
    /// Designed for BlobStorage integration tests.
    /// </summary>
    public class DbFile
    {
        public int FileId;
        public string ContentType;
        public string FileNameWithoutExtension;
        public string Extension;
        public long Size;
        public string Checksum;
        public byte[] Stream;
        public DateTime CreationDate;
        public Guid RowGuid;
        public long Timestamp;
        public bool? Staging;
        public int StagingVersionId;
        public int StagingPropertyTypeId;
        public bool? IsDeleted;
        public string BlobProvider;
        public string BlobProviderData;
        public byte[] ExternalStream;
    }
}
