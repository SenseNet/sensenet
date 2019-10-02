using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.OData;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.ODataTests
{
    //[TestClass]
    public class ODataCreationTests : ODataTestBase
    {
        [TestMethod]
        public async Task OD_POST_Creation()
        {
            await IsolatedODataTestAsync(async () =>
            {
                var testRoot = CreateTestRoot("ODataTestRoot");

                var name = "Content1";
                var displayName = "Content-1 display name";
                var path = RepositoryPath.Combine(testRoot.Path, name);

                // ACTION
                var response = await ODataPostAsync(
                    "/OData.svc/" + testRoot.Path, 
                    "",
                    $@"models=[{{""Name"":""{name}"",""DisplayName"":""{displayName}"",""Index"":42}}]")
                    .ConfigureAwait(false);

                // ASSERT
                var fileContentType = ContentType.GetByName("File");
                AssertNoError(response);
                var entity = GetEntity(response);
                Assert.AreEqual(name, entity.Name);
                Assert.AreEqual(fileContentType, entity.ContentType);
                Assert.IsTrue(entity.AllPropertiesSelected);
                Assert.IsTrue(entity.AllProperties.ContainsKey("__metadata"));

                var content = Content.Load(path);
                Assert.AreEqual(displayName, content.DisplayName);
                Assert.AreEqual(42, content.Index);
                Assert.AreEqual(fileContentType, content.ContentType);
            }).ConfigureAwait(false); ;
        }
        [TestMethod]
        public async Task OD_POST_Creation_ShortenedResponse()
        {
            await IsolatedODataTestAsync(async () =>
            {
                var testRoot = CreateTestRoot("ODataTestRoot");

                var name = "Content1";
                var displayName = "Content-1 display name";
                var path = RepositoryPath.Combine(testRoot.Path, name);

                // ACTION
                var response = await ODataPostAsync(
                    "/OData.svc/" + testRoot.Path,
                    "?metadata=no&$select=Name,Index",
                    $@"models=[{{""Name"":""{name}"",""DisplayName"":""{displayName}"",""Index"":42}}]")
                    .ConfigureAwait(false); ;

                // ASSERT
                var fileContentType = ContentType.GetByName("File");
                AssertNoError(response);
                var entity = GetEntity(response);
                Assert.IsFalse(entity.AllProperties.ContainsKey("__metadata"));
                Assert.AreEqual(name, entity.Name);
                Assert.AreEqual(2, entity.AllProperties.Count);
            }).ConfigureAwait(false); ;
        }
        [TestMethod]
        public async Task OD_POST_Creation_MissingParent()
        {
            await IsolatedODataTestAsync(async () =>
            {
                // ACTION 1: Create
                var response = await ODataPostAsync(
                        "/OData.svc/Root/MissingContent",
                        "",
                        @"models=[{""Name"":""Content1""}]")
                    .ConfigureAwait(false); ;

                // ASSERT 1
                Assert.AreEqual(404, response.StatusCode);
            }).ConfigureAwait(false); ;
        }
        [TestMethod]
        public async Task OD_POST_Creation_ExplicitType()
        {
            await IsolatedODataTestAsync(async () =>
            {
                InstallCarContentType();
                var testRoot = CreateTestRoot("ODataTestRoot");

                var name = "Content1";
                var path = RepositoryPath.Combine(testRoot.Path, name);

                // ACTION
                var response = await ODataPostAsync(
                    "/OData.svc/" + testRoot.Path,
                    "",
                    $@"models=[{{""__ContentType"":""Car"",""Name"":""{name}""}}]")
                    .ConfigureAwait(false); ;

                // ASSERT
                AssertNoError(response);
                var carContentType = ContentType.GetByName("Car");
                var entity = GetEntity(response);
                Assert.AreEqual(name, entity.Name);
                Assert.AreEqual(carContentType, entity.ContentType);

                var content = Content.Load(path);
                Assert.AreEqual(carContentType, content.ContentType);
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_POST_Creation_References()
        {
            await IsolatedODataTestAsync(async () =>
            {
                var testRoot = CreateTestRoot("ODataTestRoot");
                EnsureReferenceTestStructure(testRoot);
                var refs = new[] { Repository.Root, Repository.ImsFolder };

                var name1 = "Content1";
                var path1 = RepositoryPath.Combine(testRoot.Path, name1);

                var pathRefs = "[" + String.Join(",", refs.Select(n => "\"" + n.Path + "\"")) + "]";
                var idRefs = "[" + String.Join(",", refs.Select(n => n.Id)) + "]";
                var simpleRefNode = Node.LoadNode(4);

                // ACTION-1
                var response = await ODataPostAsync(
                        "/OData.svc/" + testRoot.Path,
                        "",
                        $@"models=[{{""__ContentType"":""OData_ReferenceTest_ContentHandler"","
                            + $@"""Name"":""{name1}"",""Reference"":{pathRefs},"
                            + $@"""References"":{pathRefs},""Reference2"":""{simpleRefNode.Path}""}}]")
                    .ConfigureAwait(false);

                // ASSERT-1
                AssertNoError(response);
                var node1 = Node.Load<OData_ReferenceTest_ContentHandler>(path1);
                var reloadedRefs1 = "[" + String.Join(",", node1.References.Select(n => +n.Id)) + "]";
                Assert.AreEqual(idRefs, reloadedRefs1);
                Assert.AreEqual(refs[0].Id, node1.Reference.Id);
                Assert.AreEqual(simpleRefNode.Id, node1.Reference2.Id);

                //--------------------------------------------------------------

                var name2 = "Content2";
                var path2 = RepositoryPath.Combine(testRoot.Path, name2);

                // ACTION-2
                response = await ODataPostAsync(
                        "/OData.svc/" + testRoot.Path,
                        "",
                        $@"models=[{{""__ContentType"":""OData_ReferenceTest_ContentHandler"","
                            + $@"""Name"":""{name2}"",""Reference"":{idRefs},"
                            + $@"""References"":{idRefs},""Reference2"":{simpleRefNode.Id}}}]")
                    .ConfigureAwait(false);

                // ASSERT-2
                AssertNoError(response);
                var node2 = Node.Load<OData_ReferenceTest_ContentHandler>(path2);
                var reloadedRefs2 = "[" + String.Join(",", node2.References.Select(n => n.Id)) + "]";
                Assert.AreEqual(idRefs, reloadedRefs2);
                Assert.AreEqual(refs[0].Id, node2.Reference.Id);
                Assert.AreEqual(simpleRefNode.Id, node2.Reference2.Id);
            });
        }
        [TestMethod]
        public async Task OD_POST_Creation_UnderById()
        {
            await IsolatedODataTestAsync(async () =>
            {
                var testRoot = CreateTestRoot("ODataTestRoot");

                var name = "Content1";
                var displayName = "Content1 display name";
                var path = RepositoryPath.Combine(testRoot.Path, name);

                // ACTION
                var response = await ODataPostAsync(
                        "/OData.svc/content(" + testRoot.Id + ")",
                        "?metadata=no&$select=Name,Type",
                        $@"models=[{{""Name"":""{name}"",""DisplayName"":""{displayName}"",""Index"":41}}]")
                    .ConfigureAwait(false);

                // ASSERT
                AssertNoError(response);

                var content = Content.Load(path);
                Assert.AreEqual(displayName, content.DisplayName);
                Assert.AreEqual(ContentType.GetByName("File"), content.ContentType);

            }).ConfigureAwait(false); ;
        }
        [TestMethod]
        public async Task OD_POST_Creation_ExplicitType_UnderById()
        {
            await IsolatedODataTestAsync(async () =>
            {
                InstallCarContentType();
                var testRoot = CreateTestRoot("ODataTestRoot");

                var name = "Content1";
                var path = RepositoryPath.Combine(testRoot.Path, name);

                // ACTION
                var response = await ODataPostAsync(
                        "/OData.svc/content(" + testRoot.Id + ")",
                        "?metadata=no&$select=Name,Type",
                        $@"models=[{{""__ContentType"":""Car"",""Name"":""{name}""}}]")
                    .ConfigureAwait(false);

                // ASSERT
                AssertNoError(response);
                var content = Content.Load(path);
                Assert.IsNotNull(content);
                Assert.AreEqual(ContentType.GetByName("Car"), content.ContentType);

            }).ConfigureAwait(false); ;
        }
        [TestMethod]
        public async Task OD_POST_Creation_FromTemplate()
        {
            await IsolatedODataTestAsync(async () =>
            {
                InstallCarContentType();
                var testRoot = CreateTestRoot("ODataTestRoot");
                EnsureTemplateStructure();

                var name = "Content1";
                var path = RepositoryPath.Combine(testRoot.Path, name);

                // ACTION
                var response = await ODataPostAsync(
                        "/OData.svc" + testRoot.Path,
                        "",
                        $@"models=[{{""__ContentType"":""Car"",""__ContentTemplate"":""Template3"","
                        + $@"""Name"":""{name}"",""EngineSize"":""3.5 l""}}]")
                    .ConfigureAwait(false);

                // ASSERT
                AssertNoError(response);

                var content = Content.Load(path);
                Assert.AreEqual("Car", content.ContentType.Name);
                Assert.AreEqual(name, content.Name);
                Assert.AreEqual("TestCar3", content["Make"]);
                Assert.AreEqual("Template3", content["Model"]);
                Assert.AreEqual("3.5 l", content["EngineSize"]);
            }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task OD_POST_Creation_FIX_InconsistentNameAfterCreation()
        {
            await IsolatedODataTestAsync(async () =>
            {
                InstallCarContentType();
                var testRoot = CreateTestRoot("ODataTestRoot");

                var name = "Car";

                // ACTION
                var names = new string[3];
                for (int i = 0; i < 3; i++)
                {
                    var response = await ODataPostAsync(
                            String.Concat("/OData.svc", ODataMiddleware.GetEntityUrl(testRoot.Path)),
                            "?metadata=no&$select=Name",
                            $@"models=[{{""__ContentType"":""Car"",""Name"":""{name}""}}]")
                        .ConfigureAwait(false);
                    var entity = GetEntity(response);
                    names[i] = entity.Name;
                }

                // ASSERT
                Assert.AreNotEqual(names[0], names[1]);
                Assert.AreNotEqual(names[0], names[2]);
                Assert.AreNotEqual(names[1], names[2]);
            });
        }

        /* ====================================================================== TOOLS */

        private void EnsureTemplateStructure()
        {
            //global template folder
            var ctfGlobal = Node.LoadNode(RepositoryStructure.ContentTemplateFolderPath);
            if (ctfGlobal == null)
            {
                ctfGlobal = new SystemFolder(Node.LoadNode("/Root")) { Name = Repository.ContentTemplatesFolderName };
                ctfGlobal.Save();
            }

            //create content template type folders
            var folderGlobalCtCar = Node.Load<Folder>(RepositoryPath.Combine(ctfGlobal.Path, "Car"));
            if (folderGlobalCtCar == null)
            {
                folderGlobalCtCar = new Folder(ctfGlobal) { Name = "Car" };
                folderGlobalCtCar.Save();
            }

            //create content templates
            for (int i = 0; i < 4; i++)
            {
                var index = i + 1;
                var templateName = "Template" + index;
                if (Node.Load<ContentRepository.File>(RepositoryPath.Combine(folderGlobalCtCar.Path, templateName)) == null)
                {
                    var template = Content.CreateNew("Car", folderGlobalCtCar, templateName);
                    template["Make"] = "TestCar" + index;
                    template["Model"] = templateName;
                    template.Save();
                }
            }

        }

        private static void EnsureReferenceTestStructure(Node testRoot)
        {
            if (ContentType.GetByName(typeof(OData_ReferenceTest_ContentHandler).Name) == null)
                ContentTypeInstaller.InstallContentType(OData_ReferenceTest_ContentHandler.CTD);

            if (ContentType.GetByName(typeof(OData_Filter_ThroughReference_ContentHandler).Name) == null)
                ContentTypeInstaller.InstallContentType(OData_Filter_ThroughReference_ContentHandler.CTD);

            var referrerContent = Content.Load(RepositoryPath.Combine(testRoot.Path, "Referrer"));
            if (referrerContent == null)
            {
                var nodes = new Node[5];
                for (int i = 0; i < nodes.Length; i++)
                {
                    var content = Content.CreateNew("OData_Filter_ThroughReference_ContentHandler", testRoot, "Referenced" + i);
                    content.Index = i + 1;
                    content.Save();
                    nodes[i] = content.ContentHandler;
                }

                referrerContent = Content.CreateNew("OData_Filter_ThroughReference_ContentHandler", testRoot, "Referrer");
                var referrer = (OData_Filter_ThroughReference_ContentHandler)referrerContent.ContentHandler;
                referrer.References = nodes;
                referrerContent.Save();
            }
        }
    }
}
