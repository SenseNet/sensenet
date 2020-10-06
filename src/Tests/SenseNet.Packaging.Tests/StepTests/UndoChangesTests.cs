using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.Packaging.Steps.Internal;
using SenseNet.Packaging.Tests.Implementations;
using SenseNet.Testing;
using SenseNet.Tests.Core;

namespace SenseNet.Packaging.Tests.StepTests
{
    [TestClass]
    public class UndoChangesTests : TestBase
    {
        private static StringBuilder _log;

        [TestInitialize]
        public void PrepareTest()
        {
            // preparing logger
            _log = new StringBuilder();
            var loggers = new[] {new PackagingTestLogger(_log)};
            var loggerAcc = new TypeAccessor(typeof(Logger));
            loggerAcc.SetStaticField("_loggers", loggers);
        }
        [TestCleanup]
        public void AfterTest()
        {
        }

        [TestMethod]
        public void Step_UndoChanges_All() => Test(() =>
        {
            var parent = CretateFolder(Repository.Root, true);
            var file = CreateFileAndCheckout(parent);

            Assert.IsTrue(GetLockedCount() > 0);
            Assert.AreEqual(VersionStatus.Locked, file.Version.Status);

            // undo all changes in the repo
            UndoChanges.UndoContentChanges(null);

            file = Node.Load<File>(file.Id);

            Assert.AreEqual(0, GetLockedCount());
            Assert.AreNotEqual(VersionStatus.Locked, file.Version.Status);
        });
        [TestMethod]
        public void Step_UndoChanges_ByPath() => Test(() =>
        {
            var parent = CretateFolder(Repository.Root);

            var file1 = CreateFileAndCheckout(parent);
            var file2 = CreateFileAndCheckout(parent);

            Assert.AreEqual(VersionStatus.Locked, file1.Version.Status);
            Assert.AreEqual(VersionStatus.Locked, file2.Version.Status);

            // undo all changes under a path
            UndoChanges.UndoContentChanges(file1.Path);

            file1 = Node.Load<File>(file1.Id);
            file2 = Node.Load<File>(file2.Id);

            Assert.AreNotEqual(VersionStatus.Locked, file1.Version.Status);
            Assert.AreEqual(VersionStatus.Locked, file2.Version.Status);
        });
        [TestMethod]
        public void Step_UndoChanges_ByType() => Test(() =>
        {
            var parent = CretateFolder(Repository.Root, true);
            var file1 = CreateFileAndCheckout(parent);

            parent = Node.Load<SystemFolder>(parent.Id);

            Assert.AreEqual(VersionStatus.Locked, parent.Version.Status);
            Assert.AreEqual(VersionStatus.Locked, file1.Version.Status);

            // undo all changes by type
            UndoChanges.UndoContentChanges(null, "File");

            file1 = Node.Load<File>(file1.Id);
            parent = Node.Load<SystemFolder>(parent.Id);

            Assert.AreEqual(VersionStatus.Locked, parent.Version.Status);
            Assert.AreNotEqual(VersionStatus.Locked, file1.Version.Status);
        });
        [TestMethod]
        public void Step_UndoChanges_ByTypeAndPath() => Test(() =>
        {
            var parent1 = CretateFolder(Repository.Root, true);
            var file1 = CreateFileAndCheckout(parent1);

            var parent2 = CretateFolder(parent1, true);
            var file2 = CreateFileAndCheckout(parent2);

            Assert.AreEqual(VersionStatus.Locked, parent1.Version.Status);
            Assert.AreEqual(VersionStatus.Locked, parent2.Version.Status);
            Assert.AreEqual(VersionStatus.Locked, file1.Version.Status);
            Assert.AreEqual(VersionStatus.Locked, file2.Version.Status);

            // undo changes under a path by type
            UndoChanges.UndoContentChanges(parent2.Path, "File", "SystemFolder");

            parent1 = Node.Load<SystemFolder>(parent1.Id);
            parent2 = Node.Load<SystemFolder>(parent2.Id);
            file1 = Node.Load<File>(file1.Id);
            file2 = Node.Load<File>(file2.Id);

            Assert.AreEqual(VersionStatus.Locked, parent1.Version.Status);
            Assert.AreEqual(VersionStatus.Locked, file1.Version.Status);
            Assert.AreNotEqual(VersionStatus.Locked, parent2.Version.Status);            
            Assert.AreNotEqual(VersionStatus.Locked, file2.Version.Status);
        });

        private static int GetLockedCount()
        {
            return CreateSafeContentQuery("+Locked:true").Execute().Count;
        }
        private static File CreateFileAndCheckout(Node parent)
        {
            var file = new File(parent) { Name = Guid.NewGuid().ToString() };
            file.Save();

            //TODO: workaround for pinned node.Content issue
            file = Node.Load<File>(file.Id);
            file.CheckOut();

            return file;
        }
        private static SystemFolder CretateFolder(Node parent, bool checkout = false)
        {
            var folder = new SystemFolder(parent) { Name = Guid.NewGuid().ToString() };
            folder.Save();

            if (checkout)
            {
                folder = Node.Load<SystemFolder>(folder.Id);
                folder.CheckOut();
            }

            return folder;
        }
    }
}
