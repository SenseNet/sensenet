﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.IntegrationTests.Platforms;
using SenseNet.IntegrationTests.TestCases;

namespace SenseNet.IntegrationTests.MsSqlTests
{
    [TestClass]
    public class MsSqlSearchTests : IntegrationTest<MsSqlPlatform, SearchTestCases>
    {
        [TestMethod, TestCategory("Services")]
        public void IntT_MsSql_Search_ReferenceField_CSrv()
        {
            TestCase.Search_ReferenceField();
        }
    }
}
