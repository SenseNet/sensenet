using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.Tests;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class ContentProtectorTests : TestBase
    {
        [TestMethod]
        public void ContentProtector_ExtendTheList()
        {
            // TEST-1: The "TestFolder" is protected.
            Test(builder =>
            {
                // Protect a content with the white list extension
                builder.ProtectContent("/Root/TestFolder");
            }, () =>
            {
                var node = new SystemFolder(Repository.Root) { Name = "TestFolder" };
                node.Save();

                try
                {
                    node.ForceDelete();
                    Assert.Fail("The expected exception was not thrown.");
                }
                catch (ApplicationException)
                {
                    // do nothing
                }
            });

            // CLEANUP: Restore the original list
            Providers.Instance.ContentProtector = new ContentProtector();

            // TEST-2: The "TestFolder" is deletable.
            Test(() =>
            {
                var node = new SystemFolder(Repository.Root) { Name = "TestFolder" };
                node.Save();

                node.ForceDelete();
            });
        }

        [TestMethod]
        public void ContentProtector_ParentAxis()
        {
            var originalList = new string[0];

            Test(builder =>
            {
                originalList = ContentProtector.GetProtectedPaths();

                // Add a deep path
                builder.ProtectContent("/Root/A/B/C");
            }, () =>
            {
                var actual = string.Join(" ", 
                    ContentProtector.GetProtectedPaths().Except(originalList));

                // The whole parent axis added but the "Except" operation removes the "/Root".
                var expected = "/Root/A /Root/A/B /Root/A/B/C";

                Assert.AreEqual(expected, actual);
            });
        }

        [TestMethod]
        public void ContentProtector_ListIsImmutable()
        {
            Test(builder =>
            {
                // Additional path
                builder.ProtectContent("/Root/TestFolder");
            }, () =>
            {
                var originalList = ContentProtector.GetProtectedPaths();
                var expectedFirst = originalList[0];
                originalList[0] = null;

                var actualList = ContentProtector.GetProtectedPaths();
                var actualFirst = actualList[0];

                Assert.AreEqual(expectedFirst, actualFirst);
            });

        }

    }
}
