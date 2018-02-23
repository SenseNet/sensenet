using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Portal;
using SenseNet.Portal.Virtualization;
using SenseNet.Tests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class PortalContextTests : TestBase
    {
        private void CreateTestSite()
        {
            var sites = new GenericContent(Repository.Root, "Sites") { Name = "Sites" };
            sites.Save();

            var site = new Site(sites)
            {
                Name = "Fake Test Site",
                UrlList = new Dictionary<string, string> {
                    {"localhost_forms", "Forms"},
                    {"localhost_windows", "Windows"},
                    { "localhost_none", "None"} }
            };            
            site.Save();
        }

        [TestMethod]
        public void PortalContext_RepositoryPathResolve_OffSite()
        {
            Test(() =>
            {
                CreateTestSite();

                string pagePath = "/Root/System/alma.jpg/";
                string expectedRepositoryPath = "/Root/System/alma.jpg";

                System.IO.StringWriter simulatedOutput = new System.IO.StringWriter();
                SimulatedHttpRequest simulatedWorkerRequest = new SimulatedHttpRequest(@"\", @"C:\Inetpub\wwwroot", pagePath, "", simulatedOutput, "localhost_forms");
                HttpContext simulatedHttpContext = new HttpContext(simulatedWorkerRequest);
                PortalContext portalContext = PortalContext.Create(simulatedHttpContext);

                Assert.AreEqual(expectedRepositoryPath, portalContext.RepositoryPath);
                Assert.AreEqual("Forms", portalContext.AuthenticationMode);
            });
        }

        [TestMethod]
        public void PortalContext_RepositoryPathResolve_OnSite()
        {
            Test(() =>
            {
                CreateTestSite();

                string pagePath = "/Pictures/alma.jpg/";
                string expectedRepositoryPath = "/Root/Sites/Fake Test Site/Pictures/alma.jpg";

                System.IO.StringWriter simulatedOutput = new System.IO.StringWriter();
                SimulatedHttpRequest simulatedWorkerRequest = new SimulatedHttpRequest(@"\", @"C:\Inetpub\wwwroot", pagePath, "", simulatedOutput, "localhost_windows");
                HttpContext simulatedHttpContext = new HttpContext(simulatedWorkerRequest);
                PortalContext portalContext = PortalContext.Create(simulatedHttpContext);

                Assert.AreEqual(expectedRepositoryPath, portalContext.RepositoryPath);
                Assert.AreEqual("Windows", portalContext.AuthenticationMode);
            });
        }
    }
}
