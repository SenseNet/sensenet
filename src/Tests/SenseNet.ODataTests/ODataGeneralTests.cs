using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Fields;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.OData;
using Task = System.Threading.Tasks.Task;
// ReSharper disable IdentifierTypo
// ReSharper disable StringLiteralTypo
// ReSharper disable CommentTypo
// ReSharper disable JoinDeclarationAndInitializer

namespace SenseNet.ODataTests
{
    [TestClass]
    public class ODataGeneralTests : ODataTestBase
    {
        [TestMethod]
        public async Task OD_GET_NoRequest()
        {
            await ODataTestAsync(async () =>
            {
                // ARRANGE
                var httpContext = new DefaultHttpContext();
                var request = httpContext.Request;
                request.Method = "GET";
                httpContext.Response.Body = new MemoryStream();

                // ACTION
                var odata = new ODataMiddleware(null);
                await odata.ProcessRequestAsync(httpContext, null).ConfigureAwait(false);

                // ASSERT
                var responseOutput = httpContext.Response.Body;
                responseOutput.Seek(0, SeekOrigin.Begin);
                string output;
                using (var reader = new StreamReader(responseOutput))
                    output = await reader.ReadToEndAsync().ConfigureAwait(false);

                var response = new ODataResponse { Result = output, StatusCode = httpContext.Response.StatusCode };

                var error = GetError(response);
                Assert.AreEqual("The Request is not an OData request.", error.Message);
                Assert.AreEqual("ODataException", error.ExceptionType);
                Assert.AreEqual(400, response.StatusCode);

            }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task OD_GET_ServiceDocument()
        {
            await ODataTestAsync(async () =>
            {
                // ACTION
                var response = await ODataGetAsync("/OData.svc", "")
                    .ConfigureAwait(false);

                // ASSERT
                Assert.AreEqual("{\"d\":{\"EntitySets\":[\"Root\"]}}", response.Result);
            }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task OD_GET_Metadata_Global()
        {
            await ODataTestAsync(async () =>
            {
                // ACTION
                var response = await ODataGetAsync(
                    "/OData.svc/$metadata",
                    "")
                    .ConfigureAwait(false);

                // ASSERT
                var metaXml = GetMetadataXml(response.Result, out var nsmgr);

                Assert.IsNotNull(metaXml);
                var allTypes = metaXml.SelectNodes("//x:EntityType", nsmgr);
                Assert.IsNotNull(allTypes);
                Assert.AreEqual(ContentType.GetContentTypes().Length, allTypes.Count);
                var rootTypes = metaXml.SelectNodes("//x:EntityType[not(@BaseType)]", nsmgr);
                Assert.IsNotNull(rootTypes);
                foreach (XmlElement node in rootTypes)
                {
                    var hasKey = node.SelectSingleNode("x:Key", nsmgr) != null;
                    var hasId = node.SelectSingleNode("x:Property[@Name = 'Id']", nsmgr) != null;
                    Assert.IsTrue(hasId == hasKey);
                }
                foreach (XmlElement node in metaXml.SelectNodes("//x:EntityType[@BaseType]", nsmgr))
                {
                    var hasKey = node.SelectSingleNode("x:Key", nsmgr) != null;
                    //var hasId = node.SelectSingleNode("x:Property[@Name = 'Id']", nsmgr) != null;
                    Assert.IsFalse(hasKey);
                }
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_GET_Metadata_Entity()
        {
            await ODataTestAsync(async () =>
            {
                // ACTION
                var response = await ODataGetAsync(
                    "/OData.svc/Root/IMS/BuiltIn/Portal/$metadata",
                    "")
                    .ConfigureAwait(false);

                // ASSERT
                var metaXml = GetMetadataXml(response.Result, out var nsmgr);
                var allTypes = metaXml.SelectNodes("//x:EntityType", nsmgr);
                Assert.IsTrue(allTypes.Count < ContentType.GetContentTypes().Length);
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_GET_Metadata_MissingEntity()
        {
            await ODataTestAsync(async () =>
            {
                // ACTION
                var response = await ODataGetAsync(
                    "/OData.svc/Root/HiEveryBody/$metadata", "")
                    .ConfigureAwait(false);

                // ASSERT: full metadata
                var metaXml = GetMetadataXml(response.Result, out var nsmgr);

                var allTypes = metaXml.SelectNodes("//x:EntityType", nsmgr);
                Assert.IsNotNull(allTypes);
                Assert.AreEqual(ContentType.GetContentTypes().Length, allTypes.Count);
            }).ConfigureAwait(false);
        }
        //TODO: Remove inconclusive test result and implement this test.
        /*//[TestMethod]*/
        /*public void OData_Metadata_Instance_Entity()
        {
            Assert.Inconclusive("InMemorySchemaWriter.CreatePropertyType is partially implemented.");

            Test(() =>
            {
                string listDef = @"<?xml version='1.0' encoding='utf-8'?>
    <ContentListDefinition xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentListDefinition'>
    	<Fields>
    		<ContentListField name='#ListField1' type='ShortText'>
    			<Configuration>
    				<MaxLength>42</MaxLength>
    			</Configuration>
    		</ContentListField>
    	</Fields>
    </ContentListDefinition>
    ";
                InstallCarContentType();
                var testRoot = CreateTestRoot("ODataTestRoot");
                var listContent = Content.CreateNew("ContentList", testRoot, Guid.NewGuid().ToString());
                var list = (ContentList)listContent.ContentHandler;
                list.AllowChildTypes(new[]
                    {ContentType.GetByName("Folder"), ContentType.GetByName("File"), ContentType.GetByName("Car")});
                list.ContentListDefinition = listDef;
                listContent.Save();

                var itemFolder = Content.CreateNew("Folder", listContent.ContentHandler, Guid.NewGuid().ToString());
                itemFolder.Save();
                var itemContent = Content.CreateNew("Car", itemFolder.ContentHandler, Guid.NewGuid().ToString());
                itemContent.Save();

                CreateTestSite();

                XmlNamespaceManager nsmgr;
                XmlDocument metaXml;
                using (var output = new StringWriter())
                {
                    var pc = CreatePortalContext(
                        String.Concat("/OData.svc", ODataHandler.GetEntityUrl(itemContent.Path), "/$metadata"), "",
                        output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    output.Flush();
                    var src = GetStringResult(output);
                    metaXml = GetMetadataXml(src, out nsmgr);
                }
                var allTypes = metaXml.SelectNodes("//x:EntityType", nsmgr);
                Assert.IsTrue(allTypes.Count == 1);
                var listProps = metaXml.SelectNodes("//x:EntityType/x:Property[@Name='#ListField1']", nsmgr);
                Assert.IsTrue(listProps.Count == 1);
            });
        }*/
        //TODO: Remove inconclusive test result and implement this test.
        /*//[TestMethod]*/
        /*public void OData_Metadata_Instance_Collection()
        {
            Assert.Inconclusive("InMemorySchemaWriter.CreatePropertyType is partially implemented.");

            Test(() =>
            {
                string listDef = @"<?xml version='1.0' encoding='utf-8'?>
    <ContentListDefinition xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentListDefinition'>
    	<Fields>
    		<ContentListField name='#ListField1' type='ShortText'>
    			<Configuration>
    				<MaxLength>42</MaxLength>
    			</Configuration>
    		</ContentListField>
    	</Fields>
    </ContentListDefinition>
    ";
                InstallCarContentType();
                var testRoot = CreateTestRoot("ODataTestRoot");
                var listContent = Content.CreateNew("ContentList", testRoot, Guid.NewGuid().ToString());
                var list = (ContentList)listContent.ContentHandler;
                list.AllowChildTypes(new[]
                    {ContentType.GetByName("Folder"), ContentType.GetByName("File"), ContentType.GetByName("Car")});
                list.ContentListDefinition = listDef;
                listContent.Save();

                var itemFolder = Content.CreateNew("Folder", listContent.ContentHandler, Guid.NewGuid().ToString());
                itemFolder.Save();

                CreateTestSite();

                XmlNamespaceManager nsmgr;
                XmlDocument metaXml;
                string src = null;
                using (var output = new StringWriter())
                {
                    var pc = CreatePortalContext(String.Concat("/OData.svc", listContent.Path, "/$metadata"), "",
                        output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    output.Flush();
                    src = GetStringResult(output);
                    metaXml = GetMetadataXml(src, out nsmgr);
                }
                var allTypes = metaXml.SelectNodes("//x:EntityType", nsmgr);
                Assert.IsTrue(allTypes.Count > 1);
                var listProps = metaXml.SelectNodes("//x:EntityType/x:Property[@Name='#ListField1']", nsmgr);
                Assert.IsTrue(listProps.Count == allTypes.Count);
            });
        }*/

        [TestMethod]
        public async Task OD_GET_MissingEntity()
        {
            await ODataTestAsync(async () =>
            {
                var response = await ODataGetAsync(
                    "/OData.svc/Root('HiEveryBody')", "")
                    .ConfigureAwait(false);

                Assert.AreEqual(404, response.StatusCode);
                Assert.AreEqual("", response.Result);
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_GET_Entity()
        {
            await ODataTestAsync(async () =>
            {
                var content = Content.Load("/Root/IMS");

                var response = await ODataGetAsync("/OData.svc/Root('IMS')", "")
                    .ConfigureAwait(false);

                var entity = GetEntity(response);
                Assert.AreEqual(content.Id, entity.Id);
                Assert.AreEqual(content.Name, entity.Name);
                Assert.AreEqual(content.Path, entity.Path);
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_GET_ChildrenCollection()
        {
            await ODataTestAsync(async () =>
            {
                var response = await ODataGetAsync(
                    "/OData.svc/Root/IMS/BuiltIn/Portal", "?metadata=no")
                    .ConfigureAwait(false);

                var entities = GetEntities(response);
                var origIds = Node.Load<Folder>("/Root/IMS/BuiltIn/Portal").Children.Select(f => f.Id).ToArray();
                var ids = entities.Select(e => e.Id).ToArray();

                Assert.AreEqual(0, origIds.Except(ids).Count());
                Assert.AreEqual(0, ids.Except(origIds).Count());
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_GET_CollectionViaProperty()
        {
            await ODataTestAsync(async () =>
            {
                var response = await ODataGetAsync(
                    "/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/Members", "")
                    .ConfigureAwait(false);

                var items = GetEntities(response);
                var origIds = Node.Load<Group>("/Root/IMS/BuiltIn/Portal/Administrators").Members.Select(f => f.Id).ToArray();
                var ids = items.Select(e => e.Id).ToArray();

                Assert.AreEqual(0, origIds.Except(ids).Count());
                Assert.AreEqual(0, ids.Except(origIds).Count());
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_GET_SimplePropertyAndRaw()
        {
            await ODataTestAsync(async () =>
            {
                var imsId = Repository.ImsFolder.Id;

                // ACTION 1
                var response1 = await ODataGetAsync("/OData.svc/Root('IMS')/Id", "")
                    .ConfigureAwait(false);

                // ASSERT 1
                var entity = GetEntity(response1);
                Assert.AreEqual(imsId, entity.Id);

                // ACTION 2
                var response2 = await ODataGetAsync("/OData.svc/Root('IMS')/Id/$value", "")
                    .ConfigureAwait(false);

                // ASSERT 2
                Assert.AreEqual(imsId.ToString(), response2.Result);
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_GET_UnknownProperty()
        {
            await ODataTestAsync(async () =>
            {
                // ACTION
                var response1 = await ODataGetAsync(
                        "/OData.svc/Root('IMS')/__unknownProperty__", "")
                    .ConfigureAwait(false);

                // ASSERT
                var error = GetError(response1);
                Assert.AreEqual("InvalidContentActionException", error.ExceptionType);
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_GET_GetEntityById()
        {
            await ODataTestAsync(async () =>
            {
                var content = Content.Load(1);
                var id = content.Id;

                var response = await ODataGetAsync(
                    "/OData.svc/Content(" + id + ")", "")
                    .ConfigureAwait(false);

                var entity = GetEntity(response);
                Assert.AreEqual(id, entity.Id);
                Assert.AreEqual(content.Path, entity.Path);
                Assert.AreEqual(content.Name, entity.Name);
                Assert.AreEqual(content.ContentType, entity.ContentType);
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_GET_GetEntityById_InvalidId()
        {
            await ODataTestAsync(async () =>
            {
                var response = await ODataGetAsync("/OData.svc/Content(qwer)", "")
                    .ConfigureAwait(false);

                var exception = GetError(response);
                Assert.AreEqual(ODataExceptionCode.InvalidId, exception.Code);
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_GET_GetPropertyOfEntityById()
        {
            await ODataTestAsync(async () =>
            {
                var content = Content.Load(1);

                var response = await ODataGetAsync(
                    "/OData.svc/Content(" + content.Id + ")/Name", "")
                    .ConfigureAwait(false);


                var entity = GetEntity(response);
                Assert.AreEqual(content.Name, entity.Name);
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_GET_Collection_Projection()
        {
            await ODataTestAsync(async () =>
            {
                var response = await ODataGetAsync(
                    "/OData.svc/Root/IMS/BuiltIn/Portal", "?metadata=minimal&$select=Id,Name")
                    .ConfigureAwait(false);

                var entities = GetEntities(response);
                foreach (var entity in entities)
                {
                    Assert.AreEqual(3, entity.AllProperties.Count);
                    Assert.IsTrue(entity.AllProperties.ContainsKey("__metadata"));
                    Assert.IsTrue(entity.AllProperties.ContainsKey("Id"));
                    Assert.IsTrue(entity.AllProperties.ContainsKey("Name"));
                    Assert.IsNull(entity.Path);
                }
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_GET_Entity_Projection()
        {
            await ODataTestAsync(async () =>
            {
                var response = await ODataGetAsync(
                    "/OData.svc/Root('IMS')",
                    "?$select=Id,Name")
                    .ConfigureAwait(false);

                var entity = GetEntity(response);

                Assert.AreEqual(3, entity.AllProperties.Count);
                Assert.IsTrue(entity.AllProperties.ContainsKey("__metadata"));
                Assert.IsTrue(entity.AllProperties.ContainsKey("Id"));
                Assert.IsTrue(entity.AllProperties.ContainsKey("Name"));
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_GET_Entity_NoProjection()
        {
            await ODataTestAsync(async () =>
            {
                var response = await ODataGetAsync(
                    "/OData.svc/Root('IMS')", "")
                    .ConfigureAwait(false);


                var entity = GetEntity(response);

                var allowedFieldNames = new List<string>();
                var c = Content.Load("/Root/IMS");
                var ct = c.ContentType;
                var fieldNames = ct.FieldSettings.Select(f => f.Name);
                allowedFieldNames.AddRange(fieldNames);
                allowedFieldNames.AddRange(new[] { "__metadata", "IsFile", "Actions", "IsFolder", "Children" });

                var entityPropNames = entity.AllProperties.Select(y => y.Key).ToArray();

                var a = entityPropNames.Except(allowedFieldNames).ToArray();
                var b = allowedFieldNames.Except(entityPropNames).ToArray();

                Assert.AreEqual(0, a.Length);
                Assert.AreEqual(0, b.Length);
            }).ConfigureAwait(false);
        }

        //TODO: Remove inconclusive test result and implement this test.
        /*//[TestMethod]*/
        /*public async Task OD_GET_ContentList_NoProjection()
        {
            //Assert.Inconclusive("InMemorySchemaWriter.CreatePropertyType is partially implemented.");

            await IsolatedODataTestAsync(async () =>
            {
                InstallCarContentType();

                var testRoot = CreateTestRoot("ODataTestRoot");

                string listDef = @"<?xml version='1.0' encoding='utf-8'?>
        <ContentListDefinition xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentListDefinition'>
        	<Fields>
        		<ContentListField name='#ListField1' type='ShortText'/>
        		<ContentListField name='#ListField2' type='Integer'/>
        		<ContentListField name='#ListField3' type='Reference'/>
        	</Fields>
        </ContentListDefinition>
        ";
                string path = RepositoryPath.Combine(testRoot.Path, "Cars");
                if (Node.Exists(path))
                    Node.ForceDelete(path);
                ContentList list = new ContentList(testRoot);
                list.Name = "Cars";
                list.ContentListDefinition = listDef;
                list.AllowedChildTypes = new ContentType[] { ContentType.GetByName("Car") };
                list.Save();

                var car = Content.CreateNew("Car", list, "Car1");
                car.Save();
                car = Content.CreateNew("Car", list, "Car2");
                car.Save();

                var response = await ODataGetAsync("/OData.svc" + list.Path, "").ConfigureAwait(false);

                var entities = GetEntities(response);
                var entity = entities.First();
                var entityPropNames = entity.AllProperties.Select(y => y.Key).ToArray();

                var allowedFieldNames = new List<string>();
                allowedFieldNames.AddRange(ContentType.GetByName("Car").FieldSettings.Select(f => f.Name));
                allowedFieldNames.AddRange(ContentType.GetByName("File").FieldSettings.Select(f => f.Name));
                allowedFieldNames.AddRange(list.ListFieldNames);
                allowedFieldNames.AddRange(new[] { "__metadata", "IsFile", "Actions", "IsFolder" });
                allowedFieldNames = allowedFieldNames.Distinct().ToList();

                var a = entityPropNames.Except(allowedFieldNames).ToArray();
                var b = allowedFieldNames.Except(entityPropNames).ToArray();

                Assert.IsTrue(a.Length == 0, String.Format("Expected empty but contains: '{0}'", string.Join("', '", a)));
                Assert.IsTrue(b.Length == 0, String.Format("Expected empty but contains: '{0}'", string.Join("', '", b)));
            }).ConfigureAwait(false);
        }*/

        [TestMethod]
        public async Task OD_GET_ContentQuery()
        {
            await IsolatedODataTestAsync(async () =>
            {
                var folderName = "OData_ContentQuery";
                InstallCarContentType();

                var site = new SystemFolder(Repository.Root) { Name = Guid.NewGuid().ToString() };
                site.Save();

                var folder = Node.Load<Folder>(RepositoryPath.Combine(site.Path, folderName));
                if (folder == null)
                {
                    folder = new Folder(site) { Name = folderName };
                    folder.Save();
                }

                Content.CreateNewAndParse("Car", site, null, new Dictionary<string, string> { { "Make", "asdf" }, { "Model", "asdf" } }).Save();
                Content.CreateNewAndParse("Car", site, null, new Dictionary<string, string> { { "Make", "asdf" }, { "Model", "qwer" } }).Save();
                Content.CreateNewAndParse("Car", site, null, new Dictionary<string, string> { { "Make", "qwer" }, { "Model", "asdf" } }).Save();
                Content.CreateNewAndParse("Car", site, null, new Dictionary<string, string> { { "Make", "qwer" }, { "Model", "qwer" } }).Save();

                Content.CreateNewAndParse("Car", folder, null, new Dictionary<string, string> { { "Make", "asdf" }, { "Model", "asdf" } }).Save();
                Content.CreateNewAndParse("Car", folder, null, new Dictionary<string, string> { { "Make", "asdf" }, { "Model", "qwer" } }).Save();
                Content.CreateNewAndParse("Car", folder, null, new Dictionary<string, string> { { "Make", "qwer" }, { "Model", "asdf" } }).Save();
                Content.CreateNewAndParse("Car", folder, null, new Dictionary<string, string> { { "Make", "qwer" }, { "Model", "qwer" } }).Save();

                var expectedQueryGlobal = "asdf AND Type:Car .SORT:Path .AUTOFILTERS:OFF";
                var expectedGlobal = string.Join(", ", CreateSafeContentQuery(expectedQueryGlobal).Execute().Nodes.Select(n => n.Id.ToString()));

                var expectedQueryLocal = $"asdf AND Type:Car AND InTree:'{folder.Path}' .SORT:Path .AUTOFILTERS:OFF";
                var expectedLocal = string.Join(", ", CreateSafeContentQuery(expectedQueryLocal).Execute().Nodes.Select(n => n.Id.ToString()));

                var response1 = await ODataGetAsync(
                        "/OData.svc/Root",
                        "?$select=Id,Path&query=asdf+AND+Type%3aCar+.SORT%3aPath+.AUTOFILTERS%3aOFF")
                    .ConfigureAwait(false);

                var entities1 = GetEntities(response1);
                var realGlobal = string.Join(", ", entities1.Select(e => e.Id));
                Assert.AreEqual(realGlobal, expectedGlobal);

                var response2 = await ODataGetAsync(
                    $"/OData.svc/Root/{site.Name}/{folderName}",
                    "?$select=Id,Path&query=asdf+AND+Type%3aCar+.SORT%3aPath+.AUTOFILTERS%3aOFF")
                    .ConfigureAwait(false);

                var entities2 = GetEntities(response2);
                var realLocal = string.Join(", ", entities2.Select(e => e.Id));
                Assert.AreEqual(realLocal, expectedLocal);
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_GET_ContentQuery_Nested()
        {
            await IsolatedODataTestAsync(async () =>
            {
                var managers = new User[3];
                var resources = new User[9];
                var container = Node.LoadNode("/Root/IMS/BuiltIn/Portal");
                for (int i = 0; i < managers.Length; i++)
                {
                    managers[i] = new User(container)
                    {
                        Name = $"Manager{i}",
                        Enabled = true,
                        Email = $"manager{i}@example.com"
                    };
                    managers[i].Save();
                }
                for (int i = 0; i < resources.Length; i++)
                {
                    resources[i] = new User(container)
                    {
                        Name = $"User{i}",
                        Enabled = true,
                        Email = $"user{i}@example.com",
                    };
                    var content = Content.Create(resources[i]);
                    content["Manager"] = managers[i % 3];
                    content.Save();
                }

                var queryText = "Manager:{{Name:Manager1}} .SORT:Name .AUTOFILTERS:OFF";
                var odataQueryText = queryText.Replace(":", "%3a").Replace(" ", "+");

                var resultNamesCql = CreateSafeContentQuery(queryText).Execute().Nodes.Select(x => x.Name).ToArray();
                Assert.AreEqual("User1, User4, User7", string.Join(", ", resultNamesCql));

                // ACTION
                var response = await ODataGetAsync(
                    "/OData.svc/Root",
                    "?metadata=no&$select=Name&query=" + odataQueryText)
                    .ConfigureAwait(false);

                // ASSERT
                var entities = GetEntities(response);
                var resultNamesOData = entities.Select(x => x.Name).ToArray();
                Assert.AreEqual("User1, User4, User7", string.Join(", ", resultNamesOData));
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_GET_Collection_OrderTopSkipCount()
        {
            await ODataTestAsync(async () =>
            {
                var response = await ODataGetAsync(
                    "/OData.svc/Root/System/Schema/ContentTypes/GenericContent",
                    "?$orderby=Name desc&$skip=4&$top=3&$inlinecount=allpages")
                    .ConfigureAwait(false);

                var entities = GetEntities(response);
                var ids = entities.Select(e => e.Id);
                var origIds = CreateSafeContentQuery(
                    "+InFolder:/Root/System/Schema/ContentTypes/GenericContent .REVERSESORT:Name .SKIP:4 .TOP:3 .AUTOFILTERS:OFF")
                    .Execute().Nodes.Select(n => n.Id);
                var expected = String.Join(", ", origIds);
                var actual = String.Join(", ", ids);
                Assert.AreEqual(expected, actual);
                Assert.AreEqual(ContentType.GetByName("GenericContent").ChildTypes.Count, entities.TotalCount);
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_GET_Collection_Count()
        {
            await ODataTestAsync(async () =>
            {
                var response = await ODataGetAsync(
                    "/OData.svc/Root/IMS/BuiltIn/Portal/$count",
                    "")
                    .ConfigureAwait(false);

                var folder = Node.Load<Folder>("/Root/IMS/BuiltIn/Portal");
                Assert.AreEqual(folder.Children.Count().ToString(), response.Result);
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_GET_Collection_CountTop()
        {
            await ODataTestAsync(async () =>
            {
                var response = await ODataGetAsync(
                    "/OData.svc/Root/IMS/BuiltIn/Portal/$count",
                    "?$top=3")
                    .ConfigureAwait(false);

                Assert.AreEqual("3", response.Result);
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_GET_Select_FieldMoreThanOnce()
        {
            await ODataTestAsync(async () =>
            {
                var path = User.Administrator.Parent.Path;
                var nodecount = CreateSafeContentQuery($"InFolder:{path} .AUTOFILTERS:OFF .COUNTONLY").Execute().Count;

                var response = await ODataGetAsync(
                    "/OData.svc" + path,
                    "?metadata=no&$orderby=Name asc&$select=Id,Id,Name,Name,Path")
                    .ConfigureAwait(false);

                var entities = GetEntities(response);
                Assert.AreEqual(nodecount, entities.Length);
                Assert.AreEqual("Id,Name,Path", string.Join(",", entities[0].AllProperties.Keys.ToArray()));
            }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task OD_GET_Filter_AspectField_AspectNotFound()
        {
            await IsolatedODataTestAsync(async () =>
            {
                var workspace = CreateWorkspace("Workspace1");

                var response = await ODataGetAsync(
                        "/OData.svc" + workspace.Path,
                        "?metadata=no&$orderby=Index&$filter=Aspect1/Field1 eq 'Value1'")
                    .ConfigureAwait(false);

                var error = GetError(response);
                Assert.IsTrue(error.Message.Contains("Field not found"));
            });
        }

        [TestMethod]
        public async Task OD_GET_Select_AspectField()
        {
            await IsolatedODataTestAsync(async () =>
            {
                InstallCarContentType();
                var workspace = CreateWorkspace();

                var allowedTypes = workspace.GetAllowedChildTypeNames().ToList();
                allowedTypes.Add("Car");
                workspace.AllowChildTypes(allowedTypes);
                workspace.Save();

                var testRoot = CreateTestRoot("ODataTestRoot");

                var aspect1 = EnsureAspect("Aspect1");
                aspect1.AspectDefinition =
                    @"<AspectDefinition xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/AspectDefinition'>
  <Fields>
      <AspectField name='Field1' type='ShortText' />
    </Fields>
  </AspectDefinition>";
                aspect1.Save();

                var folder = new Folder(testRoot) {Name = Guid.NewGuid().ToString()};
                folder.Save();

                var content1 = Content.CreateNew("Car", folder, "Car1");
                content1.AddAspects(aspect1);
                content1["Aspect1.Field1"] = "asdf";
                content1.Save();

                var content2 = Content.CreateNew("Car", folder, "Car2");
                content2.AddAspects(aspect1);
                content2["Aspect1.Field1"] = "qwer";
                content2.Save();

                var response =
                    await ODataGetAsync(
                            "/OData.svc" + folder.Path,
                            "?metadata=no&$orderby=Name asc&$select=Name,Aspect1.Field1")
                        .ConfigureAwait(false);

                var entities = GetEntities(response);
                Assert.AreEqual(2, entities.Count());
                Assert.AreEqual("Car1", entities[0].Name);
                Assert.AreEqual("Car2", entities[1].Name);
                Assert.IsTrue(entities[0].AllProperties.ContainsKey("Aspect1.Field1"));
                Assert.IsTrue(entities[1].AllProperties.ContainsKey("Aspect1.Field1"));
                var value1 = (string) ((JValue) entities[0].AllProperties["Aspect1.Field1"]).Value;
                var value2 = (string) ((JValue) entities[1].AllProperties["Aspect1.Field1"]).Value;
                Assert.AreEqual("asdf", value1);
                Assert.AreEqual("qwer", value2);
            });
        }
        private Aspect EnsureAspect(string name)
        {
            var aspect = Aspect.LoadAspectByName(name);
            if (aspect == null)
            {
                aspect = new Aspect(Repository.AspectsFolder) { Name = name };
                aspect.Save();
            }
            return aspect;
        }

        [TestMethod]
        public async Task OD_GET_Filter_AspectField()
        {
            await IsolatedODataTestAsync(async () =>
            {
                // ARRANGE
                var aspectName = "Aspect1";
                var fieldName = "Field1";
                var aspect = CreateAspectAndField(aspectName, fieldName);

                var aspectFieldName = $"{aspectName}.{fieldName}";
                var aspectFieldODataName = $"{aspectName}/{fieldName}";

                var workspace = CreateWorkspace("Workspace1");
                var content1 = Content.CreateNew("SystemFolder", workspace, "Content1");
                content1.Index = 1;
                content1.Save();
                var content2 = Content.CreateNew("SystemFolder", workspace, "Content2");
                content2.Index = 2;
                content2.Save();
                var content3 = Content.CreateNew("SystemFolder", workspace, "Content3");
                content3.Index = 3;
                content3.Save();
                var content4 = Content.CreateNew("SystemFolder", workspace, "Content4");
                content4.Index = 4;
                content4.Save();

                content2.AddAspects(aspect);
                content2[aspectFieldName] = "Value2";
                content2.Save();
                content3.AddAspects(aspect);
                content3[aspectFieldName] = "Value3";
                content3.Save();
                content4.AddAspects(aspect);
                content4[aspectFieldName] = "Value2";
                content4.Save();

                // ACTION
                var response = await ODataGetAsync(
                        "/OData.svc" + workspace.Path,
                        "?metadata=no&$orderby=Index&$filter=" + aspectFieldODataName + " eq 'Value2'")
                    .ConfigureAwait(false);

                // ASSERT
                Assert.AreEqual(4, workspace.Children.Count());
                var entities = GetEntities(response);
                var expected = string.Join(", ", (new[] { content2.Name, content4.Name }));
                var names = string.Join(", ", entities.Select(e => e.Name));
                Assert.AreEqual(expected, names);
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_GET_Filter_AspectField_FieldNotFound()
        {
            await IsolatedODataTestAsync(async () =>
            {
                // ARRANGE
                var aspectName = "Aspect1";
                var fieldName = "Field1";
                CreateAspectAndField(aspectName, fieldName);

                var workspace = CreateWorkspace("Workspace1");

                // ACTION
                var response = await ODataGetAsync(
                        "/OData.svc" + workspace.Path,
                        "?metadata=no&$orderby=Index&$filter=" + aspectName + "/Field2 eq 'Value2'")
                    .ConfigureAwait(false);

                // ASSERT
                var error = GetError(response);
                Assert.IsTrue(error.Message.Contains("Field not found"));
            });
        }
        [TestMethod]
        public async Task OD_GET_Filter_AspectField_FieldNotFoundButAspectFound()
        {
            await IsolatedODataTestAsync(async () =>
            {
                // ARRANGE
                var aspectName = "Aspect1";
                var fieldName = "Field1";
                CreateAspectAndField(aspectName, fieldName);

                var workspace = CreateWorkspace("Workspace1");

                // ACTION
                var response = await ODataGetAsync(
                        "/OData.svc" + workspace.Path,
                        "?metadata=no&$orderby=Index&$filter=" + aspectName + " eq 'Value2'")
                    .ConfigureAwait(false);

                // ASSERT
                var error = GetError(response);
                Assert.IsTrue(error.Message.Contains("Field not found"));

            });
        }
        private Aspect CreateAspectAndField(string aspectName, string fieldName)
        {
            var aspect = new Aspect(Repository.AspectsFolder) { Name = aspectName };
            aspect.AddFields(new FieldInfo
            {
                Name = fieldName,
                DisplayName = fieldName + " DisplayName",
                Description = fieldName + " description",
                Type = "ShortText",
                Indexing = new IndexingInfo
                {
                    IndexHandler = "SenseNet.Search.Indexing.LowerStringIndexHandler"
                }
            });
            return aspect;
        }

        [TestMethod]
        public async Task OD_GET_Filter_ThroughReference()
        {
            await IsolatedODataTestAsync(async () =>
            {
                // ARRANGE
                var testRoot = CreateTestRoot("ODataTestRoot");
                EnsureReferenceTestStructure(testRoot);

                // ACTION
                var resourcePath = ODataMiddleware.GetEntityUrl(testRoot.Path + "/Referrer");
                var url = $"/OData.svc{resourcePath}/References";
                var response = await ODataGetAsync(
                        url,
                        "?metadata=no&$orderby=Index&$filter=Index lt 5 and Index gt 2")
                    .ConfigureAwait(false);

                // ASSERT
                var entities = GetEntities(response);
                Assert.IsTrue(entities.Length == 2);
                Assert.IsTrue(entities[0].Index == 3);
                Assert.IsTrue(entities[1].Index == 4);

            });
        }

        [TestMethod]
        public async Task OD_GET_Filter_ThroughReference_TopSkip()
        {
            await IsolatedODataTestAsync(async () =>
            {
                // ARRANGE
                var testRoot = CreateTestRoot("ODataTestRoot");
                EnsureReferenceTestStructure(testRoot);

                // ACTION
                var resourcePath = ODataMiddleware.GetEntityUrl(testRoot.Path + "/Referrer");
                var url = $"/OData.svc{resourcePath}/References";
                var response = await ODataGetAsync(
                        url,
                        "?metadata=no&$orderby=Index&$filter=Index lt 10&$top=3&$skip=1")
                    .ConfigureAwait(false);

                // ASSERT
                var entities = GetEntities(response);
                var actual = String.Join(",", entities.Select(e => e.Index).ToArray());
                Assert.AreEqual("2,3,4", actual);
            });
        }
        private static void EnsureReferenceTestStructure(Node testRoot)
        {
            if (ContentType.GetByName(typeof(OData_ReferenceTest_ContentHandler).Name) == null)
                ContentTypeInstaller.InstallContentType(OData_ReferenceTest_ContentHandler.CTD);

            if (ContentType.GetByName(typeof(OData_Filter_ThroughReference_ContentHandler).Name) == null)
                ContentTypeInstaller.InstallContentType(OData_Filter_ThroughReference_ContentHandler.CTD);

            var referrercontent = Content.Load(RepositoryPath.Combine(testRoot.Path, "Referrer"));
            if (referrercontent == null)
            {
                var nodes = new Node[5];
                for (int i = 0; i < nodes.Length; i++)
                {
                    var content = Content.CreateNew("OData_Filter_ThroughReference_ContentHandler", testRoot, "Referenced" + i);
                    content.Index = i + 1;
                    content.Save();
                    nodes[i] = content.ContentHandler;
                }

                referrercontent = Content.CreateNew("OData_Filter_ThroughReference_ContentHandler", testRoot, "Referrer");
                var referrer = (OData_Filter_ThroughReference_ContentHandler)referrercontent.ContentHandler;
                referrer.References = nodes;
                referrercontent.Save();
            }
        }

        [TestMethod]
        public async Task OD_GET_Expand()
        {
            await IsolatedODataTestAsync(async () =>
            {
                //EnsureCleanAdministratorsGroup();
                //var count = CreateSafeContentQuery("InFolder:/Root/IMS/BuiltIn/Portal .COUNTONLY").Execute().Count;

                #region expectedJson = @"{
                var expectedJson = @"{
  ""d"": {
    ""__metadata"": {
      ""uri"": ""/odata.svc/Root/IMS/BuiltIn/Portal('Administrators')"",
      ""type"": ""Group""
    },
    ""Id"": 7,
    ""Members"": [
      {
        ""__metadata"": {
          ""uri"": ""/odata.svc/Root/IMS/BuiltIn/Portal('Admin')"",
          ""type"": ""User""
        },
        ""Id"": 1,
        ""Name"": ""Admin""
      },
      {
        ""__metadata"": {
          ""uri"": ""/odata.svc/Root/IMS/BuiltIn/Portal('Developers')"",
          ""type"": ""Group""
        },
        ""Id"": 1325,
        ""Name"": ""Developers""
      }
    ],
    ""Name"": ""Administrators""
  }
}";
                #endregion

                var response = await ODataGetAsync(
                    "/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')",
                    "?$expand=Members,ModifiedBy&$select=Id,Members/Id,Name,Members/Name&metadata=minimal")
                    .ConfigureAwait(false);

                var raw = response.Result.Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace(" ", "");
                var exp = expectedJson.Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace(" ", "");
                Assert.AreEqual(exp, raw);
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_GET_Expand_Level2_Noselect()
        {
            await ODataTestAsync(async () =>
            {
                EnsureManagerOfAdmin();

                var response = await ODataGetAsync(
                    "/OData.svc/Root/IMS/BuiltIn('Portal')",
                    "?$expand=CreatedBy/Manager")
                    .ConfigureAwait(false);

                var entity = GetEntity(response);
                var createdBy = entity.CreatedBy;
                var createdByManager = createdBy.Manager;
                Assert.IsTrue(entity.AllPropertiesSelected);
                Assert.IsTrue(createdBy.AllPropertiesSelected);
                Assert.IsTrue(createdByManager.AllPropertiesSelected);
                Assert.IsTrue(createdByManager.CreatedBy.IsDeferred);
                Assert.IsTrue(createdByManager.Manager.IsDeferred);
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_GET_Expand_Level2_Select_Level1()
        {
            await ODataTestAsync(async () =>
            {
                EnsureManagerOfAdmin();

                var response = await ODataGetAsync(
                    "/OData.svc/Root/IMS/BuiltIn('Portal')",
                    "?$expand=CreatedBy/Manager&$select=CreatedBy")
                    .ConfigureAwait(false);

                var entity = GetEntity(response);
                Assert.IsFalse(entity.AllPropertiesSelected);
                Assert.IsTrue(entity.CreatedBy.AllPropertiesSelected);
                Assert.IsTrue(entity.CreatedBy.CreatedBy.IsDeferred);
                Assert.IsTrue(entity.CreatedBy.Manager.AllPropertiesSelected);
                Assert.IsTrue(entity.CreatedBy.Manager.CreatedBy.IsDeferred);
                Assert.IsTrue(entity.CreatedBy.Manager.Manager.IsDeferred);
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_GET_Expand_Level2_Select_Level2()
        {
            await ODataTestAsync(async () =>
            {
                EnsureManagerOfAdmin();

                var response = await ODataGetAsync(
                    "/OData.svc/Root/IMS/BuiltIn('Portal')",
                    "?$expand=CreatedBy/Manager&$select=CreatedBy/Manager")
                    .ConfigureAwait(false);

                var entity = GetEntity(response);
                Assert.IsFalse(entity.AllPropertiesSelected);
                Assert.IsFalse(entity.CreatedBy.AllPropertiesSelected);
                Assert.IsNull(entity.CreatedBy.CreatedBy);
                Assert.IsTrue(entity.CreatedBy.Manager.AllPropertiesSelected);
                Assert.IsTrue(entity.CreatedBy.Manager.CreatedBy.IsDeferred);
                Assert.IsTrue(entity.CreatedBy.Manager.Manager.IsDeferred);
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_GET_Expand_Level2_Select_Level3()
        {
            await ODataTestAsync(async () =>
            {
                EnsureManagerOfAdmin();

                var response = await ODataGetAsync(
                    "/OData.svc/Root/IMS/BuiltIn('Portal')",
                    "?$expand=CreatedBy/Manager&$select=CreatedBy/Manager/Id")
                    .ConfigureAwait(false);

                var entity = GetEntity(response);
                Assert.IsFalse(entity.AllPropertiesSelected);
                Assert.IsFalse(entity.CreatedBy.AllPropertiesSelected);
                Assert.IsNull(entity.CreatedBy.CreatedBy);
                Assert.IsFalse(entity.CreatedBy.Manager.AllPropertiesSelected);
                Assert.IsTrue(entity.CreatedBy.Manager.Id > 0);
                Assert.IsNull(entity.CreatedBy.Manager.Path);
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_GET_ExpandErrors()
        {
            await ODataTestAsync(async () =>
            {
                // ACTION 1
                var response = await ODataGetAsync(
                        "/OData.svc/Root/IMS/BuiltIn/Portal",
                        "?$expand=Members&$select=Members1/Id")
                    .ConfigureAwait(false);

                // ASSERT 1
                var error = GetError(response);
                Assert.IsTrue(error.Code == ODataExceptionCode.InvalidSelectParameter);
                Assert.IsTrue(error.Message == "Bad item in $select: Members1/Id");

                // ACTION 2
                response = await ODataGetAsync(
                        "/OData.svc/Root/IMS/BuiltIn/Portal",
                        "?&$select=Members/Id")
                    .ConfigureAwait(false);

                // ASSERT 2
                error = GetError(response);
                Assert.IsTrue(error.Code == ODataExceptionCode.InvalidSelectParameter);
                Assert.IsTrue(error.Message == "Bad item in $select: Members/Id");
            });
        }
        [TestMethod]
        public async Task OD_GET_Expand_Actions()
        {
            await ODataTestAsync(async () =>
            {
                // ACTION
                var response = await ODataGetAsync(
                        "/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')",
                        "?metadata=no&$expand=Members/Actions,ModifiedBy&$select=Id,Name,Actions,Members/Id,Members/Name,Members/Actions")
                    .ConfigureAwait(false);

                // ASSERT
                AssertNoError(response);
                var entity = GetEntity(response);
                var members = entity.AllProperties["Members"] as JArray;
                Assert.IsNotNull(members);
                var member = members.FirstOrDefault() as JObject;
                Assert.IsNotNull(member);
                var actionsProperty = member.Property("Actions");
                var actions = actionsProperty.Value as JArray;
                Assert.IsNotNull(actions);
                Assert.IsTrue(actions.Any());
            });
        }

        [TestMethod]
        public async Task OD_GET_UserAvatarByRef()
        {
            await IsolatedODataTestAsync(async () =>
            {
                var testDomain = new Domain(Repository.ImsFolder) { Name = "Domain1" };
                testDomain.Save();

                var testUser = new User(testDomain) { Name = "User1" };
                testUser.Save();

                var testSite = CreateWorkspace();
                testSite.AllowChildType("Image");
                testSite.Save();

                var testAvatars = new Folder(testSite) { Name = "demoavatars" };
                testAvatars.Save();

                var testAvatar = new Image(testAvatars) { Name = "user1.jpg" };
                testAvatar.Binary = new BinaryData { FileName = "user1.jpg" };
                testAvatar.Binary.SetStream(RepositoryTools.GetStreamFromString("abcdefgh"));
                testAvatar.Save();

                // set avatar of User1
                var userContent = Content.Load(testUser.Id);
                var avatarContent = Content.Load(testAvatar.Id);
                var avatarData = new ImageField.ImageFieldData(null, (Image)avatarContent.ContentHandler, null);
                userContent["Avatar"] = avatarData;
                userContent.Save();

                // ACTION
                var response = await ODataGetAsync(
                    $"/OData.svc/Root/IMS/{testDomain.Name}('{testUser.Name}')",
                    "?metadata=no&$select=Avatar")
                    .ConfigureAwait(false);

                // ASSERT
                var entity = GetEntity(response);
                var avatarString = entity.AllProperties["Avatar"].ToString();
                Assert.IsTrue(avatarString.Contains("Url"));
                Assert.IsTrue(avatarString.Contains(testAvatar.Path));
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_GET_UserAvatarUpdateRef()
        {
            await IsolatedODataTestAsync(async () =>
            {
                var testDomain = new Domain(Repository.ImsFolder) { Name = "Domain1" };
                testDomain.Save();

                var testUser = new User(testDomain) { Name = "User1" };
                testUser.Save();

                var testSite = CreateWorkspace();
                testSite.AllowChildType("Image");
                testSite.Save();

                var testAvatars = new Folder(testSite) { Name = "demoavatars" };
                testAvatars.Save();

                var testAvatar1 = new Image(testAvatars) { Name = "user1.jpg" };
                testAvatar1.Binary = new BinaryData { FileName = "user1.jpg" };
                testAvatar1.Binary.SetStream(RepositoryTools.GetStreamFromString("abcdefgh"));
                testAvatar1.Save();

                var testAvatar2 = new Image(testAvatars) { Name = "user2.jpg" };
                testAvatar2.Binary = new BinaryData { FileName = "user2.jpg" };
                testAvatar2.Binary.SetStream(RepositoryTools.GetStreamFromString("ijklmnop"));
                testAvatar2.Save();

                // set avatar of User1
                var userContent = Content.Load(testUser.Id);
                var avatarContent = Content.Load(testAvatar1.Id);
                var avatarData = new ImageField.ImageFieldData(null, (Image)avatarContent.ContentHandler, null);
                userContent["Avatar"] = avatarData;
                userContent.Save();

                // ACTION
                var response = await ODataPatchAsync($"/OData.svc/Root/IMS/{testDomain.Name}('{testUser.Name}')",
                    "?metadata=no&$select=Avatar,ImageRef,ImageData",
                    $"(models=[{{\"Avatar\": {testAvatar2.Id}}}])")
                    .ConfigureAwait(false);

                // ASSERT
                AssertNoError(response);
                var entity = GetEntity(response);
                var avatarString = entity.AllProperties["Avatar"].ToString();
                Assert.IsTrue(avatarString.Contains("Url"));
                Assert.IsTrue(avatarString.Contains(testAvatar2.Path));
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_GET_UserAvatarUpdateRefByPath()
        {
            await IsolatedODataTestAsync(async () =>
            {
                var testDomain = new Domain(Repository.ImsFolder) { Name = "Domain1" };
                testDomain.Save();

                var testUser = new User(testDomain) { Name = "User1" };
                testUser.Save();

                var testSite = CreateWorkspace();
                testSite.AllowChildType("Image");
                testSite.Save();

                var testAvatars = new Folder(testSite) { Name = "demoavatars" };
                testAvatars.Save();

                var testAvatar1 = new Image(testAvatars) { Name = "user1.jpg" };
                testAvatar1.Binary = new BinaryData { FileName = "user1.jpg" };
                testAvatar1.Binary.SetStream(RepositoryTools.GetStreamFromString("abcdefgh"));
                testAvatar1.Save();

                var testAvatar2 = new Image(testAvatars) { Name = "user2.jpg" };
                testAvatar2.Binary = new BinaryData { FileName = "user2.jpg" };
                testAvatar2.Binary.SetStream(RepositoryTools.GetStreamFromString("ijklmnop"));
                testAvatar2.Save();

                // set avatar of User1
                var userContent = Content.Load(testUser.Id);
                var avatarContent = Content.Load(testAvatar1.Id);
                var avatarData = new ImageField.ImageFieldData(null, (Image)avatarContent.ContentHandler, null);
                userContent["Avatar"] = avatarData;
                userContent.Save();

                // ACTION
                var response = await ODataPatchAsync(
                    $"/OData.svc/Root/IMS/{testDomain.Name}('{testUser.Name}')",
                    "?metadata=no&$select=Avatar,ImageRef,ImageData",
                    $"(models=[{{\"Avatar\": \"{testAvatar2.Path}\"}}])")
                    .ConfigureAwait(false);

                // ASSERT
                AssertNoError(response);
                var entity = GetEntity(response);
                var avatarString = entity.AllProperties["Avatar"].ToString();
                Assert.IsTrue(avatarString.Contains("Url"));
                Assert.IsTrue(avatarString.Contains(testAvatar2.Path));
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_GET_UserAvatarByInnerData()
        {
            await IsolatedODataTestAsync(async () =>
            {
                var testDomain = new Domain(Repository.ImsFolder) { Name = "Domain1" };
                testDomain.Save();

                var testUser = new User(testDomain) { Name = "User1" };
                testUser.Save();

                var testSite = CreateWorkspace();
                testSite.AllowChildType("Image");
                testSite.Save();

                var testAvatars = new Folder(testSite) { Name = "demoavatars" };
                testAvatars.Save();

                var testAvatar = new Image(testAvatars) { Name = "user1.jpg" };
                testAvatar.Binary = new BinaryData { FileName = "user1.jpg" };
                testAvatar.Binary.SetStream(RepositoryTools.GetStreamFromString("abcdefgh"));
                testAvatar.Save();

                var avatarBinaryData = new BinaryData { FileName = "user2.jpg" };
                avatarBinaryData.SetStream(RepositoryTools.GetStreamFromString("ijklmnop"));

                // set avatar of User1
                var userContent = Content.Load(testUser.Id);
                var avatarData = new ImageField.ImageFieldData(null, null, avatarBinaryData);
                userContent["Avatar"] = avatarData;
                userContent.Save();

                // ACTION
                var response = await ODataGetAsync(
                    $"/OData.svc/Root/IMS/{testDomain.Name}('{testUser.Name}')",
                    "?metadata=no&$select=Avatar,ImageRef,ImageData")
                    .ConfigureAwait(false);

                // ASSERT
                AssertNoError(response);
                var entity = GetEntity(response);
                var avatarString = entity.AllProperties["Avatar"].ToString();
                Assert.IsTrue(avatarString.Contains("Url"));
                Assert.IsTrue(avatarString.Contains($"/binaryhandler.ashx?nodeid={testUser.Id}&propertyname=ImageData"));
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_GET_UserAvatarUpdateInnerDataToRef()
        {
            await IsolatedODataTestAsync(async () =>
            {
                var testDomain = new Domain(Repository.ImsFolder) { Name = "Domain1" };
                testDomain.Save();

                var testUser = new User(testDomain) { Name = "User1" };
                testUser.Save();

                var testSite = CreateWorkspace();
                testSite.AllowChildType("Image");
                testSite.Save();

                var testAvatars = new Folder(testSite) { Name = "demoavatars" };
                testAvatars.Save();

                var testAvatar = new Image(testAvatars) { Name = "user1.jpg" };
                testAvatar.Binary = new BinaryData { FileName = "user1.jpg" };
                testAvatar.Binary.SetStream(RepositoryTools.GetStreamFromString("abcdefgh"));
                testAvatar.Save();

                var avatarBinaryData = new BinaryData { FileName = "user2.jpg" };
                avatarBinaryData.SetStream(RepositoryTools.GetStreamFromString("ijklmnop"));

                // set avatar of User1
                var userContent = Content.Load(testUser.Id);
                var avatarData = new ImageField.ImageFieldData(null, null, avatarBinaryData);
                userContent["Avatar"] = avatarData;
                userContent.Save();

                // ACTION
                var response = await ODataPatchAsync(
                    $"/OData.svc/Root/IMS/{testDomain.Name}('{testUser.Name}')",
                    "?metadata=no&$select=Avatar,ImageRef,ImageData",
                    $"(models=[{{\"Avatar\": {testAvatar.Id}}}])")
                    .ConfigureAwait(false);

                // ASSERT
                AssertNoError(response);
                var entity = GetEntity(response);
                var avatarString = entity.AllProperties["Avatar"].ToString();
                Assert.IsTrue(avatarString.Contains("Url"));
                Assert.IsTrue(avatarString.Contains(testAvatar.Path));
            }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task OD_GET_OrderByNumericDouble()
        {
            await ODataTestAsync(async () =>
            {
                // ARRANGE
                var testRoot = CreateTestRoot("ODataTestRoot");

                var contentTypeName = "OData_OrderByNumericDouble_ContentType";

                var ctd = @"<?xml version='1.0' encoding='utf-8'?>
<ContentType name='" + contentTypeName + @"' parentType='GenericContent' handler='SenseNet.ContentRepository.GenericContent' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
  <DisplayName>$Ctd-Resource,DisplayName</DisplayName>
  <Description>$Ctd-Resource,Description</Description>
  <Icon>Resource</Icon>
  <Fields>
    <Field name='CustomIndex' type='Number'>
      <DisplayName>CustomIndex</DisplayName>
      <Description>CustomIndex</Description>
      <Configuration />
    </Field>
  </Fields>
</ContentType>";

                ContentTypeInstaller.InstallContentType(ctd);
                var root = new Folder(testRoot) { Name = Guid.NewGuid().ToString() };
                root.Save();

                Content content;

                content = Content.CreateNew(contentTypeName, root, "Content-1"); content["CustomIndex"] = 6.0; content.Save();
                content = Content.CreateNew(contentTypeName, root, "Content-2"); content["CustomIndex"] = 3.0; content.Save();
                content = Content.CreateNew(contentTypeName, root, "Content-3"); content["CustomIndex"] = 2.0; content.Save();
                content = Content.CreateNew(contentTypeName, root, "Content-4"); content["CustomIndex"] = 4.0; content.Save();
                content = Content.CreateNew(contentTypeName, root, "Content-5"); content["CustomIndex"] = 5.0; content.Save();

                try
                {
                    var queryResult = CreateSafeContentQuery("ParentId:" + root.Id + " .SORT:CustomIndex .AUTOFILTERS:OFF").Execute().Nodes;
                    var names = string.Join(", ", queryResult.Select(n => n.Name).ToArray());
                    var expectedNames = "Content-3, Content-2, Content-4, Content-5, Content-1";
                    Assert.AreEqual(expectedNames, names);

                    // ACTION 1
                    var response = await ODataGetAsync(
                        "/OData.svc" + root.Path,
                        "?metadata=no&enableautofilters=false$select=Id,Path,Name,CustomIndex&$expand=,CheckedOutTo&$orderby=Name asc&$filter=(ContentType eq '" + contentTypeName + "')&$top=20&$skip=0&$inlinecount=allpages&metadata=no")
                        .ConfigureAwait(false);

                    // ASSERT 1
                    var entities = GetEntities(response);
                    names = string.Join(", ", entities.Select(e => e.Name).ToArray());
                    Assert.AreEqual("Content-1, Content-2, Content-3, Content-4, Content-5", names);

                    // ACTION 2
                    response = await ODataGetAsync(
                        "/OData.svc" + root.Path,
                        "?enableautofilters=false$select=Id,Path,Name,CustomIndex&$expand=,CheckedOutTo&$orderby=CustomIndex asc&$filter=(ContentType eq '" + contentTypeName + "')&$top=20&$skip=0&$inlinecount=allpages&metadata=no")
                        .ConfigureAwait(false);

                    // ASSERT 2
                    entities = GetEntities(response);
                    names = string.Join(", ", entities.Select(e => e.Name).ToArray());
                    Assert.AreEqual(expectedNames, names);
                }
                finally
                {
                    root.ForceDelete();
                    ContentTypeInstaller.RemoveContentType(contentTypeName);
                }
            }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task OD_GET_FIX_AutoFiltersInQueryAndParams()
        {
            await ODataTestAsync(() =>
            {
                var urls = new[]
                {
                    "/OData.svc/Root/System/$count?metadata=no&query=TypeIs:Folder&$select=Path,Type",
                    "/OData.svc/Root/System/$count?metadata=no&query=TypeIs:Folder .AUTOFILTERS:OFF&$select=Path,Type",
                    "/OData.svc/Root/System/$count?metadata=no&query=TypeIs:Folder .AUTOFILTERS:ON&$select=Path,Type",
                    "/OData.svc/Root/System/$count?metadata=no&query=TypeIs:Folder&$select=Path,Type&enableautofilters=false",
                    "/OData.svc/Root/System/$count?metadata=no&query=TypeIs:Folder .AUTOFILTERS:OFF&$select=Path,Type&enableautofilters=false",
                    "/OData.svc/Root/System/$count?metadata=no&query=TypeIs:Folder .AUTOFILTERS:ON&$select=Path,Type&enableautofilters=false",
                    "/OData.svc/Root/System/$count?metadata=no&query=TypeIs:Folder&$select=Path,Type&enableautofilters=true",
                    "/OData.svc/Root/System/$count?metadata=no&query=TypeIs:Folder .AUTOFILTERS:OFF&$select=Path,Type&enableautofilters=true",
                    "/OData.svc/Root/System/$count?metadata=no&query=TypeIs:Folder .AUTOFILTERS:ON&$select=Path,Type&enableautofilters=true"
                };

                var actual = String.Join(" ",
                    urls.Select(u => GetResultFor_OData_FIX_AutoFiltersInQueryAndParams(u).ConfigureAwait(false).GetAwaiter().GetResult() == "0" ? "0" : "1"));
                Assert.AreEqual("0 1 0 1 1 1 0 0 0", actual);

                return Task.CompletedTask;
            }).ConfigureAwait(false);
        }
        private async Task<string> GetResultFor_OData_FIX_AutoFiltersInQueryAndParams(string url)
        {
            var sides = url.Split('?');
            var response = await ODataGetAsync(sides[0], "?" + sides[1]);
            return response.Result;
        }

        /* ============================================================================ OTHER TESTS */

        [TestMethod]
        public async Task OD_SnJsonConverterTest_SimpleProjection()
        {
            await IsolatedODataTestAsync(() =>
            {
                InstallCarContentType();
                var testRoot = CreateTestRoot("ODataTestRoot");

                // Create, save
                var content = Content.CreateNew("Car", testRoot, "MyCar1");
                content["Make"] = "Citroen";
                content["Model"] = "C100";
                content["Price"] = 2399999.99;
                content.Save();

                // Reload
                content = Content.Load(content.Path);
                // Generate JSON
                var generatedJson = content.ToJson(null, new[] {"Id", "Path", "Name", "Make", "Model", "Price"}, null);

                // Run assertions
                var jobj = JObject.Parse(generatedJson);
                Assert.AreEqual(jobj["Id"], content.Id);
                Assert.AreEqual(jobj["Path"], content.Path);
                Assert.AreEqual(jobj["Name"], content.Name);
                Assert.AreEqual(jobj["Make"].Value<string>(), content["Make"]);
                Assert.AreEqual(jobj["Model"].Value<string>(), content["Model"]);
                Assert.AreEqual(jobj["Price"].Value<decimal>(), content["Price"]);

                return Task.CompletedTask;
            });
        }
        [TestMethod]
        public async Task OD_SnJsonConverterTest_WithExpand()
        {
            await IsolatedODataTestAsync(() =>
            {
                InstallCarContentType();
                var testRoot = CreateTestRoot("ODataTestRoot");

                // Create, save
                var content = Content.CreateNew("Car", testRoot, "MyCar2");
                content["Make"] = "Citroen";
                content["Model"] = "C101";
                content["Price"] = 4399999.99;
                content.Save();

                // Reload
                content = Content.Load(content.Path);
                // Generate JSON
                var generatedJson =
                    content.ToJson(null,
                        new[] {"Id", "Path", "Name", "Make", "Model", "Price", "CreatedBy/Id", "CreatedBy/Path"},
                        new[] {"CreatedBy"});

                // Run assertions
                var jobj = JObject.Parse(generatedJson);
                Assert.AreEqual(jobj["Id"], content.Id);
                Assert.AreEqual(jobj["Path"], content.Path);
                Assert.AreEqual(jobj["Name"], content.Name);
                Assert.AreEqual(jobj["Make"].Value<string>(), content["Make"]);
                Assert.AreEqual(jobj["Model"].Value<string>(), content["Model"]);
                Assert.AreEqual(jobj["Price"].Value<decimal>(), content["Price"]);
                Assert.AreEqual(jobj["CreatedBy"]["Id"], content.ContentHandler.CreatedBy.Id);
                Assert.AreEqual(jobj["CreatedBy"]["Path"], content.ContentHandler.CreatedBy.Path);

                return Task.CompletedTask;
            });
        }

        [TestMethod]
        public async Task OD_GET_Urls_CurrentSite()
        {
            await IsolatedODataTestAsync(async () =>
            {
                var site = CreateWorkspace();
                var siteParentPath = RepositoryPath.GetParentPath(site.Path);
                var siteName = RepositoryPath.GetFileName(site.Path);

                string expectedJson = string.Concat(@"{""d"":{
                        ""__metadata"":{                    ""uri"":""/odata.svc", siteParentPath, @"('", siteName, @"')"",""type"":""Workspace""},
                        ""Manager"":{""__deferred"":{       ""uri"":""/odata.svc", siteParentPath, @"('", siteName, @"')/Manager""}},
                        ""CreatedBy"":{""__deferred"":{     ""uri"":""/odata.svc", siteParentPath, @"('", siteName, @"')/CreatedBy""}},
                        ""ModifiedBy"":{""__deferred"":{    ""uri"":""/odata.svc", siteParentPath, @"('", siteName, @"')/ModifiedBy""}}}}")
                    .Replace("\r\n", "").Replace("\t", "").Replace(" ", "");

                // ACTION
                var response = await ODataGetAsync(
                        ODataTools.GetODataUrl(site.Path),
                        "?$select=Manager,CreatedBy,ModifiedBy&metadata=minimal")
                    .ConfigureAwait(false);

                // ASSERT
                var result = response.Result.Replace("\r\n", "").Replace("\t", "").Replace(" ", "");
                Assert.AreEqual(expectedJson, result);
            });
        }

        [TestMethod]
        public async Task OD_SortingByMappedDateTimeAspectField()
        {
            await IsolatedODataTestAsync(async () =>
            {
                InstallCarContentType();
                var testRoot = CreateTestRoot("ODataTestRoot");

                // Create an aspect with date field that is mapped to CreationDate
                var aspect1Name = "OData_SortingByMappedDateTimeAspectField";
                var aspect1Definition =
                    @"<AspectDefinition xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/AspectDefinition'>
    <Fields>
        <AspectField name='Field1' type='DateTime'>
            <!-- not bound -->
        </AspectField>
        <AspectField name='Field2' type='DateTime'>
            <Bind property=""CreationDate""></Bind>
        </AspectField>
        <AspectField name='Field3' type='DateTime'>
            <Bind property=""ModificationDate""></Bind>
        </AspectField>
        </Fields>
    </AspectDefinition>";

                var aspect1 = new Aspect(Repository.AspectsFolder)
                {
                    Name = aspect1Name,
                    AspectDefinition = aspect1Definition
                };
                aspect1.Save();

                var field1Name = String.Concat(aspect1Name, Aspect.ASPECTFIELDSEPARATOR, "Field1");
                var field2Name = String.Concat(aspect1Name, Aspect.ASPECTFIELDSEPARATOR, "Field2");
                var field3Name = String.Concat(aspect1Name, Aspect.ASPECTFIELDSEPARATOR, "Field3");

                var container = new SystemFolder(testRoot) {Name = Guid.NewGuid().ToString()};
                container.Save();

                var today = DateTime.Now;
                var unused = (new[] {3, 1, 5, 2, 4}).Select(i =>
                {
                    var content = Content.CreateNew("Car", container, "Car-" + i + "-" + Guid.NewGuid());
                    content.AddAspects(aspect1);

                    content[field1Name] = today.AddDays(-5 + i);
                    //content[field2Name] = today.AddDays(-i);
                    //content[field3Name] = today.AddDays(-i);
                    content.CreationDate = today.AddDays(-i);
                    content.ModificationDate = today.AddDays(-i);

                    content.Save();
                    return i;
                }).ToArray();

                // check prerequisits

                var r1 = Content.All.DisableAutofilters().Where(c => c.InTree(container) && c.Name.StartsWith("Car-"))
                    .OrderBy(c => c[field1Name]).ToArray();
                var result1 = String.Join(",", r1.Select(x => x.Name[4]));
                Assert.AreEqual("1,2,3,4,5", result1);
                var r2 = Content.All.DisableAutofilters().Where(c => c.InTree(container) && c.Name.StartsWith("Car-"))
                    .OrderByDescending(c => c[field1Name]).ToArray();
                var result2 = String.Join(",", r2.Select(x => x.Name[4]));
                Assert.AreEqual("5,4,3,2,1", result2);
                var r3 = Content.All.DisableAutofilters().Where(c => c.InTree(container) && c.Name.StartsWith("Car-"))
                    .OrderBy(c => c[field2Name]).ToArray();
                var result3 = String.Join(",", r3.Select(x => x.Name[4]));
                Assert.AreEqual("5,4,3,2,1", result3);
                var r4 = Content.All.DisableAutofilters().Where(c => c.InTree(container) && c.Name.StartsWith("Car-"))
                    .OrderByDescending(c => c[field2Name]).ToArray();
                var result4 = String.Join(",", r4.Select(x => x.Name[4]));
                Assert.AreEqual("1,2,3,4,5", result4);
                var r5 = Content.All.DisableAutofilters().Where(c => c.InTree(container) && c.Name.StartsWith("Car-"))
                    .OrderBy(c => c[field3Name]).ToArray();
                var result5 = String.Join(",", r5.Select(x => x.Name[4]));
                Assert.AreEqual("5,4,3,2,1", result5);
                var r6 = Content.All.DisableAutofilters().Where(c => c.InTree(container) && c.Name.StartsWith("Car-"))
                    .OrderByDescending(c => c[field3Name]).ToArray();
                var result6 = String.Join(",", r6.Select(x => x.Name[4]));
                Assert.AreEqual("1,2,3,4,5", result6);


                // ACTION-1: Field1 ASC
                var response = await ODataGetAsync(
                        "/OData.svc/" + container.Path,
                        "?$orderby=" + field1Name + " asc")
                    .ConfigureAwait(false);

                // ASSERT-1
                var entities = GetEntities(response);
                Assert.AreEqual(5, entities.Length);
                Assert.AreEqual("1,2,3,4,5", string.Join(",", entities.Select(e => e.Name[4])));


                // ACTION-2: Field1 DESC
                response = await ODataGetAsync(
                        "/OData.svc/" + container.Path,
                        "?$orderby=" + field1Name + " desc")
                    .ConfigureAwait(false);

                // ASSERT-2
                entities = GetEntities(response);
                Assert.AreEqual(5, entities.Length);
                Assert.AreEqual("5,4,3,2,1", string.Join(",", entities.Select(e => e.Name[4])));


                // ACTION-3: Field2 ASC
                response = await ODataGetAsync(
                        "/OData.svc/" + container.Path,
                        "?$orderby=" + field2Name + " asc")
                    .ConfigureAwait(false);

                // ASSERT-3
                entities = GetEntities(response);
                Assert.AreEqual(5, entities.Length);
                Assert.AreEqual("5,4,3,2,1", string.Join(",", entities.Select(e => e.Name[4])));


                // ACTION-4: Field2 DESC
                response = await ODataGetAsync(
                        "/OData.svc/" + container.Path,
                        "?$orderby=" + field2Name + " desc")
                    .ConfigureAwait(false);

                // ASSERT-4
                entities = GetEntities(response);
                Assert.AreEqual(5, entities.Length);
                Assert.AreEqual("1,2,3,4,5", string.Join(",", entities.Select(e => e.Name[4])));


                // ACTION-5: Field3 ASC
                response = await ODataGetAsync(
                        "/OData.svc/" + container.Path,
                        "?$orderby=" + field3Name + " asc")
                    .ConfigureAwait(false);

                // ASSERT-5
                entities = GetEntities(response);
                Assert.AreEqual(5, entities.Length);
                Assert.AreEqual("5,4,3,2,1", string.Join(",", entities.Select(e => e.Name[4])));


                // ACTION-6: Field3 DESC
                response = await ODataGetAsync(
                        "/OData.svc/" + container.Path,
                        "?$orderby=" + field3Name + " desc")
                    .ConfigureAwait(false);

                // ASSERT-6
                entities = GetEntities(response);
                Assert.AreEqual(5, entities.Length);
                Assert.AreEqual("1,2,3,4,5", string.Join(",", entities.Select(e => e.Name[4])));
            });
        }

        [TestMethod]
        public async Task OD_FIX_DoNotUrlDecodeTheRequestStream()
        {
            await IsolatedODataTestAsync(async () =>
            {
                using (new ODataOperationTests.ActionResolverSwindler(new ODataOperationTests.TestActionResolver()))
                {
                    var testString = "a&b c+d%20e";

                    // ACTION
                    var response = await ODataPostAsync(
                            "/OData.svc/Root('IMS')/ParameterEcho",
                            "",
                            $"{{testString: \'{testString}\' }}")
                        .ConfigureAwait(false);

                    // ASSERT
                    Assert.AreEqual(testString, response.Result);
                }
            });
        }

        [TestMethod]
        public async Task OD_FIX_Move_RightExceptionIfTargetExists()
        {
            //Assert.Inconclusive("InMemoryDataProvider.LoadChildTypesToAllow method is not implemented.");

            await IsolatedODataTestAsync(async () =>
            {
                using (new ODataOperationTests.ActionResolverSwindler(new ODataOperationTests.TestActionResolver()))
                {
                    InstallCarContentType();
                    var testRoot = CreateTestRoot("ODataTestRoot");
                    CreateStructureFor_RightExceptionIfTargetExistsTests(testRoot, out var sourcePath,
                        out var targetContainerPath);

                    // ACTION
                    var response = await ODataPostAsync(
                            $"/OData.svc/{RepositoryPath.GetParentPath(sourcePath)}" +
                            $"('{RepositoryPath.GetFileName(sourcePath)}')/TestMoveTo",
                            "",
                            "{\"targetPath\":\"" + targetContainerPath + "\"}")
                        .ConfigureAwait(false);

                    // ASSERT
                    var error = GetError(response);
                    Assert.AreEqual(ODataExceptionCode.ContentAlreadyExists, error.Code);
                    Assert.IsTrue(error.Message.ToLowerInvariant().Contains("cannot move the content"));
                }
            });
        }
        [TestMethod]
        public async Task OD_FIX_Copy_RightExceptionIfTargetExists()
        {
            await IsolatedODataTestAsync(async () =>
            {
                using (new ODataOperationTests.ActionResolverSwindler(new ODataOperationTests.TestActionResolver()))
                {
                    InstallCarContentType();
                    var testRoot = CreateTestRoot("ODataTestRoot");
                    CreateStructureFor_RightExceptionIfTargetExistsTests(testRoot, out var sourcePath,
                        out var targetContainerPath);

                    // ACTION
                    var response = await ODataPostAsync(
                            $"/OData.svc/{RepositoryPath.GetParentPath(sourcePath)}" +
                                    $"('{RepositoryPath.GetFileName(sourcePath)}')/TestCopyTo",
                            "",
                            "{\"targetPath\":\"" + targetContainerPath + "\"}")
                        .ConfigureAwait(false);

                    // ASSERT
                    var error = GetError(response);
                    Assert.AreEqual(ODataExceptionCode.ContentAlreadyExists, error.Code);
                    Assert.IsTrue(error.Message.ToLowerInvariant().Contains("cannot copy the content"));
                }
            });
        }
        public void CreateStructureFor_RightExceptionIfTargetExistsTests(Node testRoot, out string sourcePath, out string targetContainerPath)
        {
            var sourceFolder = new SystemFolder(testRoot) { Name = Guid.NewGuid().ToString() };
            sourceFolder.Save();
            var targetFolder = new SystemFolder(testRoot) { Name = Guid.NewGuid().ToString() };
            targetFolder.Save();

            var sourceContent = new GenericContent(sourceFolder, "Car") { Name = "DemoContent" };
            sourceContent.Save();

            var targetContent = new GenericContent(targetFolder, "Car") { Name = sourceContent.Name };
            targetContent.Save();

            sourcePath = sourceContent.Path;
            targetContainerPath = targetFolder.Path;
        }

        /* ============================================================================ TOOLS */

        private XmlDocument GetMetadataXml(string src, out XmlNamespaceManager nsmgr)
        {
            var xml = new XmlDocument();
            nsmgr = new XmlNamespaceManager(xml.NameTable);
            nsmgr.AddNamespace("edmx", "http://schemas.microsoft.com/ado/2007/06/edmx");
            nsmgr.AddNamespace("m", "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata");
            nsmgr.AddNamespace("d", "http://schemas.microsoft.com/ado/2007/08/dataservices");
            nsmgr.AddNamespace("m", "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata");
            nsmgr.AddNamespace("x", "http://schemas.microsoft.com/ado/2007/05/edm");
            xml.LoadXml(src);
            return xml;
        }
    }
}
