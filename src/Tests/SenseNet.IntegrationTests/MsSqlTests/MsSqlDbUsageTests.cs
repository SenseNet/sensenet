﻿using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.IntegrationTests.Platforms;
using SenseNet.IntegrationTests.TestCases;

namespace SenseNet.IntegrationTests.MsSqlTests
{
    [TestClass]
    public class MsSqlDbUsageTests : IntegrationTest<MsSqlPlatform, DbUsageTests>
    {
        [TestMethod, TestCategory("Services")]
        public async Task IntT_MsSql_DbUsage_PreviewsVersionsBlobsTexts_CSrv()
        {
            await TestCase.DbUsage_PreviewsVersionsBlobsTexts().ConfigureAwait(false);
        }
    }
}
