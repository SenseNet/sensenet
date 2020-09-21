using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.Packaging.Tests
{
    [TestClass]
    public class PackagingEzReleaseTests : PackagingTestBase
    {
        [TestMethod]
        public void Packaging_EzRelease_CreateComponentInfo_SupportedVersionNull()
        {
            var componentName = "TestComponent-1";
            var component = new TestComponent(componentName, null);
            var componentInfo = SnComponentInfo.Create(component);
            var thisVersion = TypeHandler.GetVersion(this.GetType().Assembly);

            Assert.AreEqual(componentName, componentInfo.ComponentId);
            Assert.AreEqual(thisVersion, componentInfo.AssemblyVersion);
            Assert.AreEqual(thisVersion, componentInfo.SupportedVersion);
        }
        [TestMethod]
        public void Packaging_EzRelease_CreateComponentInfo_SupportedVersionLessThanThisVersion()
        {
            var thisVersion = TypeHandler.GetVersion(this.GetType().Assembly);
            var supportedVersion = thisVersion.Minor == 0
                ? new Version(thisVersion.Major - 1, 9, thisVersion.MajorRevision)
                : new Version(thisVersion.Major, thisVersion.Minor - 1, thisVersion.Build);
            var supportedVersionString = supportedVersion.ToString();
            var componentName = "TestComponent-1";
            var component = new TestComponent(componentName, supportedVersionString, false);
            var componentInfo = SnComponentInfo.Create(component);

            Assert.AreEqual(componentName, componentInfo.ComponentId);
            Assert.AreEqual(thisVersion, componentInfo.AssemblyVersion);
            Assert.AreEqual(supportedVersion, componentInfo.SupportedVersion);
            Assert.IsFalse(componentInfo.IsComponentAllowed.Invoke(null));
        }
        [TestMethod]
        public void Packaging_EzRelease_CreateComponentInfo_SupportedVersionGreaterThanThisVersion()
        {
            var thisVersion = TypeHandler.GetVersion(this.GetType().Assembly);
            var supportedVersion = new Version(thisVersion.Major, thisVersion.Minor + 1, 0);
            var supportedVersionString = supportedVersion.ToString();
            var componentName = "TestComponent-1";
            var component = new TestComponent(componentName, supportedVersionString, false);
            try
            {
                var _ = SnComponentInfo.Create(component);
                Assert.Fail();
            }
            catch (ApplicationException)
            {
                // do nothing
            }
        }

        [TestMethod]
        public async Task Packaging_EzRelease_MissingComponent_CannotRun()
        {
            // Missing component cannot run
            Assert.IsFalse(RunComponent(C("CompA", "7.1.0", "7.1.0")));

            // Install and run
            await SavePackage("CompA", "7.1.0", "02:00", "2016-01-01", PackageType.Install, ExecutionResult.Successful);
            Assert.IsTrue(RunComponent(C("CompA", "7.1.0", "7.1.0")));
        }
        [TestMethod]
        public async Task Packaging_EzRelease_InstalledComponent_CanRun()
        {
            await SavePackage("CompA", "7.1.0", "02:00", "2016-01-01", PackageType.Install, ExecutionResult.Successful);
            Assert.IsTrue(RunComponent(C("CompA", "7.1.0", "7.1.0")));
        }
        [TestMethod]
        public async Task Packaging_EzRelease_CustomizedVersionCheckerAllows_CanRun()
        {
            var invoked = false;
            var permittingFunction = new Func<Version, bool>(v =>
            {
                invoked = true;
                return true;
            });
            await SavePackage("CompA", "7.1.0", "02:00", "2016-01-01", PackageType.Install, ExecutionResult.Successful);
            Assert.IsTrue(RunComponent(C("CompA", "7.1.0", "7.1.0", permittingFunction)));
            Assert.IsTrue(invoked, "The function was not invoked.");
        }
        [TestMethod]
        public async Task Packaging_EzRelease_CustomizedVersionCheckerDenies_CannotRun()
        {
            var invoked = false;
            var permittingFunction = new Func<Version, bool>(v =>
            {
                invoked = true;
                return false;
            });
            await SavePackage("CompA", "7.1.0", "02:00", "2016-01-01", PackageType.Install, ExecutionResult.Successful);
            Assert.IsFalse(RunComponent(C("CompA", "7.1.0", "7.1.0", permittingFunction)));
            Assert.IsTrue(invoked, "The function was not invoked.");
        }
        [TestMethod]
        public async Task Packaging_EzRelease_CompatibleComponent_CanRun()
        {
            await SavePackage("CompA", "7.1.0", "02:00", "2016-01-01", PackageType.Install, ExecutionResult.Successful);
            await SavePackage("CompA", "7.1.1", "02:00", "2016-01-02", PackageType.Patch, ExecutionResult.Successful);
            Assert.IsTrue(RunComponent(C("CompA", "7.1.2", "7.1.0")));
        }
        [TestMethod]
        public async Task Packaging_EzRelease_InCompatibleComponent_CannotRun()
        {
            await SavePackage("CompA", "7.1.0", "02:00", "2016-01-01", PackageType.Install, ExecutionResult.Successful);
            await SavePackage("CompA", "7.1.1", "02:00", "2016-01-02", PackageType.Patch, ExecutionResult.Successful);
            await SavePackage("CompA", "7.1.2", "02:00", "2016-01-03", PackageType.Patch, ExecutionResult.Successful);
            Assert.IsFalse(RunComponent(C("CompA", "7.2.0", "7.2.0")));
        }

        /* ============================================================================== */

        private SnComponentInfo C(string id, string asmVersion, string supportedVersion, Func<Version, bool> permittingFunction = null)
        {
            return new SnComponentInfo
            {
                ComponentId = id,
                AssemblyVersion = Version.Parse(asmVersion),
                SupportedVersion = Version.Parse(supportedVersion),
                IsComponentAllowed = permittingFunction
            };
        }

        private bool RunComponent(SnComponentInfo component)
        {
            try
            {
                RepositoryVersionInfo.CheckComponentVersions(new[] { component }, true);
                return true;
            }
            catch (InvalidOperationException e)
            {
                if (e.Message.Contains("component is missing."))
                    return false;
                throw;
            }
            catch (ApplicationException e)
            {
                if (e.Message.Contains("Component and assembly version mismatch."))
                    return false;
                throw;
            }
        }

        private class TestComponent : ISnComponent
        {
            public string ComponentId { get; }
            public Version SupportedVersion { get; }

            // ReSharper disable once UnusedMember.Local
            public TestComponent()
            {
                // Default contructor is needed to support automatic instantiation.
            }
            public TestComponent(string componentId, string supportedVersion, bool allowed = true)
            {
                ComponentId = componentId;
                SupportedVersion = supportedVersion == null ? null : Version.Parse(supportedVersion);
                _allowed = allowed;
            }

            private bool _allowed;
            public bool IsComponentAllowed(Version componentVersion)
            {
                return _allowed;
            }

            public ISnPatch[] GetPatches()
            {
                return default;
            }

            public virtual void AddPatches(PatchBuilder builder) //UNDONE:PATCH:?? GetPatches or AddPatches ??
            {
                // do nothing
            }
        }
    }
}
