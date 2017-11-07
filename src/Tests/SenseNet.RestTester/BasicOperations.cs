using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Client;

namespace SenseNet.RestTester
{
    internal class BasicOperations : TestBase
    {
        [TestMethod]
        public void LoadRoot()
        {
            var root = Content.LoadAsync("/Root").Result;
            Assert.AreEqual(2, root.Id);
            Assert.AreEqual("/Root", root.Path);
        }
        [TestMethod]
        public void LoadChildrenOfRoot()
        {
            var topLevelCollection = Content.LoadCollectionAsync("/Root").Result;
            var topLevelNames = topLevelCollection.Select(c => c.Name).ToArray();

            var actual = string.Join(", ", topLevelNames);
            var expected = "(apps), IMS, Localization, System, Trash";
            Assert.AreEqual(expected, actual);
        }
        [TestMethod]
        public void CreateAndDeleteContent()
        {
            var contentName = $"Test_{Guid.NewGuid()}";

            // Create and save a new content
            var content = Content.CreateNew("/Root", "SystemFolder", contentName);
            content.SaveAsync().Wait();
            var contentId = content.Id;
            Assert.IsTrue(0 < contentId);

            // Load back the newly created content
            var loadedContent = Content.LoadAsync(contentId).Result;
            Assert.AreEqual(contentId, loadedContent.Id);
            Assert.AreEqual(contentName, loadedContent.Name);

            // Delete
            loadedContent.DeleteAsync().Wait();

            // Check that the current state and initial state are same
            var deletedContent = Content.LoadAsync(contentId).Result;
            Assert.IsNull(deletedContent);
        }
        [TestMethod]
        public void QueryContent()
        {
            var contentName = $"Test_{Guid.NewGuid()}";
            var content = Content.CreateNew("/Root", "SystemFolder", contentName);
            content.SaveAsync().Wait();
            var contentId = content.Id;

            // Query for name of the newly created content
            var foundContents = Content.QueryForAdminAsync($"Name:'{contentName}'").Result.ToArray();
            Assert.AreEqual(1, foundContents.Length);
            var foundContent = foundContents[0];
            Assert.AreEqual(contentId, foundContent.Id);
            Assert.AreEqual(contentName, foundContent.Name);
        }
    }
}
