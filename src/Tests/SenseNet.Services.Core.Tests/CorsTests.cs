using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Services.Core.Cors;
using SenseNet.Tests;

namespace SenseNet.Services.Core.Tests
{
    [TestClass]
    public class CorsTests : TestBase
    {
        [TestMethod]
        public void Cors_AllowedDomain()
        {
            // empty
            Assert.AreEqual(null, SnCorsPolicyProvider.GetAllowedDomain("abc", new string[0]));

            // regular domains
            Assert.AreEqual("abc", SnCorsPolicyProvider.GetAllowedDomain("abc", new[] { "abc", "def" }));
            Assert.AreEqual("abc", SnCorsPolicyProvider.GetAllowedDomain("abc", new[] { "def", "abc" }));
            Assert.AreEqual("abc-dev", SnCorsPolicyProvider.GetAllowedDomain("abc-dev", new[] { "abc-dev", "app123" }));
            Assert.AreEqual("app123", SnCorsPolicyProvider.GetAllowedDomain("app123", new[] { "abc-dev", "app123" }));

            // wildcard (all)
            Assert.AreEqual("*", SnCorsPolicyProvider.GetAllowedDomain("abc", new[] { "*" }));
            Assert.AreEqual("*", SnCorsPolicyProvider.GetAllowedDomain("abc", new[] { "*", "abc" }));
            Assert.AreEqual("abc", SnCorsPolicyProvider.GetAllowedDomain("abc", new[] { "abc", "*" }));
            Assert.AreEqual("*", SnCorsPolicyProvider.GetAllowedDomain("abc", new[] { "abcd", "*" }));
            Assert.AreEqual("*", SnCorsPolicyProvider.GetAllowedDomain("abc-dev", new[] { "*", "app123" }));
            Assert.AreEqual("*", SnCorsPolicyProvider.GetAllowedDomain("app123", new[] { "abc-dev", "*" }));

            // wildcard (subdomain)
            Assert.AreEqual(null, SnCorsPolicyProvider.GetAllowedDomain("abc", new[] { "*.abc" }));
            Assert.AreEqual(null, SnCorsPolicyProvider.GetAllowedDomain("def", new[] { "*.abc" }));
            Assert.AreEqual(null, SnCorsPolicyProvider.GetAllowedDomain("abc.com", new[] { "*.abc.com" }));
            Assert.AreEqual("abc", SnCorsPolicyProvider.GetAllowedDomain("abc", new[] { "*.abc", "abc" }));
            Assert.AreEqual("abc", SnCorsPolicyProvider.GetAllowedDomain("abc", new[] { "abc", "*.abc" }));
            Assert.AreEqual("*.abc", SnCorsPolicyProvider.GetAllowedDomain("sub1.abc", new[] { "*.abc" }));
            Assert.AreEqual("abc.*.abc", SnCorsPolicyProvider.GetAllowedDomain("abc.sub1.abc", new[] { "abc.*.abc" }));
            Assert.AreEqual("abc.*.abc", SnCorsPolicyProvider.GetAllowedDomain("abc.sub1.sub2.abc", new[] { "abc.*.abc" }));
            Assert.AreEqual(null, SnCorsPolicyProvider.GetAllowedDomain("abc.com", new[] { "abc.*.com" }));
            Assert.AreEqual(null, SnCorsPolicyProvider.GetAllowedDomain("abc..com", new[] { "abc.*.com" }));
            Assert.AreEqual(null, SnCorsPolicyProvider.GetAllowedDomain("nooo.abc.sub1.abc", new[] { "abc.*.abc" }));
            Assert.AreEqual("*.abc", SnCorsPolicyProvider.GetAllowedDomain("sub1.abc", new[] { "abcd", "sub1abc", "sub1abccom", "*.abc" }));
            Assert.AreEqual("*.abc", SnCorsPolicyProvider.GetAllowedDomain("sub-dev.abc", new[] { "*.abc", "app123.abc" }));
            Assert.AreEqual("*.abc", SnCorsPolicyProvider.GetAllowedDomain("a1b2c3--app-dev.abc", new[] { "sub.abc", "*.abc" }));
            Assert.AreEqual("abc.*.com", SnCorsPolicyProvider.GetAllowedDomain("abc.a1b2c3--app-dev.com", new[] { "abc.app.com", "abc.*.com" }));

            // wildcard (port)
            Assert.AreEqual(null, SnCorsPolicyProvider.GetAllowedDomain("ab:5000", new[] { "abc" }));
            Assert.AreEqual(null, SnCorsPolicyProvider.GetAllowedDomain("ab:5000", new[] { "abc:4000" }));
            Assert.AreEqual("abc:*", SnCorsPolicyProvider.GetAllowedDomain("abc", new[] { "abc:*" }));
            Assert.AreEqual(null, SnCorsPolicyProvider.GetAllowedDomain("sub1.abc", new[] { "abc:*" }));
            Assert.AreEqual("abc:*", SnCorsPolicyProvider.GetAllowedDomain("abc:5000", new[] { "abc:*" }));
            Assert.AreEqual("abc:*", SnCorsPolicyProvider.GetAllowedDomain("abc", new[] { "abc:4000", "abc:*" }));
            Assert.AreEqual("abc:*", SnCorsPolicyProvider.GetAllowedDomain("abc:5000", new[] { "abc:4000", "abc:*" }));
            Assert.AreEqual("abc.com:*", SnCorsPolicyProvider.GetAllowedDomain("abc.com", new[] { "abc.com:*" }));
            Assert.AreEqual("*.abc.com:*", SnCorsPolicyProvider.GetAllowedDomain("sub1.abc.com", new[] { "*.abc.com:*" }));
            Assert.AreEqual("*.abc.com:*", SnCorsPolicyProvider.GetAllowedDomain("sub1.sub2.abc.com", new[] { "*.abc.com:*" }));
            Assert.AreEqual("abc.*.com:*", SnCorsPolicyProvider.GetAllowedDomain("abc.admin.com", new[] { "abc.*.com:*" }));
            Assert.AreEqual(null, SnCorsPolicyProvider.GetAllowedDomain("abc.com:5000", new[] { "abc.*.com:*" }));
            Assert.AreEqual(null, SnCorsPolicyProvider.GetAllowedDomain("abc..com:5000", new[] { "abc.*.com:*" }));
            Assert.AreEqual("abc.*.com:*", SnCorsPolicyProvider.GetAllowedDomain("abc.admin.sub1.com", new[] { "abc.*.com:*" }));
            Assert.AreEqual("abc.*.com:*", SnCorsPolicyProvider.GetAllowedDomain("abc.admin.sub1.com:5000", new[] { "abc.*.com:*" }));
            Assert.AreEqual(null, SnCorsPolicyProvider.GetAllowedDomain("abc.sub1.abc.com:5000", new[] { "abc.*.abc.com" }));
            Assert.AreEqual("*.abc.com:*", SnCorsPolicyProvider.GetAllowedDomain("sub-dev.abc.com:5000", new[] { "*.abc.com:*", "app123.abc.com:5000" }));
            Assert.AreEqual("*.abc.com:*", SnCorsPolicyProvider.GetAllowedDomain("a1b2c3--app-dev.abc.com:8888", new[] { "sub.abc.com:8888", "*.abc.com:*" }));
            Assert.AreEqual("abc.*.com:*", SnCorsPolicyProvider.GetAllowedDomain("abc.a1b2c3--app-dev.com:80", new[] { "abc.app.com:80", "abc.*.com:*" }));

            // invalid config
            Assert.AreEqual(null, SnCorsPolicyProvider.GetAllowedDomain("abc", new[] { "*abc" }));
        }

        [TestMethod]
        public void Cors_SetHeader_Simple()
        {
            AssertOrigin(null, null, null);
            AssertOrigin("null", null, null);

            AssertOrigin("http://example.com", null, null);
            AssertOrigin("http://example.com", new[] { "otherdomain" }, null);
            AssertOrigin("http://example.com", new[] { "*" }, "*");
            AssertOrigin("http://example.com", new[] { "example.com" }, "example.com");
        }

        [TestMethod]
        public void Cors_SetHeader_Port()
        {
            // strict behavior: allowed domain list contains only the domain, not the port of the origin
            AssertOrigin("http://localhost:123", new[] { "localhost" }, null);

            // no defined allowed origins
            AssertOrigin("http://localhost:123", null, null);

            // default port
            AssertOrigin("http://localhost:80", null, null);

            // urls are the same plus existing allow list (makes no difference)
            AssertOrigin("http://localhost:123", new[] { "localhost" }, null);
            // urls are the same plus allow list contains the url (makes no difference)
            AssertOrigin("http://localhost:123", new[] { "localhost:123" }, "localhost:123");
        }

        [TestMethod]
        public void Cors_SetHeader_Port_Subdomain()
        {
            // allowed list contains the domain but does not contain the port
            AssertOrigin("http://sub.example.com:123", new[] { "*.example.com" }, null);
            // allowed list contains the domain but with a different port
            AssertOrigin("http://sub.example.com:123", new[] { "*.example.com:456" }, null);

            // allowed list contains a wildcard domain and the port
            AssertOrigin("http://sub.example.com:123", new[] { "*.example.com:123" }, "*.example.com:123");
        }

        [TestMethod]
        public void Cors_SetHeader_Https()
        {
            AssertOrigin("https://otherdomain.com", new[] { "localhost" }, null);
            AssertOrigin("https://otherdomain.com:443",new[] { "localhost" }, null);
            AssertOrigin("https://example.com:443", new[] {"example.com"}, null);
            AssertOrigin("https://example.com:443", new[] {"example.com:*"}, "example.com:*");
        }

        [TestMethod]
        public async Task Cors_HttpContext_PolicyNotFound()
        {
            var cpp = new SnCorsPolicyProvider(null);
            var hc = new DefaultHttpContext();

            // no origin header
            Assert.IsNull(await cpp.GetPolicyAsync(hc, "sensenet"));

            // no policy name
            hc = new DefaultHttpContext();
            hc.Request.Headers.Add("Origin", "abc");
            Assert.IsNull(await cpp.GetPolicyAsync(hc, null));

            // unknown policy name
            Assert.IsNull(await cpp.GetPolicyAsync(hc, "other"));
        }

        [TestMethod]
        public async Task Cors_HttpContext_PolicyFound()
        {
            await Test(async () =>
            {
                // default settings support localhost and sensenet.com
                var p = await AssertOriginPrivate("localhost", true);
                Assert.IsTrue(p.SupportsCredentials);
                p = await AssertOriginPrivate("localhost:123", true);
                Assert.IsTrue(p.SupportsCredentials);
                p = await AssertOriginPrivate("example.sensenet.com", true);
                Assert.IsTrue(p.SupportsCredentials);

                await AssertOriginPrivate("sensenet.com", false);
                await AssertOriginPrivate("example.com", false);
            });

            async Task<CorsPolicy> AssertOriginPrivate(string origin, bool expected)
            {
                var cpp = new SnCorsPolicyProvider(null);
                var context = new DefaultHttpContext();
                context.Request.Headers["Origin"] = origin;
                var p = await cpp.GetPolicyAsync(context, SnCorsPolicyProvider.DefaultSenseNetCorsPolicyName);
                Assert.AreEqual(expected, p.Origins.Contains(origin));

                return p;
            }
        }

        private static void AssertOrigin(string originHeader, string[] allowedOrigins, string expectedDomain)
        {
            var domainMatch = SnCorsPolicyProvider.GetAllowedDomain(originHeader, allowedOrigins);
            
            Assert.AreEqual(expectedDomain, domainMatch);
        }
    }
}
