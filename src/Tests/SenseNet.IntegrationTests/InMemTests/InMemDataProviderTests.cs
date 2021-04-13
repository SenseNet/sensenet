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

        [TestMethod]
        public Task InMem_DP_Move() { return TestCase.DP_Move(); }
        [TestMethod]
        public Task InMem_DP_Move_DataStore_NodeHead() { return TestCase.DP_Move_DataStore_NodeHead(); }
        [TestMethod]
        public Task InMem_DP_Move_DataStore_NodeData() { return TestCase.DP_Move_DataStore_NodeData(); }

        [TestMethod]
        public Task InMem_DP_RefreshCacheAfterSave() { return TestCase.DP_RefreshCacheAfterSave(); }

        [TestMethod]
        public Task InMem_DP_LazyLoadedBigText() { return TestCase.DP_LazyLoadedBigText(); }
        [TestMethod]
        public Task InMem_DP_LazyLoadedBigTextVsCache() { return TestCase.DP_LazyLoadedBigTextVsCache(); }

        [TestMethod]
        public Task InMem_DP_LoadChildTypesToAllow() { return TestCase.DP_LoadChildTypesToAllow(); }

        [TestMethod]
        public Task InMem_DP_ContentListTypesInTree() { return TestCase.DP_ContentListTypesInTree(); }

        [TestMethod]
        public Task InMem_DP_ForceDelete() { return TestCase.DP_ForceDelete(); }
        [TestMethod]
        public Task InMem_DP_DeleteDeleted() { return TestCase.DP_DeleteDeleted(); }

        [TestMethod]
        public Task InMem_DP_GetVersionNumbers() { return TestCase.DP_GetVersionNumbers(); }
        [TestMethod]
        public Task InMem_DP_GetVersionNumbers_MissingNode() { return TestCase.DP_GetVersionNumbers_MissingNode(); }

        [TestMethod]
        public Task InMem_DP_LoadBinaryPropertyValues() { return TestCase.DP_LoadBinaryPropertyValues(); }

        [TestMethod]
        public Task InMem_DP_NodeEnumerator() { return TestCase.DP_NodeEnumerator(); }

        [TestMethod]
        public Task InMem_DP_NameSuffix() { return TestCase.DP_NameSuffix(); }

        [TestMethod]
        public Task InMem_DP_TreeSize_Root() { return TestCase.DP_TreeSize_Root(); }
        [TestMethod]
        public Task InMem_DP_TreeSize_Subtree() { return TestCase.DP_TreeSize_Subtree(); }
        [TestMethod]
        public Task InMem_DP_TreeSize_Item() { return TestCase.DP_TreeSize_Item(); }
    }
}
