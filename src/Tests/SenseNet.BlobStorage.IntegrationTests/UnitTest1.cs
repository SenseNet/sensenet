using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SenseNet.BlobStorage.IntegrationTests
{
    [TestClass]
    public class UnitTest1 : BlobStorageIntegrationTests
    {
        // private enum TestMode{BuiltIn, BuiltInFs, Legacy, LegacyFs};

        protected static string DatabaseName => "sn7blobtests";

        [ClassInitialize]
        public static void InitializeClass(TestContext testContext)
        {
            EnsureDatabase(DatabaseName);
        }

    }
}
