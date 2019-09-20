using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
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
    }
}
