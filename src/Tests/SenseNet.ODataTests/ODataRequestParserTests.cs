using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.OData;
// ReSharper disable CommentTypo

namespace SenseNet.ODataTests
{
    [TestClass]
    public class ODataRequestParserTests : ODataTestBase
    {
        [TestMethod]
        public void OData_Parsing_TopSkip()
        {
            ODataTest(() =>
            {
                ODataRequest odataRequest;
                HttpContext httpContext;
                //---------------------------------------- without top, without skip
                httpContext = CreateHttpContext("/OData.svc/Root", "");
                odataRequest = ODataRequest.Parse(httpContext);
                Assert.AreEqual(0, odataRequest.Top);
                Assert.AreEqual(0, odataRequest.Skip);
                Assert.IsTrue(!odataRequest.HasTop);
                Assert.IsTrue(!odataRequest.HasSkip);

                //---------------------------------------- top 3, without skip
                httpContext = CreateHttpContext("/OData.svc/Root", "?$top=3");
                odataRequest = ODataRequest.Parse(httpContext);
                Assert.AreEqual(3, odataRequest.Top);
                Assert.AreEqual(0, odataRequest.Skip);
                Assert.IsTrue(odataRequest.HasTop);
                Assert.IsTrue(!odataRequest.HasSkip);

                //---------------------------------------- without top, skip 4
                httpContext = CreateHttpContext("/OData.svc/Root", "?$skip=4");
                odataRequest = ODataRequest.Parse(httpContext);
                Assert.AreEqual(0, odataRequest.Top);
                Assert.AreEqual(4, odataRequest.Skip);
                Assert.IsTrue(!odataRequest.HasTop);
                Assert.IsTrue(odataRequest.HasSkip);

                //---------------------------------------- top 3, skip 4
                httpContext = CreateHttpContext("/OData.svc/Root", "?$top=3&$skip=4");
                odataRequest = ODataRequest.Parse(httpContext);
                Assert.AreEqual(3, odataRequest.Top);
                Assert.AreEqual(4, odataRequest.Skip);
                Assert.IsTrue(odataRequest.HasTop);
                Assert.IsTrue(odataRequest.HasSkip);

                //---------------------------------------- top 0, skip 0
                httpContext = CreateHttpContext("/OData.svc/Root", "?$top=0&$skip=0");
                odataRequest = ODataRequest.Parse(httpContext);
                Assert.AreEqual(0, odataRequest.Top);
                Assert.AreEqual(0, odataRequest.Skip);
                Assert.IsTrue(!odataRequest.HasTop);
                Assert.IsTrue(!odataRequest.HasSkip);
            });
        }

        [TestMethod]
        public void OData_Parsing_InvalidTop()
        {
            ODataTest(() =>
            {
                var httpContext = CreateHttpContext("/OData.svc/Root", "?$top=-3");
                var odataRequest = ODataRequest.Parse(httpContext);
                var code = GetExceptionCode(odataRequest);
                Assert.AreEqual(ODataExceptionCode.NegativeTopParameter, code);
            });
        }

        /*[TestMethod]
        public void OData_Parsing_InvalidSkip()
        {
            Test(() =>
            {
                CreateTestSite();

                using (var output = new StringWriter())
                {
                    var httpContext = CreateHttpContext("/OData.svc/Root", "?$skip=-4");
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    var code = GetExceptionCode(output);
                    Assert.AreEqual(ODataExceptionCode.NegativeSkipParameter, code);
                }
            });
        }*/
        /*[TestMethod]
        public void OData_Parsing_InlineCount()
        {
            Test(() =>
            {
                CreateTestSite();

                PortalContext pc;
                ODataHandler handler;

                using (var output = new StringWriter())
                {
                    httpContext = CreateHttpContext("/OData.svc/Root", "?$inlinecount=none");
                    handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    Assert.AreEqual(InlineCount.None, odataRequest.InlineCount);
                    Assert.IsTrue(!odataRequest.HasInlineCount);
                }

                using (var output = new StringWriter())
                {
                    httpContext = CreateHttpContext("/OData.svc/Root", "?$inlinecount=0");
                    handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    Assert.AreEqual(InlineCount.None, odataRequest.InlineCount);
                    Assert.IsTrue(!odataRequest.HasInlineCount);
                }

                using (var output = new StringWriter())
                {
                    httpContext = CreateHttpContext("/OData.svc/Root", "?$inlinecount=allpages");
                    handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    Assert.AreEqual(InlineCount.AllPages, odataRequest.InlineCount);
                    Assert.IsTrue(odataRequest.HasInlineCount);
                }

                using (var output = new StringWriter())
                {
                    httpContext = CreateHttpContext("/OData.svc/Root", "?$inlinecount=1");
                    handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    Assert.AreEqual(InlineCount.AllPages, odataRequest.InlineCount);
                    Assert.IsTrue(odataRequest.HasInlineCount);
                }
            });
        }*/
        /*[TestMethod]
        public void OData_Parsing_InvalidInlineCount()
        {
            Test(() =>
            {
                CreateTestSite();

                using (var output = new StringWriter())
                {
                    var httpContext = CreateHttpContext("/OData.svc/Root", "?$inlinecount=asdf");
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    var code = GetExceptionCode(output);
                    Assert.AreEqual(ODataExceptionCode.InvalidInlineCountParameter, code);
                }
                using (var output = new StringWriter())
                {
                    var httpContext = CreateHttpContext("/OData.svc/Root", "?$inlinecount=2");
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    var code = GetExceptionCode(output);
                    Assert.AreEqual(ODataExceptionCode.InvalidInlineCountParameter, code);
                }
            });
        }*/
        /*[TestMethod]
        public void OData_Parsing_OrderBy()
        {
            Test(() =>
            {
                CreateTestSite();

                PortalContext pc;
                ODataHandler handler;

                    //----------------------------------------------------------------------------- sorting: -
                    using (var output = new StringWriter())
                {
                    httpContext = CreateHttpContext("/OData.svc/Root", "");
                    handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    var sort = odataRequest.Sort.ToArray();
                    Assert.IsFalse(odataRequest.HasSort);
                    Assert.AreEqual(0, sort.Length);
                }

                    //----------------------------------------------------------------------------- sorting: Id
                    using (var output = new StringWriter())
                {
                    httpContext = CreateHttpContext("/OData.svc/Root", "?$orderby=Id");
                    handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    var sort = odataRequest.Sort.ToArray();
                    Assert.IsTrue(odataRequest.HasSort);
                    Assert.AreEqual(1, sort.Length);
                    Assert.AreEqual("Id", sort[0].FieldName);
                    Assert.IsFalse(sort[0].Reverse);
                }

                    //----------------------------------------------------------------------------- sorting: Name asc
                    using (var output = new StringWriter())
                {
                    httpContext = CreateHttpContext("/OData.svc/Root", "?$orderby=Name asc");
                    handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    var sort = odataRequest.Sort.ToArray();
                    Assert.IsTrue(odataRequest.HasSort);
                    Assert.IsTrue(sort.Length == 1);
                    Assert.IsTrue(sort[0].FieldName == "Name");
                    Assert.IsTrue(sort[0].Reverse == false);
                }

                    //----------------------------------------------------------------------------- sorting: DisplayName desc
                    using (var output = new StringWriter())
                {
                    httpContext = CreateHttpContext("/OData.svc/Root", "?$orderby=DisplayName desc");
                    handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    var sort = odataRequest.Sort.ToArray();
                    Assert.IsTrue(odataRequest.HasSort);
                    Assert.AreEqual(1, sort.Length);
                    Assert.AreEqual("DisplayName", sort[0].FieldName);
                    Assert.IsTrue(sort[0].Reverse);
                }

                    //----------------------------------------------------------------------------- sorting: ModificationDate desc, Category, Name
                    using (var output = new StringWriter())
                {
                    httpContext = CreateHttpContext("/OData.svc/Root", "?$orderby=   ModificationDate desc    ,   Category   ,    Name");
                    handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    var sort = odataRequest.Sort.ToArray();
                    Assert.IsTrue(odataRequest.HasSort);
                    Assert.AreEqual(3, sort.Length);
                    Assert.AreEqual("ModificationDate", sort[0].FieldName);
                    Assert.IsTrue(sort[0].Reverse);
                    Assert.AreEqual("Category", sort[1].FieldName);
                    Assert.IsFalse(sort[1].Reverse);
                    Assert.AreEqual("Name", sort[2].FieldName);
                    Assert.IsFalse(sort[2].Reverse);
                }
            });
        }*/
        /*[TestMethod]
        public void OData_Parsing_InvalidOrderBy()
        {
            Test(() =>
            {
                CreateTestSite();

                using (var output = new StringWriter())
                {
                    var httpContext = CreateHttpContext("/OData.svc/Root", "?$orderby=asdf asd");
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    var code = GetExceptionCode(output);
                    Assert.IsTrue(code == ODataExceptionCode.InvalidOrderByDirectionParameter);
                }
                using (var output = new StringWriter())
                {
                    var httpContext = CreateHttpContext("/OData.svc/Root", "?$orderby=asdf asc desc");
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    var code = GetExceptionCode(output);
                    Assert.IsTrue(code == ODataExceptionCode.InvalidOrderByParameter);
                }
            });
        }*/
        /*[TestMethod]
        public void OData_Parsing_Format()
        {
            Test(() =>
            {
                CreateTestSite();

                PortalContext pc;
                ODataHandler handler;

                using (var output = new StringWriter())
                {
                    httpContext = CreateHttpContext("/OData.svc/Root", "?$format=json");
                    handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    Assert.IsTrue(odataRequest.Format == "json");
                }

                using (var output = new StringWriter())
                {
                    httpContext = CreateHttpContext("/OData.svc/Root", "?$format=verbosejson");
                    handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    Assert.IsTrue(odataRequest.Format == "verbosejson");
                }
            });
        }*/
        /*[TestMethod]
        public void OData_Parsing_InvalidFormat()
        {
            Test(() =>
            {
                CreateTestSite();

                using (var output = new StringWriter())
                {
                    var httpContext = CreateHttpContext("/OData.svc/Root", "?$format=atom");
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    var code = GetExceptionCode(output);
                    Assert.IsTrue(code == ODataExceptionCode.InvalidFormatParameter);
                }

                using (var output = new StringWriter())
                {
                    var httpContext = CreateHttpContext("/OData.svc/Root", "?$format=xxx");
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    var code = GetExceptionCode(output);
                    Assert.IsTrue(code == ODataExceptionCode.InvalidFormatParameter);
                }
            });
        }*/
        /*[TestMethod]
        public void OData_Parsing_Select()
        {
            Test(() =>
            {
                CreateTestSite();

                PortalContext pc;
                ODataHandler handler;

                    //----------------------------------------------------------------------------- select: -
                    using (var output = new StringWriter())
                {
                    httpContext = CreateHttpContext("/OData.svc/Root", "");
                    handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    var select = odataRequest.Select;
                    Assert.IsTrue(odataRequest.HasSelect == false);
                    Assert.IsTrue(select.Count == 0);
                }

                    //----------------------------------------------------------------------------- select: Id, DisplayName, ModificationDate
                    using (var output = new StringWriter())
                {
                    httpContext = CreateHttpContext("/OData.svc/Root",
                        "?$select=    Id  ,\tDisplayName\r\n\t,   ModificationDate   ");
                    handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    var select = odataRequest.Select;
                    Assert.IsTrue(odataRequest.HasSelect);
                    Assert.IsTrue(select.Count == 3);
                    Assert.IsTrue(select[0] == "Id");
                    Assert.IsTrue(select[1] == "DisplayName");
                    Assert.IsTrue(select[2] == "ModificationDate");
                }

                    //----------------------------------------------------------------------------- select: *
                    using (var output = new StringWriter())
                {
                    httpContext = CreateHttpContext("/OData.svc/Root", "?$select=*");
                    handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    var select = odataRequest.Select;
                    Assert.IsTrue(odataRequest.HasSelect == false);
                    Assert.IsTrue(select.Count == 0);
                }
            });
        }*/

        /* ======================================================================== TOOLS */

        private ODataExceptionCode GetExceptionCode(ODataRequest odataRequest)
        {
            if (odataRequest.RequestError is ODataException odataException)
                return odataException.ODataExceptionCode;
            Assert.Fail("Exception is no an ODataException");
            throw new NotSupportedException();
        }
    }
}
