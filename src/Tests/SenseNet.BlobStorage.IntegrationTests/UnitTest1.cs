using System.Security.Cryptography.X509Certificates;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SenseNet.BlobStorage.IntegrationTests
{
    [TestClass]
    public class UnitTest1 : BlobStorageIntegrationTests
    {
        // private enum TestMode{BuiltIn, BuiltInFs, Legacy, LegacyFs};

        protected override string DatabaseName => "sn7blobtests";
        protected override bool SqlFileStreamEnabled => false;

        [ClassCleanup]
        public static void CleanupClass()
        {
            TearDown(typeof(UnitTest1));
        }
    }
}
