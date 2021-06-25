using System;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.IntegrationTests.Common;

namespace SenseNet.IntegrationTests.Infrastructure
{
    public interface IBlobStoragePlatform : IPlatform
    {
        Type ExpectedExternalBlobProviderType { get; }
        Type ExpectedBlobProviderDataType { get; }
        bool CanUseBuiltInBlobProvider { get; }
        bool UseChunk { get; }

        DbFile[] LoadDbFiles(int versionId, string propertyName = "Binary");
        DbFile LoadDbFile(int fileId);

        void ConfigureMinimumSizeForFileStreamInBytes(int cheat, out int originalValue);

        byte[] GetExternalData(string blobProvider, string blobProviderData, long size);
        byte[] GetExternalData(BlobStorageContext context);
        byte[][] GetRawData(int fileId);

        void UpdateFileCreationDate(int fileId, DateTime creationDate);
        IDisposable SwindleWaitingBetweenCleanupFiles(int milliseconds);
    }
}
