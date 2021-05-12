//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using System.Threading;
//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using SenseNet.ContentRepository;
//using SenseNet.ContentRepository.Schema;
//using SenseNet.ContentRepository.Storage;
//using SenseNet.ContentRepository.Storage.Data;
//using SenseNet.Diagnostics;
//using SenseNet.Packaging.Tests.Implementations;
//using SenseNet.Tests.Core.Implementations;
//using static SenseNet.Tests.Core.Tools;
//using Task = System.Threading.Tasks.Task;

//namespace SenseNet.Packaging.Tests
//{
//    #region Test components
//    internal class TestComponentOnePatch : ISnComponent
//    {
//        public string ComponentId => nameof(TestComponentOnePatch);
//        public Version SupportedVersion { get; } = new Version(1, 1);
//        public bool IsComponentAllowed(Version componentVersion)
//        {
//            return true;
//        }
//        public ISnPatch[] Patches { get; } = 
//        {
//            new SnPatch
//            {
//                Version = new Version(1, 1),
//                MaxVersion = new Version(1, 0),
//                MinVersion = new Version(1, 0),
//                Contents = @"<?xml version='1.0' encoding='utf-8'?>
//                        <Package type='Patch'>
//                            <Id>" + nameof(TestComponentOnePatch) + @"</Id>
//                            <ReleaseDate>2018-01-01</ReleaseDate>
//                            <Version>1.1</Version>
//                            <Dependencies>
//                                <Dependency id='" + nameof(TestComponentOnePatch) + @"' minVersion='1.0' maxVersion='1.0' />
//                            </Dependencies>
//                        </Package>"
//            }
//        };
//    }
//    internal class TestComponentNoPatch : ISnComponent
//    {
//        public string ComponentId => nameof(TestComponentNoPatch);
//        public Version SupportedVersion { get; } = new Version(1, 1);
//        public bool IsComponentAllowed(Version componentVersion)
//        {
//            return true;
//        }
//        public ISnPatch[] Patches { get; } = new SnPatch[0];
//    }
//    internal class TestComponentMultiPatch : ISnComponent
//    {
//        public string ComponentId => nameof(TestComponentMultiPatch);
//        public Version SupportedVersion { get; } = new Version(1, 2);
//        public bool IsComponentAllowed(Version componentVersion)
//        {
//            return true;
//        }
//        public ISnPatch[] Patches { get; } =
//        {
//            new SnPatch
//            {
//                Version = new Version(1, 1),
//                MaxVersion = new Version(1, 0),
//                MinVersion = new Version(1, 0),
//                Contents = @"<?xml version='1.0' encoding='utf-8'?>
//                        <Package type='Patch'>
//                            <Id>" + nameof(TestComponentMultiPatch) + @"</Id>
//                            <ReleaseDate>2018-01-01</ReleaseDate>
//                            <Version>1.1</Version>
//                            <Dependencies>
//                                <Dependency id='" + nameof(TestComponentMultiPatch) + @"' minVersion='1.0' maxVersion='1.0' />
//                            </Dependencies>
//                        </Package>"
//            },
//            new SnPatch
//            {
//                Version = new Version(1, 2),
//                MaxVersion = new Version(1, 1),
//                MinVersion = new Version(1, 1),
//                Contents = @"<?xml version='1.0' encoding='utf-8'?>
//                        <Package type='Patch'>
//                            <Id>" + nameof(TestComponentMultiPatch) + @"</Id>
//                            <ReleaseDate>2018-01-01</ReleaseDate>
//                            <Version>1.2</Version>
//                            <Dependencies>
//                                <Dependency id='" + nameof(TestComponentMultiPatch) + @"' minVersion='1.1' maxVersion='1.1' />
//                            </Dependencies>
//                        </Package>"
//            }
//        };
//    }
//    internal class TestComponentMultiPatchExclusive : ISnComponent
//    {
//        public string ComponentId => nameof(TestComponentMultiPatchExclusive);
//        public Version SupportedVersion { get; } = new Version(2, 5);
//        public bool IsComponentAllowed(Version componentVersion)
//        {
//            return true;
//        }
//        public ISnPatch[] Patches { get; } =
//        {
//            new SnPatch
//            {
//                Version = new Version(3, 0),
//                MaxVersion = new Version(2, 0),
//                MaxVersionIsExclusive = true,
//                MinVersion = new Version(1, 0),
//                Contents = "<?xml>skipped"
//            },
//            new SnPatch
//            {
//                Version = new Version(3, 0),
//                MaxVersion = new Version(3, 0),
//                MaxVersionIsExclusive = true,
//                MinVersion = new Version(2, 0),
//                MinVersionIsExclusive = true,
//                Contents = "<?xml>skipped"
//            }
//        };
//    }
//    internal class TestComponentMultiPatchIncorrectFormat : ISnComponent
//    {
//        public string ComponentId => nameof(TestComponentMultiPatchIncorrectFormat);
//        public Version SupportedVersion { get; } = new Version(1, 2);
//        public bool IsComponentAllowed(Version componentVersion)
//        {
//            return true;
//        }
//        public ISnPatch[] Patches { get; } =
//        {
//            new SnPatch
//            {
//                Version = new Version(1, 1),
//                MaxVersion = new Version(1, 0),
//                MinVersion = new Version(1, 0),
//                Contents = @"<?xml version='1.0' encoding='utf-8'?>
//                        <Package type='Patch'>
//                            <Id>" + nameof(TestComponentMultiPatchIncorrectFormat) + @"</Id>
//                            <ReleaseDate>2018-01-01</ReleaseDate>
//                            <Version>1.1</Version>
//                            <Dependencies>
//                                <Dependency id='" + nameof(TestComponentMultiPatchIncorrectFormat) + @"' minVersion='1.0' maxVersion='1.0' />
//                            </Dependencies>
//                        </Package>"
//            },
//            // this package contains an invalid manifest xml
//            new SnPatch
//            {
//                Version = new Version(1, 2),
//                MaxVersion = new Version(1, 1),
//                MinVersion = new Version(1, 1),
//                Contents = @"<?xml version='1.0' encoding='utf-8'?>
//                        <Package type='Patch'>
//                            <Id>" + nameof(TestComponentMultiPatchIncorrectFormat) + @"</Id>
//                            <ReleaseDate>                            
//                        </Package>"
//            }
//        };
//    }
//    internal class TestComponentPatchStartRepository : ISnComponent
//    {
//        public string ComponentId => nameof(TestComponentPatchStartRepository);
//        public Version SupportedVersion { get; } = new Version(1, 1);
//        public bool IsComponentAllowed(Version componentVersion)
//        {
//            return true;
//        }
//        public ISnPatch[] Patches { get; } =
//        {
//            new SnPatch
//            {
//                Version = new Version(1, 1),
//                MaxVersion = new Version(1, 0),
//                MinVersion = new Version(1, 0),
//                Contents = @"<?xml version='1.0' encoding='utf-8'?>
//                        <Package type='Patch'>
//                            <Id>" + nameof(TestComponentPatchStartRepository) + @"</Id>
//                            <ReleaseDate>2018-01-01</ReleaseDate>
//                            <Version>1.1</Version>
//                            <Dependencies>
//                                <Dependency id='" + nameof(TestComponentPatchStartRepository) + @"' minVersion='1.0' maxVersion='1.0' />
//                            </Dependencies>
//                            <Steps>
//                                <StartRepository />
//                                <AddField contentType=""File"">
//                                  <FieldXml>
//                                    <Field name=""NewStartField"" type=""ShortText"">
//                                    </Field>
//                                  </FieldXml>
//                                </AddField>
//                            </Steps>
//                        </Package>"
//            }
//        };
//    }

//    internal class TestComponentPatchChangeHandler : ISnComponent
//    {
//        public string ComponentId => nameof(TestComponentPatchChangeHandler);
//        public Version SupportedVersion { get; } = new Version(1, 1);
//        public bool IsComponentAllowed(Version componentVersion)
//        {
//            return true;
//        }
//        public ISnPatch[] Patches { get; } =
//        {
//            new SnPatch
//            {
//                Version = new Version(1, 1),
//                MaxVersion = new Version(1, 0),
//                MinVersion = new Version(1, 0),
//                Execute = pc =>
//                {
//                    // restore the original handler
//                    RepositoryStartTests.SetContentHandler("GenericContent", "SenseNet.ContentRepository.GenericContent");
//                }
//            }
//        };
//    }

//    internal class TestComponentPatchInvalidVersion : ISnComponent
//    {
//        public string ComponentId => nameof(TestComponentPatchInvalidVersion);
//        public Version SupportedVersion { get; } = new Version(1, 5);
//        public bool IsComponentAllowed(Version componentVersion)
//        {
//            return true;
//        }
//        public ISnPatch[] Patches { get; } =
//        {
//            new SnPatch
//            {
//                Version = new Version(1, 5),
//                MaxVersion = new Version(2, 0),
//                MinVersion = new Version(1, 0),
//                Execute = pc => throw new InvalidOperationException("This code should be unreachable.")
//            },
//            new SnPatch
//            {
//                Version = new Version(1, 7),
//                MaxVersion = new Version(1, 0),
//                MinVersion = new Version(2, 0),
//                Execute = pc => throw new InvalidOperationException("This code should be unreachable.")
//            }
//        };
//    }

//    internal class TestComponentPatchCode : ISnComponent
//    {
//        public string ComponentId => nameof(TestComponentPatchCode);
//        public Version SupportedVersion { get; } = new Version(1, 1);
//        public bool IsComponentAllowed(Version componentVersion)
//        {
//            return true;
//        }
//        public ISnPatch[] Patches { get; } =
//        {
//            new SnPatch
//            {
//                Version = new Version(1, 1),
//                MaxVersion = new Version(1, 0),
//                MinVersion = new Version(1, 0),
//                Execute = pc =>
//                {
//                    // this patch does not do anything special
//                    SnLog.WriteInformation("Patch executed.");
//                }
//            }
//        };
//    }
//    internal class TestComponentPatchInvalidAmbigous : ISnComponent
//    {
//        public string ComponentId => nameof(TestComponentPatchInvalidAmbigous);
//        public Version SupportedVersion { get; } = new Version(1, 1);
//        public bool IsComponentAllowed(Version componentVersion)
//        {
//            return true;
//        }
//        public ISnPatch[] Patches { get; } =
//        {
//            new SnPatch
//            {
//                Version = new Version(1, 1),
//                MaxVersion = new Version(1, 0),
//                MinVersion = new Version(1, 0),
//                Contents = @"<?xml version='1.0' encoding='utf-8'?>
//                        <Package type='Patch'>
//                            <Id>" + nameof(TestComponentPatchInvalidAmbigous) + @"</Id>
//                            <ReleaseDate>2018-01-01</ReleaseDate>
//                            <Version>1.1</Version>
//                            <Dependencies>
//                                <Dependency id='" + nameof(TestComponentPatchInvalidAmbigous) + @"' minVersion='1.0' maxVersion='1.0' />
//                            </Dependencies>
//                        </Package>",
//                Execute = pc => { }
//            }
//        };
//    }
//    #endregion

//    internal class TestPackageLogger : IEventLogger
//    {
//        internal List<string> Warnings = new List<string>();
//        internal List<string> Infos = new List<string>();

//        public void Write(object message, ICollection<string> categories, int priority, int eventId, TraceEventType severity, string title,
//            IDictionary<string, object> properties)
//        {
//            if (severity == TraceEventType.Information)
//                Infos.Add((string)message);
//            if (severity == TraceEventType.Warning)
//                Warnings.Add((string)message);
//        }
//    }

//    //TODO: [auto-patch] this feature is not released yet
//    //[TestClass]
//    public class RepositoryStartTests : PackagingTestBase
//    {
//        [TestInitialize]
//        public Task InitializePackagingTest()
//        {
//            //DataProvider.Instance.SetExtension(typeof(IPackagingDataProviderExtension), new TestPackageStorageProvider());
//            Providers.Instance.DataProvider.SetExtension(typeof(IPackagingDataProviderExtension), new TestPackageStorageProvider());

//            // make sure that every test starts with a clean slate (no existing installed components)
//            return PackageManager.Storage.DeleteAllPackagesAsync(CancellationToken.None);
//        }

//        [TestMethod]
//        public async Task Packaging_NoPatchNeeded()
//        {
//            await PatchAndCheck(nameof(TestComponentOnePatch),
//                new[] {new Version(1, 0), new Version(1, 1)},
//                null,
//                null,
//                new Version(1, 1));
//        }
//        [TestMethod]
//        public async Task Packaging_OnePatch_Manifest()
//        {
//            // execute a patch that is defined as a manifest
//            await PatchAndCheck(nameof(TestComponentOnePatch),
//                new[] {new Version(1, 0)},
//                new[] {new Version(1, 1)},
//                null,
//                new Version(1, 1));
//        }
//        [TestMethod]
//        public async Task Packaging_OnePatch_Code()
//        {
//            using (var ls = new LoggerSwindler<TestPackageLogger>())
//            {
//                // execute a patch that is defined as code
//                await PatchAndCheck(nameof(TestComponentPatchCode),
//                    new[] { new Version(1, 0) },
//                    new[] { new Version(1, 1) },
//                    null,
//                    new Version(1, 1));

//                Assert.IsTrue(ls.Logger.Infos.Any(w => w.Contains("Patch executed.")));
//            }
//        }
//        [TestMethod]
//        public async Task Packaging_MultiPatch()
//        {
//            await PatchAndCheck(nameof(TestComponentMultiPatch),
//                new[] {new Version(1, 0)},
//                new[] {new Version(1, 1), new Version(1, 2)},
//                null,
//                new Version(1, 2));
//        }
//        [TestMethod]
//        public async Task Packaging_MultiPatch_SkipPatch()
//        {
//            await PatchAndCheck(nameof(TestComponentMultiPatch),
//                new[] {new Version(1, 1)},
//                new[] {new Version(1, 2)},
//                null,
//                new Version(1, 2));
//        }
//        [TestMethod]
//        public async Task Packaging_MultiPatch_Exclusive()
//        {
//            await PatchAndCheck(nameof(TestComponentMultiPatchExclusive),
//                new[] { new Version(2, 0) },
//                null,
//                null,
//                new Version(2, 0));
//        }

//        [TestMethod]
//        [ExpectedException(typeof(InvalidPackageException))]
//        public async Task Packaging_MultiPatch_IncorrectFormat()
//        {
//            await PatchAndCheck(nameof(TestComponentMultiPatchIncorrectFormat),
//                new[] { new Version(1, 0) },
//                null, null, null);
//        }
//        [TestMethod]
//        [ExpectedException(typeof(InvalidOperationException))]
//        public async Task Packaging_MissingPatch()
//        {
//            // The Supported version of this component is v1.1, but there is no patch
//            // in the assembly, so we expect an exception here.
//            await PatchAndCheck(nameof(TestComponentNoPatch),
//                new[] {new Version(1, 0)},
//                null,
//                null,
//                null);
//        }

//        [TestMethod]
//        public async Task Packaging_Invalid_Version()
//        {
//            using (var ls = new LoggerSwindler<TestPackageLogger>())
//            {
//                await PatchAndCheck(nameof(TestComponentPatchInvalidVersion),
//                    new[] { new Version(1, 0) },
//                    null,
//                    null,
//                    new Version(1, 0));

//                Assert.IsTrue(ls.Logger.Warnings.Any(w => w.Contains("invalid version numbers") && w.Contains("Patch 1.5")));
//                Assert.IsTrue(ls.Logger.Warnings.Any(w => w.Contains("invalid version numbers") && w.Contains("Patch 1.7")));
//            }
//        }
//        [TestMethod]
//        public async Task Packaging_Invalid_Ambigous()
//        {
//            using (var ls = new LoggerSwindler<TestPackageLogger>())
//            {
//                await PatchAndCheck(nameof(TestComponentPatchInvalidAmbigous),
//                    new[] { new Version(1, 0) },
//                    null,
//                    null,
//                    new Version(1, 0));

//                Assert.IsTrue(ls.Logger.Warnings.Any(w => w.Contains("multiple patch definitions")));
//            }
//        }

//        [TestMethod]
//        public void Packaging_Comparer()
//        {
//            Assert.AreEqual("C1,C2,C3",
//                string.Join(",",
//                    new[] { "C3", "C2", "C1" }.OrderBy(c => c,
//                        new RepositoryVersionInfo.SnComponentComparer(new[] { "C1", "C2", "C3" })).ToArray()));

//            Assert.AreEqual("C1,C3,C4,C5",
//                string.Join(",",
//                    new[] { "C4", "C3", "C5", "C1" }.OrderBy(c => c,
//                        new RepositoryVersionInfo.SnComponentComparer(new[] { "C1", "C2", "C3" })).ToArray()));
//        }

//        [TestMethod]
//        public void Packaging_StartRepository_AddFieldPatch()
//        {
//            const string componentId = nameof(TestComponentPatchStartRepository);

//            Test(builder =>
//             {
//                 // install a test component so that the built-in patch for that component gets executed
//                 PackageManager.Storage.SavePackageAsync(new Package
//                 {
//                     ComponentId = componentId,
//                     ComponentVersion = new Version(1, 0),
//                     ExecutionResult = ExecutionResult.Successful,
//                     PackageType = PackageType.Install
//                 }, CancellationToken.None).GetAwaiter().GetResult();
//             },
//             () =>
//             {
//                 // make sure the built-in patch has added a new field
//                 Assert.IsTrue(ContentType.GetByName("File").FieldSettings.Any(fs => fs.Name == "NewStartField"));
//             });
//        }
//        [TestMethod]
//        public void Packaging_StartRepository_ChangeCtdPatch()
//        {
//            Test(builder =>
//            {
//                SetContentHandler("GenericContent", "unknownpackagingchangectd");

//                // install a test component so that the built-in patch for that component gets executed
//                PackageManager.Storage.SavePackageAsync(new Package
//                {
//                    ComponentId = nameof(TestComponentPatchChangeHandler),
//                    ComponentVersion = new Version(1, 0),
//                    ExecutionResult = ExecutionResult.Successful,
//                    PackageType = PackageType.Install
//                }, CancellationToken.None).GetAwaiter().GetResult();
//            },
//            () =>
//            {
//                // the CTD should contain the new handler
//                Assert.AreEqual("SenseNet.ContentRepository.GenericContent",
//                    ContentType.GetByName("GenericContent").HandlerName);
//            });
//        }

//        private static async Task PatchAndCheck(string componentId, 
//            Version[] packageVersions,
//            Version[] successfulPatchVersions,
//            Version[] failedPatchVersions,
//            Version expectedVersion)
//        {
//            Version initialVersion = null;

//            // install mock packages
//            if (packageVersions?.Any() ?? false)
//            {
//                // the first should be an install package, Patch packages will follow
//                var install = true;

//                foreach (var packageVersion in packageVersions)
//                {
//                    await PackageManager.Storage.SavePackageAsync(new Package
//                    {
//                        ComponentId = componentId,
//                        ComponentVersion = packageVersion,
//                        ExecutionResult = ExecutionResult.Successful,
//                        PackageType = install ? PackageType.Install : PackageType.Patch
//                    }, CancellationToken.None);

//                    install = false;
//                }

//                RepositoryVersionInfo.Reset();

//                initialVersion = packageVersions.Last();
//            }

//            var installedComponent = RepositoryVersionInfo.Instance.Components.Single(c => c.ComponentId == componentId);
//            if (installedComponent != null)
//                Assert.AreEqual(initialVersion, installedComponent.Version);

//            var assemblyComponent = RepositoryVersionInfo.GetAssemblyComponents()
//                .Single(c => c.ComponentId == componentId);

//            // ACTION
//            var results = PackageManager.ExecuteAssemblyPatches(assemblyComponent);

//            // reload version info
//            installedComponent = RepositoryVersionInfo.Instance.Components.Single(c => c.ComponentId == componentId);
            
//            if (successfulPatchVersions?.Any() ?? false)
//            {
//                // the component was successfully upgraded
//                foreach (var patchVersion in successfulPatchVersions)
//                    Assert.IsTrue(results[patchVersion].Successful);
//            }
//            if (failedPatchVersions?.Any() ?? false)
//            {
//                // there should be failed patch results
//                foreach (var patchVersion in failedPatchVersions)
//                    Assert.IsFalse(results[patchVersion].Successful);
//            }

//            if (!(successfulPatchVersions?.Any() ?? false) && !(failedPatchVersions?.Any() ?? false))
//            {
//                // no patch is expected
//                Assert.IsFalse(results?.Keys.Any() ?? false);
//            }

//            if (installedComponent != null)
//                Assert.AreEqual(expectedVersion, installedComponent.Version);
//        }

//        internal static void SetContentHandler(string contentTypeName, string handler)
//        {
//            var testingDataProvider = Providers.Instance.DataProvider.GetExtension<ITestingDataProviderExtension>();
//            if (testingDataProvider == null)
//                Assert.Inconclusive($"{nameof(ITestingDataProviderExtension)} implementation is not available.");

//            testingDataProvider.SetContentHandler(contentTypeName, handler);
//        }
//    }
//}
