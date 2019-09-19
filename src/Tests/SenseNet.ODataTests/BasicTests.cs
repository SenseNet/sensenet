using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
    }
}
