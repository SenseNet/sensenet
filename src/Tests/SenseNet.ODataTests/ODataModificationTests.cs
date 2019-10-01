using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository;
using SenseNet.OData;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.ODataTests
{
    [TestClass]
    public class ODataModificationTests : ODataTestBase
    {
        /* ===================================================================== PUT */

        [TestMethod]
        public async Task OD_PUT_Rename()
        {
            await IsolatedODataTestAsync(async () =>
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
                var response = await ODataPutAsync("/OData.svc" + content.Path, "", requestBody);

                // ASSERT
                AssertNoError(response);
                var content1 = Content.Load(id);
                // Posted value
                Assert.AreEqual(newName, content1.Name);
                Assert.AreEqual(newDisplayName, content1.DisplayName);
                // Default value because of PUT
                Assert.AreEqual(0, content1.Index);
            });
        }
        [TestMethod]
        public async Task OD_PUT_RenameMissing()
        {
            await ODataTestAsync(async () =>
            {
                // ALIGN
                var requestBody = @"models=[{""Name"": ""newName"", ""DisplayName"": ""newDisplayName""}]";

                // ACTION
                var response = await ODataPutAsync("/OData.svc/Root('UnknownContent')'", "", requestBody);

                // ASSERT
                Assert.AreEqual(404, response.StatusCode);
            });
        }
        [TestMethod]
        public async Task OD_PUT_IllegalInvoke()
        {
            await ODataTestAsync(async () =>
            {
                // ALIGN
                var requestBody = @"models=[{""Name"": ""newName"", ""DisplayName"": ""newDisplayName""}]";

                // ACTION
                var response = await ODataPutAsync("/OData.svc/Root('UnknownContent')/Id'", "", requestBody);

                // ASSERT
                var error = GetError(response);
                Assert.AreEqual(ODataExceptionCode.IllegalInvoke, error.Code);
            });
        }

        /*[TestMethod]
public void OData_Put_Modifying()
{
    Test(() =>
    {
        InstallCarContentType();
        var testRoot = CreateTestRoot("ODataTestRoot");
        CreateTestSite();

        var name = Guid.NewGuid().ToString();
        var content = Content.CreateNew("Car", testRoot, name);
        content.DisplayName = "vadalma";
        var defaultMake = (string)content["Make"];
        content["Make"] = "Not default";
        content.Save();
        var id = content.Id;
        var path = content.Path;
        var url = GetUrl(content.Path);

        var newDisplayName = "szelídgesztenye";

        var json = String.Concat(@"models=[{
""DisplayName"": """, newDisplayName, @""",
""ModificationDate"": ""2012-10-11T03:52:01.637Z"",
""Index"": 42
}]");

        var output = new StringWriter();
        var pc = CreatePortalContext("/OData.svc/" + path, "", output);
        var handler = new ODataHandler();
        var stream = CreateRequestStream(json);

        handler.ProcessRequest(pc.OwnerHttpContext, "PUT", stream);

        var c = Content.Load(id);
        var creationDateStr = c.ContentHandler.CreationDate.ToString("yyyy-MM-dd HH:mm:ss.ffff");
        var modificationDateStr = c.ContentHandler.ModificationDate.ToString("yyyy-MM-dd HH:mm:ss.ffff");

        Assert.IsTrue(c.DisplayName == newDisplayName);
        Assert.IsTrue(modificationDateStr == "2012-10-11 03:52:01.6370");
        Assert.IsTrue(c.ContentHandler.Index == 42);
        Assert.IsTrue((string)c["Make"] == null);
    });
}*/
        /*[TestMethod]
        public void OData_Put_ModifyingById()
        {
            Test(() =>
            {
                InstallCarContentType();
                var testRoot = CreateTestRoot("ODataTestRoot");
                CreateTestSite();

                var name = Guid.NewGuid().ToString();
                var content = Content.CreateNew("Car", testRoot, name);
                content.DisplayName = "vadalma";
                var defaultMake = (string)content["Make"];
                content["Make"] = "Not default";
                content.Save();
                var id = content.Id;
                var path = content.Path;
                var url = GetUrl(content.Path);

                var newDisplayName = "szelídgesztenye";

                var json = String.Concat(@"models=[{
      ""DisplayName"": """, newDisplayName, @""",
      ""ModificationDate"": ""2012-10-11T03:52:01.637Z"",
      ""Index"": 42
    }]");

                var output = new StringWriter();
                var pc = CreatePortalContext("/OData.svc/content(" + id + ")", "", output);
                var handler = new ODataHandler();
                var stream = CreateRequestStream(json);

                handler.ProcessRequest(pc.OwnerHttpContext, "PUT", stream);

                var c = Content.Load(id);
                var creationDateStr = c.ContentHandler.CreationDate.ToString("yyyy-MM-dd HH:mm:ss.ffff");
                var modificationDateStr = c.ContentHandler.ModificationDate.ToString("yyyy-MM-dd HH:mm:ss.ffff");

                Assert.IsTrue(c.DisplayName == newDisplayName);
                Assert.IsTrue(modificationDateStr == "2012-10-11 03:52:01.6370");
                Assert.IsTrue(c.ContentHandler.Index == 42);
                Assert.IsTrue((string)c["Make"] == null);

            });
        }*/

        /* ===================================================================== PATCH */

        [TestMethod]
        public async Task OD_PATCH_Rename()
        {
            await IsolatedODataTestAsync(async () =>
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
                var response = await ODataPatchAsync("/OData.svc" + content.Path, "", requestBody);

                // ASSERT
                AssertNoError(response);
                var content1 = Content.Load(id);
                Assert.AreEqual(newName, content1.Name);
                Assert.AreEqual(newDisplayName, content1.DisplayName);
                Assert.AreEqual(42, content1.Index);
            });
        }
        [TestMethod]
        public async Task OD_PATCH_RenameMissing()
        {
            await ODataTestAsync(async () =>
            {
                // ALIGN
                var requestBody = @"models=[{""Name"": ""newName"", ""DisplayName"": ""newDisplayName""}]";

                // ACTION
                var response = await ODataPatchAsync("/OData.svc/Root('UnknownContent')'", "", requestBody);

                // ASSERT
                Assert.AreEqual(404, response.StatusCode);
            });
        }
        [TestMethod]
        public async Task OD_PATCH_IllegalInvoke()
        {
            await ODataTestAsync(async () =>
            {
                // ALIGN
                var requestBody = @"models=[{""Name"": ""newName"", ""DisplayName"": ""newDisplayName""}]";

                // ACTION
                var response = await ODataPatchAsync("/OData.svc/Root('UnknownContent')/Id'", "", requestBody);

                // ASSERT
                var error = GetError(response);
                Assert.AreEqual(ODataExceptionCode.IllegalInvoke, error.Code);
            });
        }

        /*[TestMethod]
       public void OData_Patch_Modifying()
       {
           Test(() =>
           {
               InstallCarContentType();
               var testRoot = CreateTestRoot("ODataTestRoot");
               CreateTestSite();

               var name = Guid.NewGuid().ToString();
               var content = Content.CreateNew("Car", testRoot, name);
               content.DisplayName = "vadalma";
               var defaultMake = (string)content["Make"];
               content["Make"] = "Not default";
               content.Save();
               var id = content.Id;
               var path = content.Path;
               var url = GetUrl(content.Path);

               var newDisplayName = "szelídgesztenye";

               var json = String.Concat(@"models=[{
     ""DisplayName"": """, newDisplayName, @""",
     ""ModificationDate"": ""2012-10-11T03:52:01.637Z"",
     ""Index"": 42
   }]");

               var output = new StringWriter();
               var pc = CreatePortalContext("/OData.svc/" + path, "", output);
               var handler = new ODataHandler();
               var stream = CreateRequestStream(json);

               handler.ProcessRequest(pc.OwnerHttpContext, "PATCH", stream);

               var c = Content.Load(id);
               var creationDateStr = c.ContentHandler.CreationDate.ToString("yyyy-MM-dd HH:mm:ss.ffff");
               var modificationDateStr = c.ContentHandler.ModificationDate.ToString("yyyy-MM-dd HH:mm:ss.ffff");

               Assert.IsTrue(c.DisplayName == newDisplayName);
               Assert.IsTrue(modificationDateStr == "2012-10-11 03:52:01.6370");
               Assert.IsTrue(c.ContentHandler.Index == 42);
               Assert.IsTrue((string)c["Make"] == "Not default");
           });
       }*/
        /*[TestMethod]
         public void OData_Patch_ModifyingById()
         {
             Test(() =>
             {
                 InstallCarContentType();
                 var testRoot = CreateTestRoot("ODataTestRoot");
                 CreateTestSite();

                 var name = Guid.NewGuid().ToString();
                 var content = Content.CreateNew("Car", testRoot, name);
                 content.DisplayName = "vadalma";
                 var defaultMake = (string)content["Make"];
                 content["Make"] = "Not default";
                 content.Save();
                 var id = content.Id;
                 var path = content.Path;
                 var url = GetUrl(content.Path);

                 var newDisplayName = "szelídgesztenye";

                 var json = String.Concat(@"models=[{
       ""DisplayName"": """, newDisplayName, @""",
       ""ModificationDate"": ""2012-10-11T03:52:01.637Z"",
       ""Index"": 42
     }]");

                 var output = new StringWriter();
                 var pc = CreatePortalContext("/OData.svc/content(" + id + ")", "", output);
                 var handler = new ODataHandler();
                 var stream = CreateRequestStream(json);

                 handler.ProcessRequest(pc.OwnerHttpContext, "PATCH", stream);

                 var c = Content.Load(id);
                 var creationDateStr = c.ContentHandler.CreationDate.ToString("yyyy-MM-dd HH:mm:ss.ffff");
                 var modificationDateStr = c.ContentHandler.ModificationDate.ToString("yyyy-MM-dd HH:mm:ss.ffff");

                 Assert.IsTrue(c.DisplayName == newDisplayName);
                 Assert.IsTrue(modificationDateStr == "2012-10-11 03:52:01.6370");
                 Assert.IsTrue(c.ContentHandler.Index == 42);
                 Assert.IsTrue((string)c["Make"] == "Not default");

             });
         }*/
        /*[TestMethod]
        public void OData_NameEncoding_CreateAndRename()
        {
            Test(() =>
            {
                var testRoot = CreateTestRoot("ODataTestRoot");
                CreateTestSite();

                var guid = Guid.NewGuid().ToString().Replace("-", "");
                var name = "*_|" + guid;
                var encodedName = ContentNamingProvider.GetNameFromDisplayName(name);
                var newName = ContentNamingProvider.GetNameFromDisplayName("___" + guid);

                    // creating

                    var json = string.Concat(@"models=[{""Name"":""", name, @"""}]");

                var output = new StringWriter();
                var pc = CreatePortalContext("/OData.svc/" + testRoot.Path, "", output);
                var handler = new ODataHandler();
                var stream = CreateRequestStream(json);

                handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
                CheckError(output);
                var entity = GetEntity(output);
                Assert.AreEqual(encodedName, entity.Name);

                    // renaming

                    json = string.Concat(@"models=[{""Name"":""", newName, @"""}]");

                output = new StringWriter();
                pc = CreatePortalContext("/OData.svc/" + entity.Path, "", output);
                handler = new ODataHandler();
                stream = CreateRequestStream(json);

                handler.ProcessRequest(pc.OwnerHttpContext, "PATCH", stream);
                CheckError(output);

                var node = Node.LoadNode(entity.Id);
                Assert.AreEqual(newName, node.Name);
            });
        }*/
        /*[TestMethod]
        public void OData_FIX_ModifyWithInvisibleParent()
        {
            Test(() =>
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

                CreateTestSite();
                try
                {
                    User.Current = User.Visitor;

                    ODataEntity entity;
                    using (var output = new StringWriter())
                    {
                        var json = String.Concat(@"models=[{""Index"": 42}]");
                        var pc = CreatePortalContext("/OData.svc" + node.Path, "", output);
                        var handler = new ODataHandler();
                        var stream = CreateRequestStream(json);
                        handler.ProcessRequest(pc.OwnerHttpContext, "PATCH", stream);
                        CheckError(output);
                        entity = GetEntity(output);
                    }
                    node = Node.Load<Folder>(node.Id);
                    Assert.AreEqual(42, entity.Index);
                    Assert.AreEqual(42, node.Index);
                }
                finally
                {
                    User.Current = savedUser;
                }
            });
        }*/

        /* ===================================================================== MERGE */

        /*[TestMethod]
public void OData_Merge_Modifying()
{
    Test(() =>
    {
        InstallCarContentType();
        var testRoot = CreateTestRoot("ODataTestRoot");
        CreateTestSite();

        var name = Guid.NewGuid().ToString();
        var content = Content.CreateNew("Car", testRoot, name);
        content.DisplayName = "vadalma";
        var defaultMake = (string)content["Make"];
        content["Make"] = "Not default";
        content.Save();
        var id = content.Id;
        var path = content.Path;
        var url = GetUrl(content.Path);

        var newDisplayName = "szelídgesztenye";

        var json = String.Concat(@"models=[{
""DisplayName"": """, newDisplayName, @""",
""ModificationDate"": ""2012-10-11T03:52:01.637Z"",
""Index"": 42
}]");

        var output = new StringWriter();
        var pc = CreatePortalContext("/OData.svc/" + path, "", output);
        var handler = new ODataHandler();
        var stream = CreateRequestStream(json);

        handler.ProcessRequest(pc.OwnerHttpContext, "MERGE", stream);

        var c = Content.Load(id);
        var creationDateStr = c.ContentHandler.CreationDate.ToString("yyyy-MM-dd HH:mm:ss.ffff");
        var modificationDateStr = c.ContentHandler.ModificationDate.ToString("yyyy-MM-dd HH:mm:ss.ffff");

        Assert.IsTrue(c.DisplayName == newDisplayName);
        Assert.IsTrue(modificationDateStr == "2012-10-11 03:52:01.6370");
        Assert.IsTrue(c.ContentHandler.Index == 42);
        Assert.IsTrue((string)c["Make"] == "Not default");
    });
}*/
        /*[TestMethod]
        public void OData_Merge_ModifyingById()
        {
            Test(() =>
            {
                InstallCarContentType();
                var testRoot = CreateTestRoot("ODataTestRoot");
                CreateTestSite();

                var name = Guid.NewGuid().ToString();
                var content = Content.CreateNew("Car", testRoot, name);
                content.DisplayName = "vadalma";
                var defaultMake = (string)content["Make"];
                content["Make"] = "Not default";
                content.Save();
                var id = content.Id;
                var path = content.Path;
                var url = GetUrl(content.Path);

                var newDisplayName = "szelídgesztenye";

                var json = String.Concat(@"models=[{
      ""DisplayName"": """, newDisplayName, @""",
      ""ModificationDate"": ""2012-10-11T03:52:01.637Z"",
      ""Index"": 42
    }]");

                var output = new StringWriter();
                var pc = CreatePortalContext("/OData.svc/content(" + id + ")", "", output);
                var handler = new ODataHandler();
                var stream = CreateRequestStream(json);

                handler.ProcessRequest(pc.OwnerHttpContext, "MERGE", stream);

                var c = Content.Load(id);
                var creationDateStr = c.ContentHandler.CreationDate.ToString("yyyy-MM-dd HH:mm:ss.ffff");
                var modificationDateStr = c.ContentHandler.ModificationDate.ToString("yyyy-MM-dd HH:mm:ss.ffff");

                Assert.IsTrue(c.DisplayName == newDisplayName);
                Assert.IsTrue(modificationDateStr == "2012-10-11 03:52:01.6370");
                Assert.IsTrue(c.ContentHandler.Index == 42);
                Assert.IsTrue((string)c["Make"] == "Not default");

            });
        }*/
    }
}
