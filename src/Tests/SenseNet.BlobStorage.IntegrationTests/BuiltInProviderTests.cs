using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SenseNet.BlobStorage.IntegrationTests
{
    [TestClass]
    public class BuiltInProviderTests : BlobStorageIntegrationTests
    {
        protected override string DatabaseName => "sn7blobtests_builtin";
        protected override bool SqlFileStreamEnabled => false;

        [ClassCleanup]
        public static void CleanupClass()
        {
            TearDown(typeof(BuiltInProviderTests));
        }

        [TestMethod]
        public void Blob_BuiltIn_01_CreateFile()
        {
            base.TestCase01_CreateFile();
        }
    }
}
