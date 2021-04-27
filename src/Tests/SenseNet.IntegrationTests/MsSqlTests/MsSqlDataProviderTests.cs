using System;
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

        [TestMethod]
        public Task IntT_MsSql_DP_ContentListTypesInTree() { return TestCase.DP_ContentListTypesInTree(); }

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

        /* ================================================================================================== ShortText escape */

        //[TestMethod]
        //public Task IntT_MsSql_DP_ShortText_Escape() { return TestCase.DP_ShortText_Escape(); }

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

        /* ================================================================================================== Move */

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void IntT_MsSql_DP_Move_SourceIsNotExist() { TestCase.DP_Move_SourceIsNotExist(); }
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void IntT_MsSql_DP_Move_TargetIsNotExist() { TestCase.DP_Move_TargetIsNotExist(); }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void IntT_MsSql_DP_Move_MoveTo_Null() { TestCase.DP_Move_MoveTo_Null(); }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void IntT_MsSql_DP_Move_NullSourcePath() { TestCase.DP_Move_NullSourcePath(); }
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void IntT_MsSql_DP_Move_InvalidSourcePath() { TestCase.DP_Move_InvalidSourcePath(); }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void IntT_MsSql_DP_Move_NullTargetPath() { TestCase.DP_Move_NullTargetPath(); }
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void IntT_MsSql_DP_Move_InvalidTargetPath() { TestCase.DP_Move_InvalidTargetPath(); }
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void IntT_MsSql_DP_Move_ToItsParent() { TestCase.DP_Move_ToItsParent(); }
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void IntT_MsSql_DP_Move_ToItself() { TestCase.DP_Move_ToItself(); }
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void IntT_MsSql_DP_Move_ToUnderItself() { TestCase.DP_Move_ToUnderItself(); }
        [TestMethod]
        [ExpectedException(typeof(NodeAlreadyExistsException))]
        public void IntT_MsSql_DP_Move_TargetHasSameName() { TestCase.DP_Move_TargetHasSameName(); }
        [TestMethod]
        public void IntT_MsSql_DP_Move_NodeTreeToNode() { TestCase.DP_Move_NodeTreeToNode(); }
        [TestMethod]
        public void IntT_MsSql_DP_Move_SourceIsLockedByAnother() { TestCase.DP_Move_SourceIsLockedByAnother(); }
        [TestMethod]
        public void IntT_MsSql_DP_Move_SourceIsLockedByCurrent() { TestCase.DP_Move_SourceIsLockedByCurrent(); }
        [TestMethod]
        public void IntT_MsSql_DP_Move_LockedTarget_SameUser() { TestCase.DP_Move_LockedTarget_SameUser(); }
        [TestMethod]
        public void IntT_MsSql_DP_Move_PathBeforeAfter() { TestCase.DP_Move_PathBeforeAfter(); }
        [TestMethod]
        public void IntT_MsSql_DP_Move_MinimalPermissions() { TestCase.DP_Move_MinimalPermissions(); }
        [TestMethod]
        public void IntT_MsSql_DP_Move_SourceWithoutDeletePermission() { TestCase.DP_Move_SourceWithoutDeletePermission(); }
        [TestMethod]
        public void IntT_MsSql_DP_Move_TargetWithoutAddNewPermission() { TestCase.DP_Move_TargetWithoutAddNewPermission(); }
        [TestMethod]
        public void IntT_MsSql_DP_Move_MoreVersion() { TestCase.DP_Move_MoreVersion(); }
        [TestMethod]
        public void IntT_MsSql_DP_Move_WithAspect() { TestCase.DP_Move_WithAspect(); }
        [TestMethod]
        public void IntT_MsSql_DP_Move_ContentList_LeafNodeToContentList() { TestCase.DP_Move_ContentList_LeafNodeToContentList(); }
        [TestMethod]
        public void IntT_MsSql_DP_Move_ContentList_LeafNodeToContentListItem() { TestCase.DP_Move_ContentList_LeafNodeToContentListItem(); }
        [TestMethod]
        public void IntT_MsSql_DP_Move_ContentList_NodeTreeToContentList() { TestCase.DP_Move_ContentList_NodeTreeToContentList(); }
        [TestMethod]
        public void IntT_MsSql_DP_Move_ContentList_NodeTreeToContentListItem() { TestCase.DP_Move_ContentList_NodeTreeToContentListItem(); }
        [TestMethod]
        public void IntT_MsSql_DP_Move_ContentList_NodeWithContentListToNode() { TestCase.DP_Move_ContentList_NodeWithContentListToNode(); }
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void IntT_MsSql_DP_Move_ContentList_NodeWithContentListToContentList() { TestCase.DP_Move_ContentList_NodeWithContentListToContentList(); }
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void IntT_MsSql_DP_Move_ContentList_NodeWithContentListToContentListItem() { TestCase.DP_Move_ContentList_NodeWithContentListToContentListItem(); }
        [TestMethod]
        public void IntT_MsSql_DP_Move_ContentList_ContentListToNode() { TestCase.DP_Move_ContentList_ContentListToNode(); }
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void IntT_MsSql_DP_Move_ContentList_ContentListToContentList() { TestCase.DP_Move_ContentList_ContentListToContentList(); }
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void IntT_MsSql_DP_Move_ContentList_ContentListToContentListItem() { TestCase.DP_Move_ContentList_ContentListToContentListItem(); }
        [TestMethod]
        public void IntT_MsSql_DP_Move_ContentList_ContentListTreeToNode() { TestCase.DP_Move_ContentList_ContentListTreeToNode(); }
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void IntT_MsSql_DP_Move_ContentList_ContentListTreeToContentList() { TestCase.DP_Move_ContentList_ContentListTreeToContentList(); }
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void IntT_MsSql_DP_Move_ContentList_ContentListTreeToContentListItem() { TestCase.DP_Move_ContentList_ContentListTreeToContentListItem(); }
        [TestMethod]
        public void IntT_MsSql_DP_Move_ContentList_ContentListItemToNode() { TestCase.DP_Move_ContentList_ContentListItemToNode(); }
        [TestMethod]
        public void IntT_MsSql_DP_Move_ContentList_ContentListItemToContentList() { TestCase.DP_Move_ContentList_ContentListItemToContentList(); }
        [TestMethod]
        public void IntT_MsSql_DP_Move_ContentList_ContentListItemToContentListItem() { TestCase.DP_Move_ContentList_ContentListItemToContentListItem(); }
        [TestMethod]
        public void IntT_MsSql_DP_Move_ContentList_ContentListItemTreeToNode() { TestCase.DP_Move_ContentList_ContentListItemTreeToNode(); }
        [TestMethod]
        public void IntT_MsSql_DP_Move_ContentList_ContentListItemTreeToContentList() { TestCase.DP_Move_ContentList_ContentListItemTreeToContentList(); }
        [TestMethod]
        public void IntT_MsSql_DP_Move_ContentList_ContentListItemTreeToContentListItem() { TestCase.DP_Move_ContentList_ContentListItemTreeToContentListItem(); }
        [TestMethod]
        public void IntT_MsSql_DP_Move_ContentList_ContentListItemTree2ToNode() { TestCase.DP_Move_ContentList_ContentListItemTree2ToNode(); }
        [TestMethod]
        public void IntT_MsSql_DP_Move_ContentList_ContentListItemTree2ToContentList() { TestCase.DP_Move_ContentList_ContentListItemTree2ToContentList(); }
        [TestMethod]
        public void IntT_MsSql_DP_Move_ContentList_ContentListItemTree2ToContentListItem() { TestCase.DP_Move_ContentList_ContentListItemTree2ToContentListItem(); }
        [TestMethod]
        public void IntT_MsSql_DP_Move_ContentList_ContentListItemToSameContentList() { TestCase.DP_Move_ContentList_ContentListItemToSameContentList(); }
        [TestMethod]
        public void IntT_MsSql_DP_Move_ContentList_ContentListItemToSameContentListItem() { TestCase.DP_Move_ContentList_ContentListItemToSameContentListItem(); }
        [TestMethod]
        public void IntT_MsSql_DP_Move_ContentList_ContentListItemTreeToSameContentList() { TestCase.DP_Move_ContentList_ContentListItemTreeToSameContentList(); }
        [TestMethod]
        public void IntT_MsSql_DP_Move_ContentList_ContentListItemTreeToSameContentListItem() { TestCase.DP_Move_ContentList_ContentListItemTreeToSameContentListItem(); }
    }
}
