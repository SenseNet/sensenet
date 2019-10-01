using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.ODataTests
{
    [TestClass]
    public class ODataModificationTests : ODataTestBase
    {
        [TestMethod]
        public async Task OD_PUT_Rename()
        {
            await IsolatedODataTestAsync(async () =>
            {
                // ALIGN
                InstallCarContentType();
                var testRoot = CreateTestRoot("ODataTestRoot");

                var content = Content.CreateNew("Car", testRoot, "ORIG");
                content.DisplayName = "Initial DisplayName";
                content.Index = 42;
                content.Save();
                var id = content.Id;
                var path = content.Path;

                var newName = "NEW";
                var newDisplayName = "New DisplayName";

                var requestBody= String.Concat(@"models=[{
                          ""Name"": """, newName, @""",
                          ""DisplayName"": """, newDisplayName, @"""
                        }]");

                // ACTION
                var response = await ODataPutAsync("/OData.svc" + content.Path, "", requestBody);

                // ASSERT
                AssertNoError(response);
                var content1 = Content.Load(id);
                // Posted value
                Assert.AreEqual(newName, content1.Name);
                Assert.AreEqual(newDisplayName, content1.DisplayName);
                // Default value because of PUT
                Assert.AreEqual(0, content1.Index);
            });
        }

        [TestMethod]
        public async Task OD_PATCH_Rename()
        {
            await IsolatedODataTestAsync(async () =>
            {
                // ALIGN
                InstallCarContentType();
                var testRoot = CreateTestRoot("ODataTestRoot");

                var content = Content.CreateNew("Car", testRoot, "ORIG");
                content.DisplayName = "Initial DisplayName";
                content.Index = 42;
                content.Save();
                var id = content.Id;
                var path = content.Path;

                var newName = "NEW";
                var newDisplayName = "New DisplayName";

                var requestBody = String.Concat(@"models=[{
                          ""Name"": """, newName, @""",
                          ""DisplayName"": """, newDisplayName, @"""
                        }]");

                // ACTION
                var response = await ODataPatchAsync("/OData.svc" + content.Path, "", requestBody);

                // ASSERT
                AssertNoError(response);
                var content1 = Content.Load(id);
                Assert.AreEqual(newName, content1.Name);
                Assert.AreEqual(newDisplayName, content1.DisplayName);
                Assert.AreEqual(42, content1.Index);
            });
        }
    }
}
