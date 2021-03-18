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

        DbFile[] LoadDbFiles(int versionId, string propertyName = "Binary");
        DbFile LoadDbFile(int fileId);

        //UNDONE:<?Blob: Platform independent code
        void ConfigureMinimumSizeForFileStreamInBytes(int cheat, out int originalValue);

        //UNDONE:<?Blob: Platform independent code
        byte[] GetExternalData(string blobProvider, string blobProviderData, long size);
        //UNDONE:<?Blob: Platform independent code
        byte[] GetExternalData(BlobStorageContext context);

        void UpdateFileCreationDate(int fileId, DateTime creationDate);
    }
}
