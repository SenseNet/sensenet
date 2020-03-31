using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;
using STT = System.Threading.Tasks;

namespace SenseNet.ContentRepository.InMemory
{
    public class InMemoryBlobStorageMetaDataProvider : IBlobStorageMetaDataProvider
    {
        public InMemoryDataProvider DataProvider { get; set; }

        public InMemoryBlobStorageMetaDataProvider()
        {
            
        }
        public InMemoryBlobStorageMetaDataProvider(InMemoryDataProvider dataProvider)
        {
            DataProvider = dataProvider;
        }

        public Task<BlobStorageContext> GetBlobStorageContextAsync(int fileId, bool clearStream, int versionId, int propertyTypeId,
            CancellationToken cancellationToken)
        {
            var fileDoc = DataProvider.DB.Files.FirstOrDefault(x => x.FileId == fileId);
            if (fileDoc == null)
                return null;

            var length = fileDoc.Size;
            var providerName = fileDoc.BlobProvider;
            var providerData = fileDoc.BlobProviderData;

            var provider = BlobStorageBase.GetProvider(providerName);

            var result = new BlobStorageContext(provider, providerData)
            {
                VersionId = versionId,
                PropertyTypeId = propertyTypeId,
                FileId = fileId,
                Length = length,
                BlobProviderData = provider == BlobStorageBase.BuiltInProvider
                    ? new BuiltinBlobProviderData()
                    : provider.ParseData(providerData)
            };
            return STT.Task.FromResult(result);
        }

        public STT.Task InsertBinaryPropertyAsync(IBlobProvider blobProvider, BinaryDataValue value, int versionId, int propertyTypeId,
            bool isNewNode, SnDataContext dataContext)
        {
            var streamLength = value.Stream?.Length ?? 0;
            var ctx = new BlobStorageContext(blobProvider) { VersionId = versionId, PropertyTypeId = propertyTypeId, FileId = 0, Length = streamLength };

            // blob operation

            blobProvider.AllocateAsync(ctx, CancellationToken.None).GetAwaiter().GetResult();

            using (var stream = blobProvider.GetStreamForWrite(ctx))
                value.Stream?.CopyTo(stream);

            value.BlobProviderName = ctx.Provider.GetType().FullName;
            value.BlobProviderData = BlobStorageContext.SerializeBlobProviderData(ctx.BlobProviderData);

            // metadata operation
            var db = DataProvider.DB;
            if (!isNewNode)
                DeleteBinaryPropertyAsync(versionId, propertyTypeId, dataContext).GetAwaiter().GetResult();

            var fileId = db.Files.GetNextId();
            db.Files.Insert(new FileDoc
            {
                FileId = fileId,
                ContentType = value.ContentType,
                Extension = value.FileName.Extension,
                FileNameWithoutExtension = value.FileName.FileNameWithoutExtension,
                Size = Math.Max(0, value.Size),
                BlobProvider = value.BlobProviderName,
                BlobProviderData = value.BlobProviderData
            });
            var binaryPropertyId = db.BinaryProperties.GetNextId();
            db.BinaryProperties.Insert(new BinaryPropertyDoc
            {
                BinaryPropertyId = binaryPropertyId,
                FileId = fileId,
                PropertyTypeId = propertyTypeId,
                VersionId = versionId
            });

            value.Id = binaryPropertyId;
            value.FileId = fileId;
            value.Timestamp = 0L; //TODO: file row timestamp

            return STT.Task.CompletedTask;
        }

        public STT.Task InsertBinaryPropertyWithFileIdAsync(BinaryDataValue value, int versionId, int propertyTypeId, bool isNewNode,
            SnDataContext dataContext)
        {
            var db = DataProvider.DB;
            if (!isNewNode)
                DeleteBinaryPropertyAsync(versionId, propertyTypeId, dataContext).GetAwaiter().GetResult();

            var binaryPropertyId = db.BinaryProperties.GetNextId();
            db.BinaryProperties.Insert(new BinaryPropertyDoc
            {
                BinaryPropertyId = binaryPropertyId,
                FileId = value.FileId,
                PropertyTypeId = propertyTypeId,
                VersionId = versionId
            });

            value.Id = binaryPropertyId;

            return STT.Task.CompletedTask;
        }

        public STT.Task UpdateBinaryPropertyAsync(IBlobProvider blobProvider, BinaryDataValue value, SnDataContext dataContext)
        {
            var streamLength = value.Stream?.Length ?? 0;
            var isExternal = false;
            if (streamLength > 0)
            {
                // BlobProviderData parameter is irrelevant because it will be overridden in the Allocate method
                var ctx = new BlobStorageContext(blobProvider)
                {
                    VersionId = 0,
                    PropertyTypeId = 0,
                    FileId = value.FileId,
                    Length = streamLength,
                };

                blobProvider.AllocateAsync(ctx, CancellationToken.None).GetAwaiter().GetResult();
                isExternal = true;

                value.BlobProviderName = ctx.Provider.GetType().FullName;
                value.BlobProviderData = BlobStorageContext.SerializeBlobProviderData(ctx.BlobProviderData);
            }

            var isRepositoryStream = value.Stream is RepositoryStream;
            var hasStream = isRepositoryStream || value.Stream is MemoryStream;
            if (!isExternal && !hasStream)
                // do not do any database operation if the stream is not modified
                return STT.Task.CompletedTask;

            var db = DataProvider.DB;
            var fileId = db.Files.GetNextId();
            db.Files.Insert(new FileDoc
            {
                FileId = fileId,
                ContentType = value.ContentType,
                Extension = value.FileName.Extension,
                FileNameWithoutExtension = value.FileName.FileNameWithoutExtension,
                Size = Math.Max(0, value.Size),
                BlobProvider = value.BlobProviderName,
                BlobProviderData = value.BlobProviderData
            });
            var binaryPropertyDoc = db.BinaryProperties.FirstOrDefault(x => x.BinaryPropertyId == value.Id);
            if (binaryPropertyDoc != null)
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

            return STT.Task.CompletedTask;
        }

        public STT.Task DeleteBinaryPropertyAsync(int versionId, int propertyTypeId, SnDataContext dataContext)
        {
            var db = DataProvider.DB;
            foreach (var item in db.BinaryProperties
                .Where(x => x.VersionId == versionId && x.PropertyTypeId == propertyTypeId)
                .ToArray())
                db.BinaryProperties.Remove(item);
            return STT.Task.CompletedTask;
        }

        public STT.Task DeleteBinaryPropertiesAsync(IEnumerable<int> versionIds, SnDataContext dataContext)
        {
            var db = DataProvider.DB;
            foreach (var item in db.BinaryProperties
                .Where(x => versionIds.Contains(x.VersionId))
                .ToArray())
                db.BinaryProperties.Remove(item);
            return STT.Task.CompletedTask;
        }

        [SuppressMessage("ReSharper", "ExpressionIsAlwaysNull")]
        public Task<BinaryDataValue> LoadBinaryPropertyAsync(int versionId, int propertyTypeId, SnDataContext dataContext)
        {
            var db = DataProvider.DB;

            BinaryDataValue result = null;

            var binaryDoc = db.BinaryProperties.FirstOrDefault(x =>
                x.VersionId == versionId && x.PropertyTypeId == propertyTypeId);
            if (binaryDoc == null)
                return STT.Task.FromResult(result);

            var fileDoc = db.Files.FirstOrDefault(x => x.FileId == binaryDoc.FileId);
            if (fileDoc == null)
                return STT.Task.FromResult(result);
            if (fileDoc.Staging)
                return STT.Task.FromResult(result);

            result = CreateBinaryDataValue(db, binaryDoc, fileDoc);
            return STT.Task.FromResult(result);
        }
        private BinaryDataValue CreateBinaryDataValue(InMemoryDataBase db, BinaryPropertyDoc binaryDoc, FileDoc fileDoc = null)
        {
            if (fileDoc == null)
                fileDoc = db.Files.FirstOrDefault(x => x.FileId == binaryDoc.FileId);

            return new BinaryDataValue
            {
                Id = binaryDoc.BinaryPropertyId,
                FileId = binaryDoc.FileId,
                Checksum = null,
                FileName = fileDoc == null ? null : new BinaryFileName(fileDoc.FileNameWithoutExtension.Trim('.'), fileDoc.Extension.Trim('.')),
                ContentType = fileDoc?.ContentType,
                Size = fileDoc?.Size ?? 0L,
                BlobProviderName = fileDoc?.BlobProvider,
                BlobProviderData = fileDoc?.BlobProviderData,
                Timestamp = fileDoc?.Timestamp ?? 0L
            };

        }

        public Task<BinaryCacheEntity> LoadBinaryCacheEntityAsync(int versionId, int propertyTypeId, CancellationToken cancellationToken)
        {
            // this provider does not use datacontext.
            cancellationToken.ThrowIfCancellationRequested();
            return LoadBinaryCacheEntityAsync(versionId, propertyTypeId, null);
        }
        public Task<BinaryCacheEntity> LoadBinaryCacheEntityAsync(int versionId, int propertyTypeId, SnDataContext dataContext)
        {
            var db = DataProvider.DB;
            var binaryDoc =
                db.BinaryProperties.FirstOrDefault(r => r.VersionId == versionId && r.PropertyTypeId == propertyTypeId);
            if (binaryDoc == null)
                return null;

            var fileDoc = db.Files.FirstOrDefault(x => x.FileId == binaryDoc.FileId);
            if (fileDoc == null)
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

            var result = new BinaryCacheEntity
            {
                Length = length,
                RawData = rawData,
                BinaryPropertyId = binaryPropertyId,
                FileId = fileId,
                Context = context
            };
            return STT.Task.FromResult(result);
        }

        public async Task<string> StartChunkAsync(IBlobProvider blobProvider, int versionId, int propertyTypeId, long fullSize,
            CancellationToken cancellationToken)
        {
            var db = DataProvider.DB;

            // Get related objects
            var binaryDoc =
                db.BinaryProperties.FirstOrDefault(r => r.VersionId == versionId && r.PropertyTypeId == propertyTypeId);
            if (binaryDoc == null)
                return null;

            var fileDoc = db.Files.FirstOrDefault(x => x.FileId == binaryDoc.FileId);
            if (fileDoc == null)
                return null;
            if (fileDoc.Staging)
                return null;

            // Create context
            var binaryPropertyId = binaryDoc.BinaryPropertyId;
            var fileId = fileDoc.FileId;

            var providerName = fileDoc.BlobProvider;
            var providerTextData = fileDoc.BlobProviderData;

            var provider = BlobStorageBase.GetProvider(providerName);
            var context = new BlobStorageContext(provider, providerTextData)
            {
                VersionId = versionId,
                PropertyTypeId = propertyTypeId,
                FileId = fileId,
                Length = fullSize,
            };

            // Allocate a new blob
            await blobProvider.AllocateAsync(context, cancellationToken);
            var blobProviderName = blobProvider.GetType().FullName;
            var blobProviderData = BlobStorageContext.SerializeBlobProviderData(context.BlobProviderData);

            // Insert a new file row
            var contentType = fileDoc.ContentType;
            var fileNameWithoutExtension = fileDoc.FileNameWithoutExtension;
            var extension = fileDoc.Extension;
            var newFileId = db.Files.GetNextId();
            db.Files.Insert(new FileDoc
            {
                FileId =  newFileId,
                BlobProvider = blobProviderName,
                BlobProviderData = blobProviderData,
                ContentType = contentType,
                Extension = extension,
                FileNameWithoutExtension = fileNameWithoutExtension,
                Size = fullSize,
                Staging = true,
            });

            // Return a token
            return new ChunkToken
            {
                VersionId = versionId,
                PropertyTypeId = propertyTypeId,
                BinaryPropertyId = binaryPropertyId,
                FileId = newFileId
            }.GetToken();
        }

        public STT.Task CommitChunkAsync(int versionId, int propertyTypeId, int fileId, long fullSize, BinaryDataValue source,
            CancellationToken cancellationToken)
        {
            // Get related objects
            var db = DataProvider.DB;

            var binaryDoc =
                db.BinaryProperties.FirstOrDefault(r => r.VersionId == versionId && r.PropertyTypeId == propertyTypeId);
            if (binaryDoc == null)
                return null;

            var fileDoc = db.Files.FirstOrDefault(x => x.FileId == fileId);
            if (fileDoc == null)
                return null;

            // Switch to the new file
            binaryDoc.FileId = fileId;

            // Reset staging and set metadata
            fileDoc.Staging = false;
            fileDoc.Size = fullSize;

            if (source != null)
            {
                fileDoc.ContentType = source.ContentType;
                fileDoc.Extension = source.FileName.Extension;
                fileDoc.FileNameWithoutExtension = source.FileName.FileNameWithoutExtension;
            }

            // Done
            return STT.Task.CompletedTask;
        }

        public virtual STT.Task CleanupFilesSetDeleteFlagAsync(CancellationToken cancellationToken)
        {
            // This method is not supported in this provider because the FileDoc
            // does not have enough information (IsDeleted & CreationDate).

            return STT.Task.CompletedTask;
        }

        public virtual Task<bool> CleanupFilesAsync(CancellationToken cancellationToken)
        {
            // Delete the orphaned files immediately (see the comment in the CleanupFilesSetDeleteFlagAsync).

            var db = DataProvider.DB;

            var allFileIds = db.BinaryProperties.Select(x => x.FileId).ToArray();
            var filesIdsToDelete = db.Files
                .Where(x => !x.Staging && !allFileIds.Contains(x.Id))
                .Select(x=>x.FileId)
                .ToArray();

            foreach (var fileId in filesIdsToDelete)
                db.Files.Remove(fileId);

            // Done: return false because all items are deleted.
            return STT.Task.FromResult(false);
        }
    }
}
