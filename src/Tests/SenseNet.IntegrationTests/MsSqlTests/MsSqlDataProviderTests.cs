﻿using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.IntegrationTests.Platforms;
using SenseNet.IntegrationTests.TestCases;

namespace SenseNet.IntegrationTests.MsSqlTests
{
    [TestClass]
    public class MsSqlDataProviderTests : IntegrationTest<MsSqlPlatform, DataProviderTestCases>
    {
        [TestMethod]
        public Task IntT_MsSql_DP_InsertNode() { return TestCase.DP_InsertNode(); }
        [TestMethod]
        public Task IntT_MsSql_DP_Update() { return TestCase.DP_Update(); }
        [TestMethod]
        public Task IntT_MsSql_DP_CopyAndUpdate_NewVersion() { return TestCase.DP_CopyAndUpdate_NewVersion(); }
        [TestMethod]
        public Task IntT_MsSql_DP_CopyAndUpdate_ExpectedVersion() { return TestCase.DP_CopyAndUpdate_ExpectedVersion(); }
        [TestMethod]
        public Task IntT_MsSql_DP_UpdateNodeHead() { return TestCase.DP_UpdateNodeHead(); }

        [TestMethod]
        public Task IntT_MsSql_DP_HandleAllDynamicProps() { return TestCase.DP_HandleAllDynamicProps(); }

        [TestMethod]
        public Task IntT_MsSql_DP_Rename() { return TestCase.DP_Rename(); }

        [TestMethod]
        public Task IntT_MsSql_DP_LoadChildren() { return TestCase.DP_LoadChildren(); }

        [TestMethod]
        public Task IntT_MsSql_DP_Move() { return TestCase.DP_Move(); }
        [TestMethod]
        public Task IntT_MsSql_DP_Move_DataStore_NodeHead() { return TestCase.DP_Move_DataStore_NodeHead(); }
        [TestMethod]
        public Task IntT_MsSql_DP_Move_DataStore_NodeData() { return TestCase.DP_Move_DataStore_NodeData(); }

        [TestMethod]
        public Task IntT_MsSql_DP_RefreshCacheAfterSave() { return TestCase.DP_RefreshCacheAfterSave(); }

        [TestMethod]
        public Task IntT_MsSql_DP_LazyLoadedBigText() { return TestCase.DP_LazyLoadedBigText(); }
        [TestMethod]
        public Task IntT_MsSql_DP_LazyLoadedBigTextVsCache() { return TestCase.DP_LazyLoadedBigTextVsCache(); }

        [TestMethod]
        public Task IntT_MsSql_DP_LoadChildTypesToAllow() { return TestCase.DP_LoadChildTypesToAllow(); }

        [TestMethod, TestCategory("Services")]
        public Task IntT_MsSql_DP_ContentListTypesInTree_CSrv() { return TestCase.DP_ContentListTypesInTree(); }

        [TestMethod]
        public Task IntT_MsSql_DP_ForceDelete() { return TestCase.DP_ForceDelete(); }
        [TestMethod]
        public Task IntT_MsSql_DP_DeleteDeleted() { return TestCase.DP_DeleteDeleted(); }

        [TestMethod]
        public Task IntT_MsSql_DP_GetVersionNumbers() { return TestCase.DP_GetVersionNumbers(); }
        [TestMethod]
        public Task IntT_MsSql_DP_GetVersionNumbers_MissingNode() { return TestCase.DP_GetVersionNumbers_MissingNode(); }

        [TestMethod]
        public Task IntT_MsSql_DP_LoadBinaryPropertyValues() { return TestCase.DP_LoadBinaryPropertyValues(); }

        [TestMethod]
        public Task IntT_MsSql_DP_NodeEnumerator() { return TestCase.DP_NodeEnumerator(); }

        [TestMethod]
        public Task IntT_MsSql_DP_NameSuffix() { return TestCase.DP_NameSuffix(); }

        [TestMethod]
        public Task IntT_MsSql_DP_TreeSize_Root() { return TestCase.DP_TreeSize_Root(); }
        [TestMethod]
        public Task IntT_MsSql_DP_TreeSize_Subtree() { return TestCase.DP_TreeSize_Subtree(); }
        [TestMethod]
        public Task IntT_MsSql_DP_TreeSize_Item() { return TestCase.DP_TreeSize_Item(); }

        /* ================================================================================================== NodeQuery */

        [TestMethod]
        public Task IntT_MsSql_DP_NodeQuery_InstanceCount() { return TestCase.DP_NodeQuery_InstanceCount(); }
        [TestMethod]
        public Task IntT_MsSql_DP_NodeQuery_ChildrenIdentifiers() { return TestCase.DP_NodeQuery_ChildrenIdentifiers(); }

        [TestMethod]
        public Task IntT_MsSql_DP_NodeQuery_QueryNodesByTypeAndPathAndName() { return TestCase.DP_NodeQuery_QueryNodesByTypeAndPathAndName(); }
        [TestMethod]
        public Task IntT_MsSql_DP_NodeQuery_QueryNodesByTypeAndPathAndProperty() { return TestCase.DP_NodeQuery_QueryNodesByTypeAndPathAndProperty(); }
        [TestMethod]
        public Task IntT_MsSql_DP_NodeQuery_QueryNodesByReferenceAndType() { return TestCase.DP_NodeQuery_QueryNodesByReferenceAndType(); }

        /* ================================================================================================== TreeLock */

        [TestMethod]
        public Task IntT_MsSql_DP_LoadEntityTree() { return TestCase.DP_LoadEntityTree(); }
        [TestMethod]
        public Task IntT_MsSql_DP_TreeLock() { return TestCase.DP_TreeLock(); }

        /* ================================================================================================== IndexDocument */

        [TestMethod]
        public Task IntT_MsSql_DP_LoadIndexDocuments() { return TestCase.DP_LoadIndexDocuments(); }
        [TestMethod]
        public Task IntT_MsSql_DP_SaveIndexDocumentById() { return TestCase.DP_SaveIndexDocumentById(); }

        /* ================================================================================================== IndexingActivities */

        [TestMethod]
        public Task IntT_MsSql_DP_IA_GetLastIndexingActivityId() { return TestCase.DP_IA_GetLastIndexingActivityId(); }
        [TestMethod]
        public Task IntT_MsSql_DP_IA_LoadIndexingActivities_Page() { return TestCase.DP_IA_LoadIndexingActivities_Page(); }
        [TestMethod]
        public Task IntT_MsSql_DP_IA_LoadIndexingActivities_PageUnprocessed() { return TestCase.DP_IA_LoadIndexingActivities_PageUnprocessed(); }
        [TestMethod]
        public Task IntT_MsSql_DP_IA_LoadIndexingActivities_Gaps() { return TestCase.DP_IA_LoadIndexingActivities_Gaps(); }
        [TestMethod]
        public Task IntT_MsSql_DP_IA_LoadIndexingActivities_Executable() { return TestCase.DP_IA_LoadIndexingActivities_Executable(); }
        [TestMethod]
        public Task IntT_MsSql_DP_IA_LoadIndexingActivities_ExecutableAndFinished() { return TestCase.DP_IA_LoadIndexingActivities_ExecutableAndFinished(); }
        [TestMethod]
        public Task IntT_MsSql_DP_IA_UpdateRunningState() { return TestCase.DP_IA_UpdateRunningState(); }
        [TestMethod]
        public Task IntT_MsSql_DP_IA_RefreshLockTime() { return TestCase.DP_IA_RefreshLockTime(); }
        [TestMethod]
        public Task IntT_MsSql_DP_IA_DeleteFinished() { return TestCase.DP_IA_DeleteFinished(); }
        [TestMethod]
        public Task IntT_MsSql_DP_IA_LoadFull() { return TestCase.DP_IA_LoadFull(); }

        /* ================================================================================================== Nodes */

        [TestMethod]
        public Task IntT_MsSql_DP_CopyAndUpdateNode_Rename() { return TestCase.DP_CopyAndUpdateNode_Rename(); }
        [TestMethod]
        public Task IntT_MsSql_DP_LoadNodes() { return TestCase.DP_LoadNodes(); }
        [TestMethod]
        public Task IntT_MsSql_DP_LoadNodeHeadByVersionId_Missing() { return TestCase.DP_LoadNodeHeadByVersionId_Missing(); }
        [TestMethod]
        public Task IntT_MsSql_DP_NodeAndVersion_CountsAndTimestamps() { return TestCase.DP_NodeAndVersion_CountsAndTimestamps(); }

        /* ================================================================================================== Errors */

        [TestMethod]
        public Task IntT_MsSql_DP_Error_InsertNode_AlreadyExists() { return TestCase.DP_Error_InsertNode_AlreadyExists(); }

        [TestMethod]
        public Task IntT_MsSql_DP_Error_UpdateNode_Deleted() { return TestCase.DP_Error_UpdateNode_Deleted(); }
        [TestMethod]
        public Task IntT_MsSql_DP_Error_UpdateNode_MissingVersion() { return TestCase.DP_Error_UpdateNode_MissingVersion(); }
        [TestMethod]
        public Task IntT_MsSql_DP_Error_UpdateNode_OutOfDate() { return TestCase.DP_Error_UpdateNode_OutOfDate(); }

        [TestMethod]
        public Task IntT_MsSql_DP_Error_CopyAndUpdateNode_Deleted() { return TestCase.DP_Error_CopyAndUpdateNode_Deleted(); }
        [TestMethod]
        public Task IntT_MsSql_DP_Error_CopyAndUpdateNode_MissingVersion() { return TestCase.DP_Error_CopyAndUpdateNode_MissingVersion(); }
        [TestMethod]
        public Task IntT_MsSql_DP_Error_CopyAndUpdateNode_OutOfDate() { return TestCase.DP_Error_CopyAndUpdateNode_OutOfDate(); }

        [TestMethod]
        public Task IntT_MsSql_DP_Error_UpdateNodeHead_Deleted() { return TestCase.DP_Error_UpdateNodeHead_Deleted(); }
        [TestMethod]
        public Task IntT_MsSql_DP_Error_UpdateNodeHead_OutOfDate() { return TestCase.DP_Error_UpdateNodeHead_OutOfDate(); }

        [TestMethod]
        public Task IntT_MsSql_DP_Error_DeleteNode() { return TestCase.DP_Error_DeleteNode(); }

        [TestMethod]
        public Task IntT_MsSql_DP_Error_MoveNode_MissingSource() { return TestCase.DP_Error_MoveNode_MissingSource(); }
        [TestMethod]
        public Task IntT_MsSql_DP_Error_MoveNode_MissingTarget() { return TestCase.DP_Error_MoveNode_MissingTarget(); }
        [TestMethod]
        public Task IntT_MsSql_DP_Error_MoveNode_OutOfDate() { return TestCase.DP_Error_MoveNode_OutOfDate(); }

        [TestMethod]
        public Task IntT_MsSql_DP_Error_QueryNodesByReferenceAndTypeAsync() { return TestCase.DP_Error_QueryNodesByReferenceAndTypeAsync(); }

        /* ================================================================================================== Transaction */

        [TestMethod]
        public Task IntT_MsSql_DP_Transaction_InsertNode() { return TestCase.DP_Transaction_InsertNode(); }
        [TestMethod]
        public Task IntT_MsSql_DP_Transaction_UpdateNode() { return TestCase.DP_Transaction_UpdateNode(); }
        [TestMethod]
        public Task IntT_MsSql_DP_Transaction_CopyAndUpdateNode() { return TestCase.DP_Transaction_CopyAndUpdateNode(); }
        [TestMethod]
        public Task IntT_MsSql_DP_Transaction_UpdateNodeHead() { return TestCase.DP_Transaction_UpdateNodeHead(); }
        [TestMethod]
        public Task IntT_MsSql_DP_Transaction_MoveNode() { return TestCase.DP_Transaction_MoveNode(); }
        [TestMethod]
        public Task IntT_MsSql_DP_Transaction_RenameNode() { return TestCase.DP_Transaction_RenameNode(); }
        [TestMethod]
        public Task IntT_MsSql_DP_Transaction_DeleteNode() { return TestCase.DP_Transaction_DeleteNode(); }

        /* ================================================================================================== Schema */

        [TestMethod]
        public Task IntT_MsSql_DP_Schema_ExclusiveUpdate() { return TestCase.DP_Schema_ExclusiveUpdate(); }
    }
}
