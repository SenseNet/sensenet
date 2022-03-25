﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.IntegrationTests.Platforms;
using SenseNet.IntegrationTests.TestCases;

namespace SenseNet.IntegrationTests.MsSqlTests
{
    [TestClass]
    public class MsSqlAppModelTests : IntegrationTest<MsSqlPlatform, AppModelTestCases>
    {
        [TestMethod]
        public void IntT_MsSql_AppModel_ResolveFromPredefinedPaths_First() { TestCase.AppModel_ResolveFromPredefinedPaths_First(); }
        [TestMethod, TestCategory("Services")]
        public void IntT_MsSql_AppModel_ResolveFromPredefinedPaths_All_CSrv() { TestCase.AppModel_ResolveFromPredefinedPaths_All(); }
    }
}
