﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;
using STT = System.Threading.Tasks;

namespace SenseNet.ContentRepository.InMemory
{
    public class InMemoryBlobStorageMetaDataProvider : IBlobStorageMetaDataProvider
    {
        public InMemoryDataProvider DataProvider { get; set; }

        //TODO: [DIBLOB] get these services through the constructor later
        private IBlobProviderStore BlobProviders => Providers.Instance.BlobProviders;
        private IBlobStorage BlobStorage => Providers.Instance.BlobStorage;

        public InMemoryBlobStorageMetaDataProvider(DataProvider dataProvider)
        {
            DataProvider = (InMemoryDataProvider)dataProvider;
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

            var provider = BlobProviders.GetProvider(providerName);

            var result = new BlobStorageContext(provider, providerData)
            {
                VersionId = versionId,
                PropertyTypeId = propertyTypeId,
                FileId = fileId,
                Length = length,
                BlobProviderData = provider is IBuiltInBlobProvider
                    ? new BuiltinBlobProviderData()
                    : provider.ParseData(providerData)
            };
            return STT.Task.FromResult(result);
        }

        public async STT.Task InsertBinaryPropertyAsync(IBlobProvider blobProvider, BinaryDataValue value, int versionId, int propertyTypeId,
            bool isNewNode, SnDataContext dataContext)
        {
            var streamLength = value.Stream?.Length ?? 0;
            var ctx = new BlobStorageContext(blobProvider) { VersionId = versionId, PropertyTypeId = propertyTypeId, FileId = 0, Length = streamLength };

            // blob operation

            await blobProvider.AllocateAsync(ctx, dataContext.CancellationToken).ConfigureAwait(false);

            using (var stream = blobProvider.GetStreamForWrite(ctx))
                if (value.Stream != null)
                    await value.Stream.CopyToAsync(stream);

            value.BlobProviderName = ctx.Provider.GetType().FullName;
            value.BlobProviderData = BlobStorageContext.SerializeBlobProviderData(ctx.BlobProviderData);

            // metadata operation
            var db = DataProvider.DB;
            if (!isNewNode)
                await DeleteBinaryPropertyAsync(versionId, propertyTypeId, dataContext).ConfigureAwait(false);

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
        }

        public async STT.Task InsertBinaryPropertyWithFileIdAsync(BinaryDataValue value, int versionId, int propertyTypeId, bool isNewNode,
            SnDataContext dataContext)
        {
            var db = DataProvider.DB;
            if (!isNewNode)
                await DeleteBinaryPropertyAsync(versionId, propertyTypeId, dataContext).ConfigureAwait(false);

            var binaryPropertyId = db.BinaryProperties.GetNextId();
            db.BinaryProperties.Insert(new BinaryPropertyDoc
            {
                BinaryPropertyId = binaryPropertyId,
                FileId = value.FileId,
                PropertyTypeId = propertyTypeId,
                VersionId = versionId
            });

            value.Id = binaryPropertyId;
        }

        public async STT.Task UpdateBinaryPropertyAsync(IBlobProvider blobProvider, BinaryDataValue value, SnDataContext dataContext)
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

                await blobProvider.AllocateAsync(ctx, dataContext.CancellationToken).ConfigureAwait(false);
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

            if(streamLength == 0)
            {
                await blobProvider.ClearAsync(newCtx, dataContext.CancellationToken).ConfigureAwait(false);
            }
            else
            {
                using (var stream = blobProvider.GetStreamForWrite(newCtx))
                    if (value.Stream != null)
                        await value.Stream.CopyToAsync(stream);
            }
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

            var provider = BlobProviders.GetProvider(providerName);
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

            var provider = BlobProviders.GetProvider(providerName);
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
            CleanupFilesSetDeleteFlag(false);
            return STT.Task.CompletedTask;
        }

        public virtual STT.Task CleanupFilesSetDeleteFlagImmediatelyAsync(CancellationToken cancellationToken)
        {
            CleanupFilesSetDeleteFlag(true);
            return STT.Task.CompletedTask;
        }

        private void CleanupFilesSetDeleteFlag(bool immediately)
        {
            var db = DataProvider.DB;
            var activeFileIds = db.BinaryProperties.Select(b => b.FileId).ToArray();

            var deletable = db.Files.Where(f => !f.Staging &&
                                                !activeFileIds.Contains(f.FileId));
            if (!immediately)
            {
                var timeLimit = DateTime.UtcNow.AddMinutes(-30.0d);
                deletable = deletable.Where(f => f.CreationDate < timeLimit);
            }

            foreach (var item in deletable)
                item.IsDeleted = true;
        }

        public virtual async Task<bool> CleanupFilesAsync(CancellationToken cancel)
        {
            var db = DataProvider.DB;

            var file = db.Files.FirstOrDefault(x => x.IsDeleted);
            if (file == null)
                return false;
            db.Files.Remove(file);

            // delete bytes from the blob storage
            var provider = BlobStorage.GetProvider(file.BlobProvider);
            var ctx = new BlobStorageContext(provider, file.BlobProviderData)
            {
                VersionId = 0, PropertyTypeId = 0, FileId = file.FileId, Length = file.Size
            };
            await ctx.Provider.DeleteAsync(ctx, cancel).ConfigureAwait(false);

            return true;
        }

        // Do not increase this value int he production scenario. It is only used in tests.
        private int _waitBetweenCleanupFilesMilliseconds = 0;
        public virtual async STT.Task CleanupAllFilesAsync(CancellationToken cancellationToken)
        {
            while (await CleanupFilesAsync(cancellationToken).ConfigureAwait(false))
            {
                if (_waitBetweenCleanupFilesMilliseconds != 0)
                    await STT.Task.Delay(_waitBetweenCleanupFilesMilliseconds, cancellationToken).ConfigureAwait(false);
            }
        }

        public Task<int> GetFirstFileIdAsync(CancellationToken cancel)
        {
            var first = DataProvider.DB.Files.FirstOrDefault();
            if (first == null)
                throw new Exception("No data available");
            return STT.Task.FromResult(first.FileId);
        }
    }
}
