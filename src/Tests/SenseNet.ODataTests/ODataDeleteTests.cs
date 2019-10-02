using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.OData;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.ODataTests
{
    //[TestClass]
    public class ODataDeleteTests : ODataTestBase
    {
        [TestMethod]
        public async Task OD_DELETE_Entity()
        {
            await IsolatedODataTestAsync(async () =>
            {
                // ARRANGE
                InstallCarContentType();
                var testRoot = CreateTestRoot("ODataTestRoot");

                var name = "Content1";
                var content = Content.CreateNew("Car", testRoot, name);
                content.Save();
                var repoPath = $"{testRoot.Path}/{name}";
                var resource = $"/OData.svc/{testRoot.Path}('{name}')";

                // ACTION
                var response = await ODataDeleteAsync(resource, "").ConfigureAwait(false); ;

                // ASSERT
                AssertNoError(response);
                Assert.IsFalse(Node.Exists(repoPath));
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_DELETE_Entity_Permanent()
        {
            await IsolatedODataTestAsync(async () =>
            {
                // ARRANGE
                InstallCarContentType();
                var testRoot = CreateTestRoot("ODataTestRoot");

                var name = "Content1";
                var content = Content.CreateNew("Car", testRoot, name);
                content.Save();
                var repoPath = $"{testRoot.Path}/{name}";
                var resource = $"/OData.svc/{testRoot.Path}('{name}')";

                // ACTION
                var response = await ODataDeleteAsync(resource, "?permanent=true").ConfigureAwait(false); ;

                // ASSERT
                AssertNoError(response);
                Assert.IsFalse(Node.Exists(repoPath));
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_DELETE_Collection()
        {
            await IsolatedODataTestAsync(async () =>
            {
                // ARRANGE
                InstallCarContentType();
                var testRoot = CreateTestRoot("ODataTestRoot");

                var name = "Content1";
                var content = Content.CreateNew("Car", testRoot, name);
                content.Save();
                var repoPath = $"{testRoot.Path}/{name}";
                var resource = $"/OData.svc/{repoPath}')";

                // ACTION
                var response = await ODataDeleteAsync(resource, "").ConfigureAwait(false); ;

                // ASSERT
                var error = GetError(response);
                Assert.AreEqual(ODataExceptionCode.RequestError, error.Code);
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_DELETE_Missing()
        {
            await ODataTestAsync(async () =>
            {
                // ACTION
                var response = await ODataDeleteAsync(
                        "/OData.svc/Root('Anything')'", "")
                    .ConfigureAwait(false); ;

                // ASSERT
                Assert.AreEqual(200, response.StatusCode);
            }).ConfigureAwait(false); ;
        }
        [TestMethod]
        public async Task OD_DELETE_IllegalInvoke()
        {
            await ODataTestAsync(async () =>
            {
                // ACTION
                var response = await ODataDeleteAsync(
                        "/OData.svc/Root('Anything')/Id'", "")
                    .ConfigureAwait(false); ;

                // ASSERT
                var error = GetError(response);
                Assert.AreEqual(ODataExceptionCode.IllegalInvoke, error.Code);
            }).ConfigureAwait(false); ;
        }

        [TestMethod]
        public async Task OD_DELETE_ById()
        {
            await IsolatedODataTestAsync(async () =>
            {
                // ARRANGE
                InstallCarContentType();
                var testRoot = CreateTestRoot("ODataTestRoot");

                var name = "Content1";
                var content = Content.CreateNew("Car", testRoot, name);
                content.Save();
                var repoPath = $"{testRoot.Path}/{name}";
                var resource = $"/OData.svc/content({content.Id})";

                // ACTION
                var response = await ODataDeleteAsync(resource, "?permanent=true").ConfigureAwait(false); ;

                // ASSERT
                AssertNoError(response);
                Assert.IsFalse(Node.Exists(repoPath));
            });
        }
    }
}
