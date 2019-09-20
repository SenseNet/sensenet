using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.OData;
using SenseNet.OData.Responses;

namespace SenseNet.ODataTests
{
    [TestClass]
    public class BasicTests : ODataTestBase
    {
        [TestMethod]
        public void OD_Getting_Entity()
        {
            ODataTest(() =>
            {
                var content = Content.Load("/Root/IMS");

                var response = ODataGET<ODataSingleContentResponse>("/OData.svc/Root('IMS')", "");

                var odataContent = response.Value;
                Assert.AreEqual(ODataResponseType.SingleContent, response.Type);
                Assert.AreEqual(content.Id, odataContent.Id);
                Assert.AreEqual(content.Name, odataContent.Name);
                Assert.AreEqual(content.Path, odataContent.Path);
                ////Assert.AreEqual(content.ContentType.Name, odataContent.Name);
            });
        }
        [TestMethod]
        public void OD_Getting_ChildrenCollection()
        {
            ODataTest(() =>
            {
                var response = ODataGET<ODataChildrenCollectionResponse>("/OData.svc/Root/IMS/BuiltIn/Portal", "");

                var entities = response.Value;
                var origIds = Node.Load<Folder>("/Root/IMS/BuiltIn/Portal").Children.Select(f => f.Id).ToArray();
                var ids = entities.Select(e => e.Id).ToArray();

                Assert.AreEqual(ODataResponseType.ChildrenCollection, response.Type);
                Assert.AreEqual(0, origIds.Except(ids).Count());
                Assert.AreEqual(0, ids.Except(origIds).Count());
            });
        }
        [TestMethod]
        public void OD_Getting_CollectionViaProperty()
        {
            ODataTest(() =>
            {
                var response = ODataGET<ODataMultipleContentResponse>("/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/Members", "");

                Assert.AreEqual(ODataResponseType.MultipleContent, response.Type);
                var items = response.Value;
                var origIds = Node.Load<Group>("/Root/IMS/BuiltIn/Portal/Administrators").Members.Select(f => f.Id).ToArray();
                var ids = items.Select(e => e.Id).ToArray();

                Assert.AreEqual(0, origIds.Except(ids).Count());
                Assert.AreEqual(0, ids.Except(origIds).Count());
            });
        }
        [TestMethod]
        public void OD_Getting_SimplePropertyAndRaw()
        {
            ODataTest(() =>
            {
                var imsId = Repository.ImsFolder.Id;

                var response1 = ODataGET<ODataSingleContentResponse>("/OData.svc/Root('IMS')/Id", "");

                var value = response1.Value;
                Assert.AreEqual(ODataResponseType.SingleContent, response1.Type);
                Assert.AreEqual(imsId, value.Id);

                var response2 = ODataGET<ODataRawResponse>("/OData.svc/Root('IMS')/Id/$value", "");

                Assert.AreEqual(ODataResponseType.RawData, response2.Type);
                Assert.AreEqual(imsId, response2.Value);
            });
        }
        [TestMethod]
        public void OD_GetEntityById()
        {
            ODataTest(() =>
            {
                var content = Content.Load(1);
                var id = content.Id;

                var response = ODataGET<ODataSingleContentResponse>("/OData.svc/Content(" + id + ")", "");

                var odataContent = response.Value;
                Assert.AreEqual(ODataResponseType.SingleContent, response.Type);
                Assert.AreEqual(id, odataContent.Id);
                Assert.AreEqual(content.Path, odataContent.Path);
                Assert.AreEqual(content.Name, odataContent.Name);
                Assert.AreEqual(content.ContentType.Name, odataContent.ContentType);
            });
        }
        [TestMethod]
        public void OD_GetEntityById_InvalidId()
        {
            ODataTest(() =>
            {
                var response = ODataGET<ODataErrorResponse>("/OData.svc/Content(qwer)", "");

                var exception = response.Value;
                Assert.AreEqual(ODataResponseType.Error, response.Type);
                Assert.AreEqual(ODataExceptionCode.InvalidId, exception.ODataExceptionCode);
            });
        }
        [TestMethod]
        public void OD_GetPropertyOfEntityById()
        {
            ODataTest(() =>
            {
                var content = Content.Load(1);

                var response = ODataGET<ODataSingleContentResponse>("/OData.svc/Content(" + content.Id + ")/Name", "");

                var value = response.Value;
                Assert.AreEqual(ODataResponseType.SingleContent, response.Type);
                Assert.AreEqual(content.Name, value.Name);
            });
        }
        [TestMethod]
        public void OD_Getting_Collection_Projection()
        {
            ODataTest(() =>
            {
                var response = ODataGET<ODataChildrenCollectionResponse>("/OData.svc/Root/IMS/BuiltIn/Portal", "?$select=Id,Name");

                var items = response.Value;
                Assert.AreEqual(ODataResponseType.ChildrenCollection, response.Type);
                var itemIndex = 0;
                foreach (var item in items)
                {
                    Assert.AreEqual(3, item.Count);
                    Assert.IsTrue(item.ContainsKey("__metadata"));
                    Assert.IsTrue(item.ContainsKey("Id"));
                    Assert.IsTrue(item.ContainsKey("Name"));
                    Assert.IsNull(item.Path);
                    Assert.IsNull(item.ContentType);
                    itemIndex++;
                }
            });
        }
        [TestMethod]
        public void OD_Getting_Entity_Projection()
        {
            ODataTest(() =>
            {
                var response = ODataGET<ODataSingleContentResponse>("/OData.svc/Root('IMS')", "?$select=Id,Name");

                var odataContent = response.Value;
                Assert.AreEqual(ODataResponseType.SingleContent, response.Type);

                Assert.IsTrue(odataContent.ContainsKey("__metadata"));
                Assert.IsTrue(odataContent.ContainsKey("Id"));
                Assert.IsTrue(odataContent.ContainsKey("Name"));
                Assert.IsNull(odataContent.Path);
                Assert.IsNull(odataContent.ContentType);
            });
        }
        [TestMethod]
        public void OData_Getting_Entity_NoProjection()
        {
            ODataTest(() =>
            {
                var response = ODataGET<ODataSingleContentResponse>("/OData.svc/Root('IMS')", "");

                var odataContent = response.Value;
                Assert.AreEqual(ODataResponseType.SingleContent, response.Type);

                var allowedFieldNames = new List<string>();
                var c = Content.Load("/Root/IMS");
                var ct = c.ContentType;
                var fieldNames = ct.FieldSettings.Select(f => f.Name);
                allowedFieldNames.AddRange(fieldNames);
                allowedFieldNames.AddRange(new[] { "__metadata", "IsFile", "Actions", "IsFolder", "Children" });

                var entityPropNames = odataContent.Select(y => y.Key).ToArray();

                var a = entityPropNames.Except(allowedFieldNames).ToArray();
                var b = allowedFieldNames.Except(entityPropNames).ToArray();

                Assert.AreEqual(0, a.Length);
                Assert.AreEqual(0, b.Length);
            });
        }

        //UNDONE:ODATA: Implement this test: public void OData_Getting_ContentList_NoProjection()

        [TestMethod]
        public void OData_ContentQuery()
        {
            ODataTest(() =>
            {
                var folderName = "OData_ContentQuery";
                InstallCarContentType();

                var site = new SystemFolder(Repository.Root) {Name = Guid.NewGuid().ToString()};
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

                var response1 = ODataGET<ODataChildrenCollectionResponse>("/OData.svc/Root", "?$select=Id,Path&query=asdf+AND+Type%3aCar+.SORT%3aPath+.AUTOFILTERS%3aOFF");

                var realGlobal = string.Join(", ", response1.Value.Select(e => e.Id));
                Assert.AreEqual(realGlobal, expectedGlobal);

                var response2 = ODataGET<ODataChildrenCollectionResponse>($"/OData.svc/Root/{site.Name}/{folderName}", "?$select=Id,Path&query=asdf+AND+Type%3aCar+.SORT%3aPath+.AUTOFILTERS%3aOFF");

                var realLocal = string.Join(", ", response2.Value.Select(e => e.Id));
                Assert.AreEqual(realLocal, expectedLocal);
            });
        }
        [TestMethod]
        public void OData_ContentQuery_Nested()
        {
            ODataTest(() =>
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
                var response = ODataGET<ODataChildrenCollectionResponse>("/OData.svc/Root", "?$select=Name&query=" + odataQueryText);

                // ASSERT
                var resultNamesOData = response.Value.Select(x => x.Name).ToArray();
                Assert.AreEqual("User1, User4, User7", string.Join(", ", resultNamesOData));
            });
        }
        [TestMethod]
        public void OData_Getting_Collection_OrderTopSkipCount()
        {
            ODataTest(() =>
            {
                var response = ODataGET<ODataChildrenCollectionResponse>("/OData.svc/Root/System/Schema/ContentTypes/GenericContent", "?$orderby=Name desc&$skip=4&$top=3&$inlinecount=allpages");

                var ids = response.Value.Select(e => e.Id);
                var origIds = CreateSafeContentQuery(
                    "+InFolder:/Root/System/Schema/ContentTypes/GenericContent .REVERSESORT:Name .SKIP:4 .TOP:3 .AUTOFILTERS:OFF")
                    .Execute().Nodes.Select(n => n.Id);
                var expected = String.Join(", ", origIds);
                var actual = String.Join(", ", ids);
                Assert.AreEqual(expected, actual);
                Assert.AreEqual(ContentType.GetByName("GenericContent").ChildTypes.Count, response.AllCount);
            });
        }
        [TestMethod]
        public void OData_Getting_Collection_Count()
        {
            ODataTest(() =>
            {
                var response = ODataGET<ODataCollectionCountResponse>("/OData.svc/Root/IMS/BuiltIn/Portal/$count", "");

                var folder = Node.Load<Folder>("/Root/IMS/BuiltIn/Portal");
                Assert.AreEqual(folder.Children.Count().ToString(), response.Value.ToString());
            });
        }
        [TestMethod]
        public void OData_Getting_Collection_CountTop()
        {
            ODataTest(() =>
            {
                var result = ODataGET<ODataCollectionCountResponse>("/OData.svc/Root/IMS/BuiltIn/Portal/$count", "?$top=3");
                Assert.AreEqual("3", result.Value.ToString());
            });
        }
        [TestMethod]
        public void OData_Select_FieldMoreThanOnce()
        {
            ODataTest(() =>
            {
                var path = User.Administrator.Parent.Path;
                var nodecount = CreateSafeContentQuery($"InFolder:{path} .AUTOFILTERS:OFF .COUNTONLY").Execute().Count;

                var response = ODataGET<ODataChildrenCollectionResponse>("/OData.svc" + path, "?metadata=no&$orderby=Name asc&$select=Id,Id,Name,Name,Path");

                Assert.AreEqual(nodecount, response.Value.Count());
                Assert.AreEqual("Id,Name,Path", string.Join(",", response.Value.First().Keys.ToArray()));
            });
        }

        /*[TestMethod]
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

                var entities = ODataGET<ODataEntities>("/OData.svc" + folder.Path, "?$orderby=Name asc&$select=Name,Aspect1.Field1");

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



        /*[TestMethod]
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

                var jsonText = ODataGET<ODataRaw>("/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')", "?$expand=Members,ModifiedBy&$select=Id,Members/Id,Name,Members/Name&metadata=minimal");

                var raw = jsonText.ToString().Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace(" ", "");
                var exp = expectedJson.Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace(" ", "");
                Assert.AreEqual(exp, raw);
            });
        }*/

        /*[TestMethod]
        public void OData_Expand_Level2_Noselect()
        {
            Test(() =>
            {
                CreateTestSite();

                EnsureManagerOfAdmin();

                var entity = ODataGET<ODataEntity>("/OData.svc/Root/IMS/BuiltIn('Portal')", "?$expand=CreatedBy/Manager");

                var createdBy = entity.CreatedBy;
                var createdBy_manager = createdBy.Manager;
                Assert.IsTrue(entity.AllPropertiesSelected);
                Assert.IsTrue(createdBy.AllPropertiesSelected);
                Assert.IsTrue(createdBy_manager.AllPropertiesSelected);
                Assert.IsTrue(createdBy.Manager.CreatedBy.IsDeferred);
                Assert.IsTrue(createdBy.Manager.Manager.IsDeferred);
            });
        }*/
        /*[TestMethod]
        public void OData_Expand_Level2_Select_Level1()
        {
            Test(() =>
            {
                CreateTestSite();

                EnsureManagerOfAdmin();

                var entity = ODataGET<ODataEntity>("/OData.svc/Root/IMS/BuiltIn('Portal')", "?$expand=CreatedBy/Manager&$select=CreatedBy");

                Assert.IsFalse(entity.AllPropertiesSelected);
                Assert.IsTrue(entity.CreatedBy.AllPropertiesSelected);
                Assert.IsTrue(entity.CreatedBy.CreatedBy.IsDeferred);
                Assert.IsTrue(entity.CreatedBy.Manager.AllPropertiesSelected);
                Assert.IsTrue(entity.CreatedBy.Manager.CreatedBy.IsDeferred);
                Assert.IsTrue(entity.CreatedBy.Manager.Manager.IsDeferred);
            });
        }*/
        /*[TestMethod]
        public void OData_Expand_Level2_Select_Level2()
        {
            Test(() =>
            {
                CreateTestSite();

                EnsureManagerOfAdmin();

                var entity = ODataGET<ODataEntity>("/OData.svc/Root/IMS/BuiltIn('Portal')", "?$expand=CreatedBy/Manager&$select=CreatedBy/Manager");

                Assert.IsFalse(entity.AllPropertiesSelected);
                Assert.IsFalse(entity.CreatedBy.AllPropertiesSelected);
                Assert.IsNull(entity.CreatedBy.CreatedBy);
                Assert.IsTrue(entity.CreatedBy.Manager.AllPropertiesSelected);
                Assert.IsTrue(entity.CreatedBy.Manager.CreatedBy.IsDeferred);
                Assert.IsTrue(entity.CreatedBy.Manager.Manager.IsDeferred);
            });
        }*/
        /*[TestMethod]
        public void OData_Expand_Level2_Select_Level3()
        {
            Test(() =>
            {
                CreateTestSite();

                EnsureManagerOfAdmin();

                var entity = ODataGET<ODataEntity>("/OData.svc/Root/IMS/BuiltIn('Portal')", "?$expand=CreatedBy/Manager&$select=CreatedBy/Manager/Id");

                var id = entity.CreatedBy.Manager.Id;
                Assert.IsFalse(entity.AllPropertiesSelected);
                Assert.IsFalse(entity.CreatedBy.AllPropertiesSelected);
                Assert.IsNull(entity.CreatedBy.CreatedBy);
                Assert.IsFalse(entity.CreatedBy.Manager.AllPropertiesSelected);
                Assert.IsTrue(entity.CreatedBy.Manager.Id > 0);
                Assert.IsNull(entity.CreatedBy.Manager.Path);
            });
        }*/

        /*[TestMethod]
        public void OData_UserAvatarByRef()
        {
            Test(() =>
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
                var entity = ODataGET<ODataEntity>($"/OData.svc/Root/IMS/{testDomain.Name}('{testUser.Name}')",
                    "?metadata=no&$select=Avatar");

                // ASSERT
                var avatarString = entity.AllProperties["Avatar"].ToString();
                Assert.IsTrue(avatarString.Contains("Url"));
                Assert.IsTrue(avatarString.Contains(testAvatar.Path));
            });
        }*/
        /*[TestMethod]
        public void OData_UserAvatarUpdateRef()
        {
            Test(() =>
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
            });
        }*/
        /*[TestMethod]
        public void OData_UserAvatarUpdateRefByPath()
        {
            Test(() =>
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
            });
        }*/
        /*[TestMethod]
        public void OData_UserAvatarByInnerData()
        {
            Test(() =>
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
                var entity = ODataGET<ODataEntity>($"/OData.svc/Root/IMS/{testDomain.Name}('{testUser.Name}')",
                    "?metadata=no&$select=Avatar,ImageRef,ImageData");

                // ASSERT
                var avatarString = entity.AllProperties["Avatar"].ToString();
                Assert.IsTrue(avatarString.Contains("Url"));
                Assert.IsTrue(avatarString.Contains($"/binaryhandler.ashx?nodeid={testUser.Id}&propertyname=ImageData"));
            });
        }*/
        /*[TestMethod]
        public void OData_UserAvatarUpdateInnerDataToRef()
        {
            Test(() =>
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
            });
        }*/

    }
}
