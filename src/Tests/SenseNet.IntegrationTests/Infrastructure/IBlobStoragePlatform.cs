using System;
using SenseNet.IntegrationTests.Common;

namespace SenseNet.IntegrationTests.Infrastructure
{
    public interface IBlobStoragePlatform : IPlatform
    {
        Type ExpectedExternalBlobProviderType { get; }
        Type ExpectedBlobProviderDataType { get; }
        bool CanUseBuiltInBlobProvider { get; }

        DbFile[] LoadDbFiles(int versionId, string propertyName = "Binary");
        void ConfigureMinimumSizeForFileStreamInBytes(int cheat, out int originalValue);
    }
}
