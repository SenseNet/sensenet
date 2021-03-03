using System;
using System.Collections.Generic;
using System.Text;
using SenseNet.BlobStorage.IntegrationTests;
using SenseNet.IntegrationTests.Infrastructure;

namespace SenseNet.IntegrationTests.Common
{
    internal class SizeLimitSwindler : IDisposable
    {
        private readonly BlobStorageTestCaseBase _testClass;
        private readonly int _originalValue;

        public SizeLimitSwindler(BlobStorageTestCaseBase testClass, int cheat)
        {
            _testClass = testClass;
            testClass.BlobStoragePlatform.ConfigureMinimumSizeForFileStreamInBytes(cheat, out _originalValue);
        }
        public void Dispose()
        {
            _testClass.BlobStoragePlatform.ConfigureMinimumSizeForFileStreamInBytes(_originalValue, out _);
        }
    }
}
