using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SenseNet.BlobStorage.IntegrationTests
{
    [TestClass]
    public class BuiltInProviderSqlFsTests : BlobStorageIntegrationTests
    {
        protected override string DatabaseName => "sn7blobtests_builtinfs";
        protected override bool SqlFileStreamEnabled => true;

        [ClassCleanup]
        public static void CleanupClass()
        {
            TearDown(typeof(BuiltInProviderSqlFsTests));
        }

        [TestMethod]
        public void Blob_BuiltInFS_01_CreateFile()
        {
            base.TestCase01_CreateFile();
        }
    }
}
