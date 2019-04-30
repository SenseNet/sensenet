using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Diagnostics;
using SenseNet.Packaging.Tests.Implementations;
using SenseNet.Tests.Implementations;
using SenseNet.Tools;

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
    internal class TestComponentPatchStartRepository : ISnComponent
    {
        public string ComponentId => nameof(TestComponentPatchStartRepository);
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
                            <Id>" + nameof(TestComponentPatchStartRepository) + @"</Id>
                            <ReleaseDate>2018-01-01</ReleaseDate>
                            <Version>1.1</Version>
                            <Dependencies>
                                <Dependency id='" + nameof(TestComponentPatchStartRepository) + @"' minVersion='1.0' maxVersion='1.0' />
                            </Dependencies>
                            <Steps>
                                <StartRepository />
                                <AddField contentType=""File"">
                                  <FieldXml>
                                    <Field name=""NewStartField"" type=""ShortText"">
                                    </Field>
                                  </FieldXml>
                                </AddField>
                            </Steps>
                        </Package>"
            }
        };
    }

    internal class TestComponentPatchChangeHandler : ISnComponent
    {
        public string ComponentId => nameof(TestComponentPatchChangeHandler);
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
                Execute = rss =>
                {
                    // find the GenericContent CTD file in the current db (not the prototype)
                    var proto = ((InMemoryDataProvider)DataProvider.Current).DB;
                    var gcFile = proto.Files.First(ff =>
                        ff.Extension == ".ContentType" && ff.FileNameWithoutExtension == "GenericContent");

                    // restore the original handler
                    RepositoryStartTests.SetContentHandler(gcFile, "SenseNet.ContentRepository.GenericContent");

                    return ExecutionResult.Successful;
                }
            }
        };
    }

    internal class TestComponentPatchInvalidVersion : ISnComponent
    {
        public string ComponentId => nameof(TestComponentPatchInvalidVersion);
        public Version SupportedVersion { get; } = new Version(1, 5);
        public bool IsComponentAllowed(Version componentVersion)
        {
            return true;
        }
        public SnPatch[] Patches { get; } =
        {
            new SnPatch
            {
                Version = new Version(1, 5),
                MaxVersion = new Version(2, 0),
                MinVersion = new Version(1, 0),
                Execute = rss => throw new InvalidOperationException("This code should be unreachable.")
            },
            new SnPatch
            {
                Version = new Version(1, 7),
                MaxVersion = new Version(1, 0),
                MinVersion = new Version(2, 0),
                Execute = rss => throw new InvalidOperationException("This code should be unreachable.")
            }
        };
    }
    #endregion

    internal class TestPackageLogger : IEventLogger
    {
        internal List<string> Warnings = new List<string>();

        public void Write(object message, ICollection<string> categories, int priority, int eventId, TraceEventType severity, string title,
            IDictionary<string, object> properties)
        {
            if (severity == TraceEventType.Warning)
                Warnings.Add((string)message);
        }
    }

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
        public void Packaging_InvalidVersion()
        {
            var originalLogger = SnLog.Instance;
            var logger = new TestPackageLogger();
            SnLog.Instance = logger;

            try
            {
                PatchAndCheck(nameof(TestComponentPatchInvalidVersion),
                    new[] { new Version(1, 0) },
                    null,
                    null,
                    new Version(1, 0));

                Assert.IsTrue(logger.Warnings.Any(w => w.Contains("invalid version numbers") && w.Contains("Patch 1.5")));
                Assert.IsTrue(logger.Warnings.Any(w => w.Contains("invalid version numbers") && w.Contains("Patch 1.7")));
            }
            finally
            {
                SnLog.Instance = originalLogger;
            }
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

        [TestMethod]
        public void Packaging_StartRepository_AddFieldPatch()
        {
            const string componentId = nameof(TestComponentPatchStartRepository);

            Test(builder =>
             {
                 // install a test component so that the built-in patch for that component gets executed
                 PackageManager.Storage.SavePackage(new Package
                 {
                     ComponentId = componentId,
                     ComponentVersion = new Version(1, 0),
                     ExecutionResult = ExecutionResult.Successful,
                     PackageType = PackageType.Install
                 });
             },
             () =>
             {
                 // make sure the built-in patch has added a new field
                 Assert.IsTrue(ContentType.GetByName("File").FieldSettings.Any(fs => fs.Name == "NewStartField"));
             });
        }
        [TestMethod]
        public void Packaging_StartRepository_ChangeCtdPatch()
        {
            // if this is the first test, we need to make sure the prototype is already created
            EnsurePrototypes();

            // find the GenericContent CTD content file
            var proto = InMemoryDataProvider.Prototype;
            var gcFile = proto.Files.First(ff =>
                ff.Extension == ".ContentType" && ff.FileNameWithoutExtension == "GenericContent");

            // set a fake handler
            SetContentHandler(gcFile, "unknownhandler");
            
            var thrown = false;

            try
            {
                //UNDONE: change assert logic for the invalid handler
                // When ContentTypeManager is able to handle a missing handler without throwing
                // and exception, change this logic to check for the invalid content type instead
                // of expecting and exception.

                // this should throw an exception because of the incorrect content handler
                Test(() => { });
            }
            catch (Exception e)
            {
                if (e.InnerException?.InnerException?.InnerException is TypeNotFoundException tnfe && 
                    tnfe.Message.Contains("unknownhandler"))
                {
                    thrown = true;
                }
            }
            
            if (!thrown)
                Assert.Fail("TypeNotFoundException was not thrown.");

            try
            {
                Test(builder =>
                    {
                        // install a test component so that the built-in patch for that component gets executed
                        PackageManager.Storage.SavePackage(new Package
                        {
                            ComponentId = nameof(TestComponentPatchChangeHandler),
                            ComponentVersion = new Version(1, 0),
                            ExecutionResult = ExecutionResult.Successful,
                            PackageType = PackageType.Install
                        });
                    },
                    () =>
                    {
                        // the CTD should contain the new handler
                        Assert.AreEqual("SenseNet.ContentRepository.GenericContent",
                            ContentType.GetByName("GenericContent").HandlerName);
                    });
            }
            finally
            {
                // reset the correct handler in the prototype
                SetContentHandler(gcFile, "SenseNet.ContentRepository.GenericContent");
            }
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

        internal static void SetContentHandler(InMemoryDataProvider.FileRecord fr, string handler)
        {
            string ctdString;

            using (var xmlReaderStream = new MemoryStream(fr.Stream))
            {
                var gcXmlDoc = new XmlDocument();
                gcXmlDoc.Load(xmlReaderStream);

                gcXmlDoc.DocumentElement.Attributes["handler"].Value = handler;

                ctdString = gcXmlDoc.OuterXml;
            }

            fr.Stream = Encoding.UTF8.GetBytes(ctdString);
            fr.Size = fr.Stream.Length;
        }
    }
}
