using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.IntegrationTests.TestCases
{
    public class TestCase1 : TestCase
    {
        public void TestCase_1_1()
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
        public void TestCase_1_2()
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
        public void TestCase_1_3()
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
        public void TestCase_1_4()
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
        public void TestCase_1_5()
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
        public void TestCase_1_6()
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
        public void TestCase_1_7()
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
        public void TestCase_1_8()
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
