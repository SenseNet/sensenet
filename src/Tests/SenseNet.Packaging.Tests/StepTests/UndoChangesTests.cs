using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.Packaging.Steps.Internal;
using SenseNet.Packaging.Tests.Implementations;
using SenseNet.Search;
using SenseNet.Tests;

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
            var loggerAcc = new PrivateType(typeof(Logger));
            loggerAcc.SetStaticField("_loggers", loggers);
        }

        [TestCleanup]
        public void AfterTest()
        {
        }

        [TestMethod]
        public void Step_UndoChanges_All()
        {
            Test(() =>
            {
                var parent = new SystemFolder(Repository.Root) { Name = Guid.NewGuid().ToString() };

                try
                {
                    parent.Save();

                    var file = new File(parent);
                    file.Save();
                    file.CheckOut();
                    
                    Assert.IsTrue(GetLockedCount() > 0);
                    Assert.AreEqual(VersionStatus.Locked, file.Version.Status);

                    // undo all changes in the repo
                    UndoChanges.UndoContentChanges();

                    file = Node.Load<File>(file.Id);

                    Assert.IsTrue(GetLockedCount() == 0);
                    Assert.AreNotEqual(VersionStatus.Locked, file.Version.Status);
                }
                finally 
                {
                    parent.ForceDelete();
                }
            });
        }

        private static int GetLockedCount()
        {
            return ContentQuery.Query(SafeQueries.LockedContent, QuerySettings.AdminSettings).Count;
        }
    }
}
