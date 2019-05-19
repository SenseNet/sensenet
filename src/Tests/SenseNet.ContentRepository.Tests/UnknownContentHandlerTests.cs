using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.Packaging.Tests.Implementations;
using SenseNet.Storage;
using SenseNet.Tests;
using SenseNet.Tests.Implementations;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class UnknownContentHandlerTests : TestBase
    {
        protected override RepositoryBuilder CreateRepositoryBuilderForTestInstance()
        {
            //UNDONE: temp reference to the SenseNet.Packaging.Tests 
            // because of the packaging storage provider below.
            var builder = base.CreateRepositoryBuilderForTestInstance();
            builder.UsePackagingDataProviderExtension(new TestPackageStorageProvider());

            return builder;
        }

        [TestMethod]
        public void UnknownHandler_CreateContent()
        {
            Test(() =>
            {
                // allow the File type in the root because we'll need it later
                var parent = Node.Load<GenericContent>("/Root");
                parent.AllowChildType("File", save: true);

                // create a system folder
                var sysFolderName = Guid.NewGuid().ToString();
                var sysFolder = Content.CreateNew("SystemFolder", parent, sysFolderName);
                sysFolder.Save();
                var originalFieldNames = string.Join(",", sysFolder.Fields.Keys);

                // set the handler of the Folder type to an unknown value
                SetContentHandler("Folder", "UnknownCreateContent");

                ResetAndFailToCreateContent();

                parent = Node.Load<GenericContent>("/Root");
                var folderType = ContentTypeManager.Instance.GetContentTypeByName("Folder");

                // check if all related types are marked as unknown
                foreach (var contentType in ContentTypeManager.Instance.ContentTypes.Values)
                {
                    Assert.AreEqual(string.Equals(contentType.Path, folderType.Path) ||
                                    contentType.Path.StartsWith(folderType.Path + RepositoryPath.PathSeparator),
                        contentType.IsInvalid);
                }

                // load a previously created system folder and iterate through its fields
                sysFolder = Content.Load(sysFolder.Id);
                var afterFieldNames = string.Join(",", sysFolder.Fields.Keys);
                
                Assert.AreEqual(originalFieldNames, afterFieldNames);

                foreach (var field in sysFolder.Fields)
                {
                    // check if field values can be loaded
                    var val = field.Value;
                }

                // This should be allowed because the handler is known
                // and it does not inherit from Folder.
                var file = Content.CreateNew("File", parent, Guid.NewGuid().ToString());
                file.Save();
            });
        }

        [TestMethod]
        public void UnknownHandler_GetNameByType()
        {
            Test(() =>
            {
                SetContentHandlerAndResetManagers("UnknownGetNameByType", () =>
                {
                    // This should not throw an exception. The returned type is irrelevant: 
                    // it will be one of the descendants of the Folder content type.
                    var typeName = ContentTypeManager.GetContentTypeNameByType(typeof(Folder));
                });
            });
        }
        [TestMethod]
        [ExpectedException(typeof(RegistrationException))]
        public void UnknownHandler_CreateUnknownHandler()
        {
            Test(() =>
            {
                SetContentHandlerAndResetManagers("UnknownCreateUnknownHandler", () =>
                {
                    var _ = new UnknownContentHandler(Node.Load<GenericContent>("/Root"));
                });
            });
        }
        [TestMethod]
        public void UnknownHandler_GetContentTypeByHandler()
        {
            Test(() =>
            {
                SetContentHandlerAndResetManagers("UnknownGetContentTypeByHandler", () =>
                {
                    var parent = Node.Load<GenericContent>("/Root");
                    var content = Content.CreateNew("Folder", parent, Guid.NewGuid().ToString());

                    Assert.IsTrue(content.ContentHandler is UnknownContentHandler);

                    var contentType = ContentTypeManager.Instance.GetContentTypeByHandler(content.ContentHandler);

                    // we have found the type despite the missing handler
                    Assert.AreEqual("Folder", contentType.Name);
                });
            });
        }

        [TestMethod]
        [ExpectedException(typeof(ContentRegistrationException))]
        public void UnknownHandler_InstallUnknownContentType()
        {
            var unknownHandlerCTD = @"<?xml version='1.0' encoding='utf-8'?>
<ContentType name='UnknownContent' parentType='GenericContent' handler='unknown' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
</ContentType>
";

            Test(() =>
            {
                ContentTypeInstaller.InstallContentType(unknownHandlerCTD);
            });
        }
        [TestMethod]
        [ExpectedException(typeof(RegistrationException))]
        public void UnknownHandler_InstallContentType_UnknownParent()
        {
            var testSystemHandlerCTD = @"<?xml version='1.0' encoding='utf-8'?>
<ContentType name='TestSystemFolder' parentType='SystemFolder' handler='SenseNet.ContentRepository.Tests.TestSystemFolder' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
</ContentType>
";

            Test(() =>
            {
                // set the handler of the Folder type to an unknown value
                SetContentHandler("Folder", "UnknownParent");

                NodeTypeManager.Restart();
                ContentTypeManager.Reset();
                DistributedApplication.Cache.Reset();

                // this should throw an exception: installing a content type with an unknown parent
                ContentTypeInstaller.InstallContentType(testSystemHandlerCTD);
            });
        }

        [TestMethod]
        public void UnknownHandler_CreateContent_UnknownFieldType()
        {
            Test(() =>
            {
                // add a field with an unknown short name
                AddField("Folder", "TestField", "unknown");

                ResetAndFailToCreateContent();
            });
        }
        [TestMethod]
        public void UnknownHandler_CreateContent_UnknownFieldHandler()
        {
            Test(() =>
            {
                // add a field with an unknown handler
                AddField("Folder", "TestField", null, "unknown");

                ResetAndFailToCreateContent();
            });
        }

        [TestMethod]
        public void UnknownHandler_CreateContent_FieldTable()
        {
            // This test is necessary because the OData layer calls the field name getter method.
            Test(() =>
            {
                var parent = Node.Load<GenericContent>("/Root");
                var content = Content.CreateNew("Folder", parent, Guid.NewGuid().ToString());
                var fieldNamesBefore = string.Join(",", content.GetFieldNamesInParentTable().OrderBy(fn => fn));

                // set the handler of the Folder type to an unknown value
                SetContentHandler("Folder", "UnknownFieldTable");

                DistributedApplication.Cache.Reset();
                NodeTypeManager.Restart();
                ContentTypeManager.Reload();

                parent = Node.Load<GenericContent>("/Root");
                content = Content.CreateNew("Folder", parent, Guid.NewGuid().ToString());
                var fieldNamesAfter = string.Join(",", content.GetFieldNamesInParentTable().OrderBy(fn => fn));

                Assert.AreEqual(fieldNamesBefore, fieldNamesAfter);
            });
        }

        private static void ResetAndFailToCreateContent()
        {
            DistributedApplication.Cache.Reset();
            NodeTypeManager.Restart();
            ContentTypeManager.Reload();

            var parent = Node.Load<GenericContent>("/Root");

            // try to create a content with an unknown field
            ExpectException(() =>
            {
                var content = Content.CreateNew("Folder", parent, Guid.NewGuid().ToString());
                content.Save();
            }, typeof(SnNotSupportedException));

            // try to create a content with a known handler that has an unknown parent
            ExpectException(() =>
            {
                var content = Content.CreateNew("SystemFolder", parent, Guid.NewGuid().ToString());
                content.Save();
            }, typeof(SnNotSupportedException));
        }
        private static void ExpectException(Action action, Type exceptionType)
        {
            var thrown = false;
            try
            {
                action();
            }
            catch (Exception ex)
            {
                if (exceptionType.IsInstanceOfType(ex))
                    thrown = true;
            }

            Assert.IsTrue(thrown, $"{exceptionType.Name} was not thrown.");
        }

        private static void SetContentHandlerAndResetManagers(string handlerName, Action action)
        {
            // set the handler of the Folder type to an unknown value
            SetContentHandler("Folder", handlerName);

            DistributedApplication.Cache.Reset();
            NodeTypeManager.Restart();
            ContentTypeManager.Reload();

            action();
        }

        private static void SetContentHandler(string contentTypeName, string handler)
        {
            var testingDataProvider = GetTestingDataProvider();
            if (testingDataProvider == null)
                Assert.Inconclusive($"{nameof(ITestingDataProviderExtension)} implementation is not available.");

            testingDataProvider.SetContentHandler(contentTypeName, handler);
        }
        private static void AddField(string contentTypeName, string fieldName, string fieldType = null, string fieldHandler = null)
        {
            var testingDataProvider = GetTestingDataProvider();
            if (testingDataProvider == null)
                Assert.Inconclusive($"{nameof(ITestingDataProviderExtension)} implementation is not available.");

            testingDataProvider.AddField(contentTypeName, fieldName, fieldType, fieldHandler);
        }

        private static ITestingDataProviderExtension GetTestingDataProvider()
        {
            return DataStore.Enabled
                ? DataStore.GetDataProviderExtension<ITestingDataProviderExtension>()
                : DataProvider.GetExtension<ITestingDataProviderExtension>();
        }
    }

    [ContentHandler]
    public class TestSystemFolder : SystemFolder
    {
        public TestSystemFolder(Node parent) : this(parent, null) {  }
        public TestSystemFolder(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected TestSystemFolder(NodeToken nt) : base(nt) { }
    }
}
