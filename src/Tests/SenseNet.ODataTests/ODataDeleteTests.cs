﻿using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.OData;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.ODataTests
{
    [TestClass]
    public class ODataDeleteTests : ODataTestBase
    {
        [TestMethod]
        public async Task OD_DELETE_Entity()
        {
            await ODataTestAsync(async () =>
            {
                // ARRANGE
                InstallCarContentType();
                var testRoot = CreateTestRoot("ODataTestRoot");

                var name = "Content1";
                var content = Content.CreateNew("Car", testRoot, name);
                content.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
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
            await ODataTestAsync(async () =>
            {
                // ARRANGE
                InstallCarContentType();
                var testRoot = CreateTestRoot("ODataTestRoot");

                var name = "Content1";
                var content = Content.CreateNew("Car", testRoot, name);
                content.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
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
            await ODataTestAsync(async () =>
            {
                // ARRANGE
                InstallCarContentType();
                var testRoot = CreateTestRoot("ODataTestRoot");

                var name = "Content1";
                var content = Content.CreateNew("Car", testRoot, name);
                content.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
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
            await ODataTestAsync(async () =>
            {
                // ARRANGE
                InstallCarContentType();
                var testRoot = CreateTestRoot("ODataTestRoot");

                var name = "Content1";
                var content = Content.CreateNew("Car", testRoot, name);
                content.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
                var repoPath = $"{testRoot.Path}/{name}";
                var resource = $"/OData.svc/content({content.Id})";

                // ACTION
                var response = await ODataDeleteAsync(resource, "?permanent=true").ConfigureAwait(false); ;

                // ASSERT
                AssertNoError(response);
                Assert.IsFalse(Node.Exists(repoPath));
            });
        }

        [TestMethod, TestCategory("Services")]
        public async Task OD_DELETE_ByAction_DefaultParam_CSrv()
        {
            await ODataTestAsync(async () =>
            {
                await DeleteByAction(string.Empty, false);
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_DELETE_ByAction_Trash()
        {
            await ODataTestAsync(async () =>
            {
                await DeleteByAction("{ 'permanent': false }", false);
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_DELETE_ByAction_Permanent()
        {
            await ODataTestAsync(async () =>
            {
                await DeleteByAction("{ 'permanent': true }", true);
            }).ConfigureAwait(false);
        }
        private async Task DeleteByAction(string body, bool permanent)
        {
            // workaround: remove the old Delete application that is 
            // not compatible with the .net core method
            var deleteAppPath = "/Root/(apps)/GenericContent/Delete";
            if (Node.Exists(deleteAppPath))
                Node.ForceDeleteAsync(deleteAppPath, CancellationToken.None).GetAwaiter().GetResult();

            // ARRANGE
            var testRoot = CreateTestRoot("ODataTestRoot");
            var name = "Content1";
            var content = Content.CreateNew("File", testRoot, name);
            content.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
            var repoPath = $"{testRoot.Path}/{name}";
            var resource = $"/OData.svc/{testRoot.Path}('{name}')/Delete";

            // ACTION
            var response = await ODataPostAsync(resource, "", body).ConfigureAwait(false);

            // ASSERT
            AssertNoError(response);
            Assert.IsFalse(Node.Exists(repoPath));
            
            // reload
            content = Content.Load(content.Id);

            if (permanent)
                Assert.IsNull(content);
            else
                Assert.IsTrue(TrashBin.IsInTrash(content.ContentHandler as GenericContent));
        }
    }
}
