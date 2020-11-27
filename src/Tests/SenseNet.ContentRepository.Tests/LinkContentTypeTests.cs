using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Storage;
using SenseNet.Tests.Core;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class LinkContentTypeTests : TestBase
    {
        private static readonly string[] ValidUrls = new string[]
        {
            //"http://✪df.ws/123", // unicode characters are not recognized

            "http://foo.com",
            "https://foo.com",
            "http://foo.com/",
            "http://foo.com/blah",
            "https://foo.com/blah",
            "http://foo.com/blah_blah_(wikipedia)",
            "http://www.example.com?",
            "http://www.example.com?a=34",
            "https://example.com/?a=b&c=d",
            "https://example.com/?a=b&c=d&e=%20x%57",
            "http://www.example.com/wpstyle/?a=364",
            "https://www.example.com/foo/?bar=baz&inga=42&quux",
            "http://userid:password@example.com:8080",
            "http://userid@example.com",
            "http://142.42.1.1/",
            "http://142.42.1.1",
            "http://1337.net",
            "http://foo.com/blah_blah#cite-1",
            "http://foo.com/blah_(wikipedia)_blah#cite-1",
            "http://a.b-c.de",
            "ftp://foo.bar/baz"
        };
        private static readonly string[] InvalidUrls = new string[]
        {
            "http://",
            "http://.",
            "http://../",
            "htt://foo.com",
            "http:/?",
            "http:/#",
            "http://?.com",
            "http://foo/.com",
            "http://foo.bar?q=Spaces should be encoded",
            "ftps://foo.bar/",
            "http://.www.foo.bar/",
            "http://www.foo.bar./",
            "//a"
        };

        [TestMethod]
        public void Link_Url_Regex()
        {
            static void AssertUrl(Content link, string url, bool shouldBeValid)
            {
                link["Url"] = url;

                var thrown = false;
                try
                {
                    link.SaveSameVersion();
                }
                catch (InvalidContentException)
                {
                    thrown = true;
                }

                if (shouldBeValid)
                    Assert.IsFalse(thrown, "Valid url triggered an exception: " + url);
                else
                    Assert.IsTrue(thrown, "Invalid url did not trigger an exception: " + url);
            }

            Test(() =>
            {
                // This test is for checking the regex defined in the built-in Link CTD.
                // The regex checks validity of the value provided in the Url field.
                // Saving a link containing an invalid url should throw an exception.

                var parent = CreateTestRoot();
                var link = Content.CreateNew("Link", parent, Guid.NewGuid().ToString());

                // check VALID urls
                foreach (var validUrl in ValidUrls)
                {
                    AssertUrl(link, validUrl, true);
                }

                // check INVALID urls
                foreach (var invalidUrl in InvalidUrls)
                {
                    AssertUrl(link, invalidUrl, false);
                }
            });
        }

        protected static GenericContent CreateTestRoot()
        {
            var node = new SystemFolder(Repository.Root) { Name = Guid.NewGuid().ToString() };
            node.Save();

            return node;
        }
    }
}
