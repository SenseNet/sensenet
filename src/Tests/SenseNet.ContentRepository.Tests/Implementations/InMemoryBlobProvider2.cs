using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.SqlClient;

namespace SenseNet.ContentRepository.Tests.Implementations
{
    public class InMemoryBlobStorageMetaDataProvider2 : IBlobStorageMetaDataProvider
    {
        public InMemoryDataProvider2 DataProvider { get; set; }

        public InMemoryBlobStorageMetaDataProvider2()
        {
            
        }
        public InMemoryBlobStorageMetaDataProvider2(InMemoryDataProvider2 dataProvider)
        {
            DataProvider = dataProvider;
        }

        public BlobStorageContext GetBlobStorageContext(int fileId, bool clearStream, int versionId, int propertyTypeId)
        {
            if (!DataProvider.DB.Files.TryGetValue(fileId, out var fileDoc))
                return null;

            var length = fileDoc.Size;
            var providerName = fileDoc.BlobProvider;
            var providerData = fileDoc.BlobProviderData;

            var provider = BlobStorageBase.GetProvider(providerName);

            return new BlobStorageContext(provider, providerData)
            {
                VersionId = versionId,
                PropertyTypeId = propertyTypeId,
                FileId = fileId,
                Length = length,
                BlobProviderData = provider == BlobStorageBase.BuiltInProvider
                    ? new BuiltinBlobProviderData()
                    : provider.ParseData(providerData)
            };
        }

        public Task<BlobStorageContext> GetBlobStorageContextAsync(int fileId, bool clearStream, int versionId, int propertyTypeId)
        {
            throw new NotImplementedException();
        }

        public void InsertBinaryProperty(IBlobProvider blobProvider, BinaryDataValue value, int versionId, int propertyTypeId, bool isNewNode)
        {
            var streamLength = value.Stream?.Length ?? 0;
            var ctx = new BlobStorageContext(blobProvider) { VersionId = versionId, PropertyTypeId = propertyTypeId, FileId = 0, Length = streamLength };

            // blob operation

            blobProvider.Allocate(ctx);

            using (var stream = blobProvider.GetStreamForWrite(ctx))
                value.Stream?.CopyTo(stream);

            value.BlobProviderName = ctx.Provider.GetType().FullName;
            value.BlobProviderData = BlobStorageContext.SerializeBlobProviderData(ctx.BlobProviderData);

            // metadata operation
            var db = DataProvider.DB;
            if (!isNewNode)
                foreach (var key in db.BinaryProperties
                                      .Where(x=>x.Value.VersionId == versionId && x.Value.PropertyTypeId == propertyTypeId)
                                      .Select(x=>x.Key))
                    db.BinaryProperties.Remove(key);

            var fileId = db.GetNextFileId();
            db.Files.Add(fileId, new FileDoc
            {
                FileId = fileId,
                ContentType = value.ContentType,
                Extension = value.FileName.Extension,
                FileNameWithoutExtension = value.FileName.FileNameWithoutExtension,
                Size = Math.Max(0, value.Size),
                BlobProvider = value.BlobProviderName,
                BlobProviderData = value.BlobProviderData
            });
            var binaryPropertyId = db.GetNextBinaryPropertyId();
            db.BinaryProperties.Add(binaryPropertyId, new BinaryPropertyDoc
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
            var isExternal = false;
            if (streamLength > 0)
            {
                var ctx = new BlobStorageContext(blobProvider, value.BlobProviderData)
                {
                    VersionId = 0,
                    PropertyTypeId = 0,
                    FileId = value.FileId,
                    Length = streamLength,
                };

                blobProvider.Allocate(ctx);
                isExternal = true;

                value.BlobProviderName = ctx.Provider.GetType().FullName;
                value.BlobProviderData = BlobStorageContext.SerializeBlobProviderData(ctx.BlobProviderData);
            }

            var isRepositoryStream = value.Stream is RepositoryStream;
            var hasStream = isRepositoryStream || value.Stream is MemoryStream;
            if (!isExternal && !hasStream)
                // do not do any database operation if the stream is not modified
                return;

            var db = DataProvider.DB;
            var fileId = db.GetNextFileId();
            db.Files.Add(fileId, new FileDoc
            {
                FileId = fileId,
                ContentType = value.ContentType,
                Extension = value.FileName.Extension,
                FileNameWithoutExtension = value.FileName.FileNameWithoutExtension,
                Size = Math.Max(0, value.Size),
                BlobProvider = value.BlobProviderName,
                BlobProviderData = value.BlobProviderData
            });
            if (db.BinaryProperties.TryGetValue(value.Id, out var binaryPropertyDoc))
                binaryPropertyDoc.FileId = fileId;

            if (fileId > 0 && fileId != value.FileId)
                value.FileId = fileId;

            // update stream with a new context
            var newCtx = new BlobStorageContext(blobProvider, value.BlobProviderData)
            {
                VersionId = 0,
                PropertyTypeId = 0,
                FileId = value.FileId,
                Length = streamLength,
            };

            using (var stream = blobProvider.GetStreamForWrite(newCtx))
                value.Stream?.CopyTo(stream);
        }

        public void DeleteBinaryProperty(int versionId, int propertyTypeId)
        {
            throw new NotImplementedException();
            //_dataProvider.DB.BinaryProperties
            //    .RemoveAll(r => r.VersionId == versionId && r.PropertyTypeId == propertyTypeId);
        }

        public BinaryCacheEntity LoadBinaryCacheEntity(int versionId, int propertyTypeId)
        {
            //throw new NotImplementedException();
            var db = DataProvider.DB;
            var binaryDoc =
                db.BinaryProperties.Values.FirstOrDefault(r => r.VersionId == versionId && r.PropertyTypeId == propertyTypeId);
            if (binaryDoc == null)
                return null;

            if (!db.Files.TryGetValue(binaryDoc.FileId, out var fileDoc))
                return null;
            if (fileDoc.Staging)
                return null;

            var length = fileDoc.Size;
            var binaryPropertyId = binaryDoc.BinaryPropertyId;
            var fileId = fileDoc.FileId;

            var providerName = fileDoc.BlobProvider;
            var providerTextData = fileDoc.BlobProviderData;

            var rawData = fileDoc.Buffer;

            var provider = BlobStorageBase.GetProvider(providerName);
            var context = new BlobStorageContext(provider, providerTextData)
            {
                VersionId = versionId,
                PropertyTypeId = propertyTypeId,
                FileId = fileId,
                Length = length,
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
}
