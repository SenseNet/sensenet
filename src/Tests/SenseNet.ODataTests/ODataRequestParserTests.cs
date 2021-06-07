using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.OData;
using SenseNet.Search;

// ReSharper disable CommentTypo

namespace SenseNet.ODataTests
{
    [TestClass]
    public class ODataRequestParserTests : ODataTestBase
    {
        [TestMethod]
        public void OD_GET_Parsing_TopSkip()
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
        public void OD_GET_Parsing_InvalidTop()
        {
            ODataTest(() =>
            {
                var httpContext = CreateHttpContext("/OData.svc/Root", "?$top=-3");
                var odataRequest = ODataRequest.Parse(httpContext);
                var code = GetExceptionCode(odataRequest);
                Assert.AreEqual(ODataExceptionCode.NegativeTopParameter, code);
            });
        }

        [TestMethod]
        public void OD_GET_Parsing_InvalidSkip()
        {
            ODataTest(() =>
            {
                var httpContext = CreateHttpContext("/OData.svc/Root", "?$skip=-4");
                var odataRequest = ODataRequest.Parse(httpContext);
                var code = GetExceptionCode(odataRequest);
                Assert.AreEqual(ODataExceptionCode.NegativeSkipParameter, code);
            });
        }
        [TestMethod]
        public void OD_GET_Parsing_InlineCount()
        {
            ODataTest(() =>
            {
                HttpContext httpContext;
                ODataRequest odataRequest;

                httpContext = CreateHttpContext("/OData.svc/Root", "?$inlinecount=none");
                odataRequest = ODataRequest.Parse(httpContext);
                Assert.AreEqual(InlineCount.None, odataRequest.InlineCount);
                Assert.IsTrue(!odataRequest.HasInlineCount);

                httpContext = CreateHttpContext("/OData.svc/Root", "?$inlinecount=0");
                odataRequest = ODataRequest.Parse(httpContext);
                Assert.AreEqual(InlineCount.None, odataRequest.InlineCount);
                Assert.IsTrue(!odataRequest.HasInlineCount);

                httpContext = CreateHttpContext("/OData.svc/Root", "?$inlinecount=allpages");
                odataRequest = ODataRequest.Parse(httpContext);
                Assert.AreEqual(InlineCount.AllPages, odataRequest.InlineCount);
                Assert.IsTrue(odataRequest.HasInlineCount);

                httpContext = CreateHttpContext("/OData.svc/Root", "?$inlinecount=1");
                odataRequest = ODataRequest.Parse(httpContext);
                Assert.AreEqual(InlineCount.AllPages, odataRequest.InlineCount);
                Assert.IsTrue(odataRequest.HasInlineCount);
            });
        }
        [TestMethod]
        public void OD_GET_Parsing_InvalidInlineCount()
        {
            ODataTest(() =>
            {
                var httpContext = CreateHttpContext("/OData.svc/Root", "?$inlinecount=asdf");
                var odataRequest = ODataRequest.Parse(httpContext);
                var code = GetExceptionCode(odataRequest);
                Assert.AreEqual(ODataExceptionCode.InvalidInlineCountParameter, code);

                httpContext = CreateHttpContext("/OData.svc/Root", "?$inlinecount=2");
                odataRequest = ODataRequest.Parse(httpContext);
                code = GetExceptionCode(odataRequest);
                Assert.AreEqual(ODataExceptionCode.InvalidInlineCountParameter, code);
            });
        }
        [TestMethod]
        public void OD_GET_Parsing_OrderBy()
        {
            ODataTest(() =>
            {
                HttpContext httpContext;
                ODataRequest odataRequest;
                SortInfo[] sort;

                //----------------------------------------------------------------------------- sorting: -
                httpContext = CreateHttpContext("/OData.svc/Root", "");
                odataRequest = ODataRequest.Parse(httpContext);
                sort = odataRequest.Sort.ToArray();
                Assert.IsFalse(odataRequest.HasSort);
                Assert.AreEqual(0, sort.Length);

                //----------------------------------------------------------------------------- sorting: Id
                httpContext = CreateHttpContext("/OData.svc/Root", "?$orderby=Id");
                odataRequest = ODataRequest.Parse(httpContext);
                sort = odataRequest.Sort.ToArray();
                Assert.IsTrue(odataRequest.HasSort);
                Assert.AreEqual(1, sort.Length);
                Assert.AreEqual("Id", sort[0].FieldName);
                Assert.IsFalse(sort[0].Reverse);

                //----------------------------------------------------------------------------- sorting: Name asc
                httpContext = CreateHttpContext("/OData.svc/Root", "?$orderby=Name asc");
                odataRequest = ODataRequest.Parse(httpContext);
                sort = odataRequest.Sort.ToArray();
                Assert.IsTrue(odataRequest.HasSort);
                Assert.IsTrue(sort.Length == 1);
                Assert.IsTrue(sort[0].FieldName == "Name");
                Assert.IsTrue(sort[0].Reverse == false);

                //----------------------------------------------------------------------------- sorting: DisplayName desc
                httpContext = CreateHttpContext("/OData.svc/Root", "?$orderby=DisplayName desc");
                odataRequest = ODataRequest.Parse(httpContext);
                sort = odataRequest.Sort.ToArray();
                Assert.IsTrue(odataRequest.HasSort);
                Assert.AreEqual(1, sort.Length);
                Assert.AreEqual("DisplayName", sort[0].FieldName);
                Assert.IsTrue(sort[0].Reverse);

                //----------------------------------------------------------------------------- sorting: ModificationDate desc, Category, Name
                httpContext = CreateHttpContext("/OData.svc/Root",
                    "?$orderby=   ModificationDate desc    ,   Category   ,    Name");
                odataRequest = ODataRequest.Parse(httpContext);
                sort = odataRequest.Sort.ToArray();
                Assert.IsTrue(odataRequest.HasSort);
                Assert.AreEqual(3, sort.Length);
                Assert.AreEqual("ModificationDate", sort[0].FieldName);
                Assert.IsTrue(sort[0].Reverse);
                Assert.AreEqual("Category", sort[1].FieldName);
                Assert.IsFalse(sort[1].Reverse);
                Assert.AreEqual("Name", sort[2].FieldName);
                Assert.IsFalse(sort[2].Reverse);
            });
        }
        [TestMethod]
        public void OD_GET_Parsing_InvalidOrderBy()
        {
            ODataTest(() =>
            {
                var httpContext = CreateHttpContext("/OData.svc/Root", "?$orderby=asdf asd");
                var odataRequest = ODataRequest.Parse(httpContext);
                var code = GetExceptionCode(odataRequest);
                Assert.IsTrue(code == ODataExceptionCode.InvalidOrderByDirectionParameter);

                httpContext = CreateHttpContext("/OData.svc/Root", "?$orderby=asdf asc desc");
                odataRequest = ODataRequest.Parse(httpContext);
                code = GetExceptionCode(odataRequest);
                Assert.IsTrue(code == ODataExceptionCode.InvalidOrderByParameter);
            });
        }

        [TestMethod]
        public void OD_GET_Parsing_Format()
        {
            ODataTest(() =>
            {
                var httpContext = CreateHttpContext("/OData.svc/Root", "?$format=json");
                var odataRequest = ODataRequest.Parse(httpContext);
                Assert.IsTrue(odataRequest.Format == "json");

                httpContext = CreateHttpContext("/OData.svc/Root", "?$format=verbosejson");
                odataRequest = ODataRequest.Parse(httpContext);
                Assert.IsTrue(odataRequest.Format == "verbosejson");
            });
        }
        [TestMethod]
        public async Task OD_GET_Parsing_InvalidFormat()
        {
            await ODataTestAsync(async () =>
            {
                // ACTION 1
                var response = await ODataGetAsync("/OData.svc/Root", "?$format=atom")
                    .ConfigureAwait(false);

                // ASSERT 1 
                var error = GetError(response);
                Assert.AreEqual(ODataExceptionCode.InvalidFormatParameter, error.Code);

                // ACTION 2
                response = await ODataGetAsync("/OData.svc/Root", "?$format=xxx")
                    .ConfigureAwait(false);

                // ASSERT 2
                error = GetError(response);
                Assert.AreEqual(ODataExceptionCode.InvalidFormatParameter, error.Code);

            }).ConfigureAwait(false);
        }
        [TestMethod]
        public void OD_GET_Parsing_Select()
        {
            ODataTest(() =>
            {
                HttpContext httpContext;
                ODataRequest odataRequest;
                List<string> select;

                //----------------------------------------------------------------------------- select: -
                httpContext = CreateHttpContext("/OData.svc/Root", "");

                odataRequest = ODataRequest.Parse(httpContext);
                select = odataRequest.Select;
                Assert.IsTrue(odataRequest.HasSelect == false);
                Assert.IsTrue(select.Count == 0);

                //----------------------------------------------------------------------------- select: Id, DisplayName, ModificationDate
                httpContext = CreateHttpContext("/OData.svc/Root",
                    "?$select=    Id  ,\tDisplayName\r\n\t,   ModificationDate   ");

                odataRequest = ODataRequest.Parse(httpContext);
                select = odataRequest.Select;
                Assert.IsTrue(odataRequest.HasSelect);
                Assert.IsTrue(select.Count == 3);
                Assert.IsTrue(select[0] == "Id");
                Assert.IsTrue(select[1] == "DisplayName");
                Assert.IsTrue(select[2] == "ModificationDate");

                //----------------------------------------------------------------------------- select: *
                httpContext = CreateHttpContext("/OData.svc/Root", "?$select=*");

                odataRequest = ODataRequest.Parse(httpContext);
                select = odataRequest.Select;
                Assert.IsTrue(odataRequest.HasSelect == false);
                Assert.IsTrue(select.Count == 0);
            });
        }

        [TestMethod]
        public void OD_GET_Parsing_RichTextEditor()
        {
            ODataTest(() =>
            {
                HttpContext httpContext;
                ODataRequest odataRequest;
                List<string> expanded;

                //----------------------------------------------------------------------------- there is no expanded
                httpContext = CreateHttpContext("/OData.svc/Root", "");
                odataRequest = ODataRequest.Parse(httpContext);
                expanded = odataRequest.ExpandedRichTextFields;
                Assert.IsFalse(odataRequest.HasExpandedRichTextField);
                Assert.AreEqual(0, expanded.Count);

                //----------------------------------------------------------------------------- 1 expanded
                httpContext = CreateHttpContext("/OData.svc/Root", "?richtexteditor=RichText1");
                odataRequest = ODataRequest.Parse(httpContext);
                expanded = odataRequest.ExpandedRichTextFields;
                Assert.IsTrue(odataRequest.HasExpandedRichTextField);
                Assert.AreEqual(1, expanded.Count);
                Assert.AreEqual("RichText1", expanded[0]);

                //----------------------------------------------------------------------------- 2 expanded
                httpContext = CreateHttpContext("/OData.svc/Root", "?richtexteditor=RichText1,RichText2");
                odataRequest = ODataRequest.Parse(httpContext);
                expanded = odataRequest.ExpandedRichTextFields;
                Assert.IsTrue(odataRequest.HasExpandedRichTextField);
                Assert.AreEqual(2, expanded.Count);
                Assert.AreEqual("RichText1", expanded[0]);
                Assert.AreEqual("RichText2", expanded[1]);

                //----------------------------------------------------------------------------- all expanded: "all"
                httpContext = CreateHttpContext("/OData.svc/Root", "?richtexteditor=all");
                odataRequest = ODataRequest.Parse(httpContext);
                expanded = odataRequest.ExpandedRichTextFields;
                Assert.IsTrue(odataRequest.HasExpandedRichTextField);
                Assert.AreEqual(1, expanded.Count);
                Assert.AreEqual("*", expanded[0]);

                //----------------------------------------------------------------------------- all expanded: case insensitive
                httpContext = CreateHttpContext("/OData.svc/Root", "?richtexteditor=AlL");
                odataRequest = ODataRequest.Parse(httpContext);
                expanded = odataRequest.ExpandedRichTextFields;
                Assert.IsTrue(odataRequest.HasExpandedRichTextField);
                Assert.AreEqual(1, expanded.Count);
                Assert.AreEqual("*", expanded[0]);

                //----------------------------------------------------------------------------- all expanded: "*"
                httpContext = CreateHttpContext("/OData.svc/Root", "?richtexteditor=*");
                odataRequest = ODataRequest.Parse(httpContext);
                expanded = odataRequest.ExpandedRichTextFields;
                Assert.IsTrue(odataRequest.HasExpandedRichTextField);
                Assert.AreEqual(1, expanded.Count);
                Assert.AreEqual("*", expanded[0]);

                //----------------------------------------------------------------------------- all expanded: mix 1
                httpContext = CreateHttpContext("/OData.svc/Root", "?richtexteditor=RichText1,RichText2,*");
                odataRequest = ODataRequest.Parse(httpContext);
                expanded = odataRequest.ExpandedRichTextFields;
                Assert.IsTrue(odataRequest.HasExpandedRichTextField);
                Assert.AreEqual(1, expanded.Count);
                Assert.AreEqual("*", expanded[0]);

                //----------------------------------------------------------------------------- all expanded: mix 2
                httpContext = CreateHttpContext("/OData.svc/Root", "?richtexteditor=RichText1,ALL,RichText2");
                odataRequest = ODataRequest.Parse(httpContext);
                expanded = odataRequest.ExpandedRichTextFields;
                Assert.IsTrue(odataRequest.HasExpandedRichTextField);
                Assert.AreEqual(1, expanded.Count);
                Assert.AreEqual("*", expanded[0]);
            });
        }
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
