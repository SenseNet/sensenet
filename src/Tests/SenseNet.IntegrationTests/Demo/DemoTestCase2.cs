using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.IntegrationTests.Infrastructure;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.IntegrationTests.Demo
{
    public class DemoTestCase2 : TestCaseBase
    {
        public void TestCase_2_1()
        {
            IntegrationTest((sandbox) =>
            {
                // ASSIGN
                var created = new SystemFolder(sandbox);
                created.Save();

                // ACTION
                var edited = Node.LoadNode(created.Id);
                edited.Index = 42;
                edited.Save();

                // ASSERT
                var loaded = Node.LoadNode(created.Id);
                Assert.AreEqual(42, loaded.Index);
            });
        }
        public void TestCase_2_2u()
        {
            IntegrationTest((sandbox) =>
            {
                using (CurrentUserBlock(User.Administrator))
                {
                    // ASSIGN
                    var created = new SystemFolder(sandbox);
                    created.Save();

                    // ACTION
                    var edited = Node.LoadNode(created.Id);
                    edited.Index = 42;
                    edited.Save();

                    // ASSERT
                    var loaded = Node.LoadNode(created.Id);
                    Assert.AreEqual(42, loaded.Index);
                }
            });
        }
        public void TestCase_2_3()
        {
            IntegrationTest((sandbox) =>
            {
                // ASSIGN
                var created = new SystemFolder(sandbox);
                created.Save();

                // ACTION
                var edited = Node.LoadNode(created.Id);
                edited.Index = 42;
                edited.Save();

                // ASSERT
                var loaded = Node.LoadNode(created.Id);
                Assert.AreEqual(42, loaded.Index);
            });
        }
        public void TestCase_2_4i()
        {
            IsolatedIntegrationTest(() =>
            {
                // ASSIGN
                var created = new SystemFolder(Repository.Root);
                created.Save();

                // ACTION
                var edited = Node.LoadNode(created.Id);
                edited.Index = 42;
                edited.Save();

                // ASSERT
                var loaded = Node.LoadNode(created.Id);
                Assert.AreEqual(42, loaded.Index);
            });
        }
        public void TestCase_2_5()
        {
            IntegrationTest((sandbox) =>
            {
                // ASSIGN
                var created = new SystemFolder(sandbox);
                created.Save();

                // ACTION
                var edited = Node.LoadNode(created.Id);
                edited.Index = 42;
                edited.Save();

                // ASSERT
                var loaded = Node.LoadNode(created.Id);
                Assert.AreEqual(42, loaded.Index);
            });
        }
        public async Task TestCase_2_6a()
        {
            await IntegrationTestAsync(async (sandbox) =>
            {
                // ASSIGN
                var created = new SystemFolder(sandbox);
                created.Save();

                // ACTION
                var edited = await Node.LoadNodeAsync(created.Id, CancellationToken.None);
                edited.Index = 42;
                edited.Save();

                // ASSERT
                var loaded = await Node.LoadNodeAsync(created.Id, CancellationToken.None);
                Assert.AreEqual(42, loaded.Index);
            }).ConfigureAwait(false);
        }
        public void TestCase_2_7()
        {
            IntegrationTest((sandbox) =>
            {
                // ASSIGN
                var created = new SystemFolder(sandbox);
                created.Save();

                // ACTION
                var edited = Node.LoadNode(created.Id);
                edited.Index = 42;
                edited.Save();

                // ASSERT
                var loaded = Node.LoadNode(created.Id);
                Assert.AreEqual(42, loaded.Index);
            });
        }
        public void TestCase_2_8()
        {
            IntegrationTest((sandbox) =>
            {
                // ASSIGN
                var created = new SystemFolder(sandbox);
                created.Save();

                // ACTION
                var edited = Node.LoadNode(created.Id);
                edited.Index = 42;
                edited.Save();

                // ASSERT
                var loaded = Node.LoadNode(created.Id);
                Assert.AreEqual(42, loaded.Index);
            });
        }
    }
}
