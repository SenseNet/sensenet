using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.OData;

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
                var response = ODataGET<ODataResponse>("/OData.svc/Root('IMS')", "");

                var odataContent = (ODataContent)response.Value;
                Assert.AreEqual(ODataResponseType.SingleContent, response.Type);
                Assert.AreEqual("/Root/IMS", odataContent.Path);
            });
        }
        [TestMethod]
        public void OD_Getting_ChildrenCollection()
        {
            ODataTest(() =>
            {
                var response = ODataGET<ODataResponse>("/OData.svc/Root/IMS/BuiltIn/Portal", "");

                var entities = (IEnumerable<ODataContent>) response.Value;
                var origIds = Node.Load<Folder>("/Root/IMS/BuiltIn/Portal").Children.Select(f => f.Id).ToArray();
                var ids = entities.Select(e => e.Id).ToArray();

                Assert.AreEqual(ODataResponseType.ChildrenCollection, response.Type);
                Assert.AreEqual(0, origIds.Except(ids).Count());
                Assert.AreEqual(0, ids.Except(origIds).Count());
            });
        }
    }
}
