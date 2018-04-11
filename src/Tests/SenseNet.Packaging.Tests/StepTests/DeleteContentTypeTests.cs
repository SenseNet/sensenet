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
                if (null != ContentType.GetByName("Car"))
                    Assert.Inconclusive();
                var contentTypeCount = GetContentTypeCount();
                InstallCarContentType();
                Assert.AreEqual(contentTypeCount+1, GetContentTypeCount());

                var executionContext = GetExecutionContext();
                var step = new DeleteContentType { Name = "Car" };
                step.Execute(executionContext);

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
