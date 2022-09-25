﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.IntegrationTests.MsSql.Platforms;
using SenseNet.IntegrationTests.TestCases;

namespace SenseNet.IntegrationTests.MsSql.MsSqlTests
{
    [TestClass]
    public class MsSqlSystemFlagTests : IntegrationTest<MsSqlPlatform, SystemFlagTestCases>
    {
        [TestMethod]
        public void IntT_MsSql_SystemFlag_OnFolder() { TestCase.SystemFlag_OnFolder(); }
        [TestMethod]
        public void IntT_MsSql_SystemFlag_OnSystemFolder() { TestCase.SystemFlag_OnSystemFolder(); }
        [TestMethod]
        public void IntT_MsSql_SystemFlag_OnFolderUnderSystemFolder() { TestCase.SystemFlag_OnFolderUnderSystemFolder(); }

        [TestMethod, TestCategory("Services")]
        public void IntT_MsSql_SystemFlag_Copy_FromFolderToFolder_CSrv() { TestCase.SystemFlag_Copy_FromFolderToFolder(); }
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
