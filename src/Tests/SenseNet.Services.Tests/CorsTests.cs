using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Portal.Virtualization;
using SenseNet.Tests;

namespace SenseNet.Services.Tests
{
    [TestClass]
    public class CorsTests : TestBase
    {
        private static Uri DefaultRequestUri => new Uri("http://otherdomain.com/content/method");

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
            Assert.AreEqual(null, HttpHeaderTools.GetAllowedDomain("def", new[] { "*.abc" }));
            Assert.AreEqual(null, HttpHeaderTools.GetAllowedDomain("abc.com", new[] { "*.abc.com" }));
            Assert.AreEqual("abc", HttpHeaderTools.GetAllowedDomain("abc", new[] { "*.abc", "abc" }));
            Assert.AreEqual("abc", HttpHeaderTools.GetAllowedDomain("abc", new[] { "abc", "*.abc" }));
            Assert.AreEqual("*.abc", HttpHeaderTools.GetAllowedDomain("sub1.abc", new[] { "*.abc" }));
            Assert.AreEqual("*.abc", HttpHeaderTools.GetAllowedDomain("sub1.abc", new[] { "abcd", "sub1abc", "sub1abccom", "*.abc" }));

            // invalid config
            Assert.AreEqual(null, HttpHeaderTools.GetAllowedDomain("abc", new[] { "*abc" }));
        }

        [TestMethod]
        public void Cors_SetHeader_Simple()
        {
            AssertOrigin(null, null, null, true, null);
            AssertOrigin("null", null, null, true, null);

            AssertOrigin("http://example.com", DefaultRequestUri, null, false, null);
            AssertOrigin("http://example.com", DefaultRequestUri, new[] { "otherdomain" }, false, null);
            AssertOrigin("http://example.com", DefaultRequestUri, new[] { "*" }, true, "*");
            AssertOrigin("http://example.com", DefaultRequestUri, new[] { "example.com" }, true, "http://example.com");
        }

        [TestMethod]
        public void Cors_SetHeader_Port()
        {
            // strict behavior: allowed domain list contains only the domain, not the port of the origin
            AssertOrigin("http://localhost:123", DefaultRequestUri, new[] { "localhost" }, false, null);

            // not a real CORS request: urls are the same
            AssertOrigin("http://localhost:123", GetLocalRequestUri(123), null, true, null);

            // default port
            AssertOrigin("http://localhost:80", GetLocalRequestUri(0), null, true, null);

            // urls are the same plus existing allow list (makes no difference)
            AssertOrigin("http://localhost:123", GetLocalRequestUri(123), new[] { "localhost" }, true, null);
            // urls are the same plus allow list contains the url (makes no difference)
            AssertOrigin("http://localhost:123", GetLocalRequestUri(123), new[] { "localhost:123" }, true, null);

            // same domain, different port, allow list does not contain the port
            AssertOrigin("http://localhost:123", GetLocalRequestUri(456), new[] { "localhost" }, false, null);

            // different domain, allowed list contains the port
            AssertOrigin("http://localhost:123", DefaultRequestUri, new[] { "localhost:123" }, true,
                "http://localhost:123");

            // same domain, different port, allowed list contains the port
            AssertOrigin("http://localhost:123", GetLocalRequestUri(456), new[] { "localhost:123" }, true,
                "http://localhost:123");
        }

        [TestMethod]
        public void Cors_SetHeader_Https()
        {
            AssertOrigin("https://otherdomain.com", DefaultRequestUri, new[] { "localhost" }, false, null);
            AssertOrigin("https://otherdomain.com:443", new Uri($"https://otherdomain.com/content/method"),
                new[] {"localhost"}, true, null);
            AssertOrigin("https://example.com:443", DefaultRequestUri, new[] {"example.com"}, true,
                "https://example.com:443");
        }

        private static void AssertOrigin(string originHeader, Uri requestUri,string[] allowedOrigins, bool expectedResult, string expectedDomain)
        {
            var result = HttpHeaderTools.TrySetAllowedOriginHeader(originHeader, requestUri, 
                () => allowedOrigins, out var domain);

            Assert.AreEqual(expectedResult, result);
            Assert.AreEqual(expectedDomain, domain);
        }
        private static Uri GetLocalRequestUri(int port = 0)
        {
            var portText = port > 0 ? ":" + port : string.Empty;

            return new Uri($"http://localhost{portText}/content/method");
        }
    }
}
