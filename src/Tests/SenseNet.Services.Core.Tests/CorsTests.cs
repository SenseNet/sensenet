using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.Services.Core.Cors;
using SenseNet.Tests.Core;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.Services.Core.Tests
{
    [TestClass]
    public class CorsTests : TestBase
    {
        [TestMethod, TestCategory("Services")]
        public void Cors_AllowedDomain_CSrv()
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

        [DataTestMethod]
        [DataRow("https://app.sensenet.com", "*.sensenet.com", true)]
        [DataRow("https://a.b.sensenet.com", "*.sensenet.com", true)]
        [DataRow("HTTPS://APP.SENSENET.COM", "*.sensenet.com", true)]
        [DataRow("https://attacker-sensenet.com", "*.sensenet.com", false)]
        [DataRow("https://app.sensenetXcom", "*.sensenet.com", false)]
        [DataRow("https://app.sensenet.evil.com", "*.sensenet.com", false)]
        [DataRow("https://app.sensenet.com.evil.example", "*.sensenet.com", false)]
        [DataRow("https://sensenet.com", "*.sensenet.com", false)]
        [DataRow("https://app.sensenet.com", "sensenet.com", false)]
        [DataRow("https://sensenet.evil.com", "sensenet.com", false)]
        [DataRow("https://sensenet.com.evil.example", "sensenet.com", false)]
        [DataRow("https://x.a.c.a.b", "*.a.b", true)]
        [DataRow("https://abc.one.two.com", "abc.*.com", true)]
        [DataRow("https://abc.com", "abc.*.com", false)]
        [DataRow("https://x.abc.one.com", "abc.*.com", false)]
        [DataRow("https://a.x.y.b.z.com", "a.*.b.*.com", true)]
        [DataRow("https://a.x.b.y.b.z.com", "a.*.b.*.com", true)]
        [DataRow("https://a.x.b.com", "a.*.b.*.com", false)]
        [DataRow("https://x.y.example.com", "*.*.example.com", true)]
        [DataRow("https://x.example.com", "*.*.example.com", false)]
        [DataRow("https://abc.one.two", "abc.*", true)]
        [DataRow("https://abc", "abc.*", false)]
        public void Cors_AllowedDomain_WildcardBoundaries(string origin, string template, bool expected)
        {
            Assert.AreEqual(expected ? template : null,
                SnCorsPolicyProvider.GetAllowedDomain(origin, new[] { template }));
        }

        [DataTestMethod]
        [DataRow("https://abc.one.two.com:8443", "https://abc.*.com:8443", true)]
        [DataRow("http://abc.one.two.com:8443", "https://abc.*.com:8443", false)]
        [DataRow("https://abc.one.two.com:8443", "https://abc.*.com:443", false)]
        [DataRow("https://abc.one.two.com:8443", "https://abc.*.com", false)]
        [DataRow("https://abc.one.two.com:8443", "https://abc.*.com:*", true)]
        [DataRow("https://abc.one.two.com", "https://abc.*.com:*", true)]
        [DataRow("https://example.com:443", "https://example.com", false)]
        [DataRow("example.com", "https://example.com", false)]
        [DataRow("https://example.com", "https://example.com", true)]
        [DataRow("http://localhost:5000", "localhost:*", true)]
        [DataRow("http://127.0.0.1:5000", "127.0.0.1:*", true)]
        [DataRow("http://[::1]:5000", "[::1]:*", true)]
        [DataRow("http://[::1]", "[::1]", true)]
        [DataRow("http://[::1]:5000", "[::1]", false)]
        [DataRow("http://[::2]:5000", "[::1]:*", false)]
        public void Cors_AllowedDomain_SchemeAndPort(string origin, string template, bool expected)
        {
            Assert.AreEqual(expected ? template : null,
                SnCorsPolicyProvider.GetAllowedDomain(origin, new[] { template }));
        }

        [DataTestMethod]
        [DataRow(null, "*")]
        [DataRow("", "*")]
        [DataRow("null", "*")]
        [DataRow("https://example.com", null)]
        [DataRow("https://example.com", "")]
        [DataRow("https://app..sensenet.com", "*.sensenet.com")]
        [DataRow("https://.sensenet.com", "*.sensenet.com")]
        [DataRow("https://app.sensenet.com.", "*.sensenet.com")]
        [DataRow("https://app.sensenet.com", "app*.sensenet.com")]
        [DataRow("https://app.sensenet.com", "*.sensenet.(com|net)")]
        [DataRow("https://app.sensenet.com", "*..sensenet.com")]
        [DataRow("https://app.sensenet.com\n", "*.sensenet.com")]
        [DataRow("https://user@app.sensenet.com", "*.sensenet.com")]
        [DataRow("https://app.sensenet.com/path", "*.sensenet.com")]
        [DataRow("https://app.sensenet.com?query", "*.sensenet.com")]
        [DataRow("https://app.sensenet.com#fragment", "*.sensenet.com")]
        [DataRow("https://app.sensenet.com:65536", "*.sensenet.com:*")]
        [DataRow("https://app.sensenet.com:-1", "*.sensenet.com:*")]
        [DataRow("https://app.sensenet.com:", "*.sensenet.com:*")]
        [DataRow("https://app.sensenet.com:*", "*.sensenet.com:*")]
        [DataRow("ftp://app.sensenet.com", "*.sensenet.com")]
        [DataRow("ftp://app.sensenet.com", "ftp://app.sensenet.com")]
        [DataRow("https://https://app.sensenet.com", "https://*.sensenet.com")]
        public void Cors_AllowedDomain_RejectsInvalidInput(string origin, string template)
        {
            Assert.IsNull(SnCorsPolicyProvider.GetAllowedDomain(origin, new[] { template }));
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
                // set allowed domains for test
                var setting = await Node.LoadAsync<Settings>(
                    RepositoryPath.Combine(Repository.SettingsFolderPath, "Portal.settings"), CancellationToken.None);
                var currentSettingText = RepositoryTools.GetStreamString(setting.Binary.GetStream());
                var newSettingText = EditJson(currentSettingText, @"
{
""AllowedOriginDomains"": [
    ""localhost:*"",
    ""*.sensenet.com""
  ]
}
");
                setting.Binary.SetStream(RepositoryTools.GetStreamFromString(newSettingText));
                setting.SaveAsync(SavingMode.KeepVersion, CancellationToken.None).GetAwaiter().GetResult();

                // default settings support localhost and sensenet.com
                var p = await AssertOriginPrivate("localhost", true);
                Assert.IsTrue(p.SupportsCredentials);
                p = await AssertOriginPrivate("localhost:123", true);
                Assert.IsTrue(p.SupportsCredentials);
                p = await AssertOriginPrivate("example.sensenet.com", true);
                Assert.IsTrue(p.SupportsCredentials);

                await AssertOriginPrivate("sensenet.com", false);
                await AssertOriginPrivate("example.com", false);
                await AssertOriginPrivate("https://app.sensenet.com", true);
                p = await AssertOriginPrivate("https://attacker-sensenet.com", false);
                Assert.IsFalse(p.SupportsCredentials);
                p = await AssertOriginPrivate("https://app.sensenet.evil.com", false);
                Assert.IsFalse(p.SupportsCredentials);
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

        protected static string EditJson(string text, string value)
        {
            // no change, return the original text
            if (string.IsNullOrEmpty(value))
                return text;

            var jo = JsonConvert.DeserializeObject<JObject>(text ?? string.Empty);

            // merge the provided json into the original
            var newJson = JsonConvert.DeserializeObject<JObject>(value);
            jo.Merge(newJson, new JsonMergeSettings
            {
                MergeArrayHandling = MergeArrayHandling.Replace
            });

            return jo.ToString();
        }
    }
}
