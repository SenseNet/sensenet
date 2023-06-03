using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Common;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class StringExtensionTests
    {
        [TestMethod, TestCategory("StringExtension")]
        public void StringExtension_Truncate()
        {
            Assert.AreEqual("abc", "abcdef".Truncate(3));
            Assert.AreEqual("abc", "abc".Truncate(10));
            Assert.AreEqual("abc", "abc".Truncate(3));
            Assert.AreEqual("", "abc".Truncate(0));
            Assert.AreEqual("", "".Truncate(3));
            Assert.AreEqual(null, SenseNet.Common.StringExtensions.Truncate(null, 3));

            try
            {
                Assert.AreEqual("", "abc".Truncate(-1));
                Assert.Fail("Truncate(-1) did not throw an exception.");
            }
            catch (InvalidOperationException)
            {
                // expected exception
            }
        }
    }
}
