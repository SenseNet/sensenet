using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.OData;
using SenseNet.OData.Responses;
using SenseNet.Portal;

namespace SenseNet.ODataTests
{
    [TestClass]
    public class ODataChildrenTests : ODataTestBase
    {
        #region Infrastructure
        private void ODataChildrenTest(Action callback)
        {
            IsolatedODataTest(() =>
            {
                EnsureTestStructure();
                callback();
            });
        }

        private void EnsureTestStructure()
        {
            //  root
            //      ODataChildrenTests
            //          Test1
            //              F0
            //                  F00
            //                      F000
            //                  SF01(SYSTEM)
            //              SF1(SYSTEM)
            //                  F10

            Cache.Reset();

            var testRoot = Node.Load<SystemFolder>("/Root/ODataChildrenTests");
            if (testRoot != null)
                return;
            testRoot = new SystemFolder(Repository.Root) {Name = "ODataChildrenTests"};
            testRoot.Save();
            var testFolder1 = new SystemFolder(testRoot) {Name = "Test1"};
            testFolder1.Save();
            var folder0 = new Folder(testFolder1) {Name = "F0"};
            folder0.Save();
            var folder00 = new Folder(folder0) {Name = "F00"};
            folder00.Save();
            var folder000 = new Folder(folder00) {Name = "F000"};
            folder000.Save();
            var systemFolder01 = new SystemFolder(folder0) {Name = "SF01"};
            systemFolder01.Save();
            var systemFolder1 = new SystemFolder(testFolder1) {Name = "SF1"};
            systemFolder1.Save();
            var folder10 = new Folder(systemFolder1) {Name = "F10"};
            folder10.Save();
        }

        #endregion

        [TestMethod]
        public void OData_Children_Entity_SelectChildren_NoExpand()
        {
            ODataChildrenTest(() =>
            {
                // ACTION
                var response = ODataGET<ODataSingleContentResponse>($"/OData.svc/Root/ODataChildrenTests('Test1')", "?metadata=no&$select=Id,Name,Children");

                // ASSERT
                var entity = response.Value;
                var propertyValue = entity["Children"];
                // ODataReference will be serialized as "__deferred"
                Assert.IsTrue(propertyValue is ODataReference);
            });
        }
        [TestMethod]
        public void OData_Children_Entity_SelectChildren_Expand()
        {
            ODataChildrenTest(() =>
            {
                var response = ODataGET<ODataSingleContentResponse>(
                    $"/OData.svc/Root/ODataChildrenTests('Test1')", 
                    "?metadata=no&$select=Id,Name,Children&$expand=Children");

                var entity = response.Value;
                var propertyValue = entity["Children"];
                Assert.IsTrue(propertyValue is IEnumerable<ODataContent>);
                var children = ((IEnumerable<ODataContent>)propertyValue).ToArray();
                Assert.AreEqual(2, children.Length);
                Assert.IsTrue(children[0].Count > 20);
                Assert.AreEqual("F0", children[0].Name);
                Assert.AreEqual("SystemFolder", children[1].ContentType);
            });
        }
        [TestMethod]
        public void OData_Children_Entity_SelectChildren_ExpandAndSelect()
        {
            ODataChildrenTest(() =>
            {
                var response = ODataGET<ODataSingleContentResponse>(
                    $"/OData.svc/Root/ODataChildrenTests('Test1')",
                    "?metadata=no&$select=Id,Name,Children/Id,Children/Path&$expand=Children");

                var entity = response.Value;
                var propertyValue = entity["Children"];
                Assert.IsTrue(propertyValue is IEnumerable<ODataContent>);
                var children = ((IEnumerable<ODataContent>)propertyValue).ToArray();
                Assert.AreEqual(2, children.Length);
                Assert.AreEqual(2, children[0].Count);
                Assert.AreEqual("/Root/ODataChildrenTests/Test1/F0", children[0].Path);
            });
        }
        /*[TestMethod]
        public void OData_Children_Entity_SelectChildren_Filtered()
        {
            Test(() =>
            {
                // switch on autofilters
                var entity = ODataGET<ODataEntity>($"/OData.svc/Root/ODataChildrenTests('Test1')", "?metadata=no&$select=Id,Name,Children/Id,Children/Path&$expand=Children&enableautofilters=true");

                Assert.AreEqual(1, entity.Children.Length);
            });
        }*/

        /*[TestMethod]
        public void OData_Children_Property_Expand()
        {
            Test(() =>
            {
                var entities = ODataGET<ODataEntities>($"/OData.svc/Root/ODataChildrenTests('Test1')/Children", "?metadata=no&$select=Id,Name,Children&$expand=Children");

                Assert.AreEqual(2, entities.Length);

                var f0 = entities.FirstOrDefault(e => e.Name == "F0");

                Assert.IsNotNull(f0);
                Assert.IsFalse(f0.AllPropertiesSelected);
                Assert.AreEqual(2, f0.Children.Length);
                Assert.IsTrue(f0.Children[0].AllPropertiesSelected);
            });
        }*/
        /*[TestMethod]
        public void OData_Children_Property_ExpandAndSelect()
        {
            Test(() =>
            {
                var entities = ODataGET<ODataEntities>($"/OData.svc/Root/ODataChildrenTests('Test1')/Children", "?metadata=no&$select=Id,Name,Children/Id,Children/Path&$expand=Children");

                Assert.AreEqual(2, entities.Length);

                var f0 = entities.FirstOrDefault(e => e.Name == "F0");

                Assert.IsNotNull(f0);
                Assert.IsFalse(f0.AllPropertiesSelected);
                Assert.AreEqual(2, f0.Children.Length);

                var f00Path = $"{TestSitePath}/F0/F00";
                var f00Node = Node.Load<Folder>(f00Path);
                var f00 = f0.Children.FirstOrDefault(e => e.Id == f00Node.Id);

                Assert.IsNotNull(f00);
                Assert.AreEqual(null, f00.Name); // Name is not selected
                Assert.AreEqual(f00Path, f00.Path);
            });
        }*/
        /*[TestMethod]
        public void OData_Children_Property_ExpandAndSelect_Deep()
        {
            Test(() =>
            {
                var entities = ODataGET<ODataEntities>($"/OData.svc/Root/ODataChildrenTests('Test1')/Children", "?metadata=no&$select=Id,Name,Children/Id,Children/Path,Children/Children/Id,Children/Children/Path&$expand=Children,Children/Children");

                Assert.AreEqual(2, entities.Length);

                var f00Path = $"{TestSitePath}/F0/F00";
                var f000Path = $"{TestSitePath}/F0/F00/F000";
                var f00Node = Node.Load<Folder>(f00Path);
                var f000Node = Node.Load<Folder>(f000Path);

                var f000 = entities
                    .First(e => e.Name == "F0").Children
                    .First(e => e.Id == f00Node.Id).Children
                    .First(e => e.Id == f000Node.Id);

                Assert.IsNotNull(f000);
                Assert.IsFalse(f000.AllPropertiesSelected);
                Assert.IsNull(f000.Children); // Children property is not selected on the 3rd level
            });
        }*/
        /*[TestMethod]
        public void OData_Children_Property_Filtered()
        {
            Test(() =>
            {
                // switch on autofilters
                var entities = ODataGET<ODataEntities>($"/OData.svc/Root/ODataChildrenTests('Test1')/Children", "?metadata=no&$select=Id,Name,Children/Id,Children/Path&$expand=Children&enableautofilters=true");

                Assert.AreEqual(1, entities.Length);

                // add a query filter
                entities = ODataGET<ODataEntities>($"/OData.svc/Root/ODataChildrenTests('Test1')/Children", "?metadata=no&$select=Id,Name&$filter=startswith(Name, 'SF') eq true");
                Assert.AreEqual(1, entities.Length);
                Assert.IsTrue(entities[0].Name.StartsWith("SF"));
            });
        }*/

    }
}
