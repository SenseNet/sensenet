using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.ODataTests
{
    [TestClass]
    public class ODataCreationTests : ODataTestBase
    {
        [TestMethod]
        public async Task OD_POST_Creation()
        {
            await IsolatedODataTestAsync(async () =>
            {
                var testRoot = CreateTestRoot("ODataTestRoot");

                var name = "Content1";
                var encodedName = ContentNamingProvider.GetNameFromDisplayName(name);

                // ACTION 1: Create
                var json = string.Concat(@"models=[{""Name"":""", name, @"""}]");
                var response = await ODataPostAsync("/OData.svc/" + testRoot.Path, "", json).ConfigureAwait(false); ;

                // ASSERT 1
                AssertNoError(response);
                var entity = GetEntity(response);
                Assert.AreEqual(encodedName, entity.Name);
                var node = Node.LoadNode(entity.Id);
                Assert.AreEqual(name, node.Name);
            }).ConfigureAwait(false); ;
        }
        [TestMethod]
        public async Task OD_POST_CreationMissingParent()
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
        /*[TestMethod]
        public void OData_Posting_Creating()
        {
            Test(() =>
            {
                var testRoot = CreateTestRoot("ODataTestRoot");
                CreateTestSite();

                var name = Guid.NewGuid().ToString();
                var displayName = Guid.NewGuid().ToString();
                var path = RepositoryPath.Combine(testRoot.Path, name);
                    //var json = string.Concat(@"models=[{""Id"":"""",""IsFile"":false,""Name"":"""",""DisplayName"":""", displayName, @""",""ModifiedBy"":null,""ModificationDate"":null,""CreationDate"":null,""Actions"":null}]");
                    var json = string.Concat(@"models=[{""Name"":""", name, @""",""DisplayName"":""", displayName,
                    @""",""Index"":41}]");

                var output = new StringWriter();
                var pc = CreatePortalContext("/OData.svc/" + testRoot.Path, "", output);
                var handler = new ODataHandler();
                var stream = CreateRequestStream(json);

                handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);

                var content = Content.Load(path);
                Assert.IsTrue(content.DisplayName == displayName);
            });
        }*/
        /*[TestMethod]
        public void OData_Posting_Creating_ExplicitType()
        {
            Test(() =>
            {
                InstallCarContentType();
                var testRoot = CreateTestRoot("ODataTestRoot");
                CreateTestSite();

                var name = Guid.NewGuid().ToString();
                var path = RepositoryPath.Combine(testRoot.Path, name);
                    //var json = string.Concat(@"models=[{""Id"":"""",""IsFile"":false,""Name"":"""",""DisplayName"":""", displayName, @""",""ModifiedBy"":null,""ModificationDate"":null,""CreationDate"":null,""Actions"":null}]");
                    var json = string.Concat(@"models=[{""__ContentType"":""Car"",""Name"":""", name, @"""}]");

                var output = new StringWriter();
                var pc = CreatePortalContext("/OData.svc/" + testRoot.Path, "", output);
                var handler = new ODataHandler();
                var stream = CreateRequestStream(json);

                handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);

                var content = Content.Load(path);
                Assert.IsTrue(content.ContentType.Name == "Car");
                Assert.IsTrue(content.Name == name);
            });
        }*/
        /*[TestMethod]
        public void OData_Post_References()
        {
            Test(() =>
            {
                var testRoot = CreateTestRoot("ODataTestRoot");
                EnsureReferenceTestStructure(testRoot);
                CreateTestSite();
                var refs = new[] { Repository.Root, Repository.ImsFolder };

                var name1 = Guid.NewGuid().ToString();
                var path1 = RepositoryPath.Combine(testRoot.Path, name1);

                var pathRefs = "[" + String.Join(",", refs.Select(n => "\"" + n.Path + "\"")) + "]";
                var idRefs = "[" + String.Join(",", refs.Select(n => n.Id)) + "]";
                var simpleRefNode = Node.LoadNode(4);

                var json1 = string.Concat(
                    @"models=[{""__ContentType"":""OData_ReferenceTest_ContentHandler"",""Name"":""", name1,
                    @""",""Reference"":", pathRefs, @",""References"":", pathRefs, @",""Reference2"":""",
                    simpleRefNode.Path, @"""}]");

                var output1 = new StringWriter();
                var pc1 = CreatePortalContext("/OData.svc/" + testRoot.Path, "", output1);
                var handler1 = new ODataHandler();
                var stream1 = CreateRequestStream(json1);

                handler1.ProcessRequest(pc1.OwnerHttpContext, "POST", stream1);
                CheckError(output1);

                var node1 = Node.Load<OData_ReferenceTest_ContentHandler>(path1);
                var reloadedRefs1 = "[" + String.Join(",", node1.References.Select(n => +n.Id)) + "]";
                Assert.AreEqual(idRefs, reloadedRefs1);
                Assert.AreEqual(refs[0].Id, node1.Reference.Id);
                Assert.AreEqual(simpleRefNode.Id, node1.Reference2.Id);

                //--------------------------------------------------------------

                var name2 = Guid.NewGuid().ToString();
                var path2 = RepositoryPath.Combine(testRoot.Path, name2);

                var json2 = string.Concat(
                    @"models=[{""__ContentType"":""OData_ReferenceTest_ContentHandler"",""Name"":""", name2,
                    @""",""Reference"":", idRefs, @",""References"":", idRefs, @",""Reference2"":", simpleRefNode.Id,
                    @"}]");

                var output2 = new StringWriter();
                var pc2 = CreatePortalContext("/OData.svc/" + testRoot.Path, "", output2);
                var handler2 = new ODataHandler();
                var stream2 = CreateRequestStream(json2);

                handler2.ProcessRequest(pc2.OwnerHttpContext, "POST", stream2);
                CheckError(output2);

                var node2 = Node.Load<OData_ReferenceTest_ContentHandler>(path2);
                var reloadedRefs2 = "[" + String.Join(",", node2.References.Select(n => n.Id)) + "]";
                Assert.AreEqual(idRefs, reloadedRefs2);
                Assert.AreEqual(refs[0].Id, node2.Reference.Id);
                Assert.AreEqual(simpleRefNode.Id, node2.Reference2.Id);
            });
        }*/

        /*[TestMethod]
        public void OData_Posting_Creating_UnderById()
        {
            Test(() =>
            {
                var testRoot = CreateTestRoot("ODataTestRoot");
                CreateTestSite();

                var name = Guid.NewGuid().ToString();
                var displayName = Guid.NewGuid().ToString();
                var path = RepositoryPath.Combine(testRoot.Path, name);
                    //var json = string.Concat(@"models=[{""Id"":"""",""IsFile"":false,""Name"":"""",""DisplayName"":""", displayName, @""",""ModifiedBy"":null,""ModificationDate"":null,""CreationDate"":null,""Actions"":null}]");
                    var json = string.Concat(@"models=[{""Name"":""", name, @""",""DisplayName"":""", displayName,
                    @""",""Index"":41}]");

                var output = new StringWriter();
                var pc = CreatePortalContext("/OData.svc/content(" + testRoot.Id + ")", "", output);
                var handler = new ODataHandler();
                var stream = CreateRequestStream(json);

                handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);

                var content = Content.Load(path);
                Assert.IsTrue(content.DisplayName == displayName);

            });
        }*/
        /*[TestMethod]
        public void OData_Posting_Creating_ExplicitType_UnderById()
        {
            Test(() =>
            {
                InstallCarContentType();
                var testRoot = CreateTestRoot("ODataTestRoot");
                CreateTestSite();

                var name = Guid.NewGuid().ToString();
                var path = RepositoryPath.Combine(testRoot.Path, name);
                    //var json = string.Concat(@"models=[{""Id"":"""",""IsFile"":false,""Name"":"""",""DisplayName"":""", displayName, @""",""ModifiedBy"":null,""ModificationDate"":null,""CreationDate"":null,""Actions"":null}]");
                    var json = string.Concat(@"models=[{""__ContentType"":""Car"",""Name"":""", name, @"""}]");

                var output = new StringWriter();
                var pc = CreatePortalContext("/OData.svc/content(" + testRoot.Id + ")", "", output);
                var handler = new ODataHandler();
                var stream = CreateRequestStream(json);

                handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);

                var content = Content.Load(path);
                Assert.IsTrue(content.ContentType.Name == "Car");
                Assert.IsTrue(content.Name == name);

            });
        }*/

        //UNDONE:ODATA:TEST Fix AclEditor OD_POST_TemplatedCreation
        /*[TestMethod]
        public async Task OD_POST_TemplatedCreation()
        {
            await IsolatedODataTestAsync(async () =>
            {
                InstallCarContentType();
                var testRoot = CreateTestRoot("ODataTestRoot");
                EnsureTemplateStructure();

                var name = Guid.NewGuid().ToString();
                var path = RepositoryPath.Combine(testRoot.Path, name);
                var requestBody = string.Concat(
                    @"models=[{""__ContentType"":""Car"",""__ContentTemplate"":""Template3"",""Name"":""", name,
                    @""",""EngineSize"":""3.5 l""}]");

                //var output = new StringWriter();
                //var pc = CreatePortalContext("/OData.svc/" + testRoot.Path, "", output);
                //var handler = new ODataHandler();
                //var stream = CreateRequestStream(json);
                //handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);

                // ACTION
                var response = await ODataPostAsync(
                        "/OData.svc" + testRoot.Path, "", requestBody)
                    .ConfigureAwait(false); ;

                // ASSERT
                AssertNoError(response);

                var content = Content.Load(path);
                Assert.AreEqual("Car", content.ContentType.Name);
                Assert.AreEqual(name, content.Name);
                Assert.AreEqual("TestCar3", content["Make"]);
                Assert.AreEqual("Template3", content["Model"]);
                Assert.AreEqual("3.5 l", content["EngineSize"]);
            }).ConfigureAwait(false);
        }*/
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

        /*[TestMethod]
        public void OData_FIX_InconsistentNameAfterCreating()
        {
            Test(() =>
            {
                InstallCarContentType();
                var testRoot = CreateTestRoot("ODataTestRoot");

                var name = Guid.NewGuid().ToString();
                var path = RepositoryPath.Combine(testRoot.Path, name);
                var json = string.Concat(@"models=[{""__ContentType"":""Car"",""Name"":""", name, @"""}]");

                ODataEntity entity;
                    //string result;
                    CreateTestSite();

                var names = new string[3];
                for (int i = 0; i < 3; i++)
                {
                    using (var output = new StringWriter())
                    {
                        var pc = CreatePortalContext(String.Concat("/OData.svc", ODataHandler.GetEntityUrl(testRoot.Path)),
                            "", output);
                        var handler = new ODataHandler();
                        var stream = CreateRequestStream(json);
                        handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
                        output.Flush();
                            //result = GetStringResult(output);
                            entity = GetEntity(output);
                    }
                    names[i] = entity.Name;
                }

                Assert.AreNotEqual(names[0], names[1]);
                Assert.AreNotEqual(names[0], names[2]);
                Assert.AreNotEqual(names[1], names[2]);
            });
        }*/

    }
}
