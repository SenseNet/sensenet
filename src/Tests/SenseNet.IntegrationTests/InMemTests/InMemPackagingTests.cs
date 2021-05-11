using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.InMemory;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.IntegrationTests.Platforms;
using SenseNet.IntegrationTests.TestCases;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.IntegrationTests.InMemTests
{
    [TestClass]
    public class InMemPackagingTests :  IntegrationTest<InMemPlatform, PackagingTestCases>
    {
        [TestMethod]
        public void IntT_InMem_Packaging_DependencyCheck_MissingDependency() { TestCase.Packaging_DependencyCheck_MissingDependency(); }
        [TestMethod]
        public void IntT_InMem_Packaging_DependencyCheck_CannotInstallExistingComponent() { TestCase.Packaging_DependencyCheck_CannotInstallExistingComponent(); }
        [TestMethod]
        public void IntT_InMem_Packaging_DependencyCheck_CannotUpdateMissingComponent() { TestCase.Packaging_DependencyCheck_CannotUpdateMissingComponent(); }
        [TestMethod]
        public void IntT_InMem_Packaging_DependencyCheck_TargetVersionTooSmall() { TestCase.Packaging_DependencyCheck_TargetVersionTooSmall(); }

        [TestMethod]
        public void IntT_InMem_Packaging_DependencyCheck_DependencyVersion() { TestCase.Packaging_DependencyCheck_DependencyVersion(); }
        [TestMethod]
        public void IntT_InMem_Packaging_DependencyCheck_DependencyMinimumVersion() { TestCase.Packaging_DependencyCheck_DependencyMinimumVersion(); }
        [TestMethod]
        public void IntT_InMem_Packaging_DependencyCheck_DependencyMaximumVersion() { TestCase.Packaging_DependencyCheck_DependencyMaximumVersion(); }
        [TestMethod]
        public void IntT_InMem_Packaging_DependencyCheck_DependencyMinimumVersionExclusive() { TestCase.Packaging_DependencyCheck_DependencyMinimumVersionExclusive(); }
        [TestMethod]
        public void IntT_InMem_Packaging_DependencyCheck_DependencyMaximumVersionExclusive() { TestCase.Packaging_DependencyCheck_DependencyMaximumVersionExclusive(); }

        [TestMethod]
        public void IntT_InMem_Packaging_DependencyCheck_LoggingDependencies() { TestCase.Packaging_DependencyCheck_LoggingDependencies(); }

        /* ==================================================================================== Component lifetime tests */

        [TestMethod]
        public void IntT_InMem_Packaging_Install_NoSteps() { TestCase.Packaging_Install_NoSteps(); }
        [TestMethod]
        public void IntT_InMem_Packaging_Install_ThreePhases() { TestCase.Packaging_Install_ThreePhases(); }

        [TestMethod]
        public void IntT_InMem_Packaging_Patch_ThreePhases() { TestCase.Packaging_Patch_ThreePhases(); }

        [TestMethod]
        public void IntT_InMem_Packaging_Patch_Faulty() { TestCase.Packaging_Patch_Faulty(); }
        [TestMethod]
        public void IntT_InMem_Packaging_Patch_FixFaulty() { TestCase.Packaging_Patch_FixFaulty(); }
        [TestMethod]
        public void IntT_InMem_Packaging_Patch_FixMoreFaulty() { TestCase.Packaging_Patch_FixMoreFaulty(); }

        /* ==================================================================================== RepositoryVersionInfo queries */

        [TestMethod]
        public void IntT_InMem_Packaging_VersionInfo_Empty() { TestCase.Packaging_VersionInfo_Empty(); }
        [TestMethod]
        public async Task IntT_InMem_Packaging_VersionInfo_OnlyUnfinished() { await TestCase.Packaging_VersionInfo_OnlyUnfinished().ConfigureAwait(false); }
        [TestMethod]
        public async Task IntT_InMem_Packaging_VersionInfo_OnlyFaulty() { await TestCase.Packaging_VersionInfo_OnlyFaulty().ConfigureAwait(false); }
        [TestMethod]
        public async Task IntT_InMem_Packaging_VersionInfo_Complex() { await TestCase.Packaging_VersionInfo_Complex().ConfigureAwait(false); }

        [TestMethod]
        public async Task IntT_InMem_Packaging_VersionInfo_MultipleInstall() { await TestCase.Packaging_VersionInfo_MultipleInstall().ConfigureAwait(false); }

        /* ==================================================================================== Storing manifest */

        [TestMethod]
        public async Task IntT_InMem_Packaging_Manifest_StoredButNotLoaded() { await TestCase.Packaging_Manifest_StoredButNotLoaded().ConfigureAwait(false); }

        /* ==================================================================================== Package deletion */

        [TestMethod]
        public async Task IntT_InMem_Packaging_DeleteOne() { await TestCase.Packaging_DeleteOne().ConfigureAwait(false); }
        [TestMethod]
        public async Task IntT_InMem_Packaging_DeleteAll() { await TestCase.Packaging_DeleteAll().ConfigureAwait(false); }
    }
}
