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
    internal class InMemoryBlobProviderSelector : IBlobProviderSelector
    {
        private InMemoryBlobProvider _blobProvider;

        public InMemoryBlobProviderSelector(InMemoryBlobProvider blobProvider)
        {
            _blobProvider = blobProvider;
        }

        public IBlobProvider GetProvider(long fullSize, Dictionary<string, IBlobProvider> providers, IBlobProvider builtIn)
        {
            return _blobProvider;
        }
    }

    internal class InMemoryBlobStorageMetaDataProvider : IBlobStorageMetaDataProvider
    {
        private DataProvider _dataProvider;

        public InMemoryBlobStorageMetaDataProvider()
        {
            
        }
        public InMemoryBlobStorageMetaDataProvider(DataProvider dataProvider)
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
            throw new NotImplementedException();
        }

        public void InsertBinaryPropertyWithFileId(BinaryDataValue value, int versionId, int propertyTypeId, bool isNewNode)
        {
            throw new NotImplementedException();
        }

        public void UpdateBinaryProperty(IBlobProvider blobProvider, BinaryDataValue value)
        {
            throw new NotImplementedException();
        }

        public void DeleteBinaryProperty(int versionId, int propertyTypeId)
        {
            throw new NotImplementedException();
        }

        public BinaryCacheEntity LoadBinaryCacheEntity(int versionId, int propertyTypeId)
        {
            throw new NotImplementedException();
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

    internal class InMemoryBlobProvider : IBlobProvider
    {
        private DataProvider _dataProvider;

        public InMemoryBlobProvider()
        {
        }
        public InMemoryBlobProvider(DataProvider dataProvider)
        {
            _dataProvider = dataProvider;
        }

        public void Allocate(BlobStorageContext context)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public Stream GetStreamForWrite(BlobStorageContext context)
        {
            throw new NotImplementedException();
        }

        public Stream CloneStream(BlobStorageContext context, Stream stream)
        {
            throw new NotImplementedException();
        }

        public object ParseData(string providerData)
        {
            throw new NotImplementedException();
        }
    }
}
