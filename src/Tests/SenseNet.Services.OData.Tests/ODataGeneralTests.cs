using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.Portal.OData;
using SenseNet.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.ContentRepository;
using SenseNet.Services.OData.Tests.Results;

namespace SenseNet.Services.OData.Tests
{
    [TestClass]
    public class ODataGeneralTests : ODataTestClass
    {
        [TestMethod]
        public void OData_OrderByNumericDouble()
        {
            Assert.Inconclusive("OData_OrderByNumericDouble is commented out (uses LuceneManager)");

//            var testRoot = CreateTestRoot("ODataTestRoot");

//            var contentTypeName = "OData_OrderByNumericDouble_ContentType";

//            var ctd = @"<?xml version='1.0' encoding='utf-8'?>
//<ContentType name='" + contentTypeName + @"' parentType='GenericContent' handler='SenseNet.ContentRepository.GenericContent' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
//  <DisplayName>$Ctd-Resource,DisplayName</DisplayName>
//  <Description>$Ctd-Resource,Description</Description>
//  <Icon>Resource</Icon>
//  <Fields>
//    <Field name='CustomIndex' type='Number'>
//      <DisplayName>CustomIndex</DisplayName>
//      <Description>CustomIndex</Description>
//      <Configuration />
//    </Field>
//  </Fields>
//</ContentType>";

//            ContentTypeInstaller.InstallContentType(ctd);
//            var root = new Folder(testRoot) { Name = Guid.NewGuid().ToString() };
//            root.Save();

//            Content content;

//            content = Content.CreateNew(contentTypeName, root, "Content-1"); content["CustomIndex"] = 6.0; content.Save();
//            content = Content.CreateNew(contentTypeName, root, "Content-2"); content["CustomIndex"] = 3.0; content.Save();
//            content = Content.CreateNew(contentTypeName, root, "Content-3"); content["CustomIndex"] = 2.0; content.Save();
//            content = Content.CreateNew(contentTypeName, root, "Content-4"); content["CustomIndex"] = 4.0; content.Save();
//            content = Content.CreateNew(contentTypeName, root, "Content-5"); content["CustomIndex"] = 5.0; content.Save();
//            SenseNet.Search.Indexing.LuceneManager.Commit(true);

//            try
//            {
//                var queryResult = ContentQuery.Query("ParentId:" + root.Id + " .SORT:CustomIndex .AUTOFILTERS:OFF").Nodes;
//                var names = string.Join(", ", queryResult.Select(n => n.Name).ToArray());
//                var expectedNames = "Content-3, Content-2, Content-4, Content-5, Content-1";
//                Assert.AreEqual(expectedNames, names);

//                var entities = ODataGET<ODataEntities>("/OData.svc" + root.Path, "enableautofilters=false$select=Id,Path,Name,CustomIndex&$expand=,CheckedOutTo&$orderby=Name asc&$filter=(ContentType eq '" + contentTypeName + "')&$top=20&$skip=0&$inlinecount=allpages&metadata=no");
//                names = string.Join(", ", entities.Select(e => e.Name).ToArray());
//                Assert.AreEqual("Content-1, Content-2, Content-3, Content-4, Content-5", names);

//                entities = ODataGET<ODataEntities>("/OData.svc" + root.Path, "enableautofilters=false$select=Id,Path,Name,CustomIndex&$expand=,CheckedOutTo&$orderby=CustomIndex asc&$filter=(ContentType eq '" + contentTypeName + "')&$top=20&$skip=0&$inlinecount=allpages&metadata=no");
//                names = string.Join(", ", entities.Select(e => e.Name).ToArray());
//                Assert.AreEqual(expectedNames, names);
//            }
//            finally
//            {
//                root.ForceDelete();
//                ContentTypeInstaller.RemoveContentType(contentTypeName);
//            }
        }

        [TestMethod]
        public void OData_Getting_Entity()
        {
            Test(() =>
            {
                CreateTestSite();

                var entity = ODataGET<ODataEntity>("/OData.svc/Root('IMS')", "");

                var nodeHead = NodeHead.Get(entity.Path);
                Assert.AreEqual(nodeHead.Id, entity.Id);
            });
        }

        [TestMethod]
        public void OData_Getting_Collection()
        {
            Test(() =>
            {
                CreateTestSite();

                var entities = ODataGET<ODataEntities>("/OData.svc/Root/IMS/BuiltIn/Portal", "");

                var origIds = Node.Load<Folder>("/Root/IMS/BuiltIn/Portal").Children.Select(f => f.Id).ToArray();
                var ids = entities.Select(e => e.Id).ToArray();

                Assert.AreEqual(0, origIds.Except(ids).Count());
                Assert.AreEqual(0, ids.Except(origIds).Count());
            });
        }
        [TestMethod]
        public void OData_Getting_CollectionViaProperty()
        {
            Test(() =>
            {
                CreateTestSite();

                var entities = ODataGET<ODataEntities>("/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/Members", "");

                var origIds = Node.Load<Group>("/Root/IMS/BuiltIn/Portal/Administrators").Members.Select(f => f.Id).ToArray();
                var ids = entities.Select(e => e.Id).ToArray();

                Assert.AreEqual(0, origIds.Except(ids).Count());
                Assert.AreEqual(0, ids.Except(origIds).Count());
            });
        }
        [TestMethod]
        public void OData_Getting_SimplePropertyAndRaw()
        {
            Test(() =>
            {
                CreateTestSite();

                var result = ODataGET<ODataRaw>("/OData.svc/Root('IMS')/Id", "");

                var json = result.ToString().Replace("\r\n", "").Replace("\t", "").Replace(" ", "");
                Assert.AreEqual("{\"d\":{\"Id\":3}}", json);

                result = ODataGET<ODataRaw>("/OData.svc/Root('IMS')/Id/$value", "");

                json = result.ToString();
                Assert.AreEqual("3", json);
            });
        }

        [TestMethod]
        public void OData_GetEntityById()
        {
            Test(() =>
            {
                CreateTestSite();

                var content = Content.Load(1);
                var id = content.Id;

                var entity = ODataGET<ODataEntity>("/OData.svc/Content(" + id + ")", "");

                Assert.AreEqual(id, entity.Id);
                Assert.AreEqual(content.Path, entity.Path);
                Assert.AreEqual(content.Name, entity.Name);
                Assert.AreEqual(content.ContentType, entity.ContentType);
            });
        }
        [TestMethod]
        public void OData_GetEntityById_InvalidId()
        {
            Test(() =>
            {
                CreateTestSite();

                var err = ODataGET<ODataError>("/OData.svc/Content(qwer)", "");
                Assert.AreEqual(ODataExceptionCode.InvalidId, err.Code);
            });
        }
        [TestMethod]
        public void OData_GetPropertyOfEntityById()
        {
            Test(() =>
            {
                CreateTestSite();

                var content = Content.Load(1);

                var result = ODataGET<ODataRaw>("/OData.svc/Content(" + content.Id + ")/Name", "");

                var expected = "{\"d\":{\"Name\":\"" + content.Name + "\"}}";
                var actual = result.ToString().Replace("\r", "").Replace("\n", "").Replace("\t", "").Replace(" ", "");
                Assert.AreEqual(expected, actual);
            });
        }

        [TestMethod]
        public void OData_Getting_Collection_Projection()
        {
            Test(() =>
            {
                CreateTestSite();

                var entities = ODataGET<ODataEntities>("/OData.svc/Root/IMS/BuiltIn/Portal", "$select=Id,Name");

                var itemIndex = 0;
                foreach (var entity in entities)
                {
                    var props = entity.AllProperties.ToArray();
                    Assert.AreEqual(3, props.Length);
                    Assert.AreEqual("__metadata", props[0].Key);
                    Assert.AreEqual("Id", props[1].Key);
                    Assert.AreEqual("Name", props[2].Key);
                    itemIndex++;
                }
            });
        }

        [TestMethod]
        public void OData_Getting_Entity_Projection()
        {
            Test(() =>
            {
                CreateTestSite();

                ODataEntity entity;
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc/Root('IMS')", "$select=Id,Name", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    entity = GetEntity(output);
                }
                var props = entity.AllProperties.ToArray();
                Assert.IsTrue(props.Length == 3, string.Format("AllProperties.Count id ({0}), expected: 3", props.Length));
                Assert.IsTrue(props[0].Key == "__metadata", string.Format("AllProperties[0] is ({0}), expected: '__metadata'", props[0].Key));
                Assert.IsTrue(props[1].Key == "Id", string.Format("AllProperties[1] is ({0}), expected: 'Id'", props[1].Key));
                Assert.IsTrue(props[2].Key == "Name", string.Format("AllProperties[2] is ({0}), expected: 'Name'", props[2].Key));
            });
        }
        [TestMethod]
        public void OData_Getting_Entity_NoProjection()
        {
            Test(() =>
            {
                CreateTestSite();

                var entity = ODataGET<ODataEntity>("/OData.svc/Root('IMS')", "");

                var allowedFieldNames = new List<string>();
                var c = Content.Load("/Root/IMS");
                var ct = c.ContentType;
                var fieldNames = ct.FieldSettings.Select(f => f.Name);
                allowedFieldNames.AddRange(fieldNames);
                allowedFieldNames.AddRange(new[] { "__metadata", "IsFile", "Actions", "IsFolder" });

                var entityPropNames = entity.AllProperties.Select(y => y.Key).ToArray();

                var a = entityPropNames.Except(allowedFieldNames).ToArray();
                var b = allowedFieldNames.Except(entityPropNames).ToArray();

                Assert.AreEqual(0, a.Length);
                Assert.AreEqual(0, b.Length);
            });
        }
        [TestMethod]
        public void OData_Getting_ContentList_NoProjection()
        {
            Assert.Inconclusive("InMemorySchemaWriter.CreatePropertyType is partially implemented.");

            Test(() =>
            {
                CreateTestSite();

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

                var entities = ODataGET<ODataEntities>("/OData.svc" + list.Path, "");

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
            });
        }

        [TestMethod]
        public void OData_ContentQuery()
        {
            Test(() =>
            {
                var folderName = "OData_ContentQuery";
                InstallCarContentType();
                var site = CreateTestSite();
                var allowedTypes = site.GetAllowedChildTypeNames().ToList();
                allowedTypes.Add("Car");
                site.AllowChildTypes(allowedTypes);
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

                var entities = ODataGET<ODataEntities>("/OData.svc/Root", "$select=Id,Path&query=asdf+AND+Type%3aCar+.SORT%3aPath+.AUTOFILTERS%3aOFF");

                var realGlobal = string.Join(", ", entities.Select(e => e.Id));
                Assert.AreEqual(realGlobal, expectedGlobal);

                entities = ODataGET<ODataEntities>("/OData.svc/" + folderName, "$select=Id,Path&query=asdf+AND+Type%3aCar+.SORT%3aPath+.AUTOFILTERS%3aOFF");

                var realLocal = string.Join(", ", entities.Select(e => e.Id));
                Assert.AreEqual(realLocal, expectedLocal);
            });
        }

        [TestMethod]
        public void OData_Getting_Collection_OrderTopSkipCount()
        {
            Test(() =>
            {
                CreateTestSite();

                var entities = ODataGET<ODataEntities>("/OData.svc/Root/System/Schema/ContentTypes/GenericContent", "$orderby=Name desc&$skip=4&$top=3&$inlinecount=allpages");

                var ids = entities.Select(e => e.Id);
                var origIds = CreateSafeContentQuery(
                    "+InFolder:/Root/System/Schema/ContentTypes/GenericContent .REVERSESORT:Name .SKIP:4 .TOP:3 .AUTOFILTERS:OFF")
                    .Execute().Nodes.Select(n => n.Id);
                var expected = String.Join(", ", origIds);
                var actual = String.Join(", ", ids);
                Assert.AreEqual(expected, actual);
                Assert.AreEqual(ContentType.GetByName("GenericContent").ChildTypes.Count, entities.TotalCount);
            });
        }

        [TestMethod]
        public void OData_Getting_Collection_Count()
        {
            Test(() =>
            {
                CreateTestSite();

                var result = ODataGET<ODataRaw>("/OData.svc/Root/IMS/BuiltIn/Portal/$count", "");

                var folder = Node.Load<Folder>("/Root/IMS/BuiltIn/Portal");
                Assert.AreEqual(folder.Children.Count().ToString(), result.ToString());
            });
        }
        [TestMethod]
        public void OData_Getting_Collection_CountTop()
        {
            Assert.Inconclusive("OData_Getting_Collection_CountTop is commented out (uses LucQuery)");

            //var lucQueryAcc = new PrivateType(typeof(LucQuery));
            //var originalExecutionAlgorithm = (LucQuery.ContentQueryExecutionAlgorithm)lucQueryAcc.GetStaticField("__executionAlgorithm");
            //lucQueryAcc.SetStaticField("__executionAlgorithm", LucQuery.ContentQueryExecutionAlgorithm.LuceneOnly);

            //try
            //{
            //    var result = ODataGET<ODataRaw>("/OData.svc/Root/IMS/BuiltIn/Portal/$count", "$top=3");
            //    var folder = Node.Load<Folder>("/Root/IMS/BuiltIn/Portal");
            //    Assert.AreEqual("3", result.ToString());
            //}
            //finally
            //{
            //    lucQueryAcc.SetStaticField("__executionAlgorithm", originalExecutionAlgorithm);
            //}
        }

        [TestMethod]
        public void OData_Select_FieldMoreThanOnce()
        {
            Test(() =>
            {
                CreateTestSite();

                var path = User.Administrator.Parent.Path;
                var nodecount = CreateSafeContentQuery($"InFolder:{path} .AUTOFILTERS:OFF .COUNTONLY").Execute().Count;

                var entities = ODataGET<ODataEntities>("/OData.svc" + path, "$orderby=Name asc&$select=Id,Id,Name,Name,Path");

                Assert.AreEqual(nodecount, entities.Count());
            });
        }

        [TestMethod]
        public void OData_Select_AspectField()
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

                var entities = ODataGET<ODataEntities>("/OData.svc" + folder.Path, "$orderby=Name asc&$select=Name,Aspect1.Field1");

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
        public void OData_Expand()
        {
            Test(() =>
            {
                CreateTestSite();

                EnsureCleanAdministratorsGroup();

                var count = CreateSafeContentQuery("InFolder:/Root/IMS/BuiltIn/Portal .COUNTONLY").Execute().Count;
                var expectedJson = @"
{
  ""d"": {
    ""__metadata"": {
      ""uri"": ""/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')"",
      ""type"": ""Group""
    },
    ""Id"": 7,
    ""Members"": [
      {
        ""__metadata"": {
          ""uri"": ""/OData.svc/Root/IMS/BuiltIn/Portal('Admin')"",
          ""type"": ""User""
        },
        ""Id"": 1,
        ""Name"": ""Admin""
      }
    ],
    ""Name"": ""Administrators""
  }
}";

                var jsonText = ODataGET<ODataRaw>("/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')", "$expand=Members,ModifiedBy&$select=Id,Members/Id,Name,Members/Name&metadata=minimal");

                var raw = jsonText.ToString().Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace(" ", "");
                var exp = expectedJson.Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace(" ", "");
                Assert.AreEqual(exp, raw);
            });
        }

        [TestMethod]
        public void OData_Expand_Level2_Noselect()
        {
            Test(() =>
            {
                CreateTestSite();

                EnsureManagerOfAdmin();

                var entity = ODataGET<ODataEntity>("/OData.svc/Root/IMS/BuiltIn('Portal')", "$expand=CreatedBy/Manager");

                var createdBy = entity.CreatedBy;
                var createdBy_manager = createdBy.Manager;
                Assert.IsTrue(entity.AllPropertiesSelected);
                Assert.IsTrue(createdBy.AllPropertiesSelected);
                Assert.IsTrue(createdBy_manager.AllPropertiesSelected);
                Assert.IsTrue(createdBy.Manager.CreatedBy.IsDeferred);
                Assert.IsTrue(createdBy.Manager.Manager.IsDeferred);
            });
        }
        [TestMethod]
        public void OData_Expand_Level2_Select_Level1()
        {
            Test(() =>
            {
                CreateTestSite();

                EnsureManagerOfAdmin();

                var entity = ODataGET<ODataEntity>("/OData.svc/Root/IMS/BuiltIn('Portal')", "$expand=CreatedBy/Manager&$select=CreatedBy");

                Assert.IsFalse(entity.AllPropertiesSelected);
                Assert.IsTrue(entity.CreatedBy.AllPropertiesSelected);
                Assert.IsTrue(entity.CreatedBy.CreatedBy.IsDeferred);
                Assert.IsTrue(entity.CreatedBy.Manager.AllPropertiesSelected);
                Assert.IsTrue(entity.CreatedBy.Manager.CreatedBy.IsDeferred);
                Assert.IsTrue(entity.CreatedBy.Manager.Manager.IsDeferred);
            });
        }
        [TestMethod]
        public void OData_Expand_Level2_Select_Level2()
        {
            Test(() =>
            {
                CreateTestSite();

                EnsureManagerOfAdmin();

                var entity = ODataGET<ODataEntity>("/OData.svc/Root/IMS/BuiltIn('Portal')", "$expand=CreatedBy/Manager&$select=CreatedBy/Manager");

                Assert.IsFalse(entity.AllPropertiesSelected);
                Assert.IsFalse(entity.CreatedBy.AllPropertiesSelected);
                Assert.IsNull(entity.CreatedBy.CreatedBy);
                Assert.IsTrue(entity.CreatedBy.Manager.AllPropertiesSelected);
                Assert.IsTrue(entity.CreatedBy.Manager.CreatedBy.IsDeferred);
                Assert.IsTrue(entity.CreatedBy.Manager.Manager.IsDeferred);
            });
        }
        [TestMethod]
        public void OData_Expand_Level2_Select_Level3()
        {
            Test(() =>
            {
                CreateTestSite();

                EnsureManagerOfAdmin();

                var entity = ODataGET<ODataEntity>("/OData.svc/Root/IMS/BuiltIn('Portal')", "$expand=CreatedBy/Manager&$select=CreatedBy/Manager/Id");

                var id = entity.CreatedBy.Manager.Id;
                Assert.IsFalse(entity.AllPropertiesSelected);
                Assert.IsFalse(entity.CreatedBy.AllPropertiesSelected);
                Assert.IsNull(entity.CreatedBy.CreatedBy);
                Assert.IsFalse(entity.CreatedBy.Manager.AllPropertiesSelected);
                Assert.IsTrue(entity.CreatedBy.Manager.Id > 0);
                Assert.IsNull(entity.CreatedBy.Manager.Path);
            });
        }

    }
}
