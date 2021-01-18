using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.IntegrationTests.Platforms;
using SenseNet.IntegrationTests.TestCases;

namespace SenseNet.IntegrationTests.InMemTests
{
    [TestClass]
    public class InMemNodeCopyMoveTests : IntegrationTest<InMemPlatform, NodeCopyMoveTestCases>
    {
        /* ==================================================================================== COPY */

        [TestMethod]
        public void IntT_InMem_NodeCopy_Node_from_Outer_to_Outer()
        {
            TestCase.NodeCopy_Node_from_Outer_to_Outer();
        }
        [TestMethod]
        public void IntT_InMem_NodeCopy_Tree_from_Outer_to_Outer()
        {
            TestCase.NodeCopy_Tree_from_Outer_to_Outer();
        }
        [TestMethod]
        public void IntT_InMem_NodeCopy_Node_from_Outer_to_List()
        {
            TestCase.NodeCopy_Node_from_Outer_to_List();
        }
        [TestMethod]
        public void IntT_InMem_NodeCopy_Tree_from_Outer_to_List()
        {
            TestCase.NodeCopy_Tree_from_Outer_to_List();
        }

        [TestMethod]
        public void IntT_InMem_NodeCopy_Node_from_List_to_Outer()
        {
            TestCase.NodeCopy_Node_from_List_to_Outer();
        }
        [TestMethod]
        public void IntT_InMem_NodeCopy_Tree_from_List_to_Outer()
        {
            TestCase.NodeCopy_Tree_from_List_to_Outer();
        }
        [TestMethod]
        public void IntT_InMem_NodeCopy_Node_from_List_to_SameList()
        {
            TestCase.NodeCopy_Node_from_List_to_SameList();
        }
        [TestMethod]
        public void IntT_InMem_NodeCopy_Tree_from_List_to_SameList()
        {
            TestCase.NodeCopy_Tree_from_List_to_SameList();
        }

        [TestMethod]
        public void IntT_InMem_NodeCopy_Node_from_List1_to_List2()
        {
            TestCase.NodeCopy_Node_from_List1_to_List2();
        }
        [TestMethod]
        public void IntT_InMem_NodeCopy_Tree_from_List1_to_List2()
        {
            TestCase.NodeCopy_Tree_from_List1_to_List2();
        }
        [TestMethod]
        public void IntT_InMem_NodeCopy_Node_from_List1_to_FolderOfList2()
        {
            TestCase.NodeCopy_Node_from_List1_to_FolderOfList2();
        }
        [TestMethod]
        public void IntT_InMem_NodeCopy_Tree_from_List1_to_FolderOfList2()
        {
            TestCase.NodeCopy_Tree_from_List1_to_FolderOfList2();
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void IntT_InMem_NodeCopy_List1_to_List2()
        {
            TestCase.NodeCopy_List1_to_List2();
        }
        [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
        public void IntT_InMem_NodeCopy_TreeWithList_to_List2()
        {
            TestCase.NodeCopy_TreeWithList_to_List2();
        }
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void IntT_InMem_NodeCopy_List1_to_FolderOfList2()
        {
            TestCase.NodeCopy_List1_to_FolderOfList2();
        }
        [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
        public void IntT_InMem_NodeCopy_TreeWithList_to_FolderOfList2()
        {
            TestCase.NodeCopy_TreeWithList_to_FolderOfList2();
        }

        /* ==================================================================================== CrossBinding */

        [TestMethod]
        public void IntT_InMem_NodeCopy_CrossBinding_SameName_SameType_SameSlot()
        {
            TestCase.NodeCopy_CrossBinding_SameName_SameType_SameSlot();
        }
        [TestMethod]
        public void IntT_InMem_NodeCopy_CrossBinding_SameName_SameType_DiffSlot()
        {
            TestCase.NodeCopy_CrossBinding_SameName_SameType_DiffSlot();
        }
        [TestMethod]
        public void IntT_InMem_NodeCopy_CrossBinding_SameName_DiffType()
        {
            TestCase.NodeCopy_CrossBinding_SameName_DiffType();
        }
        [TestMethod]
        public void IntT_InMem_NodeCopy_CrossBinding_DiffName_SameType_SameSlot()
        {
            TestCase.NodeCopy_CrossBinding_DiffName_SameType_SameSlot();
        }
        [TestMethod]
        public void IntT_InMem_NodeCopy_CrossBinding_DataType_LongText()
        {
            TestCase.NodeCopy_CrossBinding_DataType_LongText();
        }
        [TestMethod]
        public void IntT_InMem_NodeCopy_CrossBinding_DataType_Reference()
        {
            TestCase.NodeCopy_CrossBinding_DataType_Reference();
        }
        [TestMethod]
        public void IntT_InMem_NodeCopy_CrossBinding_DataType_Binary()
        {
            TestCase.NodeCopy_CrossBinding_DataType_Binary();
        }

        /* ==================================================================================== MOVE */

        [TestMethod]
        public void IntT_InMem_NodeMove_Node_from_Outer_to_Outer()
        {
            TestCase.NodeMove_Node_from_Outer_to_Outer();
        }
        [TestMethod]
        public void IntT_InMem_NodeMove_Tree_from_Outer_to_Outer()
        {
            TestCase.NodeMove_Tree_from_Outer_to_Outer();
        }
        //[TestMethod]
        public void IntT_InMem_NodeMove_Node_from_Outer_to_List()
        {
            TestCase.NodeMove_Node_from_Outer_to_List();
        }
        //[TestMethod]
        public void IntT_InMem_NodeMove_Tree_from_Outer_to_List()
        {
            TestCase.NodeMove_Tree_from_Outer_to_List();
        }

        //[TestMethod]
        public void IntT_InMem_NodeMove_Node_from_List_to_Outer()
        {
            TestCase.NodeMove_Node_from_List_to_Outer();
        }
        //[TestMethod]
        public void IntT_InMem_NodeMove_Tree_from_List_to_Outer()
        {
            TestCase.NodeMove_Tree_from_List_to_Outer();
        }
        [TestMethod]
        public void IntT_InMem_NodeMove_Node_from_List_to_SameList()
        {
            TestCase.NodeMove_Node_from_List_to_SameList();
        }
        [TestMethod]
        public void IntT_InMem_NodeMove_Tree_from_List_to_SameList()
        {
            TestCase.NodeMove_Tree_from_List_to_SameList();
        }

        //[TestMethod]
        public void IntT_InMem_NodeMove_Node_from_List1_to_List2()
        {
            TestCase.NodeMove_Node_from_List1_to_List2();
        }
        //[TestMethod]
        public void IntT_InMem_NodeMove_Tree_from_List1_to_List2()
        {
            TestCase.NodeMove_Tree_from_List1_to_List2();
        }
        //[TestMethod]
        public void IntT_InMem_NodeMove_Node_from_List1_to_FolderOfList2()
        {
            TestCase.NodeMove_Node_from_List1_to_FolderOfList2();
        }
        //[TestMethod]
        public void IntT_InMem_NodeMove_Tree_from_List1_to_FolderOfList2()
        {
            TestCase.NodeMove_Tree_from_List1_to_FolderOfList2();
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void IntT_InMem_NodeMove_List1_to_List2()
        {
            TestCase.NodeMove_List1_to_List2();
        }
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void IntT_InMem_NodeMove_TreeWithList_to_List2()
        {
            TestCase.NodeMove_TreeWithList_to_List2();
        }
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void IntT_InMem_NodeMove_List1_to_FolderOfList2()
        {
            TestCase.NodeMove_List1_to_FolderOfList2();
        }
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void IntT_InMem_NodeMove_TreeWithList_to_FolderOfList2()
        {
            TestCase.NodeMove_TreeWithList_to_FolderOfList2();
        }
    }
}
