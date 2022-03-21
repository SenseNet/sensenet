﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;
using SenseNet.IntegrationTests.Common;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.Testing;

namespace SenseNet.IntegrationTests.Platforms
{
    /// <summary>
    /// Platform for the most used blob storage mode: blobs in a built-in MsSql table.
    ///  The class name is a multiple compound word: MsSql-BuiltIn-BlobStorage-Platform
    /// </summary>
    public class MsSqlBuiltInBlobStoragePlatform : MsSqlPlatform, IBlobStoragePlatform
    {
        //TODO: [DIREF] get blob service through the constructor
        private IBlobStorage BlobStorage => Providers.Instance.BlobStorage;

        public virtual Type ExpectedExternalBlobProviderType => null; // typeof(BuiltInBlobProvider);
        public virtual Type ExpectedBlobProviderDataType => null; // typeof(BuiltinBlobProviderData);
        public virtual bool CanUseBuiltInBlobProvider => true;
        public virtual bool UseChunk => false;

        public DbFile[] LoadDbFiles(int versionId, string propertyName = "Binary")
        {
            //SELECT f.* FROM BinaryProperties b JOIN Files f on f.FileId = b.FileId
            //WHERE b.VersionId = {versionId} and b.PropertyTypeId = {propTypeId}";

            var propTypeId = Providers.Instance.StorageSchema.PropertyTypes[propertyName].Id;
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
        public DbFile LoadDbFile(int fileId)
        {
            var sql = $@"SELECT * FROM Files WHERE FileId = {fileId}";
            using (var ctx = GetDataContext())
                return ctx.ExecuteReaderAsync(sql, async (reader, cancel) =>
                {
                    cancel.ThrowIfCancellationRequested();
                    return await reader.ReadAsync(cancel)
                        ? GetFileFromReader(reader)
                        : null;
                }).GetAwaiter().GetResult();
        }
        private SnDataContext GetDataContext()
        {
            return ((RelationalDataProviderBase)Providers.Instance.DataStore.DataProvider).CreateDataContext(CancellationToken.None);
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
            file.ExternalStream = GetExternalData(file);

            return file;
        }
        private byte[] GetExternalData(DbFile file)
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
            return GetRawDataAsync(fileId).ConfigureAwait(false).GetAwaiter().GetResult();
        }
        protected virtual async Task<byte[][]> GetRawDataAsync(int fileId)
        {
            using (var ctx = new MsSqlDataContext(RepositoryConnectionString, new DataOptions(), CancellationToken.None))
            {
                var script = "SELECT [Stream] FROM Files WHERE FileId = @FileId";
                var scalar = await ctx.ExecuteScalarAsync(script, cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        ctx.CreateParameter("@FileId", SqlDbType.Int, fileId)
                    });
                }).ConfigureAwait(false);

                if(scalar == DBNull.Value )
                    return new byte[0][];
                var buffer = (byte[]) scalar;
                if(buffer.Length == 0)
                    return new byte[0][];
                return new [] {buffer};
            }
        }


        public void UpdateFileCreationDate(int fileId, DateTime creationDate)
        {
            var sql = $"UPDATE Files SET CreationDate = @CreationDate WHERE FileId = {fileId}";
            using (var ctx = GetDataContext())
            {
                ctx.ExecuteNonQueryAsync(sql, cmd =>
                {
                    cmd.Parameters.Add(ctx.CreateParameter("@CreationDate", DbType.DateTime2, creationDate));
                }).GetAwaiter().GetResult();
            }
        }

        public IDisposable SwindleWaitingBetweenCleanupFiles(int milliseconds)
        {
            return new MsSqlWaitingBetweenCleanupFilesSwindler(milliseconds);
        }
        private class MsSqlWaitingBetweenCleanupFilesSwindler : Swindler<int>
        {
            private static readonly string FieldName = "_waitBetweenCleanupFilesMilliseconds";
            public MsSqlWaitingBetweenCleanupFilesSwindler(int hack) : base(
                hack,
                () =>
                {
                    var metaDataProvider = (MsSqlBlobMetaDataProvider)Providers.Instance.BlobMetaDataProvider;
                    var accessor = new ObjectAccessor(metaDataProvider);
                    return (int)accessor.GetField(FieldName);
                },
                (value) =>
                {
                    var metaDataProvider = (MsSqlBlobMetaDataProvider)Providers.Instance.BlobMetaDataProvider;
                    var accessor = new ObjectAccessor(metaDataProvider);
                    accessor.SetField(FieldName, value);
                })
            {
            }
        }

    }
}
