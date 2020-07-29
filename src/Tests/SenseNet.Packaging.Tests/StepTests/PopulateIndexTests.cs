using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Versioning;
using SenseNet.ContentRepository.Workspaces;
using SenseNet.Packaging.Steps;
using SenseNet.Packaging.Tests.Implementations;
using SenseNet.Testing;
using SenseNet.Tests.Core;

namespace SenseNet.Packaging.Tests.StepTests
{
    [TestClass]
    public class PopulateIndexTests : TestBase
    {
        private static StringBuilder _log;
        [TestInitialize]
        public void PrepareTest()
        {
            // preparing logger
            _log = new StringBuilder();
            var loggers = new[] { new PackagingTestLogger(_log) };
            var loggerAcc = new TypeAccessor(typeof(Logger));
            loggerAcc.SetStaticField("_loggers", loggers);
        }
        [TestCleanup]
        public void AfterTest()
        {
        }

        [TestMethod]
        public void Packaging_PopulateIndex_HardReindexWorksOnAllVersions()
        {
            Test(() =>
            {
                // arrange
                InstallCarContentType();
                var root = new SystemFolder(Repository.Root) {Name = "TestRoot"};
                root.Save();

                var workspace = new Workspace(root)
                {
                    Name = "Workspace-1",
                    InheritableVersioningMode = InheritableVersioningType.MajorAndMinor
                };
                workspace.AllowChildType("Car");
                workspace.Save();

                var car1 = Content.CreateNew("Car", workspace, "Car1");
                car1.Save();
                var id = car1.Id;

                // create some versions
                car1 = Content.Load(id); car1.Publish();
                car1 = Content.Load(id); car1.CheckOut();
                car1 = Content.Load(id); car1.Index++; car1.Save();
                car1.CheckIn();
                car1 = Content.Load(id); car1.Index++; car1.Save();
                car1 = Content.Load(id); car1.Index++; car1.Save();
                car1 = Content.Load(id); car1.Publish();
                car1 = Content.Load(id); car1.Index++; car1.Save();
                car1 = Content.Load(id); car1.Index++; car1.Save();
                car1 = Content.Load(id); car1.Publish();

                var versions = car1.Versions.Select(n => n.Version.ToString()).ToArray();

                var step = new PopulateIndex {Path = car1.Path, Level = "DatabaseAndIndex"};

                var refreshedVersions = new List<string>();
                var indexedVersions = new List<string>();
                var refreshed = new EventHandler<NodeIndexedEventArgs>((sender, args) =>
                {
                    refreshedVersions.Add(args.Version.ToUpper());
                });
                var indexed = new EventHandler<NodeIndexedEventArgs>((sender, args) =>
                {
                    indexedVersions.Add(args.Version.ToUpper());
                });

                // action
                step.ExecuteInternal(GetExecutionContext(), refreshed, indexed);

                // assert
                Assert.AreEqual(6, versions.Length);

                refreshedVersions.Sort();
                Assert.AreEqual(string.Join(",", versions), string.Join(",", refreshedVersions));

                indexedVersions.Sort();
                Assert.AreEqual(string.Join(",", versions), string.Join(",", indexedVersions));


            });
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
