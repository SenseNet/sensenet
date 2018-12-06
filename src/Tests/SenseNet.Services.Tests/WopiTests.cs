using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32.SafeHandles;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.Portal;
using SenseNet.Portal.Virtualization;
using SenseNet.Services.Wopi;
using SenseNet.Tests;
using File = SenseNet.ContentRepository.File;

namespace SenseNet.Services.Tests
{
    [TestClass]
    public class WopiTests : TestBase
    {
        [TestMethod]
        public void Wopi_Req_GetFileRequest()
        {
            Test(() =>
            {
                CreateTestSite();
                using (var output = new StringWriter())
                {
                    var pc = CreatePortalContext("GET", "/wopi/files/123/contents", DefaultAccessTokenParameter, output,
                        new[]
                        {
                            new[] { "X-WOPI-MaxExpectedSize", "9999"},
                        });
                    Assert.IsTrue(pc.IsWopiRequest);
                    var wopiReq = pc.WopiRequest;
                    Assert.IsNotNull(wopiReq);
                    Assert.AreEqual(WopiRequestType.GetFile, wopiReq.RequestType);
                    var getFileRequest = wopiReq as GetFileRequest;
                    Assert.IsNotNull(getFileRequest);
                    Assert.AreEqual("123", getFileRequest.FileId);
                    Assert.AreEqual(9999, getFileRequest.MaxExpectedSize);

                    // TEST: without MaxExpectedSize
                    pc = CreatePortalContext("GET", "/wopi/files/123/contents", DefaultAccessTokenParameter, output,
                        new[]
                        {
                            new[] { "Header1", "value1"},
                        });
                    getFileRequest = pc.WopiRequest as GetFileRequest;
                    Assert.IsNotNull(getFileRequest);
                    Assert.IsNull(getFileRequest.MaxExpectedSize);
                }
            });
        }
        [TestMethod]
        [ExpectedException(typeof(InvalidWopiRequestException))]
        public void Wopi_Req_GetFileRequest_BadMethod()
        {
            Test(() =>
            {
                CreateTestSite();
                using (var output = new StringWriter())
                {
                    CreatePortalContext("POST", "/wopi/files/123/contents", DefaultAccessTokenParameter, output,
                        new[]
                        {
                            new[] { "X-WOPI-MaxExpectedSize", "9999"},
                        });
                }
            });
        }
        [TestMethod]
        [ExpectedException(typeof(InvalidWopiRequestException))]
        public void Wopi_Req_GetFileRequest_InvalidMaxSize()
        {
            Test(() =>
            {
                CreateTestSite();
                using (var output = new StringWriter())
                {
                    CreatePortalContext("GET", "/wopi/files/123/contents", DefaultAccessTokenParameter, output,
                        new[]
                        {
                            new[] { "X-WOPI-MaxExpectedSize", "nine-tausend"},
                        });
                }
            });
        }


        /* ======================================================================================= */

        [TestMethod]
        public void Wopi_Handler_GetFile()
        {
            Test(() =>
            {
                var site = CreateTestSite();
                var file = CreateTestFile(site, "File1.txt", "filecontent1");

                var response = WopiGet($"/wopi/files/{file.Id}/contents", DefaultAccessTokenParameter, new[]
                {
                    new[] {"X-WOPI-MaxExpectedSize", "9999"},
                });

                Assert.AreEqual(HttpStatusCode.OK, response.Status);
                Assert.AreEqual("filecontent1", RepositoryTools.GetStreamString(response.GetResponseStream()));
            });
        }
        [TestMethod]
        public void Wopi_Handler_GetFile_NotFound()
        {
            Test(() =>
            {
                var site = CreateTestSite();

                var response = WopiGet($"/wopi/files/{site.Id}/contents", DefaultAccessTokenParameter, new[]
                {
                    new[] {"X-WOPI-MaxExpectedSize", "9999"},
                });

                Assert.AreEqual(HttpStatusCode.NotFound, response.Status);
            });
        }
        [TestMethod]
        public void Wopi_Handler_GetFile_TooBig()
        {
            Test(() =>
            {
                var site = CreateTestSite();
                var file = CreateTestFile(site, "File1.txt", "filecontent1");

                var response = WopiGet($"/wopi/files/{file.Id}/contents", DefaultAccessTokenParameter, new[]
                {
                    new[] {"X-WOPI-MaxExpectedSize", "3"}, // shorter than file content
                });

                Assert.AreEqual(HttpStatusCode.PreconditionFailed, response.Status);
            });
        }

        /* ======================================================================================= */

        private static readonly string DefaultAccessToken = "__DefaultAccessToken__";
        private static readonly string DefaultAccessTokenParameter = "access_token=" + DefaultAccessToken;

        private WopiResponse WopiGet(string resource, string queryString, string[][] headers)
        {
            return GetWopiResponse("GET", resource, queryString, headers, null);
        }
        private WopiResponse WopiPost(string resource, string queryString, string[][] headers, Stream requestStream)
        {
            return GetWopiResponse("GET", resource, queryString, headers, requestStream);
        }
        private WopiResponse GetWopiResponse(string httpMethod, string resource, string queryString, string[][] headers, Stream requestStream)
        {
            using (var output = new System.IO.StringWriter())
                return new WopiHandler().GetResponse(
                   CreatePortalContext(httpMethod, resource, queryString, output, headers));
        }

        private const string TestSiteName = "WopiTestSite";
        private static string TestSitePath => RepositoryPath.Combine("/Root/Sites", TestSiteName);
        private static Site CreateTestSite()
        {
            var sites = new Folder(Repository.Root, "Sites") { Name = "Sites" };
            sites.Save();

            var site = new Site(sites) { Name = TestSiteName, UrlList = new Dictionary<string, string> { { "localhost", "None" } } };
            site.AllowChildType("File");
            site.Save();

            return site;
        }
        private File CreateTestFile(Node parent, string name, string fileContent)
        {
            var file = new File(parent) { Name = name ?? Guid.NewGuid().ToString() };
            file.Binary.SetStream(RepositoryTools.GetStreamFromString(fileContent ?? Guid.NewGuid().ToString()));
            file.Save();
            return file;
        }

        private static PortalContext CreatePortalContext(string httpMethod, string pagePath, string queryString, System.IO.TextWriter output, string[][] headers)
        {
            var simulatedWorkerRequest = new SimulatedHttpRequest(@"\", @"C:\Inetpub\wwwroot", pagePath, queryString, output, "localhost", headers, httpMethod);
            var simulatedHttpContext = new HttpContext(simulatedWorkerRequest);
            HttpContext.Current = simulatedHttpContext;
            var portalContext = PortalContext.Create(simulatedHttpContext);
            return portalContext;
        }

    }
}
