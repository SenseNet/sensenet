using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Fields;
using SenseNet.ContentRepository.InMemory;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Workspaces;
using SenseNet.Tests.Core;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class CopyMoveTests : TestBase
    {
        [TestMethod]
        public void NodeCopy_Node_from_Outer_to_Outer()
        {
            Test(() =>
            {
                // ALIGN

                var root = CreateRootWorkspace();
                var target = new Folder(root){Name = "Target"};
                target.Save();

                var expectedFileContent = "FileContent";
                var testFile = CreateFile(root, "File1", expectedFileContent);

                // ACTION
                Node.Copy(testFile.Path, target.Path);

                // ASSERT
                var copied = Node.Load<File>(RepositoryPath.Combine(target.Path, testFile.Name));
                Assert.AreEqual(expectedFileContent, 
                    RepositoryTools.GetStreamString(copied.Binary.GetStream()));
            });
        }
        [TestMethod]
        public void NodeCopy_Tree_from_Outer_to_Outer()
        {
            Test(() =>
            {
                // ALIGN

                var root = CreateRootWorkspace();
                var target = new Folder(root) { Name = "Target" };
                target.Save();

                var source = new Folder(root) { Name = "Source" };
                source.Save();
                var expectedFileContent = "FileContent";
                var _ = CreateFile(source, "File1", expectedFileContent);

                // ACTION
                Node.Copy(source.Path, target.Path);

                // ASSERT
                var copied = Node.Load<File>(RepositoryPath.Combine(target.Path, "Source/File1"));
                Assert.AreEqual(expectedFileContent,
                    RepositoryTools.GetStreamString(copied.Binary.GetStream()));
            });
        }

        [TestMethod]
        public void NodeCopy_Node_from_Outer_to_List()
        {
            Test(() =>
            {
                // ALIGN
                var root = CreateRootWorkspace();
                var targetList = CreateContentList(root, "DocLib1", 
                    "<ContentListField name='#ListField1' type='Integer'/>");

                var expectedFileContent = "FileContent";
                var testFile = CreateFile(root, "File1", expectedFileContent);

                // ACTION
                Node.Copy(testFile.Path, targetList.Path);

                // ASSERT
                var copied = Node.Load<File>(RepositoryPath.Combine(targetList.Path, testFile.Name));
                Assert.AreEqual(expectedFileContent,
                    RepositoryTools.GetStreamString(copied.Binary.GetStream()));

                Assert.IsFalse(testFile.ContentListId > 0);
                Assert.IsFalse(testFile.ContentListTypeId > 0);
                Assert.IsFalse(testFile.HasProperty("#Int_0"));
                Assert.IsTrue(copied.ContentListId > 0);
                Assert.IsTrue(copied.ContentListTypeId > 0);
                Assert.IsTrue(copied.HasProperty("#Int_0"));

                var loadedTarget = Node.LoadNode(targetList.Path);
                Assert.AreEqual(copied.ContentListId, loadedTarget.Id); // target is the list
                Assert.AreEqual(copied.ContentListTypeId, loadedTarget.ContentListTypeId);
            });
        }
        [TestMethod]
        public void NodeCopy_Tree_from_Outer_to_List()
        {
            Test(() =>
            {
                // ALIGN
                var root = CreateRootWorkspace();
                var targetList = CreateContentList(root, "DocLib1",
                    "<ContentListField name='#ListField1' type='Integer'/>");

                var source = new Folder(root) { Name = "Source" };
                source.Save();
                var expectedFileContent = "FileContent";
                var testFile = CreateFile(source, "File1", expectedFileContent);

                // ACTION
                Node.Copy(source.Path, targetList.Path);

                // ASSERT
                var copied = Node.Load<File>(RepositoryPath.Combine(targetList.Path, "Source/File1"));
                Assert.AreEqual(expectedFileContent,
                    RepositoryTools.GetStreamString(copied.Binary.GetStream()));

                Assert.IsFalse(testFile.ContentListId > 0);
                Assert.IsFalse(testFile.ContentListTypeId > 0);
                Assert.IsFalse(testFile.HasProperty("#Int_0"));
                Assert.IsTrue(copied.ContentListId > 0);
                Assert.IsTrue(copied.ContentListTypeId > 0);
                Assert.IsTrue(copied.HasProperty("#Int_0"));

                var loadedTarget = Node.LoadNode(targetList.Path);
                Assert.AreEqual(copied.ContentListId, loadedTarget.Id); // target is the list
                Assert.AreEqual(copied.ContentListTypeId, loadedTarget.ContentListTypeId);
            });
        }

        [TestMethod]
        public void NodeCopy_Node_from_List_to_Outer()
        {
            Test(() =>
            {
                // ALIGN
                var root = CreateRootWorkspace();

                var sourceList = CreateContentList(root, "DocLib1",
                    "<ContentListField name='#ListField1' type='Integer'/>");

                var expectedFileContent = "FileContent";
                var testFile = CreateFile(sourceList, "File1", expectedFileContent);

                // ACTION
                Node.Copy(testFile.Path, root.Path);

                // ASSERT
                var copied = Node.Load<File>(RepositoryPath.Combine(root.Path, testFile.Name));
                Assert.AreEqual(expectedFileContent,
                    RepositoryTools.GetStreamString(copied.Binary.GetStream()));

                Assert.IsTrue(testFile.ContentListId > 0);
                Assert.IsTrue(testFile.ContentListTypeId > 0);
                Assert.IsTrue(testFile.HasProperty("#Int_0"));
                Assert.IsFalse(copied.ContentListId > 0);
                Assert.IsFalse(copied.ContentListTypeId > 0);
                Assert.IsFalse(copied.HasProperty("#Int_0"));
            });
        }
        [TestMethod]
        public void NodeCopy_Tree_from_List_to_Outer()
        {
            Test(() =>
            {
                // ALIGN
                var root = CreateRootWorkspace();

                var sourceList = CreateContentList(root, "DocLib1",
                    "<ContentListField name='#ListField1' type='Integer'/>");

                var source = new Folder(sourceList) { Name = "Source" };
                source.Save();
                var expectedFileContent = "FileContent";
                var testFile = CreateFile(source, "File1", expectedFileContent);

                // ACTION
                Node.Copy(source.Path, root.Path);

                // ASSERT
                var copied = Node.Load<File>(RepositoryPath.Combine(root.Path, "Source/File1"));
                Assert.AreEqual(expectedFileContent,
                    RepositoryTools.GetStreamString(copied.Binary.GetStream()));

                Assert.IsTrue(testFile.ContentListId > 0);
                Assert.IsTrue(testFile.ContentListTypeId > 0);
                Assert.IsTrue(testFile.HasProperty("#Int_0"));
                Assert.IsFalse(copied.ContentListId > 0);
                Assert.IsFalse(copied.ContentListTypeId > 0);
                Assert.IsFalse(copied.HasProperty("#Int_0"));
            });
        }

        [TestMethod]
        public void NodeCopy_Node_from_List_to_SameList()
        {
            Test(() =>
            {
                // ALIGN
                var root = CreateRootWorkspace();

                var sourceList = CreateContentList(root, "DocLib1",
                    "<ContentListField name='#ListField1' type='Integer'/>");

                var expectedFileContent = "FileContent";
                var testFile = CreateFile(sourceList, "File1", expectedFileContent);
                testFile.SetProperty("#Int_0", 42);
                testFile.Save();

                var target = new Folder(sourceList) { Name = "Target" };
                target.Save();

                // ACTION
                Node.Copy(testFile.Path, target.Path);

                // ASSERT
                var copied = Node.Load<File>(RepositoryPath.Combine(target.Path, testFile.Name));
                Assert.AreEqual(expectedFileContent,
                    RepositoryTools.GetStreamString(copied.Binary.GetStream()));
                Assert.AreEqual(42, copied.GetProperty<int>("#Int_0"));

                Assert.IsTrue(testFile.ContentListId > 0);
                Assert.IsTrue(testFile.ContentListTypeId > 0);
                Assert.IsTrue(testFile.HasProperty("#Int_0"));
                Assert.AreEqual(copied.ContentListId, testFile.ContentListId);
                Assert.AreEqual(copied.ContentListTypeId, testFile.ContentListTypeId);
                Assert.IsTrue(copied.HasProperty("#Int_0"));
            });
        }
        [TestMethod]
        public void NodeCopy_Tree_from_List_to_SameList()
        {
            Test(() =>
            {
                // ALIGN
                var root = CreateRootWorkspace();

                var sourceList = CreateContentList(root, "DocLib1",
                    "<ContentListField name='#ListField1' type='Integer'/>");

                var source = new Folder(sourceList) { Name = "Source" };
                source.Save();
                var expectedFileContent = "FileContent";
                var testFile = CreateFile(source, "File1", expectedFileContent);
                testFile.SetProperty("#Int_0", 42);
                testFile.Save();

                var target = new Folder(sourceList) { Name = "Target" };
                target.Save();

                // ACTION
                Node.Copy(source.Path, target.Path);

                // ASSERT
                var copied = Node.Load<File>(RepositoryPath.Combine(target.Path, "Source/File1"));
                Assert.AreEqual(expectedFileContent,
                    RepositoryTools.GetStreamString(copied.Binary.GetStream()));
                Assert.AreEqual(42, copied.GetProperty<int>("#Int_0"));

                Assert.IsTrue(testFile.ContentListId > 0);
                Assert.IsTrue(testFile.ContentListTypeId > 0);
                Assert.IsTrue(testFile.HasProperty("#Int_0"));
                Assert.AreEqual(copied.ContentListId, testFile.ContentListId);
                Assert.AreEqual(copied.ContentListTypeId, testFile.ContentListTypeId);
                Assert.IsTrue(copied.HasProperty("#Int_0"));
            });
        }

        [TestMethod]
        public void NodeCopy_Node_from_List1_to_List2()
        {
            Test(() =>
            {
                // ALIGN
                var root = CreateRootWorkspace();

                var sourceList = CreateContentList(root, "DocLib1",
                    "<ContentListField name='#ListField1' type='ShortText'/>" +
                    "<ContentListField name='#ListField2' type='Integer'/>");

                var targetList = CreateContentList(root, "DocLib2",
                    "<ContentListField name='#ListField1' type='Integer'/>" +
                    "<ContentListField name='#ListField2' type='Integer'/>");

                var expectedFileContent = "FileContent";
                var testFile = CreateFile(sourceList, "File1", expectedFileContent);

                // ACTION
                Node.Copy(testFile.Path, targetList.Path);

                // ASSERT
                var copied = Node.Load<File>(RepositoryPath.Combine(targetList.Path, testFile.Name));
                Assert.AreEqual(expectedFileContent,
                    RepositoryTools.GetStreamString(copied.Binary.GetStream()));

                Assert.IsTrue(testFile.ContentListId > 0);
                Assert.IsTrue(testFile.ContentListTypeId > 0);
                Assert.IsTrue(copied.ContentListId > 0);
                Assert.IsTrue(copied.ContentListTypeId > 0);
                Assert.AreNotEqual(copied.ContentListId, testFile.ContentListId);
                Assert.AreNotEqual(copied.ContentListTypeId, testFile.ContentListTypeId);

                var loadedTarget = Node.LoadNode(targetList.Path);
                Assert.AreEqual(copied.ContentListId, loadedTarget.Id); // target is the list
                Assert.AreEqual(copied.ContentListTypeId, loadedTarget.ContentListTypeId);
            });
        }
        [TestMethod]
        public void NodeCopy_Tree_from_List1_to_List2()
        {
            Test(() =>
            {
                // ALIGN
                var root = CreateRootWorkspace();

                var sourceList = CreateContentList(root, "DocLib1",
                    "<ContentListField name='#ListField1' type='ShortText'/>" +
                    "<ContentListField name='#ListField2' type='Integer'/>");

                var targetList = CreateContentList(root, "DocLib2",
                    "<ContentListField name='#ListField1' type='Integer'/>" +
                    "<ContentListField name='#ListField2' type='Integer'/>");

                var source = new Folder(sourceList) { Name = "Source" };
                source.Save();
                var expectedFileContent = "FileContent";
                var testFile = CreateFile(source, "File1", expectedFileContent);

                // ACTION
                Node.Copy(source.Path, targetList.Path);

                // ASSERT
                var copied = Node.Load<File>(RepositoryPath.Combine(targetList.Path, "Source/File1"));
                Assert.AreEqual(expectedFileContent,
                    RepositoryTools.GetStreamString(copied.Binary.GetStream()));

                Assert.IsTrue(testFile.ContentListId > 0);
                Assert.IsTrue(testFile.ContentListTypeId > 0);
                Assert.IsTrue(copied.ContentListId > 0);
                Assert.IsTrue(copied.ContentListTypeId > 0);
                Assert.AreNotEqual(copied.ContentListId, testFile.ContentListId);
                Assert.AreNotEqual(copied.ContentListTypeId, testFile.ContentListTypeId);

                var loadedTarget = Node.LoadNode(targetList.Path);
                Assert.AreEqual(copied.ContentListId, loadedTarget.Id); // target is the list
                Assert.AreEqual(copied.ContentListTypeId, loadedTarget.ContentListTypeId);
            });
        }
        [TestMethod]
        public void NodeCopy_Node_from_List1_to_FolderOfList2()
        {
            Test(() =>
            {
                // ALIGN
                var root = CreateRootWorkspace();

                var sourceList = CreateContentList(root, "DocLib1",
                    "<ContentListField name='#ListField1' type='ShortText'/>" +
                    "<ContentListField name='#ListField2' type='Integer'/>");

                var targetList = CreateContentList(root, "DocLib2",
                    "<ContentListField name='#ListField1' type='Integer'/>" +
                    "<ContentListField name='#ListField2' type='Integer'/>");

                var targetFolder = new Folder(targetList){Name = "Target"};
                targetFolder.Save();

                var expectedFileContent = "FileContent";
                var testFile = CreateFile(sourceList, "File1", expectedFileContent);

                // ACTION
                Node.Copy(testFile.Path, targetFolder.Path);

                // ASSERT
                var copied = Node.Load<File>(RepositoryPath.Combine(targetFolder.Path, testFile.Name));
                Assert.AreEqual(expectedFileContent,
                    RepositoryTools.GetStreamString(copied.Binary.GetStream()));

                Assert.IsTrue(testFile.ContentListId > 0);
                Assert.IsTrue(testFile.ContentListTypeId > 0);
                Assert.IsTrue(copied.ContentListId > 0);
                Assert.IsTrue(copied.ContentListTypeId > 0);
                Assert.AreNotEqual(copied.ContentListId, testFile.ContentListId);
                Assert.AreNotEqual(copied.ContentListTypeId, testFile.ContentListTypeId);

                var loadedTarget = Node.LoadNode(targetList.Path);
                Assert.AreEqual(copied.ContentListId, loadedTarget.Id); // target is the list
                Assert.AreEqual(copied.ContentListTypeId, loadedTarget.ContentListTypeId);
            });
        }
        [TestMethod]
        public void NodeCopy_Tree_from_List1_to_FolderOfList2()
        {
            Test(() =>
            {
                // ALIGN
                var root = CreateRootWorkspace();

                var sourceList = CreateContentList(root, "DocLib1",
                    "<ContentListField name='#ListField1' type='ShortText'/>" +
                    "<ContentListField name='#ListField2' type='Integer'/>");

                var targetList = CreateContentList(root, "DocLib2",
                    "<ContentListField name='#ListField1' type='Integer'/>" +
                    "<ContentListField name='#ListField2' type='Integer'/>");

                var targetFolder = new Folder(targetList) { Name = "Target" };
                targetFolder.Save();

                var source = new Folder(sourceList) { Name = "Source" };
                source.Save();
                var expectedFileContent = "FileContent";
                var testFile = CreateFile(source, "File1", expectedFileContent);

                // ACTION
                Node.Copy(source.Path, targetFolder.Path);

                // ASSERT
                var copied = Node.Load<File>(RepositoryPath.Combine(targetFolder.Path, "Source/File1"));
                Assert.AreEqual(expectedFileContent,
                    RepositoryTools.GetStreamString(copied.Binary.GetStream()));

                Assert.IsTrue(testFile.ContentListId > 0);
                Assert.IsTrue(testFile.ContentListTypeId > 0);
                Assert.IsTrue(copied.ContentListId > 0);
                Assert.IsTrue(copied.ContentListTypeId > 0);
                Assert.AreNotEqual(copied.ContentListId, testFile.ContentListId);
                Assert.AreNotEqual(copied.ContentListTypeId, testFile.ContentListTypeId);

                var loadedTarget = Node.LoadNode(targetList.Path);
                Assert.AreEqual(copied.ContentListId, loadedTarget.Id); // target is the list
                Assert.AreEqual(copied.ContentListTypeId, loadedTarget.ContentListTypeId);
            });
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void NodeCopy_List1_to_List2()
        {
            Test(() =>
            {
                // ALIGN
                var root = CreateRootWorkspace();

                var sourceList = CreateContentList(root, "DocLib1",
                    "<ContentListField name='#ListField1' type='ShortText'/>" +
                    "<ContentListField name='#ListField2' type='Integer'/>");

                var targetList = CreateContentList(root, "DocLib2",
                    "<ContentListField name='#ListField1' type='Integer'/>" +
                    "<ContentListField name='#ListField2' type='Integer'/>");

                // ACTION
                Node.Copy(sourceList.Path, targetList.Path);
            });
        }
        [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
        public void NodeCopy_TreeWithList_to_List2()
        {
            Test(() =>
            {
                // ALIGN
                var root = CreateRootWorkspace();

                var source = new Folder(root) { Name = "Source" };
                source.Save();
                var sourceList = CreateContentList(source, "DocLib1",
                    "<ContentListField name='#ListField1' type='ShortText'/>" +
                    "<ContentListField name='#ListField2' type='Integer'/>");

                var targetList = CreateContentList(root, "DocLib2",
                    "<ContentListField name='#ListField1' type='Integer'/>" +
                    "<ContentListField name='#ListField2' type='Integer'/>");

                // ACTION
                Node.Copy(source.Path, targetList.Path);
            });
        }
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void NodeCopy_List1_to_FolderOfList2()
        {
            Test(() =>
            {
                // ALIGN
                var root = CreateRootWorkspace();

                var sourceList = CreateContentList(root, "DocLib1",
                    "<ContentListField name='#ListField1' type='ShortText'/>" +
                    "<ContentListField name='#ListField2' type='Integer'/>");

                var targetList = CreateContentList(root, "DocLib2",
                    "<ContentListField name='#ListField1' type='Integer'/>" +
                    "<ContentListField name='#ListField2' type='Integer'/>");

                var targetFolder = new Folder(targetList) { Name = "Target" };
                targetFolder.Save();
                
                // ACTION
                Node.Copy(sourceList.Path, targetFolder.Path);
            });
        }
        [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
        public void NodeCopy_TreeWithList_to_FolderOfList2()
        {
            Test(() =>
            {
                // ALIGN
                var root = CreateRootWorkspace();

                var source = new Folder(root) { Name = "Source" };
                source.Save();
                var sourceList = CreateContentList(source, "DocLib1",
                    "<ContentListField name='#ListField1' type='ShortText'/>" +
                    "<ContentListField name='#ListField2' type='Integer'/>");

                var targetList = CreateContentList(root, "DocLib2",
                    "<ContentListField name='#ListField1' type='Integer'/>" +
                    "<ContentListField name='#ListField2' type='Integer'/>");

                var targetFolder = new Folder(targetList) { Name = "Target" };
                targetFolder.Save();

                // ACTION
                Node.Copy(source.Path, targetFolder.Path);
            });
        }

        [TestMethod]
        public void NodeCopy_CrossBinding_SameName_SameType_SameSlot()
        {
            CrossBindingTest(
                @"<ContentListField name='#A' type='Integer'/>",
                @"<ContentListField name='#A' type='Integer'/>",
                source => { source.SetProperty("#Int_0", 42);},
                (source, copied) =>
                {
                    Assert.AreEqual(source.GetProperty("#Int_0"), copied.GetProperty("#Int_0"));
                });
        }
        [TestMethod]
        public void NodeCopy_CrossBinding_SameName_SameType_DiffSlot()
        {
            CrossBindingTest(
                @"<ContentListField name='#A' type='Integer'/>",
                @"<ContentListField name='#B' type='Integer'/><ContentListField name='#A' type='Integer'/>",
                source => { source.SetProperty("#Int_0", 42); },
                (source, copied) =>
                {
                    Assert.AreEqual(source.GetProperty("#Int_0"), copied.GetProperty("#Int_1"));
                });
        }
        [TestMethod]
        public void NodeCopy_CrossBinding_SameName_DiffType()
        {
            CrossBindingTest(
                @"<ContentListField name='#A' type='Integer'/>",
                @"<ContentListField name='#A' type='ShortText'/>",
                source => { source.SetProperty("#Int_0", 42); },
                (source, copied) =>
                {
                    Assert.IsFalse(copied.HasProperty("#Int_0"));
                    Assert.IsNull(copied.GetProperty("#String_0"));
                });
        }
        [TestMethod]
        public void NodeCopy_CrossBinding_DiffName_SameType_SameSlot()
        {
            CrossBindingTest(
                @"<ContentListField name='#A' type='Integer'/>",
                @"<ContentListField name='#B' type='Integer'/>",
                source => { source.SetProperty("#Int_0", 42); },
                (source, copied) =>
                {
                    Assert.AreNotEqual(source.GetProperty("#Int_0"), copied.GetProperty("#Int_0"));
                });
        }
        [TestMethod]
        public void NodeCopy_CrossBinding_DataType_LongText()
        {
            CrossBindingTest(
                "<ContentListField name='#A' type='LongText'/>" +
                "<ContentListField name='#B' type='LongText'/>",
                "<ContentListField name='#B' type='LongText'/>" +
                "<ContentListField name='#C' type='LongText'/>",
                source =>
                {
                    source.SetProperty("#Text_0", "Text A");
                    source.SetProperty("#Text_1", "Text B");
                },
                (source, copied) =>
                {
                    Assert.AreEqual(source.GetProperty("#Text_1"), copied.GetProperty("#Text_0"));
                    Assert.IsNull(copied.GetProperty("#Text_1"));
                });
        }
        [TestMethod]
        public void NodeCopy_CrossBinding_DataType_Reference()
        {
            CrossBindingTest(
                "<ContentListField name='#A' type='Reference'/>" +
                "<ContentListField name='#B' type='Reference'/>",
                "<ContentListField name='#B' type='Reference'/>" +
                "<ContentListField name='#C' type='Reference'/>",
                source =>
                {
                    var refs0 = new Node[] { Node.LoadNode(1), Node.LoadNode(2) };
                    var refs1 = new Node[] { Node.LoadNode(3), Node.LoadNode(4) };
                    source.SetProperty("#Reference_0", refs0);
                    source.SetProperty("#Reference_1", refs1);
                },
                (source, copied) =>
                {
                    var actual1 = string.Join(",", 
                        ((IEnumerable<Node>)copied.GetProperty("#Reference_0")).Select(n => n.Id));
                    Assert.AreEqual("3,4", actual1);

                    var actual2 = ((IEnumerable<Node>) copied.GetProperty("#Reference_1")).Count();
                    Assert.AreEqual(0, actual2);
                });
        }
        [TestMethod]
        public void NodeCopy_CrossBinding_DataType_Binary()
        {
            CrossBindingTest(
                "<ContentListField name='#A' type='Binary'/>" +
                "<ContentListField name='#B' type='Binary'/>",
                "<ContentListField name='#B' type='Binary'/>" +
                "<ContentListField name='#C' type='Binary'/>",
                source =>
                {
                    var bin1 = new BinaryData();
                    bin1.SetStream(RepositoryTools.GetStreamFromString("FileContent-1"));
                    source.SetProperty("#Binary_0", bin1);
                    var bin2 = new BinaryData();
                    bin2.SetStream(RepositoryTools.GetStreamFromString("FileContent-2"));
                    source.SetProperty("#Binary_1", bin2);
                },
                (source, copied) =>
                {
                    var bin1 = (BinaryData)copied.GetProperty("#Binary_0");
                    var text1 = RepositoryTools.GetStreamString(bin1.GetStream());
                    Assert.AreEqual("FileContent-2", text1);
                    var bin2 = (BinaryData)copied.GetProperty("#Binary_1");
                    Assert.IsNull(bin2.GetStream());
                });
        }

        private void CrossBindingTest(string sourceListFields, string targetListFields,
            Action<GenericContent> setSourceProperties, Action<GenericContent, GenericContent> assertCopiedValues)
        {
            Test(() =>
            {
                // ALIGN
                var root = CreateRootWorkspace();

                var doclib1 = Content.CreateNew("DocumentLibrary", root, "DocLib1");
                var sourceList = (ContentList)doclib1.ContentHandler;
                sourceList.ContentListDefinition = $@"<ContentListDefinition xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentListDefinition'><Fields>
{sourceListFields}
</Fields></ContentListDefinition>";
                doclib1.Save();

                var doclib2 = Content.CreateNew("DocumentLibrary", root, "DocLib2");
                var targetList = (ContentList)doclib2.ContentHandler;
                targetList.ContentListDefinition = $@"<ContentListDefinition xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentListDefinition'><Fields>
{targetListFields}
</Fields></ContentListDefinition>";
                doclib2.Save();

                var expectedFileContent = "FileContent";
                var testFile = new File(sourceList) { Name = "File1" };
                testFile.Binary.SetStream(RepositoryTools.GetStreamFromString(expectedFileContent));
                setSourceProperties(testFile);
                testFile.Save();

                // ACTION
                Node.Copy(testFile.Path, targetList.Path);

                // ASSERT
                var copied = Node.Load<File>(RepositoryPath.Combine(targetList.Path, testFile.Name));
                Assert.AreEqual(expectedFileContent,
                    RepositoryTools.GetStreamString(copied.Binary.GetStream()));

                Assert.IsTrue(testFile.ContentListId > 0);
                Assert.IsTrue(testFile.ContentListTypeId > 0);
                Assert.IsTrue(copied.ContentListId > 0);
                Assert.IsTrue(copied.ContentListTypeId > 0);
                Assert.AreNotEqual(copied.ContentListId, testFile.ContentListId);
                Assert.AreNotEqual(copied.ContentListTypeId, testFile.ContentListTypeId);
                var loadedTarget = Node.LoadNode(targetList.Path);
                Assert.AreEqual(copied.ContentListId, loadedTarget.Id); // target is the list
                Assert.AreEqual(copied.ContentListTypeId, loadedTarget.ContentListTypeId);

                assertCopiedValues(testFile, copied);
            });
        }

        /* ==================================================================================== */

        [TestMethod]
        public void NodeMove_Node_from_Outer_to_Outer()
        {
            Test(() =>
            {
                // ALIGN

                var root = CreateRootWorkspace();
                var target = new Folder(root) { Name = "Target" };
                target.Save();

                var expectedFileContent = "FileContent";
                var testFile = CreateFile(root, "File1", expectedFileContent);

                // ACTION
                Node.Move(testFile.Path, target.Path);

                // ASSERT
                var moved = Node.LoadNode(testFile.Id);
                Assert.AreEqual(RepositoryPath.Combine(target.Path, "File1"), moved.Path);
            });
        }
        [TestMethod]
        public void NodeMove_Tree_from_Outer_to_Outer()
        {
            Test(() =>
            {
                // ALIGN

                var root = CreateRootWorkspace();
                var target = new Folder(root) { Name = "Target" };
                target.Save();

                var source = new Folder(root) { Name = "Source" };
                source.Save();
                var expectedFileContent = "FileContent";
                var testFile = CreateFile(source, "File1", expectedFileContent);

                // ACTION
                Node.Move(source.Path, target.Path);

                // ASSERT
                var moved = Node.LoadNode(testFile.Id);
                Assert.AreEqual(RepositoryPath.Combine(target.Path, "Source/File1"), moved.Path);
            });
        }

        [TestMethod]
        public void NodeMove_Node_from_Outer_to_List()
        {
            Test(() =>
            {
                // ALIGN
                var root = CreateRootWorkspace();
                var targetList = CreateContentList(root, "DocLib1",
                    "<ContentListField name='#ListField1' type='Integer'/>");

                var expectedFileContent = "FileContent";
                var testFile = CreateFile(root, "File1", expectedFileContent);

                // ACTION
                Node.Move(testFile.Path, targetList.Path);

                // ASSERT
                Assert.Fail("Assertion is not implemented.");
            });
        }
        [TestMethod]
        public void NodeMove_Tree_from_Outer_to_List()
        {
            Test(() =>
            {
                // ALIGN
                var root = CreateRootWorkspace();
                var targetList = CreateContentList(root, "DocLib1",
                    "<ContentListField name='#ListField1' type='Integer'/>");

                var source = new Folder(root) { Name = "Source" };
                source.Save();
                var expectedFileContent = "FileContent";
                var testFile = CreateFile(source, "File1", expectedFileContent);

                // ACTION
                Node.Move(source.Path, targetList.Path);

                // ASSERT
                Assert.Fail("Assertion is not implemented.");
            });
        }

        [TestMethod]
        public void NodeMove_Node_from_List_to_Outer()
        {
            Test(() =>
            {
                // ALIGN
                var root = CreateRootWorkspace();

                var sourceList = CreateContentList(root, "DocLib1",
                    "<ContentListField name='#ListField1' type='Integer'/>");

                var expectedFileContent = "FileContent";
                var testFile = CreateFile(sourceList, "File1", expectedFileContent);

                // ACTION
                Node.Move(testFile.Path, root.Path);

                // ASSERT
                Assert.Fail("Assertion is not implemented.");
            });
        }
        [TestMethod]
        public void NodeMove_Tree_from_List_to_Outer()
        {
            Test(() =>
            {
                // ALIGN
                var root = CreateRootWorkspace();

                var sourceList = CreateContentList(root, "DocLib1",
                    "<ContentListField name='#ListField1' type='Integer'/>");

                var source = new Folder(sourceList) { Name = "Source" };
                source.Save();
                var expectedFileContent = "FileContent";
                var testFile = CreateFile(source, "File1", expectedFileContent);

                // ACTION
                Node.Move(source.Path, root.Path);

                // ASSERT
                Assert.Fail("Assertion is not implemented.");
            });
        }

        [TestMethod]
        public void NodeMove_Node_from_List_to_SameList()
        {
            Test(() =>
            {
                // ALIGN
                var root = CreateRootWorkspace();

                var sourceList = CreateContentList(root, "DocLib1",
                    "<ContentListField name='#ListField1' type='Integer'/>");

                var expectedFileContent = "FileContent";
                var testFile = CreateFile(sourceList, "File1", expectedFileContent);
                testFile.SetProperty("#Int_0", 42);
                testFile.Save();

                var target = new Folder(sourceList) { Name = "Target" };
                target.Save();

                // ACTION
                Node.Move(testFile.Path, target.Path);

                // ASSERT
                var moved = Node.LoadNode(testFile.Id);
                Assert.AreEqual(RepositoryPath.Combine(target.Path, "File1"), moved.Path);
            });
        }
        [TestMethod]
        public void NodeMove_Tree_from_List_to_SameList()
        {
            Test(() =>
            {
                // ALIGN
                var root = CreateRootWorkspace();

                var sourceList = CreateContentList(root, "DocLib1",
                    "<ContentListField name='#ListField1' type='Integer'/>");

                var source = new Folder(sourceList) { Name = "Source" };
                source.Save();
                var expectedFileContent = "FileContent";
                var testFile = CreateFile(source, "File1", expectedFileContent);
                testFile.SetProperty("#Int_0", 42);
                testFile.Save();

                var target = new Folder(sourceList) { Name = "Target" };
                target.Save();

                // ACTION
                Node.Move(source.Path, target.Path);

                // ASSERT
                var moved = Node.LoadNode(testFile.Id);
                Assert.AreEqual(RepositoryPath.Combine(target.Path, "Source/File1"), moved.Path);
            });
        }

        [TestMethod]
        public void NodeMove_Node_from_List1_to_List2()
        {
            Test(() =>
            {
                // ALIGN
                var root = CreateRootWorkspace();

                var sourceList = CreateContentList(root, "DocLib1",
                    "<ContentListField name='#ListField1' type='ShortText'/>" +
                    "<ContentListField name='#ListField2' type='Integer'/>");

                var targetList = CreateContentList(root, "DocLib2",
                    "<ContentListField name='#ListField1' type='Integer'/>" +
                    "<ContentListField name='#ListField2' type='Integer'/>");

                var expectedFileContent = "FileContent";
                var testFile = CreateFile(sourceList, "File1", expectedFileContent);

                // ACTION
                Node.Move(testFile.Path, targetList.Path);

                // ASSERT
                Assert.Fail("Assertion is not implemented.");
            });
        }
        [TestMethod]
        public void NodeMove_Tree_from_List1_to_List2()
        {
            Test(() =>
            {
                // ALIGN
                var root = CreateRootWorkspace();

                var sourceList = CreateContentList(root, "DocLib1",
                    "<ContentListField name='#ListField1' type='ShortText'/>" +
                    "<ContentListField name='#ListField2' type='Integer'/>");

                var targetList = CreateContentList(root, "DocLib2",
                    "<ContentListField name='#ListField1' type='Integer'/>" +
                    "<ContentListField name='#ListField2' type='Integer'/>");

                var source = new Folder(sourceList) { Name = "Source" };
                source.Save();
                var expectedFileContent = "FileContent";
                var testFile = CreateFile(source, "File1", expectedFileContent);

                // ACTION
                Node.Move(source.Path, targetList.Path);

                // ASSERT
                Assert.Fail("Assertion is not implemented.");
            });
        }
        [TestMethod]
        public void NodeMove_Node_from_List1_to_FolderOfList2()
        {
            Test(() =>
            {
                // ALIGN
                var root = CreateRootWorkspace();

                var sourceList = CreateContentList(root, "DocLib1",
                    "<ContentListField name='#ListField1' type='ShortText'/>" +
                    "<ContentListField name='#ListField2' type='Integer'/>");

                var targetList = CreateContentList(root, "DocLib2",
                    "<ContentListField name='#ListField1' type='Integer'/>" +
                    "<ContentListField name='#ListField2' type='Integer'/>");

                var targetFolder = new Folder(targetList) { Name = "Target" };
                targetFolder.Save();

                var expectedFileContent = "FileContent";
                var testFile = CreateFile(sourceList, "File1", expectedFileContent);

                // ACTION
                Node.Move(testFile.Path, targetFolder.Path);

                // ASSERT
                Assert.Fail("Assertion is not implemented.");
            });
        }
        [TestMethod]
        public void NodeMove_Tree_from_List1_to_FolderOfList2()
        {
            Test(() =>
            {
                // ALIGN
                var root = CreateRootWorkspace();

                var sourceList = CreateContentList(root, "DocLib1",
                    "<ContentListField name='#ListField1' type='ShortText'/>" +
                    "<ContentListField name='#ListField2' type='Integer'/>");

                var targetList = CreateContentList(root, "DocLib2",
                    "<ContentListField name='#ListField1' type='Integer'/>" +
                    "<ContentListField name='#ListField2' type='Integer'/>");

                var targetFolder = new Folder(targetList) { Name = "Target" };
                targetFolder.Save();

                var source = new Folder(sourceList) { Name = "Source" };
                source.Save();
                var expectedFileContent = "FileContent";
                var testFile = CreateFile(source, "File1", expectedFileContent);

                // ACTION
                Node.Move(source.Path, targetFolder.Path);

                // ASSERT
                Assert.Fail("Assertion is not implemented.");
            });
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void NodeMove_List1_to_List2()
        {
            Test(() =>
            {
                // ALIGN
                var root = CreateRootWorkspace();

                var sourceList = CreateContentList(root, "DocLib1",
                    "<ContentListField name='#ListField1' type='ShortText'/>" +
                    "<ContentListField name='#ListField2' type='Integer'/>");

                var targetList = CreateContentList(root, "DocLib2",
                    "<ContentListField name='#ListField1' type='Integer'/>" +
                    "<ContentListField name='#ListField2' type='Integer'/>");

                // ACTION
                Node.Move(sourceList.Path, targetList.Path);
            });
        }
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void NodeMove_TreeWithList_to_List2()
        {
            Test(() =>
            {
                // ALIGN
                var root = CreateRootWorkspace();

                var source = new Folder(root) { Name = "Source" };
                source.Save();
                var sourceList = CreateContentList(source, "DocLib1",
                    "<ContentListField name='#ListField1' type='ShortText'/>" +
                    "<ContentListField name='#ListField2' type='Integer'/>");

                var targetList = CreateContentList(root, "DocLib2",
                    "<ContentListField name='#ListField1' type='Integer'/>" +
                    "<ContentListField name='#ListField2' type='Integer'/>");

                // ACTION
                Node.Move(source.Path, targetList.Path);
            });
        }
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void NodeMove_List1_to_FolderOfList2()
        {
            Test(() =>
            {
                // ALIGN
                var root = CreateRootWorkspace();

                var sourceList = CreateContentList(root, "DocLib1",
                    "<ContentListField name='#ListField1' type='ShortText'/>" +
                    "<ContentListField name='#ListField2' type='Integer'/>");

                var targetList = CreateContentList(root, "DocLib2",
                    "<ContentListField name='#ListField1' type='Integer'/>" +
                    "<ContentListField name='#ListField2' type='Integer'/>");

                var targetFolder = new Folder(targetList) { Name = "Target" };
                targetFolder.Save();

                // ACTION
                Node.Move(sourceList.Path, targetFolder.Path);
            });
        }
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void NodeMove_TreeWithList_to_FolderOfList2()
        {
            Test(() =>
            {
                // ALIGN
                var root = CreateRootWorkspace();

                var source = new Folder(root) { Name = "Source" };
                source.Save();
                var sourceList = CreateContentList(source, "DocLib1",
                    "<ContentListField name='#ListField1' type='ShortText'/>" +
                    "<ContentListField name='#ListField2' type='Integer'/>");

                var targetList = CreateContentList(root, "DocLib2",
                    "<ContentListField name='#ListField1' type='Integer'/>" +
                    "<ContentListField name='#ListField2' type='Integer'/>");

                var targetFolder = new Folder(targetList) { Name = "Target" };
                targetFolder.Save();

                // ACTION
                Node.Move(source.Path, targetFolder.Path);
            });
        }

        /* ==================================================================================== */

        private Workspace CreateRootWorkspace()
        {
            var ws = new Workspace(Repository.Root) {Name = Guid.NewGuid().ToString()};
            ws.AllowChildType("File");
            ws.Save();

            return ws;
        }
        private File CreateFile(Node parent, string name, string fileContent)
        {
            var file = new File(parent) { Name = "File1" };
            file.Binary.SetStream(RepositoryTools.GetStreamFromString(fileContent));
            file.Save();
            return file;
        }
        private ContentList CreateContentList(Node parent, string name, string listFields)
        {
            var doclib = Content.CreateNew("DocumentLibrary", parent, name);
            var targetList = (ContentList)doclib.ContentHandler;
            targetList.ContentListDefinition = $@"<ContentListDefinition xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentListDefinition'><Fields>
{listFields}
</Fields></ContentListDefinition>";
            doclib.Save();
            return targetList;
        }
    }
}
