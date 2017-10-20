using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Tests;

namespace SenseNet.SearchImpl.Tests.DataProviderTests
{
    [TestClass]
    public class TreeLockTests : TestBase
    {
        [TestMethod]
        public void TreeLock_AcquieAndScope()
        {
            Test<object>(() =>
            {
                using (TreeLock.Acquire("/Root/A/B/C"))
                {
                    var locks = TreeLock.GetAllLocks();
                    Assert.AreEqual(1, locks.Count);
                    Assert.IsTrue(locks.ContainsValue("/Root/A/B/C"));

                    Assert.IsTrue(IsLocked("/Root/A/B/C/D"));
                    Assert.IsTrue(IsLocked("/Root/A/B/C"));
                    Assert.IsTrue(IsLocked("/Root/A/B"));
                    Assert.IsTrue(IsLocked("/Root/A"));
                    Assert.IsTrue(IsLocked("/Root"));

                    Assert.IsFalse(IsLocked("/Root/A/B/X"));
                    Assert.IsFalse(IsLocked("/Root/A/X"));
                    Assert.IsFalse(IsLocked("/Root/X"));

                    return null;
                }
            });
        }
        [TestMethod]
        public void TreeLock_Release()
        {
            Test<object>(() =>
            {
                var locks = TreeLock.GetAllLocks();
                Assert.AreEqual(0, locks.Count);

                using (TreeLock.Acquire("/Root/A/B/C1"))
                {
                    using (TreeLock.Acquire("/Root/A/B/C2"))
                    {
                        locks = TreeLock.GetAllLocks();
                        Assert.AreEqual(2, locks.Count);
                        Assert.IsTrue(locks.ContainsValue("/Root/A/B/C1"));
                        Assert.IsTrue(locks.ContainsValue("/Root/A/B/C2"));

                        Assert.IsTrue(IsLocked("/Root/A/B/C1/D"));
                        Assert.IsTrue(IsLocked("/Root/A/B/C2/D"));
                        Assert.IsTrue(IsLocked("/Root/A/B/C1"));
                        Assert.IsTrue(IsLocked("/Root/A/B/C2"));
                        Assert.IsTrue(IsLocked("/Root/A/B"));
                        Assert.IsTrue(IsLocked("/Root/A"));
                        Assert.IsTrue(IsLocked("/Root"));
                    }
                    locks = TreeLock.GetAllLocks();
                    Assert.AreEqual(1, locks.Count);
                    Assert.IsTrue(locks.ContainsValue("/Root/A/B/C1"));

                    Assert.IsTrue(IsLocked("/Root/A/B/C1/D"));
                    Assert.IsTrue(IsLocked("/Root/A/B/C1"));
                    Assert.IsTrue(IsLocked("/Root/A/B"));
                    Assert.IsTrue(IsLocked("/Root/A"));
                    Assert.IsTrue(IsLocked("/Root"));

                    Assert.IsFalse(IsLocked("/Root/A/B/C2/D"));
                    Assert.IsFalse(IsLocked("/Root/A/B/C2"));
                }
                locks = TreeLock.GetAllLocks();
                Assert.AreEqual(0, locks.Count);

                Assert.IsFalse(IsLocked("/Root/A/B/C1/D"));
                Assert.IsFalse(IsLocked("/Root/A/B/C2/D"));
                Assert.IsFalse(IsLocked("/Root/A/B/C1"));
                Assert.IsFalse(IsLocked("/Root/A/B/C2"));
                Assert.IsFalse(IsLocked("/Root/A/B"));
                Assert.IsFalse(IsLocked("/Root/A"));
                Assert.IsFalse(IsLocked("/Root"));

                return null;
            });
        }

        [TestMethod]
        public void TreeLock_CannotAcquire()
        {
            Test<object>(() =>
            {
                var locks = TreeLock.GetAllLocks();
                Assert.AreEqual(0, locks.Count);

                using (TreeLock.Acquire("/Root/A/B/C"))
                {
                    locks = TreeLock.GetAllLocks();
                    Assert.AreEqual(1, locks.Count);
                    Assert.IsTrue(locks.ContainsValue("/Root/A/B/C"));

                    foreach (var path in new[] {"/Root/A/B/C/D", "/Root/A/B/C", "/Root/A/B", "/Root/A", "/Root"})
                    {
                        try
                        {
                            TreeLock.Acquire("/Root/A/B/C/D");
                            Assert.Fail($"LockedTreeException was not thrown. Path: {path}");
                        }
                        catch (LockedTreeException)
                        {
                        }

                        locks = TreeLock.GetAllLocks();
                        Assert.AreEqual(1, locks.Count);
                        Assert.IsTrue(locks.ContainsValue("/Root/A/B/C"));
                    }
                }

                return null;
            });
        }

        private bool IsLocked(string path)
        {
            try
            {
                TreeLock.AssertFree(path);
                return false;
            }
            catch (LockedTreeException)
            {
                return true;
            }
        }

        [TestMethod]
        public void TreeLock_TSQL_Underscore()
        {
            // This test makes sure that it does not cause a problem if a path
            // contains an underscore. Previously this did not work correctly
            // because the SQL LIKE operator treated it as a special character.

            Test<object>(() =>
            {
                using (TreeLock.Acquire("/Root/A/BxB/C"))
                {
                    TreeLock.AssertFree("/Root/A/B_B");

                    using (TreeLock.Acquire("/Root/A/B_B"))
                    {
                        TreeLock.AssertFree("/Root/A/B_");
                        TreeLock.AssertFree("/Root/A/ByB");
                    }

                    var thrown = false;
                    try
                    {
                        TreeLock.AssertFree("/Root/A/BxB");
                    }
                    catch (LockedTreeException)
                    {
                        thrown = true;
                    }

                    Assert.IsTrue(thrown, "#1");

                    return null;
                }
            });
        }
    }
}
