using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Tests.Core;
using STTTask=System.Threading.Tasks.Task;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class MultiFactorTests : TestBase
    {
        [TestMethod, TestCategory("MultiFactor")]
        public STTTask MultiFactor_DefaultUserProperties()
        {
            return Test(() =>
            {
                var user = User.Administrator;

                Assert.IsFalse(user.MultiFactorEnabled);
                Assert.IsFalse(user.MultiFactorRegistered);
                Assert.IsFalse(user.EffectiveMultiFactorEnabled); // enabled globally, but disabled on the user

                var key = user.TwoFactorKey;

                // check if the key was properly saved
                AccessTokenVault.AssertTokenExists(key, user.Id, "2fa");

                // check generated values
                Assert.IsTrue(string.IsNullOrEmpty(user.QrCodeSetupImageUrl));
                Assert.IsTrue(string.IsNullOrEmpty(user.ManualEntryKey));
                
                return STTTask.CompletedTask;
            });
        }
        [TestMethod, TestCategory("MultiFactor")]
        public STTTask MultiFactor_SwitchOnOff()
        {
            return Test(async () =>
            {
                var user = User.Administrator;
                user.MultiFactorEnabled = true;
                await user.SaveAsync(SavingMode.KeepVersion, CancellationToken.None).ConfigureAwait(false);

                Assert.IsTrue(user.MultiFactorEnabled);
                Assert.IsFalse(user.MultiFactorRegistered);
                Assert.IsTrue(user.EffectiveMultiFactorEnabled);
                
                // check generated values
                Assert.IsFalse(string.IsNullOrEmpty(user.QrCodeSetupImageUrl));
                Assert.IsFalse(string.IsNullOrEmpty(user.ManualEntryKey));

                var key1 = user.TwoFactorKey;

                user.MultiFactorEnabled = false;
                await user.SaveAsync(SavingMode.KeepVersion, CancellationToken.None).ConfigureAwait(false);

                Assert.IsFalse(user.MultiFactorEnabled);
                Assert.IsFalse(user.MultiFactorRegistered);
                Assert.IsFalse(user.EffectiveMultiFactorEnabled);

                var key2 = user.TwoFactorKey;

                // check if the key was properly saved
                AccessTokenVault.AssertTokenExists(key2, user.Id, "2fa");

                Assert.AreNotEqual(key1, key2);

                // check generated values
                Assert.IsTrue(string.IsNullOrEmpty(user.QrCodeSetupImageUrl));
                Assert.IsTrue(string.IsNullOrEmpty(user.ManualEntryKey));
            });
        }
    }
}
