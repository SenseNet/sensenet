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

            // find the GenericContent CTD content file
            var proto = InMemoryDataProvider.Prototype;
            var fileRecord = proto.Files.First(ff =>
                ff.Extension == ".ContentType" && ff.FileNameWithoutExtension == "Folder");

            // set a fake handler
            InMemoryDataProvider.SetContentHandler(fileRecord, "unknownhandler", "Folder", proto);
            
            try
            {
                Test(() =>
                {
                    var currentDb = ((InMemoryDataProvider)DataProvider.Current).DB;
                    InMemoryDataProvider.SetContentHandler(fileRecord, "unknownhandler", "Folder", currentDb);

                    NodeTypeManager.Restart();
                    ContentTypeManager.Reset();

                    var parent = Node.LoadNode("/Root");

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

                    //var thrown = false;
                    //try
                    //{
                    //    content = Content.CreateNew("Folder", parent, Guid.NewGuid().ToString());
                    //    content.Save();
                    //}
                    //catch (InvalidOperationException)
                    //{
                    //    thrown = true;
                    //}

                    //Assert.IsTrue(thrown, "Exception was not thrown.");

                    //NodeTypeManager.Restart();

                    //foreach (var nodeType in ActiveSchema.NodeTypes)
                    //{
                    //    SnTrace.Test.Write($"NodeType: {nodeType.Name} *** {nodeType.ClassName}");
                    //}
                    //foreach (var contentType in ContentTypeManager.Instance.ContentTypes.Values)
                    //{
                    //    SnTrace.Test.Write($"ContentType: {contentType.Name} *** {contentType.HandlerName}");
                    //}
                });
            }
            finally
            {
                // reset the correct handler in the prototype
                InMemoryDataProvider.SetContentHandler(fileRecord, "SenseNet.ContentRepository.Folder");
            }
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
