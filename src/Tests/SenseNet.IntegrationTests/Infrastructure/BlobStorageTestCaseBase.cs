using System;
using System.Collections.Generic;
using System.Text;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;

namespace SenseNet.IntegrationTests.Infrastructure
{
    public abstract class BlobStorageTestCaseBase : TestCaseBase
    {
        public IBlobStoragePlatform BlobStoragePlatform => (IBlobStoragePlatform) Platform;

        protected bool NeedExternal(Type expectedExternalBlobProviderType, string fileContent, int sizeLimit)
        {
            if (!BlobStoragePlatform.CanUseBuiltInBlobProvider)
                return true;
            if (fileContent.Length + 3 < sizeLimit)
                return false;
            return NeedExternal(expectedExternalBlobProviderType);
        }
        protected bool NeedExternal(Type expectedExternalBlobProviderType)
        {
            if (expectedExternalBlobProviderType == null)
                return false;
            if (expectedExternalBlobProviderType == typeof(BuiltInBlobProvider))
                return false;
            return true;
        }


    }
}
