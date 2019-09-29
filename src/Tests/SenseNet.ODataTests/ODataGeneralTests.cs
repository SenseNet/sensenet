using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.OData;
using Task = System.Threading.Tasks.Task;
// ReSharper disable IdentifierTypo
// ReSharper disable StringLiteralTypo

namespace SenseNet.ODataTests
{
    [TestClass]
    public class ODataGeneralTests : ODataTestBase
    {
        [TestMethod]
        public async Task OD_GET_ServiceDocument()
        {
            await ODataTestAsync(async () =>
            {
                // ACTION
                var response = await ODataGetAsync("/OData.svc", "");

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
                var response = await ODataGetAsync("/OData.svc/$metadata", "").ConfigureAwait(false);

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
                    var hasId = node.SelectSingleNode("x:Property[@Name = 'Id']", nsmgr) != null;
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
                var response = await ODataGetAsync("/OData.svc/Root/IMS/BuiltIn/Portal/$metadata", "").ConfigureAwait(false);

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
                var response = await ODataGetAsync("/OData.svc/Root/HiEveryBody/$metadata", "").ConfigureAwait(false);

                // ASSERT: full metadata
                var metaXml = GetMetadataXml(response.Result, out var nsmgr);

                var allTypes = metaXml.SelectNodes("//x:EntityType", nsmgr);
                Assert.IsNotNull(allTypes);
                Assert.AreEqual(ContentType.GetContentTypes().Length, allTypes.Count);
            }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task OD_GET_MissingEntity()
        {
            await ODataTestAsync(async () =>
            {
                var response = await ODataGetAsync("/OData.svc/Root('HiEveryBody')", "").ConfigureAwait(false);

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

                var response = await ODataGetAsync("/OData.svc/Root('IMS')", "");

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
                var response = await ODataGetAsync("/OData.svc/Root/IMS/BuiltIn/Portal", "");

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
                var response = await ODataGetAsync("/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/Members", "");

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
                var response1 = await ODataGetAsync("/OData.svc/Root('IMS')/Id", "");

                // ASSERT 1
                var entity = GetEntity(response1);
                Assert.AreEqual(imsId, entity.Id);

                // ACTION 2
                var response2 = await ODataGetAsync("/OData.svc/Root('IMS')/Id/$value", "");

                // ASSERT 2
                Assert.AreEqual(imsId.ToString(), response2.Result);
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_GET_GetEntityById()
        {
            await ODataTestAsync(async () =>
            {
                var content = Content.Load(1);
                var id = content.Id;

                var response = await ODataGetAsync("/OData.svc/Content(" + id + ")", "");

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
                var response = await ODataGetAsync("/OData.svc/Content(qwer)", "");

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

                var response = await ODataGetAsync("/OData.svc/Content(" + content.Id + ")/Name", "");

                var entity = GetEntity(response);
                Assert.AreEqual(content.Name, entity.Name);
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_GET_Collection_Projection()
        {
            await ODataTestAsync(async () =>
            {
                var response = await ODataGetAsync("/OData.svc/Root/IMS/BuiltIn/Portal", "?$select=Id,Name");

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
                var response = await ODataGetAsync("/OData.svc/Root('IMS')", "?$select=Id,Name");

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
                var response = await ODataGetAsync("/OData.svc/Root('IMS')", "");

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

        //UNDONE:ODATA:TEST: Implement this test: OData_Getting_ContentList_NoProjection
        /**/
        //        //[TestMethod]
        //        public async Task OD_GET_ContentList_NoProjection()
        //        {
        //            Assert.Inconclusive("InMemorySchemaWriter.CreatePropertyType is partially implemented.");

        //            Test(() =>
        //            {
        //                CreateTestSite();

        //                var testRoot = CreateTestRoot("ODataTestRoot");

        //                string listDef = @"<?xml version='1.0' encoding='utf-8'?>
        //<ContentListDefinition xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentListDefinition'>
        //	<Fields>
        //		<ContentListField name='#ListField1' type='ShortText'/>
        //		<ContentListField name='#ListField2' type='Integer'/>
        //		<ContentListField name='#ListField3' type='Reference'/>
        //	</Fields>
        //</ContentListDefinition>
        //";
        //                string path = RepositoryPath.Combine(testRoot.Path, "Cars");
        //                if (Node.Exists(path))
        //                    Node.ForceDelete(path);
        //                ContentList list = new ContentList(testRoot);
        //                list.Name = "Cars";
        //                list.ContentListDefinition = listDef;
        //                list.AllowedChildTypes = new ContentType[] { ContentType.GetByName("Car") };
        //                list.Save();

        //                var car = Content.CreateNew("Car", list, "Car1");
        //                car.Save();
        //                car = Content.CreateNew("Car", list, "Car2");
        //                car.Save();

        //                var entities = await ODataGetAsync("/OData.svc" + list.Path, "");

        //                var entity = entities.First();
        //                var entityPropNames = entity.AllProperties.Select(y => y.Key).ToArray();

        //                var allowedFieldNames = new List<string>();
        //                allowedFieldNames.AddRange(ContentType.GetByName("Car").FieldSettings.Select(f => f.Name));
        //                allowedFieldNames.AddRange(ContentType.GetByName("File").FieldSettings.Select(f => f.Name));
        //                allowedFieldNames.AddRange(list.ListFieldNames);
        //                allowedFieldNames.AddRange(new[] { "__metadata", "IsFile", "Actions", "IsFolder" });
        //                allowedFieldNames = allowedFieldNames.Distinct().ToList();

        //                var a = entityPropNames.Except(allowedFieldNames).ToArray();
        //                var b = allowedFieldNames.Except(entityPropNames).ToArray();

        //                Assert.IsTrue(a.Length == 0, String.Format("Expected empty but contains: '{0}'", string.Join("', '", a)));
        //                Assert.IsTrue(b.Length == 0, String.Format("Expected empty but contains: '{0}'", string.Join("', '", b)));
        //            }).ConfigureAwait(false);
        //        }

        [TestMethod]
        public async Task OD_GET_ContentQuery()
        {
            await IsolatedODataTestAsync(async () =>
            {
                var folderName = "OData_ContentQuery";
                InstallCarContentType();

                var site = new SystemFolder(Repository.Root) { Name = Guid.NewGuid().ToString() };
                site.Save();

                //var allowedTypes = site.GetAllowedChildTypeNames().ToList();
                //allowedTypes.Add("Car");
                //site.AllowChildTypes(allowedTypes);
                //site.Save();

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

                var response1 = await ODataGetAsync("/OData.svc/Root", "?$select=Id,Path&query=asdf+AND+Type%3aCar+.SORT%3aPath+.AUTOFILTERS%3aOFF");

                var entities1 = GetEntities(response1);
                var realGlobal = string.Join(", ", entities1.Select(e => e.Id));
                Assert.AreEqual(realGlobal, expectedGlobal);

                var response2 = await ODataGetAsync($"/OData.svc/Root/{site.Name}/{folderName}", "?$select=Id,Path&query=asdf+AND+Type%3aCar+.SORT%3aPath+.AUTOFILTERS%3aOFF");

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
                var response = await ODataGetAsync("/OData.svc/Root", "?$select=Name&query=" + odataQueryText);

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
                var response = await ODataGetAsync("/OData.svc/Root/System/Schema/ContentTypes/GenericContent", "?$orderby=Name desc&$skip=4&$top=3&$inlinecount=allpages");

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
                var response = await ODataGetAsync("/OData.svc/Root/IMS/BuiltIn/Portal/$count", "");

                var folder = Node.Load<Folder>("/Root/IMS/BuiltIn/Portal");
                Assert.AreEqual(folder.Children.Count().ToString(), response.Result);
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_GET_Collection_CountTop()
        {
            await ODataTestAsync(async () =>
            {
                var response = await ODataGetAsync("/OData.svc/Root/IMS/BuiltIn/Portal/$count", "?$top=3");

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

                var response = await ODataGetAsync("/OData.svc" + path, "?metadata=no&$orderby=Name asc&$select=Id,Id,Name,Name,Path");

                var entities = GetEntities(response);
                Assert.AreEqual(nodecount, entities.Length);
                Assert.AreEqual("Id,Name,Path", string.Join(",", entities[0].AllProperties.Keys.ToArray()));
            }).ConfigureAwait(false);
        }

        //UNDONE:ODATA:TEST: Implement this test: OData_Select_AspectField
        /*[TestMethod]
          public async Task OD_GET_Select_AspectField()
          {
              Test(() =>
              {
                  InstallCarContentType();
                  var site = CreateTestSite();
                  var allowedTypes = site.GetAllowedChildTypeNames().ToList();
                  allowedTypes.Add("Car");
                  site.AllowChildTypes(allowedTypes);
                  site.Save();

                  var testRoot = CreateTestRoot("ODataTestRoot");

                  var aspect1 = EnsureAspect("Aspect1");
                  aspect1.AspectDefinition = @"<AspectDefinition xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/AspectDefinition'>
  <Fields>
      <AspectField name='Field1' type='ShortText' />
    </Fields>
  </AspectDefinition>";
                  aspect1.Save();

                  var folder = new Folder(testRoot) { Name = Guid.NewGuid().ToString() };
                  folder.Save();

                  var content1 = Content.CreateNew("Car", folder, "Car1");
                  content1.AddAspects(aspect1);
                  content1["Aspect1.Field1"] = "asdf";
                  content1.Save();

                  var content2 = Content.CreateNew("Car", folder, "Car2");
                  content2.AddAspects(aspect1);
                  content2["Aspect1.Field1"] = "qwer";
                  content2.Save();

                  var entities = await ODataGetAsync("/OData.svc" + folder.Path, "?$orderby=Name asc&$select=Name,Aspect1.Field1");

                  Assert.IsTrue(entities.Count() == 2, string.Format("entities.Count is ({0}), expected: 2", entities.Count()));
                  Assert.IsTrue(entities[0].Name == "Car1", string.Format("entities[0].Name is ({0}), expected: 'Car1'", entities[0].Name));
                  Assert.IsTrue(entities[1].Name == "Car2", string.Format("entities[1].Name is ({0}), expected: 'Car2'", entities[0].Name));
                  Assert.IsTrue(entities[0].AllProperties.ContainsKey("Aspect1.Field1"), "entities[0] does not contain 'Aspect1.Field1'");
                  Assert.IsTrue(entities[1].AllProperties.ContainsKey("Aspect1.Field1"), "entities[1] does not contain 'Aspect1.Field1'");
                  var value1 = (string)((JValue)entities[0].AllProperties["Aspect1.Field1"]).Value;
                  var value2 = (string)((JValue)entities[1].AllProperties["Aspect1.Field1"]).Value;
                  Assert.IsTrue(value1 == "asdf", string.Format("entities[0].AllProperties[\"Aspect1.Field1\"] is ({0}), expected: 'asdf'", value1));
                  Assert.IsTrue(value2 == "qwer", string.Format("entities[0].AllProperties[\"Aspect1.Field1\"] is ({0}), expected: 'qwer'", value2));
              });
          }*/
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



        //UNDONE:ODATA:TEST: Implement this test: OD_GET_Expand
        /*[TestMethod]
        public async Task OD_GET_Expand()
        {
            await ODataTestAsync(async () =>
            {
                CreateTestSite();

                EnsureCleanAdministratorsGroup();

                var count = CreateSafeContentQuery("InFolder:/Root/IMS/BuiltIn/Portal .COUNTONLY").Execute().Count;
                var expectedJson = @"
{
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
      }
    ],
    ""Name"": ""Administrators""
  }
}";

                var jsonText = await ODataGetAsync("/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')", "?$expand=Members,ModifiedBy&$select=Id,Members/Id,Name,Members/Name&metadata=minimal");

                var raw = jsonText.ToString().Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace(" ", "");
                var exp = expectedJson.Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace(" ", "");
                Assert.AreEqual(exp, raw);
            }).ConfigureAwait(false);
        }*/

        [TestMethod]
        public async Task OD_GET_Expand_Level2_Noselect()
        {
            await ODataTestAsync(async () =>
            {
                EnsureManagerOfAdmin();

                var response = await ODataGetAsync(
                    "/OData.svc/Root/IMS/BuiltIn('Portal')",
                    "?$expand=CreatedBy/Manager");

                var entity = GetEntity(response);
                var createdBy = entity.CreatedBy;
                var createdBy_manager = createdBy.Manager;
                Assert.IsTrue(entity.AllPropertiesSelected);
                Assert.IsTrue(createdBy.AllPropertiesSelected);
                Assert.IsTrue(createdBy_manager.AllPropertiesSelected);
                Assert.IsTrue(createdBy.Manager.CreatedBy.IsDeferred);
                Assert.IsTrue(createdBy.Manager.Manager.IsDeferred);
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
                    "?$expand=CreatedBy/Manager&$select=CreatedBy");

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

                var response = await ODataGetAsync("/OData.svc/Root/IMS/BuiltIn('Portal')", "?$expand=CreatedBy/Manager&$select=CreatedBy/Manager");

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

                var response = await ODataGetAsync("/OData.svc/Root/IMS/BuiltIn('Portal')", "?$expand=CreatedBy/Manager&$select=CreatedBy/Manager/Id");

                var entity = GetEntity(response);
                var id = entity.CreatedBy.Manager.Id;
                Assert.IsFalse(entity.AllPropertiesSelected);
                Assert.IsFalse(entity.CreatedBy.AllPropertiesSelected);
                Assert.IsNull(entity.CreatedBy.CreatedBy);
                Assert.IsFalse(entity.CreatedBy.Manager.AllPropertiesSelected);
                Assert.IsTrue(entity.CreatedBy.Manager.Id > 0);
                Assert.IsNull(entity.CreatedBy.Manager.Path);
            }).ConfigureAwait(false);
        }

        //UNDONE:ODATA:TEST: Implement this 5 tests
        /*[TestMethod]
        public async Task OD_GET_UserAvatarByRef()
        {
            await ODataTestAsync(async () =>
            {
                var testDomain = new Domain(Repository.ImsFolder) { Name = "Domain1" };
                testDomain.Save();

                var testUser = new User(testDomain) { Name = "User1" };
                testUser.Save();

                var testSite = CreateTestSite();

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
                var entity = await ODataGetAsync($"/OData.svc/Root/IMS/{testDomain.Name}('{testUser.Name}')",
                    "?metadata=no&$select=Avatar");

                // ASSERT
                var avatarString = entity.AllProperties["Avatar"].ToString();
                Assert.IsTrue(avatarString.Contains("Url"));
                Assert.IsTrue(avatarString.Contains(testAvatar.Path));
            }).ConfigureAwait(false);
        }*/
        /*[TestMethod]
        public async Task OD_GET_UserAvatarUpdateRef()
        {
            await ODataTestAsync(async () =>
            {
                var testDomain = new Domain(Repository.ImsFolder) { Name = "Domain1" };
                testDomain.Save();

                var testUser = new User(testDomain) { Name = "User1" };
                testUser.Save();

                var testSite = CreateTestSite();

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
                var result = ODataPATCH<ODataEntity>($"/OData.svc/Root/IMS/{testDomain.Name}('{testUser.Name}')",
                    "metadata=no&$select=Avatar,ImageRef,ImageData",
                    $"(models=[{{\"Avatar\": {testAvatar2.Id}}}])");

                // ASSERT
                if (result is ODataError error)
                    Assert.AreEqual("", error.Message);
                var entity = result as ODataEntity;
                if (entity == null)
                    Assert.Fail($"Result is {result.GetType().Name} but ODataEntity is expected.");

                var avatarString = entity.AllProperties["Avatar"].ToString();
                Assert.IsTrue(avatarString.Contains("Url"));
                Assert.IsTrue(avatarString.Contains(testAvatar2.Path));
            }).ConfigureAwait(false);
        }*/
        /*[TestMethod]
        public async Task OD_GET_UserAvatarUpdateRefByPath()
        {
            await ODataTestAsync(async () =>
            {
                var testDomain = new Domain(Repository.ImsFolder) { Name = "Domain1" };
                testDomain.Save();

                var testUser = new User(testDomain) { Name = "User1" };
                testUser.Save();

                var testSite = CreateTestSite();

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
                var result = ODataPATCH<ODataEntity>($"/OData.svc/Root/IMS/{testDomain.Name}('{testUser.Name}')",
                    "metadata=no&$select=Avatar,ImageRef,ImageData",
                    $"(models=[{{\"Avatar\": \"{testAvatar2.Path}\"}}])");

                // ASSERT
                if (result is ODataError error)
                    Assert.AreEqual("", error.Message);
                var entity = result as ODataEntity;
                if (entity == null)
                    Assert.Fail($"Result is {result.GetType().Name} but ODataEntity is expected.");

                var avatarString = entity.AllProperties["Avatar"].ToString();
                Assert.IsTrue(avatarString.Contains("Url"));
                Assert.IsTrue(avatarString.Contains(testAvatar2.Path));
            }).ConfigureAwait(false);
        }*/
        /*[TestMethod]
        public async Task OD_GET_UserAvatarByInnerData()
        {
            await ODataTestAsync(async () =>
            {
                var testDomain = new Domain(Repository.ImsFolder) { Name = "Domain1" };
                testDomain.Save();

                var testUser = new User(testDomain) { Name = "User1" };
                testUser.Save();

                var testSite = CreateTestSite();

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
                var entity = await ODataGetAsync($"/OData.svc/Root/IMS/{testDomain.Name}('{testUser.Name}')",
                    "?metadata=no&$select=Avatar,ImageRef,ImageData");

                // ASSERT
                var avatarString = entity.AllProperties["Avatar"].ToString();
                Assert.IsTrue(avatarString.Contains("Url"));
                Assert.IsTrue(avatarString.Contains($"/binaryhandler.ashx?nodeid={testUser.Id}&propertyname=ImageData"));
            }).ConfigureAwait(false);
        }*/
        /*[TestMethod]
        public async Task OD_GET_UserAvatarUpdateInnerDataToRef()
        {
            await ODataTestAsync(async () =>
            {
                var testDomain = new Domain(Repository.ImsFolder) { Name = "Domain1" };
                testDomain.Save();

                var testUser = new User(testDomain) { Name = "User1" };
                testUser.Save();

                var testSite = CreateTestSite();

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
                var result = ODataPATCH<ODataEntity>($"/OData.svc/Root/IMS/{testDomain.Name}('{testUser.Name}')",
                    "metadata=no&$select=Avatar,ImageRef,ImageData",
                    $"(models=[{{\"Avatar\": {testAvatar.Id}}}])");

                // ASSERT
                if (result is ODataError error)
                    Assert.AreEqual("", error.Message);
                var entity = result as ODataEntity;
                if (entity == null)
                    Assert.Fail($"Result is {result.GetType().Name} but ODataEntity is expected.");

                var avatarString = entity.AllProperties["Avatar"].ToString();
                Assert.IsTrue(avatarString.Contains("Url"));
                Assert.IsTrue(avatarString.Contains(testAvatar.Path));
            }).ConfigureAwait(false);
        }*/

        //UNDONE:ODATA:TEST: Remove inconclusive test result and implement this test.
        /*[TestMethod]
        //public async Task OD_GET_OrderByNumericDouble()
        //{
        //    Assert.Inconclusive("OData_OrderByNumericDouble is commented out (uses LuceneManager)");

        //    //            var testRoot = CreateTestRoot("ODataTestRoot");

        //    //            var contentTypeName = "OData_OrderByNumericDouble_ContentType";

        //    //            var ctd = @"<?xml version='1.0' encoding='utf-8'?>
        //    //<ContentType name='" + contentTypeName + @"' parentType='GenericContent' handler='SenseNet.ContentRepository.GenericContent' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
        //    //  <DisplayName>$Ctd-Resource,DisplayName</DisplayName>
        //    //  <Description>$Ctd-Resource,Description</Description>
        //    //  <Icon>Resource</Icon>
        //    //  <Fields>
        //    //    <Field name='CustomIndex' type='Number'>
        //    //      <DisplayName>CustomIndex</DisplayName>
        //    //      <Description>CustomIndex</Description>
        //    //      <Configuration />
        //    //    </Field>
        //    //  </Fields>
        //    //</ContentType>";

        //    //            ContentTypeInstaller.InstallContentType(ctd);
        //    //            var root = new Folder(testRoot) { Name = Guid.NewGuid().ToString() };
        //    //            root.Save();

        //    //            Content content;

        //    //            content = Content.CreateNew(contentTypeName, root, "Content-1"); content["CustomIndex"] = 6.0; content.Save();
        //    //            content = Content.CreateNew(contentTypeName, root, "Content-2"); content["CustomIndex"] = 3.0; content.Save();
        //    //            content = Content.CreateNew(contentTypeName, root, "Content-3"); content["CustomIndex"] = 2.0; content.Save();
        //    //            content = Content.CreateNew(contentTypeName, root, "Content-4"); content["CustomIndex"] = 4.0; content.Save();
        //    //            content = Content.CreateNew(contentTypeName, root, "Content-5"); content["CustomIndex"] = 5.0; content.Save();
        //    //            SenseNet.Search.Indexing.LuceneManager.Commit(true);

        //    //            try
        //    //            {
        //    //                var queryResult = ContentQuery.Query("ParentId:" + root.Id + " .SORT:CustomIndex .AUTOFILTERS:OFF").Nodes;
        //    //                var names = string.Join(", ", queryResult.Select(n => n.Name).ToArray());
        //    //                var expectedNames = "Content-3, Content-2, Content-4, Content-5, Content-1";
        //    //                Assert.AreEqual(expectedNames, names);

        //    //                var entities = await ODataGetAsync("/OData.svc" + root.Path, "enableautofilters=false$select=Id,Path,Name,CustomIndex&$expand=,CheckedOutTo&$orderby=Name asc&$filter=(ContentType eq '" + contentTypeName + "')&$top=20&$skip=0&$inlinecount=allpages&metadata=no");
        //    //                names = string.Join(", ", entities.Select(e => e.Name).ToArray());
        //    //                Assert.AreEqual("Content-1, Content-2, Content-3, Content-4, Content-5", names);

        //    //                entities = await ODataGetAsync("/OData.svc" + root.Path, "enableautofilters=false$select=Id,Path,Name,CustomIndex&$expand=,CheckedOutTo&$orderby=CustomIndex asc&$filter=(ContentType eq '" + contentTypeName + "')&$top=20&$skip=0&$inlinecount=allpages&metadata=no");
        //    //                names = string.Join(", ", entities.Select(e => e.Name).ToArray());
        //    //                Assert.AreEqual(expectedNames, names);
        //    //            }
        //    //            finally
        //    //            {
        //    //                root.ForceDelete();
        //    //                ContentTypeInstaller.RemoveContentType(contentTypeName);
        //    //            }
        //}*/

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
