using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

namespace SenseNet.Services.Tests
{
    [TestClass]
    public class WopiTests : TestBase
    {
        [TestMethod]
        public void Wopi_Req_()
        {
            Test(() =>
            {
                CreateTestSite();
                using (var output = new StringWriter())
                {
                    var pc = CreatePortalContext("/wopi/files/123/contents", DefaultAccessTokenParameter, output,
                        new[]
                        {
                            new[] { "X-WOPI-MaxExpectedSize", "9999"},
                        });
                    Assert.IsTrue(pc.IsWopiRequest);
                    var wopiReq = pc.WopiRequest;
                    Assert.IsNotNull(wopiReq);
                }
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
                return new WopiHandler().ProcessRequest(
                   CreatePortalContext(resource, queryString, output, headers).OwnerHttpContext,  httpMethod, requestStream);
        }

        private const string TestSiteName = "WopiTestSite";
        private static string TestSitePath => RepositoryPath.Combine("/Root/Sites", TestSiteName);
        private static Site CreateTestSite()
        {
            var sites = new Folder(Repository.Root, "Sites") { Name = "Sites" };
            sites.Save();

            var site = new Site(sites) { Name = TestSiteName, UrlList = new Dictionary<string, string> { { "localhost", "None" } } };
            site.Save();

            return site;
        }

        private static PortalContext CreatePortalContext(string pagePath, string queryString, System.IO.TextWriter output, string[][] headers)
        {
            var simulatedWorkerRequest = new SimulatedHttpRequest(@"\", @"C:\Inetpub\wwwroot", pagePath, queryString, output, "localhost");
            var simulatedHttpContext = new HttpContext(simulatedWorkerRequest);
            HttpContext.Current = simulatedHttpContext;
            var portalContext = PortalContext.Create(simulatedHttpContext);
            return portalContext;
        }

    }
}
