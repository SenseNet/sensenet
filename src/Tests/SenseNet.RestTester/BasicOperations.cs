using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Client;

namespace SenseNet.RestTester
{
    internal class BasicOperations : RestTestBase
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

            var topLevelNames = topLevelCollection
                .Where(c => !c.Name.StartsWith("Test"))
                .Select(c => c.Name)
                .ToArray();

            Assert.IsTrue(topLevelNames.Contains("(apps)"));
            Assert.IsTrue(topLevelNames.Contains("IMS"));
            Assert.IsTrue(topLevelNames.Contains("Localization"));
            Assert.IsTrue(topLevelNames.Contains("System"));
            Assert.IsTrue(topLevelNames.Contains("Trash"));
        }
        [TestMethod]
        public void CreateAndDeleteContent()
        {
            var contentName = $"Test_{Guid.NewGuid()}";

            // Create and save a new content
            var content = Content.CreateNew("/Root", "SystemFolder", contentName);
            content.SaveAsync().GetAwaiter().GetResult();
            var contentId = content.Id;
            Assert.IsTrue(0 < contentId);

            // Load back the newly created content
            var loadedContent = Content.LoadAsync(contentId).Result;
            Assert.AreEqual(contentId, loadedContent.Id);
            Assert.AreEqual(contentName, loadedContent.Name);

            // Delete
            loadedContent.DeleteAsync().GetAwaiter().GetResult();

            // Check that the current state and initial state are same
            var deletedContent = Content.LoadAsync(contentId).Result;
            Assert.IsNull(deletedContent);
        }
        [TestMethod]
        public void QueryContent()
        {
            var contentName = $"Test_{Guid.NewGuid()}";
            var content = Content.CreateNew("/Root", "SystemFolder", contentName);
            content.SaveAsync().GetAwaiter().GetResult();
            var contentId = content.Id;

            // Query for name of the newly created content
            var foundContents = Content.QueryForAdminAsync($"Name:'{contentName}'").Result.ToArray();
            Assert.AreEqual(1, foundContents.Length);
            var foundContent = foundContents[0];
            Assert.AreEqual(contentId, foundContent.Id);
            Assert.AreEqual(contentName, foundContent.Name);
        }
        [TestMethod]
        public void UpdateAndQuery()
        {
            var contentName = $"Test_{Guid.NewGuid()}";
            var displayNameBefore = Guid.NewGuid().ToString();
            var displayNameAfter = Guid.NewGuid().ToString();

            // Create and save a new content
            var content = Content.CreateNew("/Root", "SystemFolder", contentName);
            content["DisplayName"] = displayNameBefore;
            content.SaveAsync().GetAwaiter().GetResult();
            var contentId = content.Id;
            Assert.IsTrue(0 < contentId);

            // Query for displayname of the content
            var foundContents = Content.QueryForAdminAsync($"DisplayName:'{displayNameBefore}'").Result.ToArray();
            Assert.AreEqual(1, foundContents.Length);

            // Load back the newly created content
            var updatedContent = foundContents[0];
            updatedContent["DisplayName"] = displayNameAfter;
            updatedContent.SaveAsync().GetAwaiter().GetResult();

            // Query for updated displayname of the content
            var foundUpdatedContents = Content.QueryForAdminAsync($"DisplayName:'{displayNameAfter}'").Result.ToArray();
            Assert.AreEqual(1, foundUpdatedContents.Length);
        }
    }
}
