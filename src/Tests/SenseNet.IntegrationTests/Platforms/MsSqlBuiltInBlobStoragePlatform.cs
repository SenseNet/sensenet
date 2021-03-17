using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;
using SenseNet.IntegrationTests.Common;
using SenseNet.IntegrationTests.Infrastructure;

namespace SenseNet.IntegrationTests.Platforms
{
    /// <summary>
    /// Platform for the most used blob storage mode: blobs in a built-in MsSql table.
    ///  The class name is a multiple compound word: MsSql-BuiltIn-BlobStorage-Platform
    /// </summary>
    public class MsSqlBuiltInBlobStoragePlatform : MsSqlPlatform, IBlobStoragePlatform
    {
        public Type ExpectedExternalBlobProviderType => null; // typeof(BuiltInBlobProvider);
        public Type ExpectedBlobProviderDataType => null; // typeof(BuiltinBlobProviderData);
        public bool CanUseBuiltInBlobProvider => true;

        public DbFile[] LoadDbFiles(int versionId, string propertyName = "Binary")
        {
            //SELECT f.* FROM BinaryProperties b JOIN Files f on f.FileId = b.FileId
            //WHERE b.VersionId = {versionId} and b.PropertyTypeId = {propTypeId}";

            var propTypeId = ActiveSchema.PropertyTypes[propertyName].Id;
            var sql = $@"SELECT f.* FROM BinaryProperties b JOIN Files f on f.FileId = b.FileId WHERE b.VersionId = {versionId} and b.PropertyTypeId = {propTypeId}";
            var dbFiles = new List<DbFile>();

            using (var ctx = GetDataContext())
            {
                var _ = ctx.ExecuteReaderAsync(sql, async (reader, cancel) =>
                {
                    cancel.ThrowIfCancellationRequested();
                    while (await reader.ReadAsync(cancel))
                    {
                        cancel.ThrowIfCancellationRequested();
                        dbFiles.Add(GetFileFromReader(reader));
                    }
                    return true;
                }).GetAwaiter().GetResult();
            }

            return dbFiles.ToArray();
        }
        private SnDataContext GetDataContext()
        {
            return ((RelationalDataProviderBase)DataStore.DataProvider).CreateDataContext(CancellationToken.None);
        }
        private DbFile GetFileFromReader(IDataReader reader)
        {
            var file = new DbFile
            {
                FileId = reader.GetInt32("FileId"),
                BlobProvider = reader.GetSafeString("BlobProvider"),
                BlobProviderData = reader.GetSafeString("BlobProviderData"),
                ContentType = reader.GetSafeString("ContentType"),
                FileNameWithoutExtension = reader.GetSafeString("FileNameWithoutExtension"),
                Extension = reader.GetSafeString("Extension"),
                Size = reader.GetSafeInt64("Size"),
                CreationDate = reader.GetSafeDateTime("CreationDate") ?? DateTime.MinValue,
                IsDeleted = reader.GetSafeBooleanFromBoolean("IsDeleted"),
                Staging = reader.GetSafeBooleanFromBoolean("Staging"),
                StagingPropertyTypeId = reader.GetSafeInt32("StagingPropertyTypeId"),
                StagingVersionId = reader.GetSafeInt32("StagingVersionId"),
                Stream = reader.GetSafeByteArray("Stream"),
                Checksum = reader.GetSafeString("Checksum"),
                RowGuid = reader.GetGuid(reader.GetOrdinal("RowGuid")),
                Timestamp = reader.GetSafeLongFromBytes("Timestamp")
            };
            if (reader.FieldCount > 16)
                file.FileStream = reader.GetSafeByteArray("FileStream");
            file.ExternalStream = GetExternalData(file);

            return file;
        }
        private byte[] GetExternalData(DbFile file)
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
