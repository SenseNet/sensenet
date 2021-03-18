using System;
using System.Collections.Generic;
using System.Text;

namespace SenseNet.IntegrationTests.Infrastructure
{
    public abstract class BlobStorageIntegrationTest<TPlatform, TTestCase> : IntegrationTest<TPlatform, TTestCase>
        where TPlatform : IBlobStoragePlatform, new()
        where TTestCase : BlobStorageTestCaseBase, new()
    {

    }
}
