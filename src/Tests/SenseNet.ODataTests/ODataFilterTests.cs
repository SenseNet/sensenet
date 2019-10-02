using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.Search;
using Task = System.Threading.Tasks.Task;

// ReSharper disable StringLiteralTypo

namespace SenseNet.ODataTests
{
    //[TestClass]
    public class ODataFilterTests : ODataTestBase
    {
        [TestMethod]
        public async Task OD_GET_Filter_StartsWithEqTrue()
        {
            await ODataTestAsync(async () =>
            {
                var response = await ODataGetAsync(
                    "/OData.svc/Root",
                    "?$filter=startswith(Name, 'IM') eq true")
                    .ConfigureAwait(false);

                var entities = GetEntities(response);
                var origIds = Repository.Root.Children
                    .Where(x => x.Name.StartsWith("IM", StringComparison.OrdinalIgnoreCase))
                    .Select(f => f.Id)
                    .ToArray();
                var ids = entities.Select(e => e.Id).ToArray();

                Assert.IsTrue(origIds.Length > 0);
                Assert.AreEqual(0, origIds.Except(ids).Count());
                Assert.AreEqual(0, ids.Except(origIds).Count());
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_GET_Filter_EndsWithEqTrue()
        {
            await ODataTestAsync(async () =>
            {
                var response = await ODataGetAsync(
                    "/OData.svc/Root",
                    "?$filter=endswith(Name, 'MS') eq true")
                    .ConfigureAwait(false);

                var entities = GetEntities(response);
                var origIds = Repository.Root.Children
                    .Where(x => x.Name.EndsWith("MS", StringComparison.OrdinalIgnoreCase))
                    .Select(f => f.Id)
                    .ToArray();
                var ids = entities.Select(e => e.Id).ToArray();

                Assert.IsTrue(origIds.Length > 0);
                Assert.AreEqual(0, origIds.Except(ids).Count());
                Assert.AreEqual(0, ids.Except(origIds).Count());
            }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task OD_GET_Filter_SubstringOfEqTrue()
        {
            await ODataTestAsync(async () =>
            {
                var response = await ODataGetAsync(
                    "/OData.svc/Root",
                    "?$filter=substringof('yste', Name) eq true")
                    .ConfigureAwait(false);

                var entities = GetEntities(response);
                var origIds = Repository.Root.Children
                    .Where(x => x.Name.IndexOf("yste", StringComparison.OrdinalIgnoreCase) >= 0)
                    .Select(f => f.Id)
                    .ToArray();
                var ids = entities.Select(e => e.Id);

                Assert.IsTrue(origIds.Length > 0);
                Assert.AreEqual(0, origIds.Except(ids).Count());
                Assert.AreEqual(0, ids.Except(origIds).Count());
            }).ConfigureAwait(false);
        }

        //TODO: Remove inconclusive test result and implement this test.
        //[TestMethod]
        public async Task OD_GET_Filter_SubstringOfEqListField()
        {
            //Assert.Inconclusive("InMemorySchemaWriter.CreatePropertyType is partially implemented.");

            await ODataTestAsync(async () =>
            {
                var testRoot = CreateTestRoot("ODataTestRoot");

                var listDef = @"<?xml version='1.0' encoding='utf-8'?>
        <ContentListDefinition xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentListDefinition'>
        	<DisplayName>[DisplayName]</DisplayName>
        	<Description>[Description]</Description>
        	<Icon>[icon.gif]</Icon>
        	<Fields>
        		<ContentListField name='#CustomField' type='ShortText'>
        			<DisplayName>CustomField</DisplayName>
        			<Description>CustomField Description</Description>
        			<Icon>icon.gif</Icon>
        			<Configuration>
        				<MaxLength>100</MaxLength>
        			</Configuration>
        		</ContentListField>
        	</Fields>
        </ContentListDefinition>
        ";
                var itemType = "HTMLContent";
                var fieldValue = "qwer asdf yxcv";

                // create list
                var list = new ContentList(testRoot) { Name = Guid.NewGuid().ToString() };
                list.ContentListDefinition = listDef;
                list.AllowedChildTypes = new ContentType[] { ContentType.GetByName(itemType) };
                list.Save();

                // create item
                var item = Content.CreateNew(itemType, list, Guid.NewGuid().ToString());
                item["#CustomField"] = fieldValue;
                item.Save();

                // check expando field accessibility
                item = Content.Load(item.Id);
                Assert.AreEqual(fieldValue, (string)item["#CustomField"]);

                // get base count
                var countByCQ = ContentQuery.Query("#CustomField:*asdf* .AUTOFILTERS:OFF").Count;

                // get ids by SnLinq
                var origIds = Content.All
                    .DisableAutofilters()
                    .Where(x => ((string)x["#CustomField"]).Contains("asdf"))
                    .AsEnumerable()
                    .Select(f => f.Id)
                    .ToArray();
                Assert.IsTrue(origIds.Length > 0);

                // get ids by filter
                var response1 = await ODataGetAsync(
                    "/OData.svc" + list.Path,
                    "enableautofilters=false&$filter=substringof('asdf', #CustomField) eq true")
                    .ConfigureAwait(false);
                var entities1 = GetEntities(response1);
                var ids1 = entities1.Select(e => e.Id).ToArray();
                Assert.AreEqual(0, origIds.Except(ids1).Count());
                Assert.AreEqual(0, ids1.Except(origIds).Count());

                // get ids by filter URLencoded
                var response2 = await ODataGetAsync(
                    "/OData.svc" + list.Path,
                    "enableautofilters=false&$filter=substringof('asdf', %23CustomField) eq true")
                    .ConfigureAwait(false);
                var entities2 = GetEntities(response1);
                var ids2 = entities2.Select(e => e.Id).ToArray();
                Assert.AreEqual(0, origIds.Except(ids2).Count());
                Assert.AreEqual(0, ids2.Except(origIds).Count());
            }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task OD_GET_Filter_IsOf()
        {
            await ODataTestAsync(async () =>
            {
                InstallCarContentType();
                var testRoot = CreateTestRoot();

                var folder = new Folder(testRoot) { Name = Guid.NewGuid().ToString() };
                folder.Save();
                var folder1 = new Folder(folder) { Name = "Folder1" };
                folder1.Save();
                var folder2 = new Folder(folder) { Name = "Folder2" };
                folder2.Save();
                var content = Content.CreateNew("Car", folder, null);
                content.Save();

                var response = await ODataGetAsync(
                    "/OData.svc" + folder.Path,
                    "?$filter=isof('Folder')")
                    .ConfigureAwait(false);

                var entities = GetEntities(response).ToArray();
                Assert.AreEqual(2, entities.Length);
                Assert.AreEqual(folder1.Id, entities[0].Id);
                Assert.AreEqual(folder2.Id, entities[1].Id);

                response = await ODataGetAsync(
                    "/OData.svc" + folder.Path,
                    "?&$filter=not isof('Folder')")
                    .ConfigureAwait(false);

                entities = GetEntities(response).ToArray();
                Assert.AreEqual(1, entities.Length);
                Assert.AreEqual(content.Id, entities[0].Id);
            }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task OD_GET_Filter_IsOfEqTrue()
        {
            await ODataTestAsync(async () =>
            {
                var response = await ODataGetAsync(
                    "/OData.svc/Root",
                    "?$filter=isof('Folder') eq true")
                    .ConfigureAwait(false);

                var origIds = Repository.Root.Children
                    .Where(x => x.NodeType.IsInstaceOfOrDerivedFrom("Folder"))
                    .Select(f => f.Id)
                    .ToArray();

                var entities = GetEntities(response).ToArray();
                var ids = entities.Select(e => e.Id);

                Assert.IsTrue(origIds.Length > 0);
                Assert.AreEqual(0, origIds.Except(ids).Count());
                Assert.AreEqual(0, ids.Except(origIds).Count());
            }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task OD_GET_Filter_ContentField()
        {
            await ODataTestAsync(async () =>
            {
                InstallCarContentType();
                var testRoot = CreateTestRoot();

                foreach (var item in new[] { "Ferrari", "Porsche", "Ferrari", "Mercedes" })
                {
                    var car = Content.CreateNew("Car", testRoot, Guid.NewGuid().ToString());
                    car["Make"] = item;
                    car.Save();
                }

                var response = await ODataGetAsync(
                    "/OData.svc" + testRoot.Path,
                    "?$filter=Make eq 'Ferrari'&enableautofilters=false")
                    .ConfigureAwait(false);

                var entities = GetEntities(response).ToArray();
                Assert.AreEqual(2, entities.Length);
            }).ConfigureAwait(false);
        }


        [TestMethod]
        public async Task OD_GET_Filter_InFolder()
        {
            await ODataTestAsync(async () =>
            {
                var response = await ODataGetAsync(
                    "/OData.svc/Root/IMS/BuiltIn/Portal",
                    "?$orderby=Id&$filter=Id lt (9 sub 2)")
                    .ConfigureAwait(false);

                var entities = GetEntities(response).ToArray();
                Assert.AreEqual(2, entities.Length);
                Assert.AreEqual(1, entities[0].Id);
                Assert.AreEqual(6, entities[1].Id);
            }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task OD_GET_Filter_IsFolder()
        {
            await ODataTestAsync(async () =>
            {
                InstallCarContentType();
                var testRoot = CreateTestRoot();

                var folder = new Folder(testRoot) { Name = Guid.NewGuid().ToString() };
                folder.Save();
                var folder1 = new Folder(folder) { Name = "Folder1" };
                folder1.Save();
                var folder2 = new Folder(folder) { Name = "Folder2" };
                folder2.Save();

                var content = Content.CreateNew("Car", folder, null);
                content.Save();

                var response = await ODataGetAsync(
                    "/OData.svc" + folder.Path,
                    "?$filter=IsFolder eq true")
                    .ConfigureAwait(false);

                var entities = GetEntities(response).ToArray();
                Assert.AreEqual(2, entities.Length);
                Assert.AreEqual(folder1.Id, entities[0].Id);
                Assert.AreEqual(folder2.Id, entities[1].Id);

                response = await ODataGetAsync(
                    "/OData.svc" + folder.Path,
                    "?$filter=IsFolder eq false")
                    .ConfigureAwait(false);

                entities = GetEntities(response).ToArray();
                Assert.AreEqual(1, entities.Length);
                Assert.AreEqual(content.Id, entities[0].Id);
            }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task OD_GET_Filter_NamespaceAndMemberChain()
        {
            await ODataTestAsync(async () =>
            {
                var name = typeof(ODataFilterTestHelper).FullName;

                var response = await ODataGetAsync(
                    "/OData.svc/Root/IMS/BuiltIn/Portal",
                    $"?$filter={name}/TestValue eq Name")
                    .ConfigureAwait(false);

                var entities = GetEntities(response).ToArray();
                Assert.AreEqual(1, entities.Count());
                Assert.AreEqual(Group.Administrators.Path, entities.First().Path);
            }).ConfigureAwait(false);
        }
    }
}
