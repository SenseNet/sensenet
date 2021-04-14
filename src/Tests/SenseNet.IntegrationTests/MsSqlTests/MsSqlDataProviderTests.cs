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

        [TestMethod]
        public Task MsSql_DP_Rename() { return TestCase.DP_Rename(); }

        [TestMethod]
        public Task MsSql_DP_LoadChildren() { return TestCase.DP_LoadChildren(); }

        [TestMethod]
        public Task MsSql_DP_Move() { return TestCase.DP_Move(); }
        [TestMethod]
        public Task MsSql_DP_Move_DataStore_NodeHead() { return TestCase.DP_Move_DataStore_NodeHead(); }
        [TestMethod]
        public Task MsSql_DP_Move_DataStore_NodeData() { return TestCase.DP_Move_DataStore_NodeData(); }

        [TestMethod]
        public Task MsSql_DP_RefreshCacheAfterSave() { return TestCase.DP_RefreshCacheAfterSave(); }

        [TestMethod]
        public Task MsSql_DP_LazyLoadedBigText() { return TestCase.DP_LazyLoadedBigText(); }
        [TestMethod]
        public Task MsSql_DP_LazyLoadedBigTextVsCache() { return TestCase.DP_LazyLoadedBigTextVsCache(); }

        [TestMethod]
        public Task MsSql_DP_LoadChildTypesToAllow() { return TestCase.DP_LoadChildTypesToAllow(); }

        [TestMethod]
        public Task MsSql_DP_ContentListTypesInTree() { return TestCase.DP_ContentListTypesInTree(); }

        [TestMethod]
        public Task MsSql_DP_ForceDelete() { return TestCase.DP_ForceDelete(); }
        [TestMethod]
        public Task MsSql_DP_DeleteDeleted() { return TestCase.DP_DeleteDeleted(); }

        [TestMethod]
        public Task MsSql_DP_GetVersionNumbers() { return TestCase.DP_GetVersionNumbers(); }
        [TestMethod]
        public Task MsSql_DP_GetVersionNumbers_MissingNode() { return TestCase.DP_GetVersionNumbers_MissingNode(); }

        [TestMethod]
        public Task MsSql_DP_LoadBinaryPropertyValues() { return TestCase.DP_LoadBinaryPropertyValues(); }

        [TestMethod]
        public Task MsSql_DP_NodeEnumerator() { return TestCase.DP_NodeEnumerator(); }

        [TestMethod]
        public Task MsSql_DP_NameSuffix() { return TestCase.DP_NameSuffix(); }

        [TestMethod]
        public Task MsSql_DP_TreeSize_Root() { return TestCase.DP_TreeSize_Root(); }
        [TestMethod]
        public Task MsSql_DP_TreeSize_Subtree() { return TestCase.DP_TreeSize_Subtree(); }
        [TestMethod]
        public Task MsSql_DP_TreeSize_Item() { return TestCase.DP_TreeSize_Item(); }

        /* ================================================================================================== ShortText escape */

        //[TestMethod]
        //public async Task DP_ShortText_Escape() { return TestCase.DP_ShortText_Escape(); }

        /* ================================================================================================== NodeQuery */

        [TestMethod]
        public Task MsSql_DP_NodeQuery_InstanceCount() { return TestCase.DP_NodeQuery_InstanceCount(); }
        [TestMethod]
        public Task MsSql_DP_NodeQuery_ChildrenIdentifiers() { return TestCase.DP_NodeQuery_ChildrenIdentifiers(); }

        [TestMethod]
        public Task MsSql_DP_NodeQuery_QueryNodesByTypeAndPathAndName() { return TestCase.DP_NodeQuery_QueryNodesByTypeAndPathAndName(); }
        [TestMethod]
        public Task MsSql_DP_NodeQuery_QueryNodesByTypeAndPathAndProperty() { return TestCase.DP_NodeQuery_QueryNodesByTypeAndPathAndProperty(); }
        [TestMethod]
        public Task MsSql_DP_NodeQuery_QueryNodesByReferenceAndType() { return TestCase.DP_NodeQuery_QueryNodesByReferenceAndType(); }
    }
}
