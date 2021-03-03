using System;
using System.Linq;
using System.Xml.Schema;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.InMemory;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;
using SenseNet.IntegrationTests.Common;
using SenseNet.IntegrationTests.Infrastructure;

namespace SenseNet.IntegrationTests.Platforms
{
    public class InMemBlobStoragePlatform : InMemPlatform, IBlobStoragePlatform
    {
        public Type ExpectedExternalBlobProviderType => typeof(InMemoryBlobProvider);
        public Type ExpectedBlobProviderDataType => typeof(InMemoryBlobProviderData);
        public bool CanUseBuiltInBlobProvider => false;

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
                //CreationDate = DateTime.MinValue,
                //RowGuid = Guid.Empty,
                Timestamp = file.Timestamp,
                Staging = file.Staging,
                //StagingVersionId = 0,
                //StagingPropertyTypeId = 0,
                IsDeleted = false,
                BlobProvider = file.BlobProvider,
                BlobProviderData = file.BlobProviderData,
                //FileStream = null,
                ExternalStream = GetExternalData(file),
            };
        }
        private byte[] GetExternalData(FileDoc file)
        {
            if (file.BlobProvider == null)
                return new byte[0];

            var provider = BlobStorageBase.GetProvider(file.BlobProvider);
            var context = new BlobStorageContext(provider, file.BlobProviderData) { Length = file.Size };
            return GetExternalData(context);
        }
        private byte[] GetExternalData(BlobStorageContext context)
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


        public void ConfigureMinimumSizeForFileStreamInBytes(int cheat, out int originalValue)
        {
            originalValue = Configuration.BlobStorage.MinimumSizeForBlobProviderInBytes;
            Configuration.BlobStorage.MinimumSizeForBlobProviderInBytes = cheat;
        }
    }
}
