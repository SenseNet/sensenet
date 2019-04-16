using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Packaging.Tests.Implementations;

namespace SenseNet.Packaging.Tests
{
    #region Test components
    internal class TestComponentOnePatch : ISnComponent
    {
        public string ComponentId => nameof(TestComponentOnePatch);
        public Version SupportedVersion { get; } = new Version(1, 1);
        public bool IsComponentAllowed(Version componentVersion)
        {
            return true;
        }
        public SnPatch[] Patches { get; } = 
        {
            new SnPatch
            {
                Version = new Version(1, 1),
                MaxVersion = new Version(1, 0),
                MinVersion = new Version(1, 0),
                Contents = @"<?xml version='1.0' encoding='utf-8'?>
                        <Package type='Patch'>
                            <Id>" + nameof(TestComponentOnePatch) + @"</Id>
                            <ReleaseDate>2018-01-01</ReleaseDate>
                            <Version>1.1</Version>
                            <Dependencies>
                                <Dependency id='" + nameof(TestComponentOnePatch) + @"' minVersion='1.0' maxVersion='1.0' />
                            </Dependencies>
                        </Package>"
            }
        };
    }
    internal class TestComponentNoPatch : ISnComponent
    {
        public string ComponentId => nameof(TestComponentNoPatch);
        public Version SupportedVersion { get; } = new Version(1, 1);
        public bool IsComponentAllowed(Version componentVersion)
        {
            return true;
        }
        public SnPatch[] Patches { get; } = new SnPatch[0];
    }
    internal class TestComponentMultiPatch : ISnComponent
    {
        public string ComponentId => nameof(TestComponentMultiPatch);
        public Version SupportedVersion { get; } = new Version(1, 2);
        public bool IsComponentAllowed(Version componentVersion)
        {
            return true;
        }
        public SnPatch[] Patches { get; } =
        {
            new SnPatch
            {
                Version = new Version(1, 1),
                MaxVersion = new Version(1, 0),
                MinVersion = new Version(1, 0),
                Contents = @"<?xml version='1.0' encoding='utf-8'?>
                        <Package type='Patch'>
                            <Id>" + nameof(TestComponentMultiPatch) + @"</Id>
                            <ReleaseDate>2018-01-01</ReleaseDate>
                            <Version>1.1</Version>
                            <Dependencies>
                                <Dependency id='" + nameof(TestComponentMultiPatch) + @"' minVersion='1.0' maxVersion='1.0' />
                            </Dependencies>
                        </Package>"
            },
            new SnPatch
            {
                Version = new Version(1, 2),
                MaxVersion = new Version(1, 1),
                MinVersion = new Version(1, 1),
                Contents = @"<?xml version='1.0' encoding='utf-8'?>
                        <Package type='Patch'>
                            <Id>" + nameof(TestComponentMultiPatch) + @"</Id>
                            <ReleaseDate>2018-01-01</ReleaseDate>
                            <Version>1.2</Version>
                            <Dependencies>
                                <Dependency id='" + nameof(TestComponentMultiPatch) + @"' minVersion='1.1' maxVersion='1.1' />
                            </Dependencies>
                        </Package>"
            }
        };
    }
    internal class TestComponentMultiPatchExclusive : ISnComponent
    {
        public string ComponentId => nameof(TestComponentMultiPatchExclusive);
        public Version SupportedVersion { get; } = new Version(2, 5);
        public bool IsComponentAllowed(Version componentVersion)
        {
            return true;
        }
        public SnPatch[] Patches { get; } =
        {
            new SnPatch
            {
                Version = new Version(3, 0),
                MaxVersion = new Version(2, 0),
                MaxVersionIsExclusive = true,
                MinVersion = new Version(1, 0),
                Contents = "<?xml>skipped"
            },
            new SnPatch
            {
                Version = new Version(3, 0),
                MaxVersion = new Version(3, 0),
                MaxVersionIsExclusive = true,
                MinVersion = new Version(2, 0),
                MinVersionIsExclusive = true,
                Contents = "<?xml>skipped"
            }
        };
    }
    internal class TestComponentMultiPatchIncorrectFormat : ISnComponent
    {
        public string ComponentId => nameof(TestComponentMultiPatchIncorrectFormat);
        public Version SupportedVersion { get; } = new Version(1, 2);
        public bool IsComponentAllowed(Version componentVersion)
        {
            return true;
        }
        public SnPatch[] Patches { get; } =
        {
            new SnPatch
            {
                Version = new Version(1, 1),
                MaxVersion = new Version(1, 0),
                MinVersion = new Version(1, 0),
                Contents = @"<?xml version='1.0' encoding='utf-8'?>
                        <Package type='Patch'>
                            <Id>" + nameof(TestComponentMultiPatchIncorrectFormat) + @"</Id>
                            <ReleaseDate>2018-01-01</ReleaseDate>
                            <Version>1.1</Version>
                            <Dependencies>
                                <Dependency id='" + nameof(TestComponentMultiPatchIncorrectFormat) + @"' minVersion='1.0' maxVersion='1.0' />
                            </Dependencies>
                        </Package>"
            },
            // this package contains an invalid manifest xml
            new SnPatch
            {
                Version = new Version(1, 2),
                MaxVersion = new Version(1, 1),
                MinVersion = new Version(1, 1),
                Contents = @"<?xml version='1.0' encoding='utf-8'?>
                        <Package type='Patch'>
                            <Id>" + nameof(TestComponentMultiPatchIncorrectFormat) + @"</Id>
                            <ReleaseDate>                            
                        </Package>"
            }
        };
    }
    #endregion

    [TestClass]
    public class RepositoryStartTests : PackagingTestBase
    {
        [TestInitialize]
        public void InitializePackagingTest()
        {
            DataProvider.Instance.SetExtension(typeof(IPackagingDataProviderExtension), new TestPackageStorageProvider());

            // make sure that every test starts with a clean slate (no existing installed components)
            PackageManager.Storage.DeleteAllPackages();
        }

        [TestMethod]
        public void Packaging_NoPatchNeeded()
        {
            PatchAndCheck(nameof(TestComponentOnePatch),
                new[] {new Version(1, 0), new Version(1, 1)},
                null,
                null,
                new Version(1, 1));
        }
        [TestMethod]
        public void Packaging_OnePatch()
        {
            PatchAndCheck(nameof(TestComponentOnePatch),
                new[] {new Version(1, 0)},
                new[] {new Version(1, 1)},
                null,
                new Version(1, 1));
        }
        [TestMethod]
        public void Packaging_MultiPatch()
        {
            PatchAndCheck(nameof(TestComponentMultiPatch),
                new[] {new Version(1, 0)},
                new[] {new Version(1, 1), new Version(1, 2)},
                null,
                new Version(1, 2));
        }
        [TestMethod]
        public void Packaging_MultiPatch_SkipPatch()
        {
            PatchAndCheck(nameof(TestComponentMultiPatch),
                new[] {new Version(1, 1)},
                new[] {new Version(1, 2)},
                null,
                new Version(1, 2));
        }
        [TestMethod]
        public void Packaging_MultiPatch_Exclusive()
        {
            PatchAndCheck(nameof(TestComponentMultiPatchExclusive),
                new[] { new Version(2, 0) },
                null,
                null,
                new Version(2, 0));
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidPackageException))]
        public void Packaging_MultiPatch_IncorrectFormat()
        {
            PatchAndCheck(nameof(TestComponentMultiPatchIncorrectFormat),
                new[] { new Version(1, 0) },
                null, null, null);
        }
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Packaging_MissingPatch()
        {
            // The Supported version of this component is v1.1, but there is no patch
            // in the assembly, so we expect an exception here.
            PatchAndCheck(nameof(TestComponentNoPatch),
                new[] {new Version(1, 0)},
                null,
                null,
                null);
        }

        [TestMethod]
        public void Packaging_Comparer()
        {
            Assert.AreEqual("C1,C2,C3",
                string.Join(",",
                    new[] { "C3", "C2", "C1" }.OrderBy(c => c,
                        new RepositoryVersionInfo.SnComponentComparer(new[] { "C1", "C2", "C3" })).ToArray()));

            Assert.AreEqual("C1,C3,C4,C5",
                string.Join(",",
                    new[] { "C4", "C3", "C5", "C1" }.OrderBy(c => c,
                        new RepositoryVersionInfo.SnComponentComparer(new[] { "C1", "C2", "C3" })).ToArray()));
        }

        private static void PatchAndCheck(string componentId, 
            Version[] packageVersions,
            Version[] successfulPatchVersions,
            Version[] failedPatchVersions,
            Version expectedVersion)
        {
            Version initialVersion = null;

            // install mock packages
            if (packageVersions?.Any() ?? false)
            {
                // the first should be an install package, Patch packages will follow
                var install = true;

                foreach (var packageVersion in packageVersions)
                {
                    PackageManager.Storage.SavePackage(new Package
                    {
                        ComponentId = componentId,
                        ComponentVersion = packageVersion,
                        ExecutionResult = ExecutionResult.Successful,
                        PackageType = install ? PackageType.Install : PackageType.Patch
                    });

                    install = false;
                }

                RepositoryVersionInfo.Reset();

                initialVersion = packageVersions.Last();
            }

            var installedComponent = RepositoryVersionInfo.Instance.Components.Single(c => c.ComponentId == componentId);
            if (installedComponent != null)
                Assert.AreEqual(initialVersion, installedComponent.Version);

            var assemblyComponent = RepositoryVersionInfo.GetAssemblyComponents()
                .Single(c => c.ComponentId == componentId);

            // ACTION
            var results = PackageManager.ExecuteAssemblyPatch(assemblyComponent);

            // reload version info
            installedComponent = RepositoryVersionInfo.Instance.Components.Single(c => c.ComponentId == componentId);
            
            if (successfulPatchVersions?.Any() ?? false)
            {
                // the component was successfully upgraded
                foreach (var patchVersion in successfulPatchVersions)
                    Assert.IsTrue(results[patchVersion].Successful);
            }
            if (failedPatchVersions?.Any() ?? false)
            {
                // there should be failed patch results
                foreach (var patchVersion in failedPatchVersions)
                    Assert.IsFalse(results[patchVersion].Successful);
            }

            if (!(successfulPatchVersions?.Any() ?? false) && !(failedPatchVersions?.Any() ?? false))
            {
                // no patch is expected
                Assert.IsFalse(results?.Keys.Any() ?? false);
            }

            if (installedComponent != null)
                Assert.AreEqual(expectedVersion, installedComponent.Version);
        }
    }
}
