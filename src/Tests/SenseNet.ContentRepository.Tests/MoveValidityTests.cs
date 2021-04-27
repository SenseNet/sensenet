using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Tests.Core;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class MoveValidityTests : TestBase
    {
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Move_SourceIsNotExist()
        {
            MoveTest(testRoot =>
            {
                Node.Move("/Root/osiejfvchxcidoklg6464783930020398473/iygfevfbvjvdkbu9867513125615", testRoot.Path);
            });
        }
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Move_TargetIsNotExist()
        {
            MoveTest(testRoot =>
            {
                Node.Move(testRoot.Path, "/Root/fdgdffgfccxdxdsffcv31945581316942/udjkcmdkeieoeoodoc542364737827");
            });
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Move_MoveTo_Null()
        {
            MoveTest(testRoot =>
            {
                testRoot.MoveTo(null);
            });
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Move_NullSourcePath()
        {
            MoveTest(testRoot =>
            {
                Node.Move(null, testRoot.Path);
            });
        }
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Move_InvalidSourcePath()
        {
            MoveTest(testRoot =>
            {
                Node.Move(string.Empty, testRoot.Path);
            });
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Move_NullTargetPath()
        {
            MoveTest(testRoot =>
            {
                Node.Move(testRoot.Path, null);
            });
        }
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Move_InvalidTargetPath()
        {
            MoveTest(testRoot =>
            {
                Node.Move(testRoot.Path, string.Empty);
            });
        }
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Move_ToItsParent()
        {
            MoveTest(testRoot =>
            {
                MoveNode(testRoot.Path, testRoot.ParentPath, testRoot);
            });
        }
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Move_ToItself()
        {
            MoveTest(testRoot =>
            {
                Node.Move(testRoot.Path, testRoot.Path);
            });
        }
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Move_ToUnderItself()
        {
            MoveTest(testRoot =>
            {
                EnsureNode(testRoot, "Source/N3");
                MoveNode("Source", "Source/N3", testRoot);
            });
        }
        [TestMethod]
        [ExpectedException(typeof(NodeAlreadyExistsException))]
        public void Move_TargetHasSameName()
        {
            MoveTest(testRoot =>
            {
                EnsureNode(testRoot, "Source");
                EnsureNode(testRoot, "Target/Source");
                MoveNode("Source", "Target", testRoot);
            });
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Move_ContentList_NodeWithContentListToContentList()
        {
            MoveTest(testRoot =>
            {
                //6: MoveNodeWithContentListToContentList
                EnsureNode(testRoot, "SourceFolder/SourceContentList/SourceContentListItem");
                EnsureNode(testRoot, "TargetContentList");
                MoveNode("SourceFolder", "TargetContentList", testRoot);
            });
        }
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Move_ContentList_NodeWithContentListToContentListItem()
        {
            MoveTest(testRoot =>
            {
                //7: MoveNodeWithContentListToContentListItem
                EnsureNode(testRoot, "SourceFolder/SourceContentList/SourceContentListItem");
                EnsureNode(testRoot, "TargetContentList/TargetItemFolder");
                MoveNode("SourceFolder", "TargetContentList/TargetItemFolder", testRoot);
            });
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Move_ContentList_ContentListToContentList()
        {
            MoveTest(testRoot =>
            {
                //9: MoveContentListToContentList
                EnsureNode(testRoot, "SourceContentList");
                EnsureNode(testRoot, "TargetContentList");
                MoveNode("SourceContentList", "TargetContentList", testRoot);
            });
        }
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Move_ContentList_ContentListToContentListItem()
        {
            MoveTest(testRoot =>
            {
                //10: MoveContentListToContentListItem
                EnsureNode(testRoot, "SourceContentList");
                EnsureNode(testRoot, "TargetContentList/TargetItemFolder");
                MoveNode("SourceContentList", "TargetContentList/TargetItemFolder", testRoot);
            });
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Move_ContentList_ContentListTreeToContentList()
        {
            MoveTest(testRoot =>
            {
                //12: MoveContentListTreeToContentList
                EnsureNode(testRoot, "SourceContentList/SourceContentListItem");
                EnsureNode(testRoot, "TargetContentList");
                MoveNode("SourceContentList", "TargetContentList", testRoot);
            });
        }
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Move_ContentList_ContentListTreeToContentListItem()
        {
            MoveTest(testRoot =>
            {
                //13: MoveContentListTreeToContentListItem
                EnsureNode(testRoot, "SourceContentList/SourceContentListItem");
                EnsureNode(testRoot, "TargetContentList/TargetItemFolder");
                MoveNode("SourceContentList", "TargetContentList/TargetItemFolder", testRoot);
            });
        }

        /* ==================================================================================================== TOOLS */

        private void MoveTest(Action<SystemFolder> callback)
        {
            try
            {
                Test(() =>
                {
                    if (ContentType.GetByName("Car") == null)
                        InstallCarContentType();

                    var testRoot = new SystemFolder(Repository.Root) { Name = "MoveTest" };
                    testRoot.Save();

                    try
                    {
                        callback(testRoot);
                    }
                    finally
                    {

                    }
                });
            }
            catch (AggregateException e)
            {
                if (e.InnerException == null)
                    throw;
                throw e.InnerException;
            }
        }

        private void MoveNode(string encodedSourcePath, string encodedTargetPath, SystemFolder testRoot, bool clearTarget = false)
        {
            string sourcePath = DecodePath(testRoot, encodedSourcePath);
            string targetPath = DecodePath(testRoot, encodedTargetPath);
            int sourceId = Node.LoadNode(sourcePath).Id;
            int targetId = Node.LoadNode(targetPath).Id;

            //make sure target does not contain the source node
            if (clearTarget)
            {
                var sourceName = RepositoryPath.GetFileNameSafe(sourcePath);
                if (!string.IsNullOrEmpty(sourceName))
                {
                    var targetPathWithName = RepositoryPath.Combine(targetPath, sourceName);
                    if (Node.Exists(targetPathWithName))
                        Node.ForceDelete(targetPathWithName);
                }
            }

            Node.Move(sourcePath, targetPath);

            Node parentNode = Node.LoadNode(targetId);
            Node childNode = Node.LoadNode(sourceId);
            Assert.IsTrue(childNode.ParentId == parentNode.Id, "Source was not moved.");
        }

        private void EnsureNode(SystemFolder testRoot, string relativePath)
        {
            string path = DecodePath(testRoot, relativePath);
            if (Node.Exists(path))
                return;

            string name = RepositoryPath.GetFileName(path);
            string parentPath = RepositoryPath.GetParentPath(path);
            EnsureNode(testRoot, parentPath);

            switch (name)
            {
                case "ContentList":
                case "SourceContentList":
                    CreateContentList(parentPath, name, _listDef1);
                    break;
                case "TargetContentList":
                    CreateContentList(parentPath, name, _listDef2);
                    break;
                case "Folder":
                case "Folder1":
                case "Folder2":
                case "SourceFolder":
                case "SourceItemFolder":
                case "SourceItemFolder1":
                case "SourceItemFolder2":
                case "TargetFolder":
                case "TargetFolder1":
                case "TargetFolder2":
                case "TargetItemFolder":
                    CreateNode(parentPath, name, "Folder");
                    break;

                case "(apps)":
                case "SystemFolder":
                case "SystemFolder1":
                case "SystemFolder2":
                case "SystemFolder3":
                    CreateNode(parentPath, name, "SystemFolder");
                    break;

                case "SourceContentListItem":
                    CreateContentListItem(parentPath, name, "Car");
                    break;
                case "SourceNode":
                    CreateNode(parentPath, name, "Car");
                    break;
                default:
                    CreateNode(parentPath, name, "Car");
                    break;
            }
        }
        private void CreateContentList(string parentPath, string name, string listDef)
        {
            Node parent = Node.LoadNode(parentPath);
            ContentList contentlist = new ContentList(parent);
            contentlist.Name = name;
            contentlist.ContentListDefinition = listDef;
            contentlist.AllowChildTypes(new[] { "Folder", "Car" });
            contentlist.Save();
        }
        private void CreateContentListItem(string parentPath, string name, string typeName)
        {
            Content parent = Content.Load(parentPath);
            Content content = Content.CreateNew(typeName, parent.ContentHandler, name);
            if (typeName != "SystemFolder" && typeName != "Folder" && typeName != "Page")
                ((GenericContent)content.ContentHandler).AllowChildTypes(new[] { "Folder", "ContentList", "Car" });
            content["#TestField"] = "TestValue";
            content.Save();
        }
        private void CreateNode(string parentPath, string name, string typeName)
        {
            Content parent = Content.Load(parentPath);
            Content content = Content.CreateNew(typeName, parent.ContentHandler, name);
            if (typeName != "SystemFolder" && typeName != "Folder" && typeName != "Page")
                ((GenericContent)content.ContentHandler).AllowChildTypes(new[] { "Folder", "ContentList", "Car" });
            if (content.Fields.ContainsKey("#TestField"))
                content["#TestField"] = "TestValue";
            content.Save();
        }
        private string DecodePath(SystemFolder testRoot, string relativePath)
        {
            if (relativePath == "/Root" || relativePath.StartsWith("/Root/"))
                return relativePath;
            if (relativePath.StartsWith("[TestRoot]"))
                Assert.Fail("[TestRoot] is not allowed. Use relative path instead.");
            return RepositoryPath.Combine(testRoot.Path, relativePath);
        }

        private static readonly string _listDef1 = @"<?xml version='1.0' encoding='utf-8'?>
<ContentListDefinition xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentListDefinition'>
	<DisplayName>Cars title</DisplayName>
	<Description>Cars description</Description>
	<Icon>automobile.gif</Icon>
	<Fields>
		<ContentListField name='#TestField' type='ShortText' />
		<ContentListField name='#ContentListField1' type='ShortText'>
			<DisplayName>ContentListField1</DisplayName>
			<Description>ContentListField1 Description</Description>
			<Icon>icon.gif</Icon>
			<Configuration>
				<MaxLength>100</MaxLength>
			</Configuration>
		</ContentListField>
		<ContentListField name='#ContentListField2' type='WhoAndWhen'>
			<DisplayName>ContentListField2</DisplayName>
			<Description>ContentListField2 Description</Description>
			<Icon>icon.gif</Icon>
			<Configuration>
			</Configuration>
		</ContentListField>
		<ContentListField name='#ContentListField3' type='ShortText'>
			<DisplayName>ContentListField3</DisplayName>
			<Description>ContentListField3 Description</Description>
			<Icon>icon.gif</Icon>
			<Configuration>
				<MaxLength>200</MaxLength>
			</Configuration>
		</ContentListField>
	</Fields>
</ContentListDefinition>
";
        private static readonly string _listDef2 = @"<?xml version='1.0' encoding='utf-8'?>
<ContentListDefinition xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentListDefinition'>
	<DisplayName>Trucks title</DisplayName>
	<Description>Trucks description</Description>
	<Icon>automobile.gif</Icon>
	<Fields>
		<ContentListField name='#TestField' type='ShortText' />
		<ContentListField name='#ContentListField1' type='Integer' />
	</Fields>
</ContentListDefinition>
";

    }
}
