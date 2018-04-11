using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Schema;
using SenseNet.Packaging.Steps;
using SenseNet.Packaging.Tests.Implementations;
using SenseNet.Tests;

namespace SenseNet.Packaging.Tests.StepTests
{
    [TestClass]
    public class DeleteContentTypeTests : TestBase
    {
        private static StringBuilder _log;
        [TestInitialize]
        public void PrepareTest()
        {
            // preparing logger
            _log = new StringBuilder();
            var loggers = new[] { new PackagingTestLogger(_log) };
            var loggerAcc = new PrivateType(typeof(Logger));
            loggerAcc.SetStaticField("_loggers", loggers);
        }
        [TestCleanup]
        public void AfterTest()
        {
        }

        [TestMethod]
        public void Step_DeleteContentType_Leaf()
        {
            Test(() =>
            {
                // init
                if (null != ContentType.GetByName("Car"))
                    Assert.Inconclusive();
                var contentTypeCount = GetContentTypeCount();
                InstallCarContentType();
                Assert.IsNotNull(ContentType.GetByName("Car"));
                Assert.AreEqual(contentTypeCount+1, GetContentTypeCount());

                // test
                var step = new DeleteContentType { Name = "Car" };
                step.Execute(GetExecutionContext());

                // check
                Assert.IsNull(ContentType.GetByName("Car"));
                Assert.AreEqual(contentTypeCount, GetContentTypeCount());
            });
        }

        [TestMethod]
        public void Step_DeleteContentType_Subtree()
        {
            var contentTypeTemplate =
                @"<?xml version='1.0' encoding='utf-8'?><ContentType name='{0}' parentType='Car' handler='SenseNet.ContentRepository.GenericContent' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition' />";

            Test(() =>
            {
                // init
                var contentTypeCount = GetContentTypeCount();
                InstallCarContentType();
                ContentTypeInstaller.InstallContentType(
                    string.Format(contentTypeTemplate, "Car1"),
                    string.Format(contentTypeTemplate, "Car2"));
                Assert.IsNotNull(ContentType.GetByName("Car"));
                Assert.IsNotNull(ContentType.GetByName("Car1"));
                Assert.IsNotNull(ContentType.GetByName("Car2"));
                Assert.AreEqual(contentTypeCount + 3, GetContentTypeCount());

                // test
                var step = new DeleteContentType {Name = "Car"};
                step.Execute(GetExecutionContext());

                // check
                Assert.IsNull(ContentType.GetByName("Car"));
                Assert.AreEqual(contentTypeCount, GetContentTypeCount());
            });
        }


        private int GetContentTypeCount()
        {
            return ContentType.GetContentTypes().Length;
        }
        private ExecutionContext GetExecutionContext()
        {
            var manifestXml = new XmlDocument();
            manifestXml.LoadXml(@"<?xml version='1.0' encoding='utf-8'?>
                    <Package type='Install'>
                        <Id>MyCompany.MyComponent</Id>
                        <ReleaseDate>2017-01-01</ReleaseDate>
                        <Version>1.0</Version>
                        <Steps>
                            <Trace>Package is running.</Trace>
                        </Steps>
                    </Package>");

            var phase = 0;
            var console = new StringWriter();
            var manifest = Manifest.Parse(manifestXml, phase, true, new PackageParameter[0]);
            var executionContext = ExecutionContext.CreateForTest("packagePath", "targetPath", new string[0], "sandboxPath", manifest, phase, manifest.CountOfPhases, null, console);
            executionContext.RepositoryStarted = true;
            return executionContext;
        }
    }
}
