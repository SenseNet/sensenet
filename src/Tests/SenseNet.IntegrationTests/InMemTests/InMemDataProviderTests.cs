using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.IntegrationTests.Platforms;
using SenseNet.IntegrationTests.TestCases;

namespace SenseNet.IntegrationTests.InMemTests
{
    [TestClass]
    public class InMemDataProviderTests : IntegrationTest<InMemPlatform, DataProviderTestCases>
    {
        [TestMethod]
        public Task InMem_DP_InsertNode() { return TestCase.DP_InsertNode(); }
        [TestMethod]
        public Task InMem_DP_Update() { return TestCase.DP_Update(); }
        [TestMethod]
        public Task InMem_DP_CopyAndUpdate_NewVersion() { return TestCase.DP_CopyAndUpdate_NewVersion(); }
        [TestMethod]
        public Task InMem_DP_CopyAndUpdate_ExpectedVersion() { return TestCase.DP_CopyAndUpdate_ExpectedVersion(); }
        [TestMethod]
        public Task InMem_DP_UpdateNodeHead() { return TestCase.DP_UpdateNodeHead(); }

        [TestMethod]
        public Task InMem_DP_HandleAllDynamicProps() { return TestCase.DP_HandleAllDynamicProps(); }

        [TestMethod]
        public Task InMem_DP_Rename() { return TestCase.DP_Rename(); }

        [TestMethod]
        public Task InMem_DP_LoadChildren() { return TestCase.DP_LoadChildren(); }
    }
}
