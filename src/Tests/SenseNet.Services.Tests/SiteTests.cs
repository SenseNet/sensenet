using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Portal;
using SenseNet.Tests;

namespace SenseNet.Services.Tests
{
    [TestClass]
    public class SiteTests : TestBase
    {
        [TestMethod]
        public void Site_UppercaseUrl()
        {
            Assert.IsTrue(Site.IsValidSiteUrl("example.com"));
            Assert.IsTrue(Site.IsValidSiteUrl("example.com:1234"));         // a port is allowed
            Assert.IsTrue(Site.IsValidSiteUrl("exAmPlE.cOm"));              // uppercase url
            Assert.IsTrue(Site.IsValidSiteUrl("www.example.com"));
            Assert.IsTrue(Site.IsValidSiteUrl("sub1.sub2.example.com"));

            Assert.IsFalse(Site.IsValidSiteUrl(""));
            Assert.IsFalse(Site.IsValidSiteUrl("."));
            Assert.IsFalse(Site.IsValidSiteUrl(".com"));
            Assert.IsFalse(Site.IsValidSiteUrl("http://example.com"));      // a schema prefix is not allowed
            Assert.IsFalse(Site.IsValidSiteUrl("http:/example.com"));
            Assert.IsFalse(Site.IsValidSiteUrl("/example.com"));
            Assert.IsFalse(Site.IsValidSiteUrl("//example.com"));
            Assert.IsFalse(Site.IsValidSiteUrl(":/example.com"));
            Assert.IsFalse(Site.IsValidSiteUrl("example.com//"));
            Assert.IsFalse(Site.IsValidSiteUrl("example.com/file.txt"));    // only a domain is allowed
            Assert.IsFalse(Site.IsValidSiteUrl("example.com/page1/page2")); // only a domain is allowed
        }
    }
}
