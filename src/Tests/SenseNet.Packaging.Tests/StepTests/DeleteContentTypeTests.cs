using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository;
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
        public void Step_DeleteContentType_Parse()
        {
            DeleteContentType CreateStep(string stepElementString)
            {
                var manifestXml = new XmlDocument();
                manifestXml.LoadXml($@"<?xml version='1.0' encoding='utf-8'?>
                    <Package type='Install'>
                        <Id>MyCompany.MyComponent</Id>
                        <ReleaseDate>2017-01-01</ReleaseDate>
                        <Version>1.0</Version>
                        <Steps>
                            {stepElementString}
                        </Steps>
                    </Package>");
                var manifest = Manifest.Parse(manifestXml, 0, true, new PackageParameter[0]);
                var executionContext = ExecutionContext.CreateForTest("packagePath", "targetPath", new string[0], "sandboxPath", manifest, 0, manifest.CountOfPhases, null, null);
                var stepElement = (XmlElement)manifestXml.SelectSingleNode("/Package/Steps/DeleteContentType");
                var result = (DeleteContentType)Step.Parse(stepElement, 0, executionContext);
                return result;
            }

            var step = CreateStep("<DeleteContentType>ContenType1</DeleteContentType>");
            Assert.AreEqual("ContenType1", step.Name);
            Assert.AreEqual(DeleteContentType.Mode.No, step.Delete);

            step = CreateStep("<DeleteContentType name='ContenType1'/>");
            Assert.AreEqual("ContenType1", step.Name);
            Assert.AreEqual(DeleteContentType.Mode.No, step.Delete);

            step = CreateStep("<DeleteContentType name='ContenType2' delete='no'/>");
            Assert.AreEqual("ContenType2", step.Name);
            Assert.AreEqual(DeleteContentType.Mode.No, step.Delete);

            step = CreateStep("<DeleteContentType name='ContenType3' delete='ifNotUsed'/>");
            Assert.AreEqual("ContenType3", step.Name);
            Assert.AreEqual(DeleteContentType.Mode.IfNotUsed, step.Delete);

            step = CreateStep("<DeleteContentType name='ContenType4' delete='FORCE'/>");
            Assert.AreEqual("ContenType4", step.Name);
            Assert.AreEqual(DeleteContentType.Mode.Force, step.Delete);
        }

        [TestMethod]
        public void Step_DeleteContentType_DefaultOrInformationOnly()
        {
            Test(() =>
            {
                // init
                if (null != ContentType.GetByName("Car"))
                    Assert.Inconclusive();
                var contentTypeCount = GetContentTypeCount();
                InstallCarContentType();
                Assert.IsNotNull(ContentType.GetByName("Car"));
                Assert.AreEqual(contentTypeCount + 1, GetContentTypeCount());

                // test-1
                var step = new DeleteContentType { Name = "Car" };
                step.Execute(GetExecutionContext());

                // check-1
                Assert.IsNotNull(ContentType.GetByName("Car"));
                Assert.AreEqual(contentTypeCount + 1, GetContentTypeCount());

                // test-1
                step = new DeleteContentType { Name = "Car", Delete = DeleteContentType.Mode.No };
                step.Execute(GetExecutionContext());

                // check-1
                Assert.IsNotNull(ContentType.GetByName("Car"));
                Assert.AreEqual(contentTypeCount + 1, GetContentTypeCount());
            });
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
                Assert.AreEqual(contentTypeCount + 1, GetContentTypeCount());

                // test-1
                var step = new DeleteContentType { Name = "Car", Delete = DeleteContentType.Mode.Force };
                step.Execute(GetExecutionContext());

                // check-1
                Assert.IsNull(ContentType.GetByName("Car"));
                Assert.AreEqual(contentTypeCount, GetContentTypeCount());

                // test-2
                step = new DeleteContentType { Name = "Car", Delete = DeleteContentType.Mode.IfNotUsed };
                step.Execute(GetExecutionContext());

                // check-1
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
                var step = new DeleteContentType {Name = "Car", Delete = DeleteContentType.Mode.Force};
                step.Execute(GetExecutionContext());

                // check
                Assert.IsNull(ContentType.GetByName("Car"));
                Assert.IsNull(ContentType.GetByName("Car1"));
                Assert.IsNull(ContentType.GetByName("Car2"));
                Assert.AreEqual(contentTypeCount, GetContentTypeCount());
            });
        }

        [TestMethod]
        public void Step_DeleteContentType_WithInstances()
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
                var root = new SystemFolder(Repository.Root) {Name = "TestRoot"};
                root.Save();
                var car0 = Content.CreateNew("Car", root, "Car0");
                car0.Save();
                var car1 = Content.CreateNew("Car1", root, "Car1");
                car1.Save();
                var car2 = Content.CreateNew("Car2", root, "Car2");
                car2.Save();

                // test-1
                var step = new DeleteContentType { Name = "Car", Delete = DeleteContentType.Mode.Force };
                var dependencies = step.GetDependencies(ContentType.GetByName("Car"));

                Assert.AreEqual(3, dependencies.InstanceCount);
                Assert.AreEqual(0, dependencies.RelatedContentTypes.Length);
                Assert.AreEqual(0, dependencies.RelatedFieldSettings.Length);
                Assert.AreEqual(0, dependencies.RelatedContentPaths.Length);

                // test-2
                step.Execute(GetExecutionContext());

                Assert.IsNull(ContentType.GetByName("Car"));
                Assert.IsNull(ContentType.GetByName("Car1"));
                Assert.IsNull(ContentType.GetByName("Car2"));
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
