using System;
using System.IO;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Caching.Dependency;
using System.Globalization;
using SenseNet.ContentRepository.Storage.Data.SqlClient;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.ContentRepository.Storage
{
    /// <summary>
    /// BinaryData class handles the data of binary properties.
    /// </summary>
	public class BinaryData : IDynamicDataAccessor
    {
        private BinaryDataValue __privateValue;

        // =============================================== Accessor Interface

        Node IDynamicDataAccessor.OwnerNode
        {
            get { return OwnerNode; }
            set { OwnerNode = value; }
        }
        PropertyType IDynamicDataAccessor.PropertyType
        {
            get { return PropertyType; }
            set { PropertyType = value; }
        }
        object IDynamicDataAccessor.RawData
        {
            get { return RawData; }
            set { RawData = (BinaryDataValue)value; }
        }
        object IDynamicDataAccessor.GetDefaultRawData() { return GetDefaultRawData(); }

        // =============================================== Accessor Implementation

        internal Node OwnerNode { get; set; }
        internal PropertyType PropertyType { get; set; }
        internal static BinaryDataValue GetDefaultRawData()
        {
            return new BinaryDataValue
            {
                Id = 0,
                FileId = 0,
                ContentType = String.Empty,
                FileName = String.Empty,
                Size = -1,
                BlobProviderName = null,
                BlobProviderData = null,
                Checksum = string.Empty,
                Stream = null
            };
        }

        private BinaryDataValue RawData
        {
            get
            {
                if (OwnerNode == null)
                    return __privateValue;

                var value = (BinaryDataValue)OwnerNode.Data.GetDynamicRawData(PropertyType);
                return value;
            }
            set
            {
                __privateValue = new BinaryDataValue
                {
                    Id = value.Id,
                    FileId = value.FileId,
                    ContentType = value.ContentType,
                    FileName = value.FileName,
                    Size = value.Size,
                    BlobProviderName = value.BlobProviderName,
                    BlobProviderData = value.BlobProviderData,
                    Checksum = value.Checksum,
                    Stream = CloneStream(value.Stream),
                    Timestamp = value.Timestamp
                };
            }
        }
        public bool IsEmpty
        {
            get
            {
                if (OwnerNode == null)
                    return __privateValue.IsEmpty;
                if (RawData == null)
                    return true;
                return RawData.IsEmpty;
            }
        }

        // =============================================== Data

        public bool IsModified
        {
            get
            {
                if (OwnerNode == null)
                    return true;
                return OwnerNode.Data.IsModified(PropertyType);
            }
        }
        private void Modifying()
        {
            if (IsModified)
                return;

            // Clone
            var orig = (BinaryDataValue)OwnerNode.Data.GetDynamicRawData(PropertyType);
            BinaryDataValue data;
            if (orig == null)
            {
                data = GetDefaultRawData();
            }
            else
            {
                data = new BinaryDataValue
                {
                    Id = orig.Id,
                    FileId = orig.FileId,
                    ContentType = orig.ContentType,
                    FileName = orig.FileName,
                    Size = orig.Size,
                    BlobProviderName = orig.BlobProviderName,
                    BlobProviderData = orig.BlobProviderData,
                    Checksum = orig.Checksum,
                    Stream = orig.Stream
                };
            }
            OwnerNode.MakePrivateData();
            OwnerNode.Data.SetDynamicRawData(PropertyType, data, false);
        }
        private void Modified()
        {
            if (OwnerNode != null)
                if (OwnerNode.Data.SharedData != null)
                    OwnerNode.Data.CheckChanges(PropertyType);
        }

        // =============================================== Accessors

        public int Id
        {
            get { return RawData == null ? 0 : RawData.Id; }
            internal set
            {
                Modifying();
                RawData.Id = value;
                Modified();
            }
        }
        public int FileId
        {
            get { return RawData == null ? 0 : RawData.FileId; }
            internal set
            {
                Modifying();
                RawData.FileId = value;
                Modified();
            }
        }
        public long Size
        {
            get { return RawData == null ? -1 : RawData.Size; }
            internal set
            {
                Modifying();
                RawData.Size = value;
                Modified();
            }
        }
        public BinaryFileName FileName
        {
            get { return RawData == null ? new BinaryFileName("") : RawData.FileName; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                Modifying();
                var rawData = this.RawData;
                value = NormalizeFileName(value);
                rawData.FileName = value;
                rawData.ContentType = GetMimeType(value);
                Modified();
            }
        }
        public string ContentType
        {
            get { return RawData == null ? string.Empty : RawData.ContentType; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                Modifying();
                RawData.ContentType = value;
                Modified();
            }
        }
        public string Checksum
        {
            get
            {
                var raw = RawData;
                if (raw == null)
                    return null;
                return raw.Checksum;
            }
        }
        public long Timestamp
        {
            get
            {
                var raw = RawData;
                return raw == null ? 0 : raw.Timestamp;
            }
        }

        /// <summary>
        /// Gets a token that represents this particular binary in the database. It can be used to read
        /// the bytes directly through the blob storage component.
        /// </summary>
        public string GetToken()
        {
            return new ChunkToken
            {
                VersionId = OwnerNode.VersionId,
                PropertyTypeId = PropertyType.Id,
                BinaryPropertyId = RawData.Id,
                FileId = FileId
            }.ToString();
        }

        public string BlobProvider
        {
            get { return RawData == null ? null : RawData.BlobProviderName; }
            internal set
            {
                Modifying();
                RawData.BlobProviderName = value;
                Modified();
            }
        }
        public string BlobProviderData
        {
            get { return RawData == null ? null : RawData.BlobProviderData; }
            internal set
            {
                Modifying();
                RawData.BlobProviderData = value;
                Modified();
            }
        }

        public Stream GetStream()
        {
            var raw = RawData;
            if (raw == null)
                return null;
            var stream = raw.Stream;
            if (stream != null)
                return CloneStream(stream);
            if (OwnerNode == null)
                return null;

            if (this.OwnerNode.SavingState != ContentSavingState.Finalized)
                throw new InvalidOperationException(SR.GetString(SR.Exceptions.General.Error_AccessToNotFinalizedBinary_2, this.OwnerNode.Path, this.PropertyType.Name));

            return DataBackingStore.GetBinaryStream(OwnerNode.Id, OwnerNode.VersionId, PropertyType.Id);
        }
        public Stream GetStreamWithoutDbRead()
        {
            var raw = RawData;
            if (raw == null)
                return null;
            var stream = raw.Stream;
            if (stream != null)
                return CloneStream(stream);
            if (OwnerNode == null)
                return null;

            if (this.OwnerNode.SavingState != ContentSavingState.Finalized)
                throw new InvalidOperationException(SR.GetString(SR.Exceptions.General.Error_AccessToNotFinalizedBinary_2, this.OwnerNode.Path, this.PropertyType.Name));

            return null;
        }
        public void SetStream(Stream stream)
        {
            Modifying();
            var rawData = this.RawData;
            if (stream == null)
            {
                rawData.Size = -1;
                rawData.Checksum = null;
                rawData.Stream = null;
                rawData.Timestamp = 0;
            }
            else
            {
                rawData.Size = stream.Length;
                rawData.Stream = stream;
                rawData.Checksum = null;
                rawData.Timestamp = 0;
            }
            Modified();
        }
        public static string CalculateChecksum(Stream stream)
        {
            var pos = stream.Position;
            stream.Position = 0;
            var b64 = Convert.ToBase64String(new System.Security.Cryptography.MD5CryptoServiceProvider().ComputeHash(stream));
            stream.Position = pos;
            return b64;
        }

        private BinaryFileName NormalizeFileName(BinaryFileName fileName)
        {
            if (fileName.FullFileName.Contains("\\"))
                return Path.GetFileName(fileName.FullFileName);
            if (fileName.FullFileName.Contains("/"))
                return RepositoryPath.GetFileName(fileName.FullFileName);
            return fileName;
        }

        // ===============================================

        public BinaryData()
        {
            __privateValue = GetDefaultRawData();
        }

        public string ToBase64()
        {
            Stream stream = null;
            MemoryStream ms = null;

            try
            {
                stream = this.GetStream();

                if (stream is MemoryStream)
                {
                    ms = (MemoryStream)stream;
                    stream = null;
                }
                else
                {
                    ms = new MemoryStream();
                    stream.CopyTo(ms);
                }

                var arr = ms.ToArray();
                return Convert.ToBase64String(arr);
            }
            finally
            {
                if (stream != null)
                    stream.Dispose();
                if (ms != null)
                    ms.Dispose();
            }
        }

        public void Reset()
        {
            Id = 0;
            FileId = 0;
            FileName = String.Empty;
            ContentType = String.Empty;
            Size = -1;
            BlobProvider = null;
            BlobProviderData = null;
            this.SetStream(null);
        }
        public void CopyFrom(BinaryData data)
        {
            FileId = data.FileId;
            FileName = data.FileName;
            ContentType = data.ContentType;
            Size = data.Size;
            BlobProvider = data.BlobProvider;
            BlobProviderData = data.BlobProviderData;
            this.SetStream(data.GetStream());
        }
        public void CopyFromWithoutDbRead(BinaryData data)
        {
            FileId = data.FileId;
            FileName = data.FileName;
            ContentType = data.ContentType;
            Size = data.Size;
            BlobProvider = data.BlobProvider;
            BlobProviderData = data.BlobProviderData;

            var stream = GetStreamWithoutDbRead();
            if (stream != null)
                this.SetStream(stream);
        }

        private static Stream CloneStream(Stream stream)
        {
            if (stream == null || !stream.CanRead)
                return null;

            var snStream = stream as SnStream;
            if (snStream != null)
                return snStream.Clone();

            long pos = stream.Position;
            stream.Seek(0, SeekOrigin.Begin);
            Stream clone = new MemoryStream(new BinaryReader(stream).ReadBytes((int)stream.Length));
            clone.Seek(0, SeekOrigin.Begin);
            stream.Seek(pos, SeekOrigin.Begin);

            return clone;
        }
        private static string GetMimeType(BinaryFileName value)
        {
            if (value == null)
                return string.Empty;
            string ext = value.Extension;
            if (ext == null)
                return string.Empty;
            if (ext.Length > 0 && ext[0] == '.')
                ext = ext.Substring(1);
            var mimeType = MimeTable.GetMimeType(ext.ToLower(CultureInfo.InvariantCulture));
            return mimeType;
        }

        // =============================================== Chunk upload/download

        /// <summary>
        /// Starts the chunk saving process by providing the token that is necessary for saving 
        /// chunks and committing the changes. It is possible to start the process without
        /// providing parameters: in this case the content does not exist yet, it will be
        /// created just before the commit method call.
        /// </summary>
        /// <param name="contentId">Id of the content</param>
        /// <param name="fullSize">Full size of the binary stream.</param>
        /// <param name="fieldName">Name of the binary field. Default: Binary</param>
        /// <returns>The token that is needed for chunk upload. This token must be passed to the SaveChunk method when adding binary chunks.</returns>
        public static string StartChunk(int contentId, long fullSize, string fieldName = "Binary")
        {
            Node node;
            PropertyType pt;

            // workaround for empty string (not null)
            if (string.IsNullOrEmpty(fieldName))
                fieldName = "Binary";

            AssertChunk(contentId, fieldName, out node, out pt);

            return DataProvider.Current.StartChunk(node.VersionId, pt.Id, fullSize); //DB:??
        }

        /// <summary>
        /// Inserts a set of bytes into a binary field. Can be used to upload large files in chunks. After calling this method with all the chunks, CommitChunk method must be called to finalize the process.
        /// </summary>
        /// <param name="token">The token received from the StartChunk method that needs to be called before the chunk saving operation starts.</param>
        /// <param name="fullStreamSize">Full size of the binary stream</param>
        /// <param name="buffer">Byte array that contains the chunk to write</param>
        /// <param name="offset">The position where the write operation should start</param>
        /// <param name="count">Number of bytes to write. If -1, the full buffer will be written</param>
        [Obsolete("Use WriteChunk instead.", true)]
        public static void SaveChunk(string token, long fullStreamSize, byte[] buffer, long offset, int count)
        {
            // call the new method that is able to write chunks in random order
            WriteChunk(0, token, fullStreamSize, buffer, offset);
        }

        /// <summary>
        /// Finalizes a chunk saving process: sets the stream size and length for the binary.
        /// </summary>
        /// <param name="contentId">Id of the content</param>
        /// <param name="token">The token received from the StartChunk method that needs to be called before the chunk saving operation starts.</param>
        /// <param name="fullSize">Full size of the binary stream</param>
        /// <param name="fieldName">Name of the field. Default: Binary</param>
        /// <param name="binaryMetadata">Additional metadata for the binary row: file name, extension, content type.</param>
        public static void CommitChunk(int contentId, string token, long fullSize, string fieldName = "Binary", BinaryData binaryMetadata = null)
        {
            Node node;
            PropertyType pt;

            // workaround for empty string (not null)
            if (string.IsNullOrEmpty(fieldName))
                fieldName = "Binary";

            AssertChunk(contentId, fieldName, out node, out pt);

            DataProvider.Current.CommitChunk(node.VersionId, pt.Id, token, fullSize, binaryMetadata == null ? null : binaryMetadata.RawData); //DB:??

            NodeIdDependency.FireChanged(node.Id);
            StorageContext.L2Cache.Clear();
        }

        /// <summary>
        /// Inserts a set of bytes into a binary field. Can be used to upload large files in chunks. After calling this method with all the chunks, CommitChunk method must be called to finalize the process.
        /// </summary>
        /// <param name="contentId">Id of the content</param>
        /// <param name="token">The token received from the StartChunk method that needs to be called before the chunk saving operation starts.</param>
        /// <param name="fullStreamSize">Full size of the binary stream</param>
        /// <param name="buffer">Byte array that contains the chunk to write</param>
        /// <param name="offset">The position where the write operation should start</param>
        /// <param name="fieldName">Name of the field. Default: Binary</param>
        public static void WriteChunk(int contentId, string token, long fullStreamSize, byte[] buffer, long offset, string fieldName = "Binary")
        {
            Node node;
            PropertyType pt;

            AssertChunk(contentId, fieldName, out node, out pt);

            DataProvider.Current.WriteChunk(node.VersionId, token, buffer, offset, fullStreamSize); //DB:??
        }

        public static void CopyFromStream(int contentId, Stream input, string fieldName = "Binary", BinaryData binaryData = null)
        {
            var token = StartChunk(contentId, input.Length, fieldName);

            Node node;
            PropertyType pt;

            AssertChunk(contentId, fieldName, out node, out pt);

            DataProvider.Current.CopyFromStream(node.VersionId, token, input); //DB:??

            CommitChunk(contentId, token, input.Length, fieldName, binaryData);
        }

        private static void AssertChunk(int contentId, string fieldName, out Node node, out PropertyType propertyType)
        {
            if (contentId < 1)
                throw new ContentNotFoundException("Unknown content during chunk upload. Id: " + contentId);

            node = Node.LoadNode(contentId);
            if (node == null)
                throw new ContentNotFoundException(contentId.ToString());

            // check if the content is locked by the current user
            var currentUser = AccessProvider.Current.GetOriginalUser();

            // Workaround for SystemAccount: only check the user if the content is not locked 
            // by the system account. That can happen only in server-side elevated code.
            if (node.LockedById != -1 && node.LockedById != currentUser.Id)
                throw new SenseNetSecurityException(contentId, "It is only allowed to upload a binary chunk if the content is locked by the current user.");

            // check the destination property type
            propertyType = node.PropertyTypes[fieldName];
            if (propertyType == null || propertyType.DataType != DataType.Binary)
                throw new InvalidOperationException("Binary property not found with the name: " + fieldName);
        }
    }
}