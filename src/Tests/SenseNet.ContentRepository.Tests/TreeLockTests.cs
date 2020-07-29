using System.Threading;
using STT=System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Storage;
using SenseNet.Tests.Core;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class TreeLockTests : TestBase
    {
        [TestMethod]
        public void TreeLock_AcquireAndScope()
        {
            Test(() =>
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
                }
            });
        }
        [TestMethod]
        public async STT.Task TreeLock_AcquireAndScopeAsync()
        {
            var token = CancellationToken.None;

            await Test(async () => 
            {
                using (await TreeLock.AcquireAsync(CancellationToken.None, "/Root/A/B/C"))
                {
                    var locks = await TreeLock.GetAllLocksAsync(CancellationToken.None);
                    Assert.AreEqual(1, locks.Count);
                    Assert.IsTrue(locks.ContainsValue("/Root/A/B/C"));

                    Assert.IsTrue(await IsLockedAsync("/Root/A/B/C/D", token));
                    Assert.IsTrue(await IsLockedAsync("/Root/A/B/C", token));
                    Assert.IsTrue(await IsLockedAsync("/Root/A/B", token));
                    Assert.IsTrue(await IsLockedAsync("/Root/A", token));
                    Assert.IsTrue(await IsLockedAsync("/Root", token));

                    Assert.IsFalse(await IsLockedAsync("/Root/A/B/X", token));
                    Assert.IsFalse(await IsLockedAsync("/Root/A/X", token));
                    Assert.IsFalse(await IsLockedAsync("/Root/X", token));
                }
            });
        }
        [TestMethod]
        public void TreeLock_Release()
        {
            Test(() =>
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
            });
        }
        [TestMethod]
        public async STT.Task TreeLock_ReleaseAsync()
        {
            var token = CancellationToken.None;

            await Test(async () =>
            {
                var locks = await TreeLock.GetAllLocksAsync(token);
                Assert.AreEqual(0, locks.Count);

                using (await TreeLock.AcquireAsync(token, "/Root/A/B/C1"))
                {
                    using (await TreeLock.AcquireAsync(token, "/Root/A/B/C2"))
                    {
                        locks = await TreeLock.GetAllLocksAsync(token);
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
                    locks = await TreeLock.GetAllLocksAsync(token);
                    Assert.AreEqual(1, locks.Count);
                    Assert.IsTrue(locks.ContainsValue("/Root/A/B/C1"));

                    Assert.IsTrue(await IsLockedAsync("/Root/A/B/C1/D", token));
                    Assert.IsTrue(await IsLockedAsync("/Root/A/B/C1", token));
                    Assert.IsTrue(await IsLockedAsync("/Root/A/B", token));
                    Assert.IsTrue(await IsLockedAsync("/Root/A", token));
                    Assert.IsTrue(await IsLockedAsync("/Root", token));

                    Assert.IsFalse(await IsLockedAsync("/Root/A/B/C2/D", token));
                    Assert.IsFalse(await IsLockedAsync("/Root/A/B/C2", token));
                }
                locks = await TreeLock.GetAllLocksAsync(token);
                Assert.AreEqual(0, locks.Count);

                Assert.IsFalse(await IsLockedAsync("/Root/A/B/C1/D", token));
                Assert.IsFalse(await IsLockedAsync("/Root/A/B/C2/D", token));
                Assert.IsFalse(await IsLockedAsync("/Root/A/B/C1", token));
                Assert.IsFalse(await IsLockedAsync("/Root/A/B/C2", token));
                Assert.IsFalse(await IsLockedAsync("/Root/A/B", token));
                Assert.IsFalse(await IsLockedAsync("/Root/A", token));
                Assert.IsFalse(await IsLockedAsync("/Root", token));
            });
        }

        [TestMethod]
        public void TreeLock_CannotAcquire()
        {
            Test(() =>
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
            });
        }
        [TestMethod]
        public async STT.Task TreeLock_CannotAcquireAsync()
        {
            var token = CancellationToken.None;

            await Test(async () =>
            {
                var locks = await TreeLock.GetAllLocksAsync(token);
                Assert.AreEqual(0, locks.Count);

                using (await TreeLock.AcquireAsync(token,"/Root/A/B/C"))
                {
                    locks = await TreeLock.GetAllLocksAsync(token);
                    Assert.AreEqual(1, locks.Count);
                    Assert.IsTrue(locks.ContainsValue("/Root/A/B/C"));

                    foreach (var path in new[] { "/Root/A/B/C/D", "/Root/A/B/C", "/Root/A/B", "/Root/A", "/Root" })
                    {
                        try
                        {
                            await TreeLock.AcquireAsync(token,"/Root/A/B/C/D");
                            Assert.Fail($"LockedTreeException was not thrown. Path: {path}");
                        }
                        catch (LockedTreeException)
                        {
                        }

                        locks = await TreeLock.GetAllLocksAsync(token);
                        Assert.AreEqual(1, locks.Count);
                        Assert.IsTrue(locks.ContainsValue("/Root/A/B/C"));
                    }
                }
            });
        }

        private static bool IsLocked(string path)
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
        private static async STT.Task<bool> IsLockedAsync(string path, CancellationToken cancellationToken)
        {
            try
            {
                await TreeLock.AssertFreeAsync(cancellationToken, path);
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

            Test(() =>
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
                }
            });
        }
        [TestMethod]
        public async STT.Task TreeLock_TSQL_UnderscoreAsync()
        {
            // This test makes sure that it does not cause a problem if a path
            // contains an underscore. Previously this did not work correctly
            // because the SQL LIKE operator treated it as a special character.

            var token = CancellationToken.None;

            await Test(async () =>
            {
                using (await TreeLock.AcquireAsync(token,"/Root/A/BxB/C"))
                {
                    await TreeLock.AssertFreeAsync(token,"/Root/A/B_B");

                    using (await TreeLock.AcquireAsync(token,"/Root/A/B_B"))
                    {
                        await TreeLock.AssertFreeAsync(token, "/Root/A/B_");
                        await TreeLock.AssertFreeAsync(token, "/Root/A/ByB");
                    }

                    var thrown = false;
                    try
                    {
                        await TreeLock.AssertFreeAsync(token,"/Root/A/BxB");
                    }
                    catch (LockedTreeException)
                    {
                        thrown = true;
                    }

                    Assert.IsTrue(thrown, "#1");
                }
            });
        }
    }
}
