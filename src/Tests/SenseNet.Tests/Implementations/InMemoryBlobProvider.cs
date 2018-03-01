using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;

namespace SenseNet.Tests.Implementations
{
    public class InMemoryBlobProviderSelector : IBlobProviderSelector
    {
        public IBlobProvider GetProvider(long fullSize, Dictionary<string, IBlobProvider> providers, IBlobProvider builtIn)
        {
            return providers[typeof(InMemoryBlobProvider).FullName];
        }
    }

    public class InMemoryBlobStorageMetaDataProvider : IBlobStorageMetaDataProvider
    {
        private InMemoryDataProvider _dataProvider;

        public InMemoryBlobStorageMetaDataProvider()
        {
            
        }
        public InMemoryBlobStorageMetaDataProvider(InMemoryDataProvider dataProvider)
        {
            _dataProvider = dataProvider;
        }

        public bool IsFilestreamEnabled()
        {
            return false;
        }

        public BlobStorageContext GetBlobStorageContext(int fileId, bool clearStream, int versionId, int propertyTypeId)
        {
            throw new NotImplementedException();
        }

        public Task<BlobStorageContext> GetBlobStorageContextAsync(int fileId, bool clearStream, int versionId, int propertyTypeId)
        {
            throw new NotImplementedException();
        }

        public void InsertBinaryProperty(IBlobProvider blobProvider, BinaryDataValue value, int versionId, int propertyTypeId, bool isNewNode)
        {
            var streamLength = value.Stream?.Length ?? 0;
            var ctx = new BlobStorageContext(blobProvider) { VersionId = versionId, PropertyTypeId = propertyTypeId, FileId = 0, Length = streamLength, UseFileStream = false };

            // blob operation

            blobProvider.Allocate(ctx);

            using (var stream = blobProvider.GetStreamForWrite(ctx))
                value.Stream?.CopyTo(stream);

            value.BlobProviderName = ctx.Provider.GetType().FullName;
            value.BlobProviderData = BlobStorageContext.SerializeBlobProviderData(ctx.BlobProviderData);

            // metadata operation
            var db = _dataProvider.DB;
            if (!isNewNode)
                db.BinaryProperties.RemoveAll(r => r.VersionId == versionId && r.PropertyTypeId == propertyTypeId);

            var fileId = db.Files.Max(r => r.FileId) + 1;
            db.Files.Add(new InMemoryDataProvider.FileRecord
            {
                FileId = fileId,
                ContentType = value.ContentType,
                Extension = value.FileName.Extension,
                FileNameWithoutExtension = value.FileName.FileNameWithoutExtension,
                Size = Math.Max(0, value.Size),
                BlobProvider = value.BlobProviderName,
                BlobProviderData = value.BlobProviderData
            });
            var binaryPropertyId = db.BinaryProperties.Max(r => r.BinaryPropertyId) + 1;
            db.BinaryProperties.Add(new InMemoryDataProvider.BinaryPropertyRecord
            {
                BinaryPropertyId = binaryPropertyId,
                FileId = fileId,
                PropertyTypeId = propertyTypeId, 
                VersionId = versionId
            });

            value.Id = binaryPropertyId;
            value.FileId = fileId;
            value.Timestamp = 0L; //TODO: file row timestamp
        }

        public void InsertBinaryPropertyWithFileId(BinaryDataValue value, int versionId, int propertyTypeId, bool isNewNode)
        {
            throw new NotImplementedException();
        }

        public void UpdateBinaryProperty(IBlobProvider blobProvider, BinaryDataValue value)
        {
            var streamLength = value.Stream?.Length ?? 0;
            if (streamLength > 0)
            {
                var ctx = new BlobStorageContext(blobProvider, value.BlobProviderData)
                {
                    VersionId = 0,
                    PropertyTypeId = 0,
                    FileId = value.FileId,
                    Length = streamLength,
                    UseFileStream = false
                };

                blobProvider.Allocate(ctx);
                using (var stream = blobProvider.GetStreamForWrite(ctx))
                    value.Stream?.CopyTo(stream);

                value.BlobProviderName = ctx.Provider.GetType().FullName;
                value.BlobProviderData = BlobStorageContext.SerializeBlobProviderData(ctx.BlobProviderData);
            }

            var isRepositoryStream = value.Stream is RepositoryStream || value.Stream is SenseNetSqlFileStream;
            var hasStream = isRepositoryStream || value.Stream is MemoryStream;
            if (!hasStream)
                // do not do any database operation if the stream is not modified
                return;

            var db = _dataProvider.DB;
            var fileId = db.Files.Max(r => r.FileId) + 1;
            db.Files.Add(new InMemoryDataProvider.FileRecord
            {
                FileId = fileId,
                ContentType = value.ContentType,
                Extension = value.FileName.Extension,
                FileNameWithoutExtension = value.FileName.FileNameWithoutExtension,
                Size = Math.Max(0, value.Size),
                BlobProvider = value.BlobProviderName,
                BlobProviderData = value.BlobProviderData
            });

            var binaryPropertyRow = db.BinaryProperties.FirstOrDefault(r => r.BinaryPropertyId == value.Id);
            if(binaryPropertyRow != null)
                binaryPropertyRow.FileId = fileId;

            if (fileId > 0 && fileId != value.FileId)
                value.FileId = fileId;
        }

        public void DeleteBinaryProperty(int versionId, int propertyTypeId)
        {
            _dataProvider.DB.BinaryProperties
                .RemoveAll(r => r.VersionId == versionId && r.PropertyTypeId == propertyTypeId);
        }

        public BinaryCacheEntity LoadBinaryCacheEntity(int versionId, int propertyTypeId)
        {
            var db = _dataProvider.DB;
            var binaryPropertyRow =
                db.BinaryProperties.FirstOrDefault(r => r.VersionId == versionId && r.PropertyTypeId == propertyTypeId);
            if (binaryPropertyRow == null)
                return null;
            var fileRow = db.Files.FirstOrDefault(r => r.FileId == binaryPropertyRow.FileId && !r.Staging);
            if (fileRow == null)
                return null;

            var length = fileRow.Size;
            var binaryPropertyId = binaryPropertyRow.BinaryPropertyId;
            var fileId = fileRow.FileId;

            var providerName = fileRow.BlobProvider;
            var providerTextData = fileRow.BlobProviderData;

            var rawData = fileRow.Stream;

            var provider = BlobStorageBase.GetProvider(providerName);
            var context = new BlobStorageContext(provider, providerTextData)
            {
                VersionId = versionId,
                PropertyTypeId = propertyTypeId,
                FileId = fileId,
                Length = length,
                UseFileStream = false
            };

            return new BinaryCacheEntity
            {
                Length = length,
                RawData = rawData,
                BinaryPropertyId = binaryPropertyId,
                FileId = fileId,
                Context = context
            };
        }

        public string StartChunk(IBlobProvider blobProvider, int versionId, int propertyTypeId, long fullSize)
        {
            throw new NotImplementedException();
        }

        public void CommitChunk(int versionId, int propertyTypeId, int fileId, long fullSize, BinaryDataValue source)
        {
            throw new NotImplementedException();
        }

        public void CleanupFilesSetDeleteFlag()
        {
            throw new NotImplementedException();
        }

        public bool CleanupFiles()
        {
            throw new NotImplementedException();
        }
    }

    public class InMemoryBlobProviderData
    {
        private Guid _blobId;
        public Guid BlobId
        {
            get => _blobId;
            set => _blobId = value;
        }
    }

    public class InMemoryStreamForWrite : Stream
    {
        private readonly byte[] _buffer;

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length { get; }

        public override long Position { get; set; }

        public InMemoryStreamForWrite(byte[] buffer)
        {
            _buffer = buffer;
            Length = _buffer.Length;
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Array.Copy(buffer, 0, _buffer, offset, count);
        }
    }

    public class InMemoryStreamForRead : Stream
    {
        private byte[] _buffer;
        private long _position;

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length { get; }

        public override long Position { get; set; }

        public InMemoryStreamForRead( byte[] buffer)
        {
            _buffer = buffer;
            Length = _buffer.Length;
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    _position = offset;
                    break;
                case SeekOrigin.Current:
                    _position += offset;
                    break;
                case SeekOrigin.End:
                    _position -= offset;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(origin), origin, null);
            }
            return _position;
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var realCount = Math.Min(_buffer.LongLength - _position - offset, count);
            if (realCount > 0)
                Array.Copy(_buffer, _position, buffer, 0, realCount);
            _position += realCount;
            return Convert.ToInt32(realCount);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
    }

    public class InMemoryBlobProvider : IBlobProvider
    {
        private Dictionary<Guid, byte[]> _blobStorage = new Dictionary<Guid, byte[]>();

        public void Allocate(BlobStorageContext context)
        {
            var id = Guid.NewGuid();
            _blobStorage.Add(id, new byte[0]);

            context.BlobProviderData = new InMemoryBlobProviderData {BlobId = id};
        }

        public void Write(BlobStorageContext context, long offset, byte[] buffer)
        {
            throw new NotImplementedException();
        }

        public Task WriteAsync(BlobStorageContext context, long offset, byte[] buffer)
        {
            throw new NotImplementedException();
        }

        public void Delete(BlobStorageContext context)
        {
            throw new NotImplementedException();
        }

        public Stream GetStreamForRead(BlobStorageContext context)
        {
            var data = (InMemoryBlobProviderData) context.BlobProviderData;
            var buffer = _blobStorage[data.BlobId];
            return new InMemoryStreamForRead(buffer);
        }

        public Stream GetStreamForWrite(BlobStorageContext context)
        {
            var providerData = (InMemoryBlobProviderData)context.BlobProviderData;
            var buffer = new byte[context.Length];
            _blobStorage[providerData.BlobId] = buffer;
            return new InMemoryStreamForWrite(buffer);
        }

        public Stream CloneStream(BlobStorageContext context, Stream stream)
        {
            throw new NotImplementedException();
        }

        public object ParseData(string providerData)
        {
            return BlobStorageContext.DeserializeBlobProviderData<InMemoryBlobProviderData>(providerData);
        }
    }
}
