using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.IntegrationTests.Platforms;
using SenseNet.IntegrationTests.TestCases;

namespace SenseNet.IntegrationTests.InMemTests
{
    [TestClass]
    public class InMemNodeMove2Tests : IntegrationTest<InMemPlatform, NodeMove2TestCases>
    {
        [TestMethod]
        public void IntT_InMem_NodeMove_2_NodeTreeToNode() { TestCase.NodeMove_2_NodeTreeToNode(); }
        [TestMethod]
        public void IntT_InMem_NodeMove_2_SourceIsLockedByAnother() { TestCase.NodeMove_2_SourceIsLockedByAnother(); }
        [TestMethod]
        public void IntT_InMem_NodeMove_2_SourceIsLockedByCurrent() { TestCase.NodeMove_2_SourceIsLockedByCurrent(); }
        [TestMethod]
        public void IntT_InMem_NodeMove_2_LockedTarget_SameUser() { TestCase.NodeMove_2_LockedTarget_SameUser(); }
        [TestMethod]
        public void IntT_InMem_NodeMove_2_PathBeforeAfter() { TestCase.NodeMove_2_PathBeforeAfter(); }
        [TestMethod]
        public void IntT_InMem_NodeMove_2_MinimalPermissions() { TestCase.NodeMove_2_MinimalPermissions(); }
        [TestMethod]
        public void IntT_InMem_NodeMove_2_SourceWithoutDeletePermission() { TestCase.NodeMove_2_SourceWithoutDeletePermission(); }
        [TestMethod]
        public void IntT_InMem_NodeMove_2_TargetWithoutAddNewPermission() { TestCase.NodeMove_2_TargetWithoutAddNewPermission(); }
        [TestMethod]
        public void IntT_InMem_NodeMove_2_MoreVersion() { TestCase.NodeMove_2_MoreVersion(); }
        [TestMethod]
        public void IntT_InMem_NodeMove_2_WithAspect() { TestCase.NodeMove_2_WithAspect(); }
        [TestMethod]
        public void IntT_InMem_NodeMove_2_ContentList_LeafNodeToContentList() { TestCase.NodeMove_2_ContentList_LeafNodeToContentList(); }
        [TestMethod]
        public void IntT_InMem_NodeMove_2_ContentList_LeafNodeToContentListItem() { TestCase.NodeMove_2_ContentList_LeafNodeToContentListItem(); }
        [TestMethod]
        public void IntT_InMem_NodeMove_2_ContentList_NodeTreeToContentList() { TestCase.NodeMove_2_ContentList_NodeTreeToContentList(); }
        [TestMethod]
        public void IntT_InMem_NodeMove_2_ContentList_NodeTreeToContentListItem() { TestCase.NodeMove_2_ContentList_NodeTreeToContentListItem(); }
        [TestMethod]
        public void IntT_InMem_NodeMove_2_ContentList_NodeWithContentListToNode() { Assert.Inconclusive(); TestCase.NodeMove_2_ContentList_NodeWithContentListToNode(); }
        [TestMethod]
        public void IntT_InMem_NodeMove_2_ContentList_ContentListToNode() { Assert.Inconclusive(); TestCase.NodeMove_2_ContentList_ContentListToNode(); }
        [TestMethod]
        public void IntT_InMem_NodeMove_2_ContentList_ContentListTreeToNode() { Assert.Inconclusive(); TestCase.NodeMove_2_ContentList_ContentListTreeToNode(); }
        [TestMethod]
        public void IntT_InMem_NodeMove_2_ContentList_ContentListItemToNode() { TestCase.NodeMove_2_ContentList_ContentListItemToNode(); }
        [TestMethod]
        public void IntT_InMem_NodeMove_2_ContentList_ContentListItemToContentList() { TestCase.NodeMove_2_ContentList_ContentListItemToContentList(); }
        [TestMethod]
        public void IntT_InMem_NodeMove_2_ContentList_ContentListItemToContentListItem() { TestCase.NodeMove_2_ContentList_ContentListItemToContentListItem(); }
        [TestMethod]
        public void IntT_InMem_NodeMove_2_ContentList_ContentListItemTreeToNode() { TestCase.NodeMove_2_ContentList_ContentListItemTreeToNode(); }
        [TestMethod]
        public void IntT_InMem_NodeMove_2_ContentList_ContentListItemTreeToContentList() { TestCase.NodeMove_2_ContentList_ContentListItemTreeToContentList(); }
        [TestMethod]
        public void IntT_InMem_NodeMove_2_ContentList_ContentListItemTreeToContentListItem() { TestCase.NodeMove_2_ContentList_ContentListItemTreeToContentListItem(); }
        [TestMethod]
        public void IntT_InMem_NodeMove_2_ContentList_ContentListItemTree2ToNode() { TestCase.NodeMove_2_ContentList_ContentListItemTree2ToNode(); }
        [TestMethod]
        public void IntT_InMem_NodeMove_2_ContentList_ContentListItemTree2ToContentList() { TestCase.NodeMove_2_ContentList_ContentListItemTree2ToContentList(); }
        [TestMethod]
        public void IntT_InMem_NodeMove_2_ContentList_ContentListItemTree2ToContentListItem() { TestCase.NodeMove_2_ContentList_ContentListItemTree2ToContentListItem(); }
        [TestMethod]
        public void IntT_InMem_NodeMove_2_ContentList_ContentListItemToSameContentList() { TestCase.NodeMove_2_ContentList_ContentListItemToSameContentList(); }
        [TestMethod]
        public void IntT_InMem_NodeMove_2_ContentList_ContentListItemToSameContentListItem() { TestCase.NodeMove_2_ContentList_ContentListItemToSameContentListItem(); }
        [TestMethod]
        public void IntT_InMem_NodeMove_2_ContentList_ContentListItemTreeToSameContentList() { TestCase.NodeMove_2_ContentList_ContentListItemTreeToSameContentList(); }
        [TestMethod]
        public void IntT_InMem_NodeMove_2_ContentList_ContentListItemTreeToSameContentListItem() { TestCase.NodeMove_2_ContentList_ContentListItemTreeToSameContentListItem(); }
    }
}
