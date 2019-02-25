using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Portal.Virtualization;
using SenseNet.Tests;

namespace SenseNet.Services.Tests
{
    [TestClass]
    public class CorsTests : TestBase
    {
        [TestMethod]
        public void Cors_AllowedDomain()
        {
            // empty
            Assert.AreEqual(null, HttpHeaderTools.GetAllowedDomain("abc", new string[0]));

            // regular domains
            Assert.AreEqual("abc", HttpHeaderTools.GetAllowedDomain("abc", new[] { "abc", "def" }));
            Assert.AreEqual("abc", HttpHeaderTools.GetAllowedDomain("abc", new[] { "def", "abc" }));

            // wildcard (all)
            Assert.AreEqual("*", HttpHeaderTools.GetAllowedDomain("abc", new []{"*"}));
            Assert.AreEqual("*", HttpHeaderTools.GetAllowedDomain("abc", new []{"*", "abc"}));
            Assert.AreEqual("abc", HttpHeaderTools.GetAllowedDomain("abc", new []{"abc", "*" }));
            Assert.AreEqual("*", HttpHeaderTools.GetAllowedDomain("abc", new []{"abcd", "*" }));

            // wildcard (subdomain)
            Assert.AreEqual(null, HttpHeaderTools.GetAllowedDomain("abc", new[] { "*.abc" }));
            Assert.AreEqual("abc", HttpHeaderTools.GetAllowedDomain("abc", new[] { "*.abc", "abc" }));
            Assert.AreEqual("abc", HttpHeaderTools.GetAllowedDomain("abc", new[] { "abc", "*.abc" }));
            Assert.AreEqual("*.abc", HttpHeaderTools.GetAllowedDomain("sub1.abc", new[] { "*.abc" }));
            Assert.AreEqual("*.abc", HttpHeaderTools.GetAllowedDomain("sub1.abc", new[] { "abcd", "sub1abc", "sub1abccom", "*.abc" }));
        }
    }
}
