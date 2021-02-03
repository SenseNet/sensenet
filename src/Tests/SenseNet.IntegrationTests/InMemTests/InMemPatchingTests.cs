using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.InMemory;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.IntegrationTests.Common;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.IntegrationTests.Platforms;
using SenseNet.IntegrationTests.TestCases;
using SenseNet.Testing;

namespace SenseNet.IntegrationTests.InMemTests
{
    [TestClass]
    public class InMemPatchingTests : IntegrationTest<InMemPlatform, PatchingTestCases>
    {
        #region InMem specific infrastructure

        private static StringBuilder _log;
        private static InMemoryDataProvider Db => (InMemoryDataProvider)Providers.Instance.DataProvider;

        private void InitializePackagingTest(RepositoryBuilder builder)
        {
            _log = new StringBuilder();
            var loggers = new[] { new PackagingTestLogger(_log) };
            var loggerAcc = new TypeAccessor(typeof(SenseNet.Packaging.Logger));
            loggerAcc.SetStaticField("_loggers", loggers);

            RepositoryVersionInfo.Reset();
        }

        [TestInitialize]
        public void InitializeTest()
        {
            TestCase.TestInitializer = InitializePackagingTest;
        }

        #endregion


        [TestMethod]
        public void IntT_InMem_PatchingSystem_InstalledComponents()
        {
            TestCase.PatchingSystem_InstalledComponents();
        }
        [TestMethod]
        public void IntT_InMem_PatchingSystem_InstalledComponents_Descriptions()
        {
            TestCase.PatchingSystem_InstalledComponents_Descriptions();
        }

        /* ===================================================================== INFRASTRUCTURE TESTS */

        [TestMethod]
        public void IntT_InMem_Patching_System_SaveAndReloadFaultyInstaller()
        {
            TestCase.Patching_System_SaveAndReloadFaultyInstaller();
        }
        [TestMethod]
        public void IntT_InMem_Patching_System_ReSaveAndReloadInstaller()
        {
            TestCase.Patching_System_ReSaveAndReloadInstaller();
        }
        [TestMethod]
        public void IntT_InMem_Patching_System_SaveAndReloadSnPatch()
        {
            TestCase.Patching_System_SaveAndReloadSnPatch();
        }

        [TestMethod]
        public void IntT_InMem_Patching_System_SaveAndReload_Installer_FaultyBefore()
        {
            TestCase.Patching_System_SaveAndReload_Installer_FaultyBefore();
        }
        [TestMethod]
        public void IntT_InMem_Patching_System_SaveAndReload_Installer_SuccessfulBefore()
        {
            TestCase.Patching_System_SaveAndReload_Installer_SuccessfulBefore();
        }
        [TestMethod]
        public void IntT_InMem_Patching_System_SaveAndReload_SnPatch_FaultyBefore()
        {
            TestCase.Patching_System_SaveAndReload_SnPatch_FaultyBefore();
        }
        [TestMethod]
        public void IntT_InMem_Patching_System_SaveAndReload_SnPatch_SuccessfulBefore()
        {
            TestCase.Patching_System_SaveAndReload_SnPatch_SuccessfulBefore();
        }
        [TestMethod]
        public void IntT_InMem_Patching_System_SaveAndReloadExecutionError()
        {
            TestCase.Patching_System_SaveAndReloadExecutionError();
        }

        [TestMethod]
        public void IntT_InMem_Patching_System_InstalledComponents()
        {
            TestCase.Patching_System_InstalledComponents();
        }
        [TestMethod]
        public void IntT_InMem_Patching_System_InstalledComponents_Descriptions()
        {
            TestCase.Patching_System_InstalledComponents_Descriptions();
        }

        [TestMethod]
        public void IntT_InMem_Patching_System_LoadInstalledComponents()
        {
            TestCase.Patching_System_LoadInstalledComponents();
        }

        [TestMethod]
        public void IntT_InMem_Patching_System_Load_Issue1174()
        {
            TestCase.Patching_System_Load_Issue1174();
        }
        [TestMethod]
        public void IntT_InMem_Patching_System_Load_Issue1174_All_Installers()
        {
            TestCase.Patching_System_Load_Issue1174_All_Installers();
        }
        [TestMethod]
        public void IntT_InMem_Patching_System_Load_Issue1174_All_Patches()
        {
            TestCase.Patching_System_Load_Issue1174_All_Patches();
        }


        /* ===================================================================== EXECUTION TESTS */

        [TestMethod]
        public void IntT_InMem_Patching_Exec_NoAction()
        {
            TestCase.Patching_Exec_NoAction();
        }
        [TestMethod]
        public void IntT_InMem_Patching_Exec_NoAfterAction()
        {
            TestCase.Patching_Exec_NoAfterAction();
        }

        [TestMethod]
        public void IntT_InMem_Patching_Exec_InstallOne_Success()
        {
            TestCase.Patching_Exec_InstallOne_Success();
        }
        [TestMethod]
        public void IntT_InMem_Patching_Exec_InstallOne_FaultyBefore()
        {
            TestCase.Patching_Exec_InstallOne_FaultyBefore();
        }
        [TestMethod]
        public void IntT_InMem_Patching_Exec_PatchOne_Success()
        {
            TestCase.Patching_Exec_PatchOne_Success();
        }
        [TestMethod]
        public void IntT_InMem_Patching_Exec_PatchOne_Faulty()
        {
            TestCase.Patching_Exec_PatchOne_Faulty();
        }
        [TestMethod]
        public void IntT_InMem_Patching_Exec_SkipPatch_FaultyInstaller()
        {
            TestCase.Patching_Exec_SkipPatch_FaultyInstaller();
        }
        [TestMethod]
        public void IntT_InMem_Patching_Exec_SkipPatch_FaultySnPatch()
        {
            TestCase.Patching_Exec_SkipPatch_FaultySnPatch();
        }

        [TestMethod]
        public void IntT_InMem_Patching_Exec_SkipPatch_MoreFaultyChains()
        {
            TestCase.Patching_Exec_SkipPatch_MoreFaultyChains();
        }

        [TestMethod]
        public void IntT_InMem_Patching_Exec_WaitForDependency_WaitingBeforeAndAfter()
        {
            TestCase.Patching_Exec_WaitForDependency_WaitingBeforeAndAfter();
        }
        [TestMethod]
        public void IntT_InMem_Patching_Exec_InstallerIsLast()
        {
            TestCase.Patching_Exec_InstallerIsLast();
        }

        /* ===================================================================== EXECUTION VS VERSIONINFO TESTS */

        [TestMethod]
        public void IntT_InMem_Patching_Exec_ComponentLifeCycleVsVersionInfo()
        {
            TestCase.Patching_Exec_ComponentLifeCycleVsVersionInfo();
        }

        /* ======================================================================= CONDITIONAL EXECUTION TESTS */

        // Patch vary component versions conditionally
        [TestMethod]
        public void IntT_InMem_Patching_Exec_ConditionalActions_a()
        {
            TestCase.Patching_Exec_ConditionalActions_a();
        }
        [TestMethod]
        public void IntT_InMem_Patching_Exec_ConditionalActions_b()
        {
            TestCase.Patching_Exec_ConditionalActions_b();
        }
        [TestMethod]
        public void IntT_InMem_Patching_Exec_ConditionalActions_c()
        {
            TestCase.Patching_Exec_ConditionalActions_c();
        }

        [TestMethod]
        public void IntT_InMem_Patching_Exec_ConditionalActions_AllConditions_a()
        {
            TestCase.Patching_Exec_ConditionalActions_AllConditions_a();
        }
        [TestMethod]
        public void IntT_InMem_Patching_Exec_ConditionalActions_AllConditions_b()
        {
            TestCase.Patching_Exec_ConditionalActions_AllConditions_b();
        }
        [TestMethod]
        public void IntT_InMem_Patching_Exec_ConditionalActions_AllConditions_c()
        {
            TestCase.Patching_Exec_ConditionalActions_AllConditions_c();
        }
        [TestMethod]
        public void IntT_InMem_Patching_Exec_ConditionalActions_AllConditions_d()
        {
            TestCase.Patching_Exec_ConditionalActions_AllConditions_d();
        }
        [TestMethod]
        public void IntT_InMem_Patching_Exec_ConditionalActions_AllConditions_e()
        {
            TestCase.Patching_Exec_ConditionalActions_AllConditions_e();
        }
    }
}
