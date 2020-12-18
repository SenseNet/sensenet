﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.IntegrationTests.TestCases
{
    public class TestCase2 : TestCase
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
        public void TestCase_2_2()
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
        public void TestCase_2_6()
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
