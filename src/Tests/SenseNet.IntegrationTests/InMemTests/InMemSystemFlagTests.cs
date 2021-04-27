using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.IntegrationTests.Platforms;
using SenseNet.IntegrationTests.TestCases;

namespace SenseNet.IntegrationTests.InMemTests
{
    [TestClass]
    public class InMemSystemFlagTests : IntegrationTest<InMemPlatform, SystemFlagTestCases>
    {
        [TestMethod]
        public void IntT_MsSql_SystemFlag_OnFolder() { TestCase.SystemFlag_OnFolder(); }
        [TestMethod]
        public void IntT_MsSql_SystemFlag_OnSystemFolder() { TestCase.SystemFlag_OnSystemFolder(); }
        [TestMethod]
        public void IntT_MsSql_SystemFlag_OnFolderUnderSystemFolder() { TestCase.SystemFlag_OnFolderUnderSystemFolder(); }

        [TestMethod]
        public void IntT_MsSql_SystemFlag_Copy_FromFolderToFolder() { TestCase.SystemFlag_Copy_FromFolderToFolder(); }
        [TestMethod]
        public void IntT_MsSql_SystemFlag_Copy_FromSystemFolderToSystemFolder() { TestCase.SystemFlag_Copy_FromSystemFolderToSystemFolder(); }
        [TestMethod]
        public void IntT_MsSql_SystemFlag_Copy_FromFolderToSystemFolder() { TestCase.SystemFlag_Copy_FromFolderToSystemFolder(); }
        [TestMethod]
        public void IntT_MsSql_SystemFlag_Copy_FromSystemFolderToFolder() { TestCase.SystemFlag_Copy_FromSystemFolderToFolder(); }

        [TestMethod]
        public void IntT_MsSql_SystemFlag_Move_FromFolderToFolder() { TestCase.SystemFlag_Move_FromFolderToFolder(); }
        [TestMethod]
        public void IntT_MsSql_SystemFlag_Move_FromSystemFolderToSystemFolder() { TestCase.SystemFlag_Move_FromSystemFolderToSystemFolder(); }
        [TestMethod]
        public void IntT_MsSql_SystemFlag_Move_FromFolderToSystemFolder() { TestCase.SystemFlag_Move_FromFolderToSystemFolder(); }
        [TestMethod]
        public void IntT_MsSql_SystemFlag_Move_FromSystemFolderToFolder() { TestCase.SystemFlag_Move_FromSystemFolderToFolder(); }

        [TestMethod]
        public void IntT_MsSql_SystemFlag_Move_FromFolderToSystemFolder_Descendant() { TestCase.SystemFlag_Move_FromFolderToSystemFolder_Descendant(); }
    }
}
