using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Fields;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Workspaces;
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
            if (DataStore.Enabled)
                Assert.Inconclusive("This test uses custom script that is not supported feature in the new storage API.");

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
            if (DataStore.Enabled)
                Assert.Inconclusive("This test uses custom script that is not supported feature in the new storage API.");

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
            if (DataStore.Enabled)
                Assert.Inconclusive("This test uses custom script that is not supported feature in the new storage API.");

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
                var step = new DeleteContentType { Name = "Car", Delete = DeleteContentType.Mode.Force };
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
            if (DataStore.Enabled)
                Assert.Inconclusive("This test uses custom script that is not supported feature in the new storage API.");

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
                var root = new SystemFolder(Repository.Root) { Name = "TestRoot" };
                root.Save();
                var car0 = Content.CreateNew("Car", root, "Car0");
                car0.Save();
                var car1 = Content.CreateNew("Car1", root, "Car1");
                car1.Save();
                var car2 = Content.CreateNew("Car2", root, "Car2");
                car2.Save();

                // test-1
                var step = new DeleteContentType { Name = "Car", Delete = DeleteContentType.Mode.Force };
                var dependencies = step.GetDependencies(new[] { "Car" });

                Assert.AreEqual(3, dependencies.InstanceCount);
                Assert.AreEqual(0, dependencies.PermittingContentTypes.Length);
                Assert.AreEqual(0, dependencies.PermittingFieldSettings.Length);
                Assert.AreEqual(0, dependencies.PermittingContentCollection.Count);
                Assert.AreEqual(0, dependencies.Applications.Length);
                Assert.AreEqual(0, dependencies.ContentTemplates.Length);
                Assert.AreEqual(0, dependencies.ContentViews.Length);

                // test-2
                step.Execute(GetExecutionContext());

                Assert.IsNull(ContentType.GetByName("Car"));
                Assert.IsNull(ContentType.GetByName("Car1"));
                Assert.IsNull(ContentType.GetByName("Car2"));
                Assert.AreEqual(contentTypeCount, GetContentTypeCount());
            });
        }

        [TestMethod]
        public void Step_DeleteContentType_WithRelatedContentType()
        {
            if (DataStore.Enabled)
                Assert.Inconclusive("This test uses custom script that is not supported feature in the new storage API.");

            var contentTypeTemplate =
                @"<?xml version='1.0' encoding='utf-8'?>
<ContentType name='{0}' parentType='{1}' handler='SenseNet.ContentRepository.GenericContent' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
<AllowedChildTypes>{2}</AllowedChildTypes>
</ContentType>";

            Test(() =>
            {
                // init
                var contentTypeCount = GetContentTypeCount();
                InstallCarContentType();
                ContentTypeInstaller.InstallContentType(
                    string.Format(contentTypeTemplate, "Car1", "Car", ""),
                    string.Format(contentTypeTemplate, "Car2", "Car", ""),
                    string.Format(contentTypeTemplate, "Garage1", "GenericContent", "Car,Folder"),
                    string.Format(contentTypeTemplate, "Garage2", "GenericContent", "Folder,Car2"));
                var root = new SystemFolder(Repository.Root) { Name = "TestRoot" };
                root.Save();

                // test-1
                var step = new DeleteContentType { Name = "Car", Delete = DeleteContentType.Mode.Force };
                var dependencies = step.GetDependencies(new[] { "Car" });

                Assert.AreEqual(0, dependencies.InstanceCount);
                Assert.AreEqual(2, dependencies.PermittingContentTypes.Length);
                Assert.AreEqual(0, dependencies.PermittingFieldSettings.Length);
                Assert.AreEqual(0, dependencies.PermittingContentCollection.Count);
                Assert.AreEqual(0, dependencies.Applications.Length);
                Assert.AreEqual(0, dependencies.ContentTemplates.Length);
                Assert.AreEqual(0, dependencies.ContentViews.Length);

                // test-2
                step.Execute(GetExecutionContext());

                Assert.IsNull(ContentType.GetByName("Car"));
                Assert.IsNull(ContentType.GetByName("Car1"));
                Assert.IsNull(ContentType.GetByName("Car2"));
                Assert.AreEqual(contentTypeCount + 2, GetContentTypeCount());

                var names = new[] { "Car", "Car1", "Car2" };

                var garage1 = ContentType.GetByName("Garage1");
                Assert.IsFalse(garage1.AllowedChildTypeNames.Intersect(names).Any());
                var garage2 = ContentType.GetByName("Garage2");
                Assert.IsFalse(garage1.AllowedChildTypeNames.Intersect(names).Any());
            });
        }
        [TestMethod]
        public void Step_DeleteContentType_WithRelatedFieldSetting()
        {
            if (DataStore.Enabled)
                Assert.Inconclusive("This test uses custom script that is not supported feature in the new storage API.");

            var contentTypeTemplate =
                @"<?xml version='1.0' encoding='utf-8'?>
<ContentType name='{0}' parentType='{1}' handler='SenseNet.ContentRepository.GenericContent' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
<Fields><Field name='Ref1' type='Reference'><Configuration>
  <AllowedTypes>{2}</AllowedTypes>
</Configuration></Field></Fields></ContentType>";

            Test(() =>
            {
                // init
                var contentTypeCount = GetContentTypeCount();
                InstallCarContentType();
                ContentTypeInstaller.InstallContentType(
                    string.Format(contentTypeTemplate, "Car1", "Car", ""),
                    string.Format(contentTypeTemplate, "Car2", "Car", ""),
                    string.Format(contentTypeTemplate, "Garage1", "GenericContent", "<Type>Car</Type><Type>Folder</Type>"),
                    string.Format(contentTypeTemplate, "Garage2", "GenericContent", "<Type>Folder</Type><Type>Car2</Type>"));

                // test-1
                var step = new DeleteContentType { Name = "Car", Delete = DeleteContentType.Mode.Force };
                var dependencies = step.GetDependencies(new[] { "Car" });

                Assert.AreEqual(0, dependencies.InstanceCount);
                Assert.AreEqual(0, dependencies.PermittingContentTypes.Length);
                Assert.AreEqual(2, dependencies.PermittingFieldSettings.Length);
                Assert.AreEqual(0, dependencies.PermittingContentCollection.Count);
                Assert.AreEqual(0, dependencies.Applications.Length);
                Assert.AreEqual(0, dependencies.ContentTemplates.Length);
                Assert.AreEqual(0, dependencies.ContentViews.Length);

                // test-2
                step.Execute(GetExecutionContext());

                Assert.IsNull(ContentType.GetByName("Car"));
                Assert.IsNull(ContentType.GetByName("Car1"));
                Assert.IsNull(ContentType.GetByName("Car2"));
                Assert.AreEqual(contentTypeCount + 2, GetContentTypeCount());

                Assert.AreEqual(@"<ContentType name=""Garage1"" parentType=""GenericContent"" handler=""SenseNet.ContentRepository.GenericContent"" xmlns=""http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition"">
  <Fields>
    <Field name=""Ref1"" type=""Reference"">
      <Configuration>
        <AllowedTypes>
          <Type>Folder</Type>
        </AllowedTypes>
      </Configuration>
    </Field>
  </Fields>
</ContentType>", ContentType.GetByName("Garage1").ToXml());

                Assert.AreEqual(@"<ContentType name=""Garage2"" parentType=""GenericContent"" handler=""SenseNet.ContentRepository.GenericContent"" xmlns=""http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition"">
  <Fields>
    <Field name=""Ref1"" type=""Reference"">
      <Configuration>
        <AllowedTypes>
          <Type>Folder</Type>
        </AllowedTypes>
      </Configuration>
    </Field>
  </Fields>
</ContentType>", ContentType.GetByName("Garage2").ToXml());
            });
        }
        [TestMethod]
        public void Step_DeleteContentType_WithRelatedContent()
        {
            if (DataStore.Enabled)
                Assert.Inconclusive("This test uses custom script that is not supported feature in the new storage API.");

            Workspace CreateWorkspace(Node parent, string name, string[] allowedChildTypes)
            {
                var w = new Workspace(parent) { Name = name };
                w.AllowedChildTypes = new ContentType[0];
                w.AllowChildTypes(allowedChildTypes);
                w.Save();
                return w;
            }

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
                var root = new SystemFolder(Repository.Root) { Name = "TestRoot" };
                root.Save();

                var w1 = CreateWorkspace(root, "W1", new[] { "Car1", "Folder" });
                var w2 = CreateWorkspace(root, "W2", new[] { "Car", "Folder", "Car2", "File" });
                var w3 = CreateWorkspace(root, "W3", new[] { "Workspace", "Car2", "Folder", "Car1", "File" });
                var w4 = CreateWorkspace(root, "W4", new[] { "Car", "Car2", "Car1" });
                var w5 = CreateWorkspace(root, "W5", new[] { "Workspace", "Folder", "File" });


                // test-1
                var step = new DeleteContentType { Name = "Car", Delete = DeleteContentType.Mode.Force };
                var dependencies = step.GetDependencies(new[] { "Car" });

                Assert.AreEqual(0, dependencies.InstanceCount);
                Assert.AreEqual(0, dependencies.PermittingContentTypes.Length);
                Assert.AreEqual(0, dependencies.PermittingFieldSettings.Length);
                Assert.AreEqual(4, dependencies.PermittingContentCollection.Count);
                Assert.AreEqual(0, dependencies.Applications.Length);
                Assert.AreEqual(0, dependencies.ContentTemplates.Length);
                Assert.AreEqual(0, dependencies.ContentViews.Length);

                // test-2
                step.Execute(GetExecutionContext());

                Assert.IsNull(ContentType.GetByName("Car"));
                Assert.IsNull(ContentType.GetByName("Car1"));
                Assert.IsNull(ContentType.GetByName("Car2"));
                Assert.AreEqual(contentTypeCount, GetContentTypeCount());

                w1 = Node.Load<Workspace>(w1.Id);
                w2 = Node.Load<Workspace>(w2.Id);
                w3 = Node.Load<Workspace>(w3.Id);
                w4 = Node.Load<Workspace>(w4.Id);
                w5 = Node.Load<Workspace>(w5.Id);
                var names = new[] { "Car", "Car2", "Car1" };
                Assert.IsFalse(w1.GetAllowedChildTypeNames().Intersect(names).Any());
                Assert.IsFalse(w2.GetAllowedChildTypeNames().Intersect(names).Any());
                Assert.IsFalse(w3.GetAllowedChildTypeNames().Intersect(names).Any());
                Assert.IsFalse(w4.GetAllowedChildTypeNames().Intersect(names).Any());
                Assert.IsFalse(w5.GetAllowedChildTypeNames().Intersect(names).Any());
            });
        }

        [TestMethod]
        public void Step_DeleteContentType_Applications()
        {
            if (DataStore.Enabled)
                Assert.Inconclusive("This test uses custom script that is not supported feature in the new storage API.");

            var contentTypeTemplate =
                @"<?xml version='1.0' encoding='utf-8'?><ContentType name='{0}' parentType='Car' handler='SenseNet.ContentRepository.GenericContent' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition' />";

            Test(() =>
            {
                InstallCarContentType();
                ContentTypeInstaller.InstallContentType(
                    string.Format(contentTypeTemplate, "Car1"),
                    string.Format(contentTypeTemplate, "Car2"));

                var rootApps = Node.Load<Folder>("/Root/(apps)");
                var rooAppCount = rootApps.Children.Count();

                rootApps
                    .CreateChild("Car", "Folder", out Node rootCar)
                    .CreateChild("MyOperation", "GenericODataApplication");
                rootApps
                    .CreateChild("Car1", "Folder", out Node rootCar1)
                    .CreateChild("MyOperation", "GenericODataApplication");
                Repository.Root
                    .CreateChild("MyStructure", "SystemFolder", out Node myRoot)
                    .CreateChild("(apps)", "SystemFolder")
                    .CreateChild("Car", "Folder", out Node deepCar)
                    .CreateChild("MyOperation", "GenericODataApplication");
                myRoot.CreateChild("Car", "Folder", out Node carFolder);

                // test-1
                var step = new DeleteContentType { Name = "Car", Delete = DeleteContentType.Mode.Force };
                var dependencies = step.GetDependencies(new[] { "Car" });

                Assert.AreEqual(0, dependencies.InstanceCount); // Any instance in a content template is irrelevant.
                Assert.AreEqual(0, dependencies.PermittingContentTypes.Length);
                Assert.AreEqual(0, dependencies.PermittingFieldSettings.Length);
                Assert.AreEqual(0, dependencies.PermittingContentCollection.Count);
                Assert.AreEqual(3, dependencies.Applications.Length);
                Assert.AreEqual(0, dependencies.ContentTemplates.Length);
                Assert.AreEqual(0, dependencies.ContentViews.Length);

                // test-2
                step.Execute(GetExecutionContext());

                Assert.IsNull(Node.LoadNode(rootCar.Id));
                Assert.IsNull(Node.LoadNode(rootCar1.Id));
                Assert.IsNull(Node.LoadNode(deepCar.Id));
                Assert.IsNotNull(Node.LoadNode(carFolder.Id));
                Assert.AreEqual(rooAppCount, rootApps.Children.Count());
            });
        }
        [TestMethod]
        public void Step_DeleteContentType_ContentTemplate()
        {
            if (DataStore.Enabled)
                Assert.Inconclusive("This test uses custom script that is not supported feature in the new storage API.");

            var contentTypeTemplate =
                @"<?xml version='1.0' encoding='utf-8'?><ContentType name='{0}' parentType='Car' handler='SenseNet.ContentRepository.GenericContent' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition' />";

            Test(() =>
            {
                InstallCarContentType();
                ContentTypeInstaller.InstallContentType(
                    string.Format(contentTypeTemplate, "Car1"),
                    string.Format(contentTypeTemplate, "Car2"));

                Repository.Root
                    .CreateChild("ContentTemplates", "SystemFolder", out Node globalTemp)
                    .CreateChild("Car", "Folder", out Node globalCarTemp)
                    .CreateChild("Car", "Car", out Node globalCar);
                Repository.Root
                    .CreateChild("Sites", "Sites")
                    .CreateChild("Site1", "Site")
                    .CreateChild("WS1", "Workspace")
                    .CreateChild("WS1", "Workspace")
                    .CreateChild("ContentTemplates", "SystemFolder", out Node localTemp)
                    .CreateChild("Car", "Folder", out Node localCarTemp)
                    .CreateChild("Car", "Car", out Node localCar);

                // test-1
                var step = new DeleteContentType { Name = "Car", Delete = DeleteContentType.Mode.Force };
                var dependencies = step.GetDependencies(new[] { "Car" });

                Assert.AreEqual(0, dependencies.InstanceCount); // Any instance in a content template is irrelevant.
                Assert.AreEqual(0, dependencies.PermittingContentTypes.Length);
                Assert.AreEqual(0, dependencies.PermittingFieldSettings.Length);
                Assert.AreEqual(0, dependencies.PermittingContentCollection.Count);
                Assert.AreEqual(0, dependencies.Applications.Length);
                Assert.AreEqual(2, dependencies.ContentTemplates.Length);
                Assert.AreEqual(0, dependencies.ContentViews.Length);

                // test-2
                step.Execute(GetExecutionContext());

                Assert.IsNotNull(Node.LoadNode(globalTemp.Id));
                Assert.IsNull(Node.LoadNode(globalCarTemp.Id));
                Assert.IsNull(Node.LoadNode(globalCar.Id));
                Assert.IsNotNull(Node.LoadNode(localTemp.Id));
                Assert.IsNull(Node.LoadNode(localCarTemp.Id));
                Assert.IsNull(Node.LoadNode(localCar.Id));
            });
        }
        [TestMethod]
        public void Step_DeleteContentType_ContentView()
        {
            if (DataStore.Enabled)
                Assert.Inconclusive("This test uses custom script that is not supported feature in the new storage API.");

            var contentTypeTemplate =
                @"<?xml version='1.0' encoding='utf-8'?><ContentType name='{0}' parentType='Car' handler='SenseNet.ContentRepository.GenericContent' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition' />";

            Test(() =>
            {
                InstallCarContentType();
                ContentTypeInstaller.InstallContentType(
                    string.Format(contentTypeTemplate, "Car1"),
                    string.Format(contentTypeTemplate, "Car2"),
                        @"<?xml version='1.0' encoding='utf-8'?>
                        <ContentType name='ContentViews' parentType='Folder' handler='SenseNet.ContentRepository.Folder' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
                          <AllowedChildTypes>ContentView ContentViews</AllowedChildTypes>
                        </ContentType>",
                        @"<?xml version='1.0' encoding='utf-8'?>
                        <ContentType name='Skins' parentType='SystemFolder' handler='SenseNet.ContentRepository.SystemFolder' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
                          <AllowedChildTypes>Skin</AllowedChildTypes>
                        </ContentType>",
                        @"<?xml version='1.0' encoding='utf-8'?>
                        <ContentType name='Skin' parentType='SystemFolder' handler='SenseNet.ContentRepository.SystemFolder' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
                          <AllowedChildTypes>SystemFolder</AllowedChildTypes>
                        </ContentType>",
                        @"<?xml version='1.0' encoding='utf-8'?>
                        <ContentType name='ContentView' parentType='File' handler='SenseNet.ContentRepository.File' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
                        </ContentType>"
                    );

                Repository.Root
                    .CreateChild("Global", "SystemFolder")
                    .CreateChild("ContentViews", "ContentViews", out Node globalViews)
                    .CreateChild("Car", "ContentViews", out Node globalCar)
                    .CreateChild("something.ascx", "ContentView");
                globalViews
                    .CreateChild("Car1", "ContentViews", out Node globalCar1)
                    .CreateChild("something.ascx", "ContentView");
                Repository.Root
                    .CreateChild("Skins", "Skins", out Node skins)
                    .CreateChild("Skin-1", "Skin")
                    .CreateChild("ContentViews", "SystemFolder", out Node skin1views)
                    .CreateChild("Car", "ContentViews", out Node skin1Car)
                    .CreateChild("something.ascx", "ContentView");
                skin1views
                    .CreateChild("File", "ContentViews", out Node skin1File)
                    .CreateChild("something.ascx", "ContentView");
                skins
                    .CreateChild("Skin-2", "Skin")
                    .CreateChild("ContentViews", "SystemFolder")
                    .CreateChild("Car2", "ContentViews", out Node skin2Car2)
                    .CreateChild("something.ascx", "ContentView");

                // test-1
                var step = new DeleteContentType { Name = "Car", Delete = DeleteContentType.Mode.Force };
                var dependencies = step.GetDependencies(new[] { "Car" });

                Assert.AreEqual(0, dependencies.InstanceCount); // Any instance in a content template is irrelevant.
                Assert.AreEqual(0, dependencies.PermittingContentTypes.Length);
                Assert.AreEqual(0, dependencies.PermittingFieldSettings.Length);
                Assert.AreEqual(0, dependencies.PermittingContentCollection.Count);
                Assert.AreEqual(0, dependencies.Applications.Length);
                Assert.AreEqual(0, dependencies.ContentTemplates.Length);
                Assert.AreEqual(4, dependencies.ContentViews.Length);

                // test-2
                step.Execute(GetExecutionContext());

                Assert.IsNull(Node.LoadNode(globalCar.Id));
                Assert.IsNull(Node.LoadNode(globalCar1.Id));
                Assert.IsNull(Node.LoadNode(skin1Car.Id));
                Assert.IsNotNull(Node.LoadNode(skin1File.Id));
                Assert.IsNull(Node.LoadNode(skin2Car2.Id));
            });
        }

        [TestMethod]
        public void Step_DeleteContentType_IfNotUsed()
        {
            Test(() =>
            {
                var dependencies =
                    new DeleteContentType.ContentTypeDependencies { ContentTypeNames = new[] { "MyContentType" } };
                Assert.IsFalse(dependencies.HasDependency);

                // allowed types
                dependencies.PermittingContentTypes = new[] { ContentType.GetByName(typeof(GenericContent).Name) };
                Assert.IsFalse(dependencies.HasDependency);
                dependencies.PermittingFieldSettings = new[] { new ReferenceFieldSetting() };
                Assert.IsFalse(dependencies.HasDependency);
                dependencies.PermittingContentCollection = new Dictionary<string, string> { { "/root/mycontent", "" } };
                Assert.IsFalse(dependencies.HasDependency);

                // sensitive items
                dependencies.ContentTemplates = new[] { "/root/temp" };
                Assert.IsFalse(dependencies.HasDependency);
                dependencies.ContentViews = new[] { "/root/view" };
                Assert.IsFalse(dependencies.HasDependency);
                dependencies.Applications = new[] { "/root/app" };
                Assert.IsFalse(dependencies.HasDependency);

                // dependencies
                dependencies.InstanceCount = 1;
                Assert.IsTrue(dependencies.HasDependency);

                // check again
                dependencies.InstanceCount = 0;
                Assert.IsFalse(dependencies.HasDependency);

                dependencies.InheritedTypeNames = new[] { "MyInheritedContentType", "MyAnotherInheritedContentType" };
                Assert.IsTrue(dependencies.HasDependency);
            });
        }


        private Node CreateContent(Node parent, string type, string name)
        {
            var content = Content.CreateNew(type, parent, name);
            content.Save();
            return content.ContentHandler;
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
