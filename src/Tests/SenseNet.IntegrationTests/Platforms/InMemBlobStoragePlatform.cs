﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.InMemory;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;
using SenseNet.IntegrationTests.Common;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.Testing;

namespace SenseNet.IntegrationTests.Platforms
{
    public class InMemBlobStoragePlatform : InMemPlatform, IBlobStoragePlatform
    {
        //TODO: [DIREF] get blob service through the constructor
        private IBlobStorage BlobStorage => Providers.Instance.BlobStorage;

        public override void BuildServices(IConfiguration configuration, IServiceCollection services)
        {
            base.BuildServices(configuration, services);

            services
                .AddSingleton<IBlobProvider, InMemoryBlobProvider>()
                ;
        }

        public virtual Type ExpectedExternalBlobProviderType => typeof(InMemoryBlobProvider);
        public virtual Type ExpectedBlobProviderDataType => typeof(InMemoryBlobProviderData);
        public virtual bool CanUseBuiltInBlobProvider => false;
        public virtual bool UseChunk => false;

        public DbFile[] LoadDbFiles(int versionId, string propertyName = "Binary")
        {
            //SELECT f.* FROM BinaryProperties b JOIN Files f on f.FileId = b.FileId
            //WHERE b.VersionId = {versionId} and b.PropertyTypeId = {propTypeId}";

            var db = ((InMemoryDataProvider) Providers.Instance.DataProvider).DB;
            var propertyType = db.Schema.PropertyTypes.FirstOrDefault(x => x.Name == propertyName);
            if (propertyType == null)
                throw new ApplicationException("Unknown property: " + propertyName);

            var fileIds = db.BinaryProperties
                .Where(x => x.VersionId == versionId && x.PropertyTypeId == propertyType.Id)
                .Select(x=>x.FileId)
                .ToArray();

            var result = db.Files
                .Where(x => fileIds.Contains(x.FileId))
                .Select(CreateFromFileRow)
                .ToArray();

            return result;
        }
        public DbFile LoadDbFile(int fileId)
        {
            var db = ((InMemoryDataProvider)Providers.Instance.DataProvider).DB;
            var result = db.Files
                .Where(x => fileId == x.FileId)
                .Select(CreateFromFileRow)
                .FirstOrDefault();
            return result;
        }
        private DbFile CreateFromFileRow(FileDoc file)
        {
            return new DbFile
            {
                FileId = file.FileId,
                ContentType = file.ContentType,
                FileNameWithoutExtension = file.FileNameWithoutExtension,
                Extension = file.Extension,
                Size = file.Size,
                //Checksum = null,
                Stream = file.Buffer,
                CreationDate = file.CreationDate,
                //RowGuid = Guid.Empty,
                Timestamp = file.Timestamp,
                Staging = file.Staging,
                //StagingVersionId = 0,
                //StagingPropertyTypeId = 0,
                IsDeleted = file.IsDeleted,
                BlobProvider = file.BlobProvider,
                BlobProviderData = file.BlobProviderData,
                ExternalStream = GetExternalData(file),
            };
        }
        private byte[] GetExternalData(FileDoc file)
        {
            return GetExternalData(file.BlobProvider, file.BlobProviderData, file.Size);
        }

        public void ConfigureMinimumSizeForFileStreamInBytes(int cheat, out int originalValue)
        {
            originalValue = Configuration.BlobStorage.MinimumSizeForBlobProviderInBytes;
            Configuration.BlobStorage.MinimumSizeForBlobProviderInBytes = cheat;
        }

        public byte[] GetExternalData(string blobProvider, string blobProviderData, long size)
        {
            if (blobProvider == null)
                return new byte[0];

            var provider = BlobStorage.GetProvider(blobProvider);
            var context = new BlobStorageContext(provider, blobProviderData) { Length = size };
            return GetExternalData(context);
        }
        public byte[] GetExternalData(BlobStorageContext context)
        {
            try
            {
                using (var stream = context.Provider.GetStreamForRead(context))
                {
                    var buffer = new byte[stream.Length.ToInt()];
                    stream.Read(buffer, 0, buffer.Length);
                    return buffer;
                }
            }
            catch
            {
                return null;
            }
        }

        public byte[][] GetRawData(int fileId)
        {
            var dataProvider = (InMemoryDataProvider)Providers.Instance.DataProvider;
            var db = dataProvider.DB;
            var file = db.Files.Single(f => f.FileId == fileId);

            return GetRawData(file.BlobProvider, file.BlobProviderData);
        }
        protected virtual byte[][] GetRawData(string blobProvider, string blobProviderData)
        {
            var provider = (InMemoryBlobProvider)BlobStorage.GetProvider(blobProvider);
            var providerAcc = new ObjectAccessor(provider);
            var providerData = (InMemoryBlobProviderData)provider.ParseData(blobProviderData);

            var data = (Dictionary<Guid, byte[]>)providerAcc.GetField("_blobStorage");
            var buffer = data[providerData.BlobId];

            if (buffer.Length == 0)
                return new byte[0][];
            return new[] { buffer };
        }

        public void UpdateFileCreationDate(int fileId, DateTime creationDate)
        {
            // $"UPDATE Files SET CreationDate = @CreationDate WHERE FileId = {fileId}";
            var db = ((InMemoryDataProvider)Providers.Instance.DataProvider).DB;
            var file = db.Files.FirstOrDefault(x => fileId == x.FileId);
            if (file != null)
                file.CreationDate = creationDate;
        }

        public IDisposable SwindleWaitingBetweenCleanupFiles(int milliseconds)
        {
            return new InMemWaitingBetweenCleanupFilesSwindler(milliseconds);
        }
        private class InMemWaitingBetweenCleanupFilesSwindler : Swindler<int>
        {
            private static readonly string FieldName = "_waitBetweenCleanupFilesMilliseconds";
            public InMemWaitingBetweenCleanupFilesSwindler(int hack) : base(
                hack,
                () =>
                {
                    var metaDataProvider = (InMemoryBlobStorageMetaDataProvider)Providers.Instance.BlobMetaDataProvider;
                    var accessor = new ObjectAccessor(metaDataProvider);
                    return (int)accessor.GetField(FieldName);
                },
                (value) =>
                {
                    var metaDataProvider = (InMemoryBlobStorageMetaDataProvider)Providers.Instance.BlobMetaDataProvider;
                    var accessor = new ObjectAccessor(metaDataProvider);
                    accessor.SetField(FieldName, value);
                })
            {
            }
        }

    }
}
