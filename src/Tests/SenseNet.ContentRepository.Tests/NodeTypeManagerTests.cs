using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.Diagnostics;
using SenseNet.Packaging.Tests.Implementations;
using SenseNet.Tests;
using SenseNet.Tests.Implementations;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class NodeTypeManagerTests : TestBase
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
        public void NodeTypeManager_UnknownHandler()
        {
            EnsurePrototypes();

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
                var currentDb = ((InMemoryDataProvider) DataProvider.Current).DB;
                InMemoryDataProvider.SetContentHandler(currentDb, "Folder", "unknownhandler");

                NodeTypeManager.Restart();
                ContentTypeManager.Reset();
                DistributedApplication.Cache.Reset();

                // reload
                parent = Node.Load<GenericContent>("/Root");

                // try to create a content with an unknown handler
                ExpectException(() =>
                {
                    var content = Content.CreateNew("Folder", parent, Guid.NewGuid().ToString());
                    content.Save();
                }, typeof(InvalidOperationException));

                // try to create a content with a known handler that has an unknown parent
                ExpectException(() =>
                {
                    var content = Content.CreateNew("SystemFolder", parent, Guid.NewGuid().ToString());
                    content.Save();
                }, typeof(InvalidOperationException));

                var folderType = ContentTypeManager.Instance.GetContentTypeByName("Folder");

                // check if all related types are marked as unknown
                foreach (var contentType in ContentTypeManager.Instance.ContentTypes.Values)
                {
                    Assert.AreEqual(string.Equals(contentType.Path, folderType.Path) ||
                                    contentType.Path.StartsWith(folderType.Path + RepositoryPath.PathSeparator),
                        contentType.UnknownHandler);
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
    }
}
