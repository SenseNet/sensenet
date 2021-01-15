using System;
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
        public void NodeCopy_Outer_to_Outer()
        {
            Test(() =>
            {
                // ALIGN
                var expectedFileContent = "FileContent";

                var root = CreateRootWorkspace();
                var target = new Folder(root){Name = "Target"};
                target.Save();

                var testFile = new File(root) { Name = "File1" };
                testFile.Binary.SetStream(RepositoryTools.GetStreamFromString(expectedFileContent));
                testFile.Save();

                // ACTION
                Node.Copy(testFile.Path, target.Path);

                // ASSERT
                var copied = Node.Load<File>(RepositoryPath.Combine(target.Path, testFile.Name));
                Assert.AreEqual(expectedFileContent,
                    RepositoryTools.GetStreamString(copied.Binary.GetStream()));
            });
        }

        [TestMethod]
        //UNDONE:<?copy: Write tests to check copying deep structures into a list.
        public void NodeCopy_Outer_to_List()
        {
            Test(() =>
            {
                // ALIGN
                var expectedFileContent = "FileContent";

                var root = CreateRootWorkspace();
                var doclib = Content.CreateNew("DocumentLibrary", root, "DocLib1");
                var targetList = (ContentList)doclib.ContentHandler;
                targetList.ContentListDefinition = @"<ContentListDefinition xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentListDefinition'><Fields>
  <ContentListField name='#ListField1' type='Integer'/>
</Fields></ContentListDefinition>";
                doclib.Save();

                var testFile = new File(root) { Name = "File1" };
                testFile.Binary.SetStream(RepositoryTools.GetStreamFromString(expectedFileContent));
                testFile.Save();

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
        public void NodeCopy_List_to_Outer()
        {
            Test(() =>
            {
                // ALIGN
                var expectedFileContent = "FileContent";

                var root = CreateRootWorkspace();

                var doclib = Content.CreateNew("DocumentLibrary", root, "DocLib1");
                var sourceList = (ContentList)doclib.ContentHandler;
                sourceList.ContentListDefinition = @"<ContentListDefinition xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentListDefinition'><Fields>
  <ContentListField name='#ListField1' type='Integer'/>
</Fields></ContentListDefinition>";
                doclib.Save();

                var testFile = new File(sourceList) { Name = "File1" };
                testFile.Binary.SetStream(RepositoryTools.GetStreamFromString(expectedFileContent));
                testFile.Save();

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
        public void NodeCopy_List_to_List()
        {
            Test(() =>
            {
                // ALIGN
                var expectedFileContent = "FileContent";

                var root = CreateRootWorkspace();
                var doclib = Content.CreateNew("DocumentLibrary", root, "DocLib1");
                var sourceList = (ContentList)doclib.ContentHandler;
                sourceList.ContentListDefinition = @"<ContentListDefinition xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentListDefinition'><Fields>
  <ContentListField name='#ListField1' type='Integer'/>
</Fields></ContentListDefinition>";
                doclib.Save();

                var testFile = new File(sourceList) { Name = "File1" };
                testFile.SetProperty("#Int_0", 42);
                testFile.Binary.SetStream(RepositoryTools.GetStreamFromString(expectedFileContent));
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
        //UNDONE:<?copy: Write tests to check copying deep structures into a list.
        //UNDONE:<?copy: Write tests to check cross binding for all dataTypes in the GenericContent.CopyDynamicProperties
        public void NodeCopy_List1_to_List2()
        {
            Test(() =>
            {
                // ALIGN
                var expectedFileContent = "FileContent";

                var root = CreateRootWorkspace();

                var doclib1 = Content.CreateNew("DocumentLibrary", root, "DocLib1");
                var sourceList = (ContentList)doclib1.ContentHandler;
                sourceList.ContentListDefinition = @"<ContentListDefinition xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentListDefinition'><Fields>
  <ContentListField name='#ListField1' type='ShortText'/>
  <ContentListField name='#ListField2' type='Integer'/>
</Fields></ContentListDefinition>";
                doclib1.Save();

                var doclib2 = Content.CreateNew("DocumentLibrary", root, "DocLib2");
                var targetList = (ContentList)doclib2.ContentHandler;
                targetList.ContentListDefinition = @"<ContentListDefinition xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentListDefinition'><Fields>
  <ContentListField name='#ListField1' type='Integer'/>
  <ContentListField name='#ListField2' type='Integer'/>
</Fields></ContentListDefinition>";
                doclib2.Save();

                var testFile = new File(sourceList) { Name = "File1" };
                testFile.Binary.SetStream(RepositoryTools.GetStreamFromString(expectedFileContent));
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
        private void CrossBindingTest(string sourceListFields, string targetListFields,
            Action<GenericContent> setSourceProperties, Action<GenericContent, GenericContent> assertCopiedValues)
        {
            Test(() =>
            {
                // ALIGN
                var expectedFileContent = "FileContent";

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

        /* ====================================================== */
        private Workspace CreateRootWorkspace()
        {
            var ws = new Workspace(Repository.Root) {Name = Guid.NewGuid().ToString()};
            ws.AllowChildType("File");
            ws.Save();

            return ws;
        }
    }
}
