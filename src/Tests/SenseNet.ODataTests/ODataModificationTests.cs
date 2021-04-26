using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.OData;
using SenseNet.Security;
using Task = System.Threading.Tasks.Task;
// ReSharper disable StringLiteralTypo

namespace SenseNet.ODataTests
{
    [TestClass]
    public class ODataModificationTests : ODataTestBase
    {
        /* ===================================================================== PUT */

        [TestMethod]
        public async Task OD_PUT_Rename()
        {
            await ODataTestAsync(async () =>
            {
                // ALIGN
                InstallCarContentType();
                var testRoot = CreateTestRoot("ODataTestRoot");

                var content = Content.CreateNew("Car", testRoot, "ORIG");
                content.DisplayName = "Initial DisplayName";
                content.Index = 42;
                content.Save();
                var id = content.Id;
                var path = content.Path;

                var newName = "NEW";
                var newDisplayName = "New DisplayName";

                var requestBody= String.Concat(@"models=[{
                          ""Name"": """, newName, @""",
                          ""DisplayName"": """, newDisplayName, @"""
                        }]");

                // ACTION
                var response = await ODataPutAsync(
                    "/OData.svc" + content.Path, "", requestBody)
                    .ConfigureAwait(false); ;

                // ASSERT
                AssertNoError(response);
                var content1 = Content.Load(id);
                // Posted value
                Assert.AreEqual(newName, content1.Name);
                Assert.AreEqual(newDisplayName, content1.DisplayName);
                // Default value because of PUT
                Assert.AreEqual(0, content1.Index);
            }).ConfigureAwait(false); ;
        }
        [TestMethod]
        public async Task OD_PUT_RenameMissing()
        {
            await ODataTestAsync(async () =>
            {
                // ALIGN
                var requestBody = @"models=[{""Name"": ""newName"", ""DisplayName"": ""newDisplayName""}]";

                // ACTION
                var response = await ODataPutAsync(
                    "/OData.svc/Root('UnknownContent')'", "", requestBody)
                    .ConfigureAwait(false); ;

                // ASSERT
                Assert.AreEqual(404, response.StatusCode);
            }).ConfigureAwait(false); ;
        }
        [TestMethod]
        public async Task OD_PUT_IllegalInvoke()
        {
            await ODataTestAsync(async () =>
            {
                // ALIGN
                var requestBody = @"models=[{""Name"": ""newName"", ""DisplayName"": ""newDisplayName""}]";

                // ACTION
                var response = await ODataPutAsync(
                    "/OData.svc/Root('UnknownContent')/Id'", "", requestBody)
                    .ConfigureAwait(false); ;

                // ASSERT
                var error = GetError(response);
                Assert.AreEqual(ODataExceptionCode.IllegalInvoke, error.Code);
            }).ConfigureAwait(false); ;
        }

        [TestMethod]
        public async Task OD_PUT_Modifying()
        {
            await ModifyingTest("PUT", false);
        }

        [TestMethod]
        public async Task OD_PUT_ModifyingById()
        {
            await ModifyingTest("PUT", true);
        }

        /* ===================================================================== PATCH */

        [TestMethod]
        public async Task OD_PATCH_Rename()
        {
            await ODataTestAsync(async () =>
            {
                // ALIGN
                InstallCarContentType();
                var testRoot = CreateTestRoot("ODataTestRoot");

                var content = Content.CreateNew("Car", testRoot, "ORIG");
                content.DisplayName = "Initial DisplayName";
                content.Index = 42;
                content.Save();
                var id = content.Id;
                var path = content.Path;

                var newName = "NEW";
                var newDisplayName = "New DisplayName";

                var requestBody = String.Concat(@"models=[{
                          ""Name"": """, newName, @""",
                          ""DisplayName"": """, newDisplayName, @"""
                        }]");

                // ACTION
                var response = await ODataPatchAsync(
                    "/OData.svc" + content.Path, "", requestBody)
                    .ConfigureAwait(false); ;

                // ASSERT
                AssertNoError(response);
                var content1 = Content.Load(id);
                Assert.AreEqual(newName, content1.Name);
                Assert.AreEqual(newDisplayName, content1.DisplayName);
                Assert.AreEqual(42, content1.Index);
            }).ConfigureAwait(false); ;
        }
        [TestMethod]
        public async Task OD_PATCH_RenameMissing()
        {
            await ODataTestAsync(async () =>
            {
                // ALIGN
                var requestBody = @"models=[{""Name"": ""newName"", ""DisplayName"": ""newDisplayName""}]";

                // ACTION
                var response = await ODataPatchAsync(
                    "/OData.svc/Root('UnknownContent')'", "", requestBody)
                    .ConfigureAwait(false); ;

                // ASSERT
                Assert.AreEqual(404, response.StatusCode);
            }).ConfigureAwait(false); ;
        }
        [TestMethod]
        public async Task OD_PATCH_IllegalInvoke()
        {
            await ODataTestAsync(async () =>
            {
                // ALIGN
                var requestBody = @"models=[{""Name"": ""newName"", ""DisplayName"": ""newDisplayName""}]";

                // ACTION
                var response = await ODataPatchAsync(
                    "/OData.svc/Root('UnknownContent')/Id'", "", requestBody)
                    .ConfigureAwait(false); ;

                // ASSERT
                var error = GetError(response);
                Assert.AreEqual(ODataExceptionCode.IllegalInvoke, error.Code);
            }).ConfigureAwait(false); ;
        }

        [TestMethod]
        public async Task OD_PATCH_Modifying()
        {
            await ModifyingTest("PATCH", false);
        }
        [TestMethod]
        public async Task OD_PATCH_ModifyingById()
        {
            await ModifyingTest("PATCH", true);
        }

        [TestMethod]
        public async Task OD_FIX_NameEncoding_CreateAndRename()
        {
            await ODataTestAsync(async () =>
            {
                var testRoot = CreateTestRoot("ODataTestRoot");

                var guid = Guid.NewGuid().ToString().Replace("-", "");
                var name = "*_|" + guid;
                var encodedName = ContentNamingProvider.GetNameFromDisplayName(name);
                var newName = ContentNamingProvider.GetNameFromDisplayName("___" + guid);

                // ACTION 1: Create
                var json = string.Concat(@"models=[{""Name"":""", name, @"""}]");
                var response = await ODataPostAsync("/OData.svc/" + testRoot.Path, "", json).ConfigureAwait(false); ;

                // ASSERT 1
                AssertNoError(response);
                var entity = GetEntity(response);
                Assert.AreEqual(encodedName, entity.Name);

                // ACTION 2: Rename
                json = string.Concat(@"models=[{""Name"":""", newName, @"""}]");
                response = await ODataPatchAsync("/OData.svc/" + testRoot.Path, "", json).ConfigureAwait(false); ;

                // ASSERT 2
                AssertNoError(response);
                entity = GetEntity(response);
                var node = Node.LoadNode(entity.Id);
                Assert.AreEqual(newName, node.Name);
            }).ConfigureAwait(false); ;
        }
        [TestMethod]
        public async Task OD_FIX_ModifyWithInvisibleParent()
        {
            await ODataTestAsync(
                builder => { builder.AddAllTestPolicies(); },
                async () =>
            {
                var testRoot = CreateTestRoot("ODataTestRoot");
                var root = new Folder(testRoot) { Name = Guid.NewGuid().ToString() };
                root.Save();
                var node = new Folder(root) { Name = Guid.NewGuid().ToString() };
                node.Save();

                SecurityHandler.CreateAclEditor()
                    .BreakInheritance(root.Id, new[] { EntryType.Normal })
                    .ClearPermission(root.Id, User.Visitor.Id, false, PermissionType.See)
                    .Allow(node.Id, User.Visitor.Id, false, PermissionType.Save)
                    .Apply();

                var savedUser = User.Current;

                try
                {
                    User.Current = User.Visitor;

                    var json = @"models=[{""Index"": 42}]";
                    var response = await ODataPatchAsync("/OData.svc" + node.Path, "", json).ConfigureAwait(false); ;

                    AssertNoError(response);
                    var entity = GetEntity(response);
                    node = Node.Load<Folder>(node.Id);
                    Assert.AreEqual(42, entity.Index);
                    Assert.AreEqual(42, node.Index);
                }
                finally
                {
                    User.Current = savedUser;
                }
            }).ConfigureAwait(false); ;
        }

        /* ===================================================================== MERGE */

        [TestMethod]
        public async Task OD_MERGE_Modifying()
        {
            await ModifyingTest("MERGE", false);
        }
        [TestMethod]
        public async Task OD_MERGE_ModifyingById()
        {
            await ModifyingTest("MERGE", true);
        }

        /* ===================================================================== Common */

        private async Task ModifyingTest(string httpMethod, bool byId)
        {
            await ODataTestAsync(async () =>
            {
                // ARRANGE
                InstallCarContentType();
                var testRoot = CreateTestRoot("ODataTestRoot");

                var name = Guid.NewGuid().ToString();
                var content = Content.CreateNew("Car", testRoot, name);
                content.DisplayName = "vadalma";
                var defaultMake = (string)content["Make"];
                content["Make"] = "Not default";
                content.Save();
                var id = content.Id;
                var path = content.Path;

                var newDisplayName = "szelídgesztenye";

                var json = String.Concat(@"models=[{
""DisplayName"": """, newDisplayName, @""",
""ModificationDate"": ""2012-10-11T03:52:01.637Z"",
""Index"": 42
}]");

                var resource = byId ? "/OData.svc/content(" + id + ")" : "/OData.svc" + path;

                // ACTION
                ODataResponse response;
                var queryString = "?metadata=no&$select=Id,Name,Path";
                switch (httpMethod)
                {
                    case "PUT":
                        response = await ODataPutAsync(resource, queryString, json).ConfigureAwait(false); ;
                        break;
                    case "PATCH":
                        response = await ODataPatchAsync(resource, queryString, json).ConfigureAwait(false); ;
                        break;
                    case "MERGE":
                        response = await ODataMergeAsync(resource, queryString, json).ConfigureAwait(false); ;
                        break;
                    default:
                        throw new NotImplementedException($"HttpMethod {httpMethod} is not implemented.");
                }

                // ASSERT
                AssertNoError(response);
                var entity = GetEntity(response);
                Assert.AreEqual(3, entity.AllProperties.Count);
                Assert.AreEqual(id, entity.Id);
                Assert.AreEqual(name, entity.Name);
                Assert.AreEqual(path, entity.Path);

                var c = Content.Load(id);
                var creationDateStr = c.ContentHandler.CreationDate.ToString("yyyy-MM-dd HH:mm:ss.ffff");
                var modificationDateStr = c.ContentHandler.ModificationDate.ToString("yyyy-MM-dd HH:mm:ss.ffff");

                Assert.IsTrue(c.DisplayName == newDisplayName);
                Assert.IsTrue(modificationDateStr == "2012-10-11 03:52:01.6370");
                Assert.IsTrue(c.ContentHandler.Index == 42);
                if(httpMethod == "PUT")
                    Assert.IsTrue((string)c["Make"] == null);
                else
                    Assert.IsTrue((string)c["Make"] == "Not default");
            }).ConfigureAwait(false); ;
        }

    }
}
