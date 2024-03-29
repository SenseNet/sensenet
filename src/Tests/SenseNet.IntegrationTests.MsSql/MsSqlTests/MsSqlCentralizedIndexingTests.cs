﻿using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.IntegrationTests.MsSql.Platforms;
using SenseNet.IntegrationTests.TestCases;

namespace SenseNet.IntegrationTests.MsSql.MsSqlTests
{
    [TestClass]
    public class MsSqlCentralizedIndexingTests : IntegrationTest<MsSqlPlatform, CentralizedIndexingTestCases>
    {
        [TestMethod, TestCategory("IR")]
        public async Task IntT_MsSql_Indexing_Centralized_InMemory_RegisterAndReload()
        {
            await TestCase.Indexing_Centralized_RegisterAndReload().ConfigureAwait(false);
        }

        [TestMethod, TestCategory("IR")]
        public async Task IntT_MsSql_Indexing_Centralized_InMemory_UpdateStateToDone()
        {
            await TestCase.Indexing_Centralized_UpdateStateToDone().ConfigureAwait(false);
        }

        [TestMethod, TestCategory("IR"), TestCategory("Services")]
        public async Task IntT_MsSql_Indexing_Centralized_InMemory_Allocate01_SelectWaiting_CSrv()
        {
            await TestCase.Indexing_Centralized_Allocate01_SelectWaiting().ConfigureAwait(false);
        }
        [TestMethod, TestCategory("IR")]
        public async Task IntT_MsSql_Indexing_Centralized_InMemory_Allocate02_IdDependency()
        {
            await TestCase.Indexing_Centralized_Allocate02_IdDependency().ConfigureAwait(false);
        }
        [TestMethod, TestCategory("IR")]
        public async Task IntT_MsSql_Indexing_Centralized_InMemory_Allocate02_IdDependency_VersionId0()
        {
            await TestCase.Indexing_Centralized_Allocate02_IdDependency_VersionId0().ConfigureAwait(false);
        }
        [TestMethod, TestCategory("IR")]
        public async Task IntT_MsSql_Indexing_Centralized_InMemory_Allocate03_InactiveDependency()
        {
            await TestCase.Indexing_Centralized_Allocate03_InactiveDependency().ConfigureAwait(false);
        }
        [TestMethod, TestCategory("IR")]
        public async Task IntT_MsSql_Indexing_Centralized_InMemory_Allocate04_SelectMore()
        {
            await TestCase.Indexing_Centralized_Allocate04_SelectMore().ConfigureAwait(false);
        }
        [TestMethod, TestCategory("IR")]
        public async Task IntT_MsSql_Indexing_Centralized_InMemory_Allocate05_PathDependency()
        {
            await TestCase.Indexing_Centralized_Allocate05_PathDependency().ConfigureAwait(false);
        }
        [TestMethod, TestCategory("IR")]
        public async Task IntT_MsSql_Indexing_Centralized_InMemory_Allocate06_Timeout()
        {
            await TestCase.Indexing_Centralized_Allocate06_Timeout().ConfigureAwait(false);
        }
        [TestMethod, TestCategory("IR")]
        public async Task IntT_MsSql_Indexing_Centralized_InMemory_Allocate07_MaxRecords()
        {
            await TestCase.Indexing_Centralized_Allocate07_MaxRecords().ConfigureAwait(false);
        }
        [TestMethod, TestCategory("IR")]
        public async Task IntT_MsSql_Indexing_Centralized_InMemory_Allocate08_StateUpdated()
        {
            await TestCase.Indexing_Centralized_Allocate08_StateUpdated().ConfigureAwait(false);
        }

        [TestMethod, TestCategory("IR")]
        public async Task IntT_MsSql_Indexing_Centralized_InMemory_AllocateAndState()
        {
            await TestCase.Indexing_Centralized_AllocateAndState().ConfigureAwait(false);
        }
        [TestMethod, TestCategory("IR")]
        public async Task IntT_MsSql_Indexing_Centralized_InMemory_RefreshLock()
        {
            await TestCase.Indexing_Centralized_RefreshLock().ConfigureAwait(false);
        }
        [TestMethod, TestCategory("IR")]
        public async Task IntT_MsSql_Indexing_Centralized_InMemory_DeleteFinished()
        {
            await TestCase.Indexing_Centralized_DeleteFinished().ConfigureAwait(false);
        }
    }
}
