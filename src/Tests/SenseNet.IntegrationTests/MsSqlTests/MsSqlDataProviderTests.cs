using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.IntegrationTests.Platforms;
using SenseNet.IntegrationTests.TestCases;
using SenseNet.Tests.Core.Implementations;

namespace SenseNet.IntegrationTests.MsSqlTests
{
    [TestClass]
    public class MsSqlDataProviderTests : IntegrationTest<MsSqlPlatform, DataProviderTestCases>
    {
        [TestMethod]
        public Task MsSql_DP_InsertNode() { return TestCase.DP_InsertNode(); }
        [TestMethod]
        public Task MsSql_DP_Update() { return TestCase.DP_Update(); }
        [TestMethod]
        public Task MsSql_DP_CopyAndUpdate_NewVersion() { return TestCase.DP_CopyAndUpdate_NewVersion(); }
        [TestMethod]
        public Task MsSql_DP_CopyAndUpdate_ExpectedVersion() { return TestCase.DP_CopyAndUpdate_ExpectedVersion(); }
        [TestMethod]
        public Task MsSql_DP_UpdateNodeHead() { return TestCase.DP_UpdateNodeHead(); }

        [TestMethod]
        public Task MsSql_DP_HandleAllDynamicProps() { return TestCase.DP_HandleAllDynamicProps(); }
    }
}
