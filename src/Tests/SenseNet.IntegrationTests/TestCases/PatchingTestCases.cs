using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.Packaging;

namespace SenseNet.IntegrationTests.TestCases
{
    public class PatchingTestCases : TestCaseBase
    {
        // NoRepoIntegrationTest(() =>
        // {
        // });

        public void PatchingSystem_InstalledComponents()
        {
            NoRepoIntegrationTest(() =>
            {
                var installer1 = new ComponentInstaller
                {
                    ComponentId = "C1",
                    Version = new Version(1, 0),
                    Description = "C1 description",
                    ReleaseDate = new DateTime(2020, 07, 30),
                    Dependencies = null
                };
                var installer2 = new ComponentInstaller
                {
                    ComponentId = "C2",
                    Version = new Version(2, 0),
                    Description = "C2 description",
                    ReleaseDate = new DateTime(2020, 07, 31),
                    Dependencies = new[]
                    {
                        Dep("C1", "1.0 <= v <= 1.0"),
                    }
                };
                PackageManager.SavePackage(Manifest.Create(installer1), null, true, null);
                PackageManager.SavePackage(Manifest.Create(installer2), null, true, null);

                var verInfo = RepositoryVersionInfo.Create(CancellationToken.None);

                var components = verInfo.Components.ToArray();
                Assert.AreEqual(2, components.Length);

                Assert.AreEqual("C1", components[0].ComponentId);
                Assert.AreEqual("C2", components[1].ComponentId);

                Assert.AreEqual("1.0", components[0].Version.ToString());
                Assert.AreEqual("2.0", components[1].Version.ToString());

                Assert.AreEqual("C1 description", components[0].Description);
                Assert.AreEqual("C2 description", components[1].Description);

                Assert.AreEqual(0, components[0].Dependencies.Length);
                Assert.AreEqual(1, components[1].Dependencies.Length);
                Assert.AreEqual("C1: 1.0 <= v <= 1.0", components[1].Dependencies[0].ToString());
            });
        }

        public void PatchingSystem_InstalledComponents_Descriptions()
        {
            NoRepoIntegrationTest(() =>
            {
                var installer = new ComponentInstaller
                {
                    ComponentId = "C1",
                    Version = new Version(1, 0),
                    Description = "C1 component",
                    ReleaseDate = new DateTime(2020, 07, 30),
                    Dependencies = null
                };
                var patch = new SnPatch
                {
                    ComponentId = "C1",
                    Version = new Version(2, 0),
                    Description = "C1 patch",
                    ReleaseDate = new DateTime(2020, 07, 31),
                    Boundary = new VersionBoundary
                    {
                        MinVersion = new Version(1, 0)
                    },
                    Dependencies = new[]
                    {
                        Dep("C2", "1.0 <= v <= 1.0"),
                    }
                };

                PackageManager.SavePackage(Manifest.Create(installer), null, true, null);
                PackageManager.SavePackage(Manifest.Create(patch), null, true, null);

                var verInfo = RepositoryVersionInfo.Create(CancellationToken.None);

                var components = verInfo.Components.ToArray();
                Assert.AreEqual(1, components.Length);
                Assert.AreEqual("C1", components[0].ComponentId);
                Assert.AreEqual("2.0", components[0].Version.ToString());
                Assert.AreEqual("C1 component", components[0].Description);
                Assert.AreEqual(1, components[0].Dependencies.Length);
                Assert.AreEqual("C2: 1.0 <= v <= 1.0", components[0].Dependencies[0].ToString());
            });
        }

        /* ===================================================================== INFRASTRUCTURE TESTS */

        public void Patching_System_SaveAndReloadFaultyInstaller()
        {
            NoRepoIntegrationTest(() =>
            {
                var installer = new ComponentInstaller
                {
                    ComponentId = "C7",
                    Version = new Version(1, 0),
                    Description = "C7 description",
                    ReleaseDate = new DateTime(2020, 07, 31),
                    Dependencies = new[]
                    {
                        Dep("C1", "1.0 <= v <= 1.0"),
                        Dep("C2", "1.0 <= v       "),
                        Dep("C3", "1.0 <  v       "),
                        Dep("C4", "       v <= 2.0"),
                        Dep("C5", "       v <  2.0"),
                        Dep("C6", "1.0 <= v <  2.0"),
                    }
                };

                var packages = PackageManager.Storage.LoadInstalledPackagesAsync(CancellationToken.None)
                    .ConfigureAwait(false).GetAwaiter().GetResult().ToArray();
                Assert.IsFalse(packages.Any());

                // SAVE
                PackageManager.SavePackage(Manifest.Create(installer), null, false,
                    new PackagingException(PackagingExceptionType.DependencyNotFound));

                // RELOAD
                packages = PackageManager.Storage.LoadInstalledPackagesAsync(CancellationToken.None)
                    .ConfigureAwait(false).GetAwaiter().GetResult().ToArray();

                // ASSERT
                var patches = packages.Select(PatchManager.CreatePatch).ToArray();
                Assert.AreEqual(1, patches.Length);
                Assert.IsTrue(patches[0].Id > 0);
                Assert.AreEqual("C7", patches[0].ComponentId);
                Assert.AreEqual(new Version(1, 0), patches[0].Version);
                Assert.AreEqual("C7 description", patches[0].Description);
                Assert.AreEqual(new DateTime(2020, 07, 31), patches[0].ReleaseDate);
                var dep = string.Join(",", patches[0].Dependencies).Replace(" ", "");
                Assert.AreEqual("C1:1.0<=v<=1.0,C2:1.0<=v,C3:1.0<v,C4:v<=2.0,C5:v<2.0,C6:1.0<=v<2.0", dep);
                Assert.IsTrue(patches[0].ExecutionDate > DateTime.UtcNow.AddMinutes(-1));
                Assert.IsTrue(patches[0].ExecutionDate <= DateTime.UtcNow);
                Assert.AreEqual(ExecutionResult.Faulty, patches[0].ExecutionResult);
                Assert.IsNotNull(patches[0].ExecutionError);
                var data = patches[0].ExecutionError.Data;
                Assert.IsNotNull(data);
                Assert.IsTrue(data.Contains("ErrorType"));
                Assert.AreEqual(PackagingExceptionType.DependencyNotFound.ToString(), data["ErrorType"].ToString());

            });
        }
        public void Patching_System_ReSaveAndReloadInstaller()
        {
            NoRepoIntegrationTest(() =>
            {
                var installer = new ComponentInstaller
                {
                    ComponentId = "C7",
                    Version = new Version(1, 0),
                    Description = "C7 description",
                    ReleaseDate = new DateTime(2020, 07, 31),
                    Dependencies = new[]
                    {
                        Dep("C1", "1.0 <= v <= 1.0"),
                        Dep("C2", "1.0 <= v       "),
                        Dep("C3", "1.0 <  v       "),
                        Dep("C4", "       v <= 2.0"),
                        Dep("C5", "       v <  2.0"),
                        Dep("C6", "1.0 <= v <  2.0"),
                    }
                };

                var packages = PackageManager.Storage.LoadInstalledPackagesAsync(CancellationToken.None)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
                Assert.IsFalse(packages.Any());

                // SAVE-1
                PackageManager.SavePackage(Manifest.Create(installer), null, false,
                    new PackagingException(PackagingExceptionType.DependencyNotFound));

                // SAVE-2
                PackageManager.SavePackage(Manifest.Create(installer), null, true, null);

                // RELOAD
                packages = PackageManager.Storage.LoadInstalledPackagesAsync(CancellationToken.None)
                    .ConfigureAwait(false).GetAwaiter().GetResult();

                // ASSERT
                var patches = packages.Select(PatchManager.CreatePatch).ToArray();
                Assert.AreEqual(1, patches.Length);
                Assert.IsTrue(patches[0].Id > 0);
                Assert.AreEqual("C7", patches[0].ComponentId);
                Assert.AreEqual(new Version(1, 0), patches[0].Version);
                Assert.AreEqual("C7 description", patches[0].Description);
                Assert.AreEqual(new DateTime(2020, 07, 31), patches[0].ReleaseDate);
                var dep = string.Join(",", patches[0].Dependencies).Replace(" ", "");
                Assert.AreEqual("C1:1.0<=v<=1.0,C2:1.0<=v,C3:1.0<v,C4:v<=2.0,C5:v<2.0,C6:1.0<=v<2.0", dep);
                Assert.IsTrue(patches[0].ExecutionDate > DateTime.UtcNow.AddMinutes(-1));
                Assert.IsTrue(patches[0].ExecutionDate <= DateTime.UtcNow);
                Assert.AreEqual(ExecutionResult.Successful, patches[0].ExecutionResult);
                Assert.IsNull(patches[0].ExecutionError);
            });
        }
        public void Patching_System_SaveAndReloadSnPatch()
        {
            NoRepoIntegrationTest(() =>
            {
                var snPatch = new SnPatch
                {
                    ComponentId = "C7",
                    Version = new Version(2, 0),
                    Description = "C7 description",
                    ReleaseDate = new DateTime(2020, 07, 31),
                    Boundary = ParseBoundary("1.0 <= v <  2.0"),
                    Dependencies = new[]
                    {
                        Dep("C1", "1.0 <= v <= 1.0"),
                        Dep("C2", "1.0 <= v       "),
                        Dep("C3", "1.0 <  v       "),
                        Dep("C4", "       v <= 2.0"),
                        Dep("C5", "       v <  2.0"),
                        Dep("C6", "1.0 <= v <  2.0"),
                    }
                };

                var packages = PackageManager.Storage.LoadInstalledPackagesAsync(CancellationToken.None)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
                Assert.IsFalse(packages.Any());

                // SAVE
                PackageManager.SavePackage(Manifest.Create(snPatch), null, true, null);

                // RELOAD
                packages = PackageManager.Storage.LoadInstalledPackagesAsync(CancellationToken.None)
                    .ConfigureAwait(false).GetAwaiter().GetResult();

                // ASSERT
                var patches = packages.Select(PatchManager.CreatePatch).ToArray();
                var patch = (SnPatch) patches[0];
                Assert.AreEqual(1, patches.Length);
                Assert.IsTrue(patch.Id > 0);
                Assert.AreEqual(PackageType.Patch, patch.Type);
                Assert.AreEqual("C7", patch.ComponentId);
                Assert.AreEqual(new Version(2, 0), patch.Version);
                Assert.AreEqual("1.0 <= v < 2.0", patch.Boundary.ToString());
                Assert.AreEqual("C7 description", patch.Description);
                Assert.AreEqual(new DateTime(2020, 07, 31), patch.ReleaseDate);
                var dep = string.Join(",", patch.Dependencies).Replace(" ", "");
                Assert.AreEqual("C1:1.0<=v<=1.0,C2:1.0<=v,C3:1.0<v,C4:v<=2.0,C5:v<2.0,C6:1.0<=v<2.0", dep);
                Assert.IsTrue(patch.ExecutionDate > DateTime.UtcNow.AddMinutes(-1));
                Assert.IsTrue(patch.ExecutionDate <= DateTime.UtcNow);
                Assert.AreEqual(ExecutionResult.Successful, patch.ExecutionResult);
                Assert.IsNull(patch.ExecutionError);
            });
        }

        public void Patching_System_SaveAndReload_Installer_FaultyBefore()
        {
            NoRepoIntegrationTest(() =>
            {
                var installer = new ComponentInstaller
                {
                    ComponentId = "C7",
                    Version = new Version(1, 0),
                    Description = "C7 description",
                    ReleaseDate = new DateTime(2020, 07, 31),
                };

                var packages = PackageManager.Storage.LoadInstalledPackagesAsync(CancellationToken.None)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
                Assert.IsFalse(packages.Any());

                // SAVE
                PackageManager.SavePackage(Manifest.Create(installer), ExecutionResult.FaultyBefore, null);

                // RELOAD
                packages = PackageManager.Storage.LoadInstalledPackagesAsync(CancellationToken.None)
                    .ConfigureAwait(false).GetAwaiter().GetResult();

                // ASSERT
                var patches = packages.Select(PatchManager.CreatePatch).ToArray();
                Assert.AreEqual(1, patches.Length);
                Assert.IsTrue(patches[0].Id > 0);
                Assert.AreEqual("C7: 1.0", patches[0].ToString());
                Assert.AreEqual(ExecutionResult.FaultyBefore, patches[0].ExecutionResult);
            });
        }
        public void Patching_System_SaveAndReload_Installer_SuccessfulBefore()
        {
            NoRepoIntegrationTest(() =>
            {
                var installer = new ComponentInstaller
                {
                    ComponentId = "C7",
                    Version = new Version(1, 0),
                    Description = "C7 description",
                    ReleaseDate = new DateTime(2020, 07, 31),
                };

                var packages = PackageManager.Storage.LoadInstalledPackagesAsync(CancellationToken.None)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
                Assert.IsFalse(packages.Any());

                // SAVE
                PackageManager.SavePackage(Manifest.Create(installer), ExecutionResult.SuccessfulBefore, null);

                // RELOAD
                packages = PackageManager.Storage.LoadInstalledPackagesAsync(CancellationToken.None)
                    .ConfigureAwait(false).GetAwaiter().GetResult();

                // ASSERT
                var patches = packages.Select(PatchManager.CreatePatch).ToArray();
                Assert.AreEqual(1, patches.Length);
                Assert.IsTrue(patches[0].Id > 0);
                Assert.AreEqual("C7: 1.0", patches[0].ToString());
                Assert.AreEqual(ExecutionResult.SuccessfulBefore, patches[0].ExecutionResult);

                // ACTION-2 Load components
                var installed = PackageManager.Storage
                    .LoadInstalledComponentsAsync(CancellationToken.None)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
                var incomplete = PackageManager.Storage
                    .LoadIncompleteComponentsAsync(CancellationToken.None)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
                var components = SnComponentDescriptor.CreateComponents(installed, incomplete);
                Assert.AreEqual("C7v(1.0,,SuccessfulBefore)", ComponentsToStringWithResult(components));
            });
        }
        public void Patching_System_SaveAndReload_SnPatch_FaultyBefore()
        {
            NoRepoIntegrationTest(() =>
            {
                var snPatch = new SnPatch
                {
                    ComponentId = "C7",
                    Version = new Version(2, 0),
                    Description = "C7 description",
                    ReleaseDate = new DateTime(2020, 07, 31),
                    Boundary = ParseBoundary("1.0 <= v <  2.0"),
                };

                var packages = PackageManager.Storage.LoadInstalledPackagesAsync(CancellationToken.None)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
                Assert.IsFalse(packages.Any());

                // SAVE
                PackageManager.SavePackage(Manifest.Create(snPatch), ExecutionResult.FaultyBefore, null);

                // RELOAD
                packages = PackageManager.Storage.LoadInstalledPackagesAsync(CancellationToken.None)
                    .ConfigureAwait(false).GetAwaiter().GetResult();

                // ASSERT
                var patches = packages.Select(PatchManager.CreatePatch).ToArray();
                var patch = (SnPatch)patches[0];
                Assert.AreEqual(1, patches.Length);
                Assert.IsTrue(patch.Id > 0);
                Assert.AreEqual("C7: 1.0 <= v < 2.0 --> 2.0", patch.ToString());
                Assert.AreEqual(ExecutionResult.FaultyBefore, patch.ExecutionResult);
            });
        }
        public void Patching_System_SaveAndReload_SnPatch_SuccessfulBefore()
        {
            NoRepoIntegrationTest(() =>
            {
                var snPatch = new SnPatch
                {
                    ComponentId = "C7",
                    Version = new Version(2, 0),
                    Description = "C7 description",
                    ReleaseDate = new DateTime(2020, 07, 31),
                    Boundary = ParseBoundary("1.0 <= v <  2.0"),
                };

                var packages = PackageManager.Storage.LoadInstalledPackagesAsync(CancellationToken.None)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
                Assert.IsFalse(packages.Any());

                // SAVE
                PackageManager.SavePackage(Manifest.Create(snPatch), ExecutionResult.SuccessfulBefore, null);

                // RELOAD
                packages = PackageManager.Storage.LoadInstalledPackagesAsync(CancellationToken.None)
                    .ConfigureAwait(false).GetAwaiter().GetResult();

                // ASSERT
                var patches = packages.Select(PatchManager.CreatePatch).ToArray();
                var patch = (SnPatch)patches[0];
                Assert.AreEqual(1, patches.Length);
                Assert.IsTrue(patch.Id > 0);
                Assert.AreEqual("C7: 1.0 <= v < 2.0 --> 2.0", patch.ToString());
                Assert.AreEqual(ExecutionResult.SuccessfulBefore, patch.ExecutionResult);
            });
        }
        public void Patching_System_SaveAndReloadExecutionError()
        {
            NoRepoIntegrationTest(() =>
            {
                var installer = Inst("C7", "1.0");

                var packages = PackageManager.Storage.LoadInstalledPackagesAsync(CancellationToken.None)
                    .ConfigureAwait(false).GetAwaiter().GetResult().ToArray();
                Assert.IsFalse(packages.Any());

                // SAVE
                PackageManager.SavePackage(Manifest.Create(installer), null, false,
                    new PackagingException(PackagingExceptionType.DependencyNotFound));

                // RELOAD
                packages = PackageManager.Storage.LoadInstalledPackagesAsync(CancellationToken.None)
                    .ConfigureAwait(false).GetAwaiter().GetResult().ToArray();

                // ASSERT
                var data = packages[0].ExecutionError.Data;
                Assert.IsNotNull(data);
                Assert.IsTrue(data.Contains("ErrorType"));
                Assert.AreEqual(PackagingExceptionType.DependencyNotFound.ToString(), data["ErrorType"].ToString());
            });
        }

        public void Patching_System_InstalledComponents()
        {
            NoRepoIntegrationTest(() =>
            {
                var installer1 = new ComponentInstaller
                {
                    ComponentId = "C1",
                    Version = new Version(1, 0),
                    Description = "C1 description",
                    ReleaseDate = new DateTime(2020, 07, 30),
                    Dependencies = null
                };
                var installer2 = new ComponentInstaller
                {
                    ComponentId = "C2",
                    Version = new Version(2, 0),
                    Description = "C2 description",
                    ReleaseDate = new DateTime(2020, 07, 31),
                    Dependencies = new[]
                    {
                        Dep("C1", "1.0 <= v <= 1.0"),
                    }
                };
                PackageManager.SavePackage(Manifest.Create(installer1), null, true, null);
                PackageManager.SavePackage(Manifest.Create(installer2), null, true, null);

                var verInfo = RepositoryVersionInfo.Create(CancellationToken.None);

                var components = verInfo.Components.ToArray();
                Assert.AreEqual(2, components.Length);

                Assert.AreEqual("C1", components[0].ComponentId);
                Assert.AreEqual("C2", components[1].ComponentId);

                Assert.AreEqual("1.0", components[0].Version.ToString());
                Assert.AreEqual("2.0", components[1].Version.ToString());

                Assert.AreEqual("C1 description", components[0].Description);
                Assert.AreEqual("C2 description", components[1].Description);

                Assert.AreEqual(0, components[0].Dependencies.Length);
                Assert.AreEqual(1, components[1].Dependencies.Length);
                Assert.AreEqual("C1: 1.0 <= v <= 1.0", components[1].Dependencies[0].ToString());
            });
        }
        public void Patching_System_InstalledComponents_Descriptions()
        {
            NoRepoIntegrationTest(() =>
            {
                var installer = new ComponentInstaller
                {
                    ComponentId = "C1",
                    Version = new Version(1, 0),
                    Description = "C1 component",
                    ReleaseDate = new DateTime(2020, 07, 30),
                    Dependencies = null
                };
                var patch = new SnPatch
                {
                    ComponentId = "C1",
                    Version = new Version(2, 0),
                    Description = "C1 patch",
                    ReleaseDate = new DateTime(2020, 07, 31),
                    Boundary = new VersionBoundary
                    {
                        MinVersion = new Version(1, 0)
                    },
                    Dependencies = new[]
                    {
                        Dep("C2", "1.0 <= v <= 1.0"),
                    }
                };

                PackageManager.SavePackage(Manifest.Create(installer), null, true, null);
                PackageManager.SavePackage(Manifest.Create(patch), null, true, null);

                var verInfo = RepositoryVersionInfo.Create(CancellationToken.None);

                var components = verInfo.Components.ToArray();
                Assert.AreEqual(1, components.Length);
                Assert.AreEqual("C1", components[0].ComponentId);
                Assert.AreEqual("2.0", components[0].Version.ToString());
                Assert.AreEqual("C1 component", components[0].Description);
                Assert.AreEqual(1, components[0].Dependencies.Length);
                Assert.AreEqual("C2: 1.0 <= v <= 1.0", components[0].Dependencies[0].ToString());
            });
        }

        public void Patching_System_LoadInstalledComponents()
        {
            NoRepoIntegrationTest(() =>
            {
                // Installers only
                SavePackage(Inst("C01", "1.0"), ExecutionResult.Unfinished);
                SavePackage(Inst("C02", "1.0"), ExecutionResult.FaultyBefore);
                SavePackage(Inst("C03", "1.0"), ExecutionResult.SuccessfulBefore);
                SavePackage(Inst("C04", "1.0"), ExecutionResult.Faulty);
                SavePackage(Inst("C05", "1.0"), ExecutionResult.Successful);

                // Installers and patches
                SavePackage(Inst("C06", "1.0"), ExecutionResult.Successful);
                SavePackage(Patch("C06", "1.0 <= v < 2.0", "2.0"), ExecutionResult.Unfinished);
                SavePackage(Inst("C07", "1.0"), ExecutionResult.Successful);
                SavePackage(Patch("C07", "1.0 <= v < 2.0", "2.0"), ExecutionResult.FaultyBefore);
                SavePackage(Inst("C08", "1.0"), ExecutionResult.Successful);
                SavePackage(Patch("C08", "1.0 <= v < 2.0", "2.0"), ExecutionResult.SuccessfulBefore);
                SavePackage(Inst("C09", "1.0"), ExecutionResult.Successful);
                SavePackage(Patch("C09", "1.0 <= v < 2.0", "2.0"), ExecutionResult.Faulty);
                SavePackage(Inst("C10", "1.0"), ExecutionResult.Successful);
                SavePackage(Patch("C10", "1.0 <= v < 2.0", "2.0"), ExecutionResult.Successful);

                // ACTION
                var installed = PackageManager.Storage?
                    .LoadInstalledComponentsAsync(CancellationToken.None)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
                var faulty = PackageManager.Storage?
                    .LoadIncompleteComponentsAsync(CancellationToken.None)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
                var currentComponents = SnComponentDescriptor.CreateComponents(installed, faulty);

                // ASSERT
                Assert.AreEqual("C01v(1.0,,Unfinished) " +
                                "C02v(1.0,,FaultyBefore) " +
                                "C03v(1.0,,SuccessfulBefore) " +
                                "C04v(,1.0,Faulty) " +
                                "C05v1.0(,,Successful) " +
                                "C06v1.0(2.0,,Unfinished) " +
                                "C07v1.0(2.0,,FaultyBefore) " +
                                "C08v1.0(2.0,,SuccessfulBefore) " +
                                "C09v1.0(,2.0,Faulty) " +
                                "C10v2.0(,,Successful)",
                    ComponentsToStringWithResult(currentComponents));
            });
        }

        public void Patching_System_Load_Issue1174()
        {
            NoRepoIntegrationTest(() =>
            {
                SavePackage(Inst("REF", "1.0"), ExecutionResult.Successful, true);
                SavePackage(Inst("C01", "1.0"), ExecutionResult.FaultyBefore, true);
                SavePackage(Inst("C01", "1.0"), ExecutionResult.Successful, true);

                // ACTION
                var installed = PackageManager.Storage?
                    .LoadInstalledComponentsAsync(CancellationToken.None)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
                var faulty = PackageManager.Storage?
                    .LoadIncompleteComponentsAsync(CancellationToken.None)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
                var currentComponents = SnComponentDescriptor.CreateComponents(installed, faulty);

                // ASSERT
                Assert.AreEqual("C01v1.0(,,Successful) REFv1.0(,,Successful)", ComponentsToStringWithResult(currentComponents));
            });
        }
        public void Patching_System_Load_Issue1174_All_Installers()
        {
            NoRepoIntegrationTest(() =>
            {
                //  S  F Sb Fb  U  ->              -> result
                //  0  0  0  0  0                        -
                //  0  0  0  0  1      U                 U
                //  0  0  0  1  0      Fb               Fb
                //  0  0  0  1  1      U, Fb            Fb
                // ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~
                //  1  1  1  0  0      Sb, F, S          S
                //  1  1  1  0  1      U, Sb, F, S       S
                //  1  1  1  1  0      F, Sb, F, S       S
                //  1  1  1  1  1      U, Fb, Sb, F, S   S

                var results = new[] {ExecutionResult.Unfinished, ExecutionResult.FaultyBefore,
                ExecutionResult.SuccessfulBefore, ExecutionResult.Faulty, ExecutionResult.Successful};

                for (var id = 1; id < 32; id++)
                    for (var r = 0; r < results.Length; r++)
                        if (0 != (id & 1 << r))
                            SavePackage(Inst($"C{id:0#}", "1.0"), results[r], true);

                // ACTION
                var installed = PackageManager.Storage?
                    .LoadInstalledComponentsAsync(CancellationToken.None)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
                var faulty = PackageManager.Storage?
                    .LoadIncompleteComponentsAsync(CancellationToken.None)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
                var currentComponents = SnComponentDescriptor.CreateComponents(installed, faulty)
                    .OrderBy(x => x.ComponentId).ToArray();

                // ASSERT
                for (var id = 1; id < 32; id++)
                {
                    var expectedResult = results[Convert.ToInt32(Math.Floor(Math.Log(id, 2)))];
                    var comp = currentComponents[id - 1];

                    var expectedVersion = new Version(1, 0);
                    Assert.AreEqual(expectedResult, comp.State);
                    switch (comp.State)
                    {
                        case ExecutionResult.Unfinished:
                        case ExecutionResult.FaultyBefore:
                        case ExecutionResult.SuccessfulBefore:
                            Assert.AreEqual(expectedVersion, comp.TempVersionBefore);
                            Assert.IsNull(comp.TempVersionAfter);
                            Assert.IsNull(comp.Version);
                            break;
                        case ExecutionResult.Faulty:
                            Assert.IsNull(comp.TempVersionBefore);
                            Assert.AreEqual(expectedVersion, comp.TempVersionAfter);
                            Assert.IsNull(comp.Version);
                            break;
                        case ExecutionResult.Successful:
                            Assert.IsNull(comp.TempVersionBefore);
                            Assert.IsNull(comp.TempVersionAfter);
                            Assert.AreEqual(expectedVersion, comp.Version);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                /* equivalent assertion
                // ASSERT
                Assert.AreEqual("C01v(1.0,,Unfinished)" +
                                " C02v(1.0,,FaultyBefore)" +
                                " C03v(1.0,,FaultyBefore)" +
                                " C04v(1.0,,SuccessfulBefore)" +
                                " C05v(1.0,,SuccessfulBefore)" +
                                " C06v(1.0,,SuccessfulBefore)" +
                                " C07v(1.0,,SuccessfulBefore)" +
                                " C08v(,1.0,Faulty)" +
                                " C09v(,1.0,Faulty)" +
                                " C10v(,1.0,Faulty)" +
                                " C11v(,1.0,Faulty)" +
                                " C12v(,1.0,Faulty)" +
                                " C13v(,1.0,Faulty)" +
                                " C14v(,1.0,Faulty)" +
                                " C15v(,1.0,Faulty)" +
                                " C16v1.0(,,Successful)" +
                                " C17v1.0(,,Successful)" +
                                " C18v1.0(,,Successful)" +
                                " C19v1.0(,,Successful)" +
                                " C20v1.0(,,Successful)" +
                                " C21v1.0(,,Successful)" +
                                " C22v1.0(,,Successful)" +
                                " C23v1.0(,,Successful)" +
                                " C24v1.0(,,Successful)" +
                                " C25v1.0(,,Successful)" +
                                " C26v1.0(,,Successful)" +
                                " C27v1.0(,,Successful)" +
                                " C28v1.0(,,Successful)" +
                                " C29v1.0(,,Successful)" +
                                " C30v1.0(,,Successful)" +
                                " C31v1.0(,,Successful)", ComponentsToStringWithResult(currentComponents));
                */
            });
        }
        public void Patching_System_Load_Issue1174_All_Patches()
        {
            NoRepoIntegrationTest(() =>
            {
                //  S  F Sb Fb  U  ->              -> result
                //  0  0  0  0  0                        -
                //  0  0  0  0  1      U                 U
                //  0  0  0  1  0      Fb               Fb
                //  0  0  0  1  1      U, Fb            Fb
                // ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~
                //  1  1  1  0  0      Sb, F, S          S
                //  1  1  1  0  1      U, Sb, F, S       S
                //  1  1  1  1  0      F, Sb, F, S       S
                //  1  1  1  1  1      U, Fb, Sb, F, S   S

                var results = new[] {ExecutionResult.Unfinished, ExecutionResult.FaultyBefore,
                ExecutionResult.SuccessfulBefore, ExecutionResult.Faulty, ExecutionResult.Successful};

                for (var id = 1; id < 32; id++)
                    SavePackage(Inst($"C{id:0#}", "1.0"), ExecutionResult.Successful, true);
                for (var id = 1; id < 32; id++)
                    for (var r = 0; r < results.Length; r++)
                        if (0 != (id & 1 << r))
                            SavePackage(Patch($"C{id:0#}", "1.0 <= v < 2.0", "2.0"), results[r], true);

                // ACTION
                var installed = PackageManager.Storage?
                    .LoadInstalledComponentsAsync(CancellationToken.None)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
                var faulty = PackageManager.Storage?
                    .LoadIncompleteComponentsAsync(CancellationToken.None)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
                var currentComponents = SnComponentDescriptor.CreateComponents(installed, faulty)
                    .OrderBy(x => x.ComponentId).ToArray();

                // ASSERT
                for (var id = 1; id < 32; id++)
                {
                    var expectedResult = results[Convert.ToInt32(Math.Floor(Math.Log(id, 2)))];
                    var comp = currentComponents[id - 1];

                    var expectedOldVersion = new Version(1, 0);
                    var expectedVersion = new Version(2, 0);
                    Assert.AreEqual(expectedResult, comp.State);
                    switch (comp.State)
                    {
                        case ExecutionResult.Unfinished:
                        case ExecutionResult.FaultyBefore:
                        case ExecutionResult.SuccessfulBefore:
                            Assert.AreEqual(expectedVersion, comp.TempVersionBefore);
                            Assert.IsNull(comp.TempVersionAfter);
                            Assert.AreEqual(expectedOldVersion, comp.Version);
                            break;
                        case ExecutionResult.Faulty:
                            Assert.IsNull(comp.TempVersionBefore);
                            Assert.AreEqual(expectedVersion, comp.TempVersionAfter);
                            Assert.AreEqual(expectedOldVersion, comp.Version);
                            break;
                        case ExecutionResult.Successful:
                            Assert.IsNull(comp.TempVersionBefore);
                            Assert.IsNull(comp.TempVersionAfter);
                            Assert.AreEqual(expectedVersion, comp.Version);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                /* equivalent assertion
                // ASSERT
                Assert.AreEqual("C01v(1.0,,Unfinished)" +
                                " C02v(1.0,,FaultyBefore)" +
                                " C03v(1.0,,FaultyBefore)" +
                                " C04v(1.0,,SuccessfulBefore)" +
                                " C05v(1.0,,SuccessfulBefore)" +
                                " C06v(1.0,,SuccessfulBefore)" +
                                " C07v(1.0,,SuccessfulBefore)" +
                                " C08v(,1.0,Faulty)" +
                                " C09v(,1.0,Faulty)" +
                                " C10v(,1.0,Faulty)" +
                                " C11v(,1.0,Faulty)" +
                                " C12v(,1.0,Faulty)" +
                                " C13v(,1.0,Faulty)" +
                                " C14v(,1.0,Faulty)" +
                                " C15v(,1.0,Faulty)" +
                                " C16v1.0(,,Successful)" +
                                " C17v1.0(,,Successful)" +
                                " C18v1.0(,,Successful)" +
                                " C19v1.0(,,Successful)" +
                                " C20v1.0(,,Successful)" +
                                " C21v1.0(,,Successful)" +
                                " C22v1.0(,,Successful)" +
                                " C23v1.0(,,Successful)" +
                                " C24v1.0(,,Successful)" +
                                " C25v1.0(,,Successful)" +
                                " C26v1.0(,,Successful)" +
                                " C27v1.0(,,Successful)" +
                                " C28v1.0(,,Successful)" +
                                " C29v1.0(,,Successful)" +
                                " C30v1.0(,,Successful)" +
                                " C31v1.0(,,Successful)", ComponentsToStringWithResult(currentComponents));
                */
            });
        }

        /* ===================================================================== EXECUTION TESTS */

        public void Patching_Exec_NoAction()
        {
            NoRepoIntegrationTest(() =>
            {
                // Faulty execution blocks the following patches on the same component.
                var packages = new List<Package[]>();
                var log = new List<PatchExecutionLogRecord>();
                void Log(PatchExecutionLogRecord record) { packages.Add(LoadPackages()); log.Add(record); }
                var executed = new List<ISnPatch>();
                void Exec(PatchExecutionContext ctx) { executed.Add(ctx.CurrentPatch); }

                var installed = new List<SnComponentDescriptor>();
                var candidates = new List<ISnPatch>
                {
                    Patch("C1", "1.0 <= v < 2.0", "v2.0"),
                    Inst("C1", "v2.0"),
                };

                // ACTION BEFORE
                var pm = new PatchManager(null, Log);
                pm.ExecuteOnBefore(candidates, installed, false);

                // ASSERT BEFORE
                Assert.AreEqual(2, candidates.Count);
                Assert.AreEqual("C1v(2.0,,SuccessfulBefore)", ComponentsToStringWithResult(installed));

                // ACTION AFTER
                pm.ExecuteOnAfter(candidates, installed, false);

                // ASSERT AFTER
                Assert.AreEqual(0, candidates.Count);
                Assert.AreEqual("C1v2.0(2.0,2.0,Successful)",
                    ComponentsToStringWithResult(installed));
                Assert.AreEqual("[C1: 2.0] OnBeforeActionStarts.|" +
                                "[C1: 2.0] OnBeforeActionFinished.|" +
                                "[C1: 2.0] OnAfterActionStarts.|" +
                                "[C1: 2.0] OnAfterActionFinished.",
                    string.Join("|", log.Select(x => x.ToString(false))));
                Assert.AreEqual("", ErrorsToString(pm.Errors));
                Assert.AreEqual(4, packages.Count);
                Assert.AreEqual("1, C1: Install Successful, 2.0",
                    PackagesToString(packages[3]));
            });
        }
        public void Patching_Exec_NoAfterAction()
        {
            NoRepoIntegrationTest(() =>
            {
                // Faulty execution blocks the following patches on the same component.
                var packages = new List<Package[]>();
                var log = new List<PatchExecutionLogRecord>();
                void Log(PatchExecutionLogRecord record) { packages.Add(LoadPackages()); log.Add(record); }
                var executed = new List<ISnPatch>();
                void Exec(PatchExecutionContext ctx) { executed.Add(ctx.CurrentPatch); }

                var installed = new List<SnComponentDescriptor>();
                var candidates = new List<ISnPatch>
                {
                    Patch("C1", "1.0 <= v < 2.0", "v2.0", Exec, null),
                    Inst("C1", "v2.0", Exec, null),
                };

                // ACTION BEFORE
                var pm = new PatchManager(null, Log);
                pm.ExecuteOnBefore(candidates, installed, false);

                // ASSERT BEFORE
                Assert.AreEqual(2, candidates.Count);
                Assert.AreEqual("C1v(2.0,,SuccessfulBefore)", ComponentsToStringWithResult(installed));

                // ACTION AFTER
                pm.ExecuteOnAfter(candidates, installed, false);

                // ASSERT AFTER
                Assert.AreEqual(0, candidates.Count);
                Assert.AreEqual("C1v2.0(2.0,2.0,Successful)",
                    ComponentsToStringWithResult(installed));
                Assert.AreEqual("[C1: 2.0] OnBeforeActionStarts.|" +
                                "[C1: 2.0] OnBeforeActionFinished.|" +
                                "[C1: 2.0] OnAfterActionStarts.|" +
                                "[C1: 2.0] OnAfterActionFinished.",
                    string.Join("|", log.Select(x => x.ToString(false))));
                Assert.AreEqual("", ErrorsToString(pm.Errors));
                Assert.AreEqual(4, packages.Count);
                Assert.AreEqual("1, C1: Install Successful, 2.0",
                    PackagesToString(packages[3]));
            });
        }

        public void Patching_Exec_InstallOne_Success()
        {
            NoRepoIntegrationTest(() =>
            {
                var packages = new List<Package[]>();
                var log = new List<PatchExecutionLogRecord>();
                void Log(PatchExecutionLogRecord record) { packages.Add(LoadPackages()); log.Add(record); }
                var executed = new List<ISnPatch>();
                void Exec(PatchExecutionContext ctx) { executed.Add(ctx.CurrentPatch); }

                var installed = new List<SnComponentDescriptor>();
                var candidates = new List<ISnPatch>
                {
                    Inst("C1", "v1.0", Exec, Exec),
                };

                // ACTION BEFORE
                var pm = new PatchManager(null, Log);
                pm.ExecuteOnBefore(candidates, installed, false);

                // ASSERT BEFORE
                Assert.AreEqual(1, candidates.Count);
                Assert.AreEqual("C1v(1.0,,SuccessfulBefore)", ComponentsToStringWithResult(installed));

                // ACTION AFTER
                pm.ExecuteOnAfter(candidates, installed, false);

                // ASSERT AFTER
                Assert.AreEqual(0, candidates.Count);
                Assert.AreEqual("C1v1.0(1.0,1.0,Successful)", ComponentsToStringWithResult(installed));
                Assert.AreEqual("[C1: 1.0] OnBeforeActionStarts.|" +
                                "[C1: 1.0] OnBeforeActionFinished.|" +
                                "[C1: 1.0] OnAfterActionStarts.|" +
                                "[C1: 1.0] OnAfterActionFinished.",
                    string.Join("|", log.Select(x => x.ToString(false))));
                Assert.AreEqual("", ErrorsToString(pm.Errors));
                Assert.AreEqual(4, packages.Count);
                Assert.AreEqual("1, C1: Install Unfinished, 1.0", PackagesToString(packages[0]));
                Assert.AreEqual("1, C1: Install SuccessfulBefore, 1.0", PackagesToString(packages[1]));
                Assert.AreEqual("1, C1: Install SuccessfulBefore, 1.0", PackagesToString(packages[2]));
                Assert.AreEqual("1, C1: Install Successful, 1.0", PackagesToString(packages[3]));
            });
        }
        public void Patching_Exec_InstallOne_FaultyBefore()
        {
            NoRepoIntegrationTest(() =>
            {
                var packages = new List<Package[]>();
                var log = new List<PatchExecutionLogRecord>();
                void Log(PatchExecutionLogRecord record) { packages.Add(LoadPackages()); log.Add(record); }
                var executed = new List<ISnPatch>();
                void Exec(PatchExecutionContext ctx) { executed.Add(ctx.CurrentPatch); }


                var installed = new List<SnComponentDescriptor>();
                var candidates = new List<ISnPatch>
                {
                    Inst("C1", "v1.0", Error, Exec),
                };

                // ACTION BEFORE
                var pm = new PatchManager(null, Log);
                pm.ExecuteOnBefore(candidates, installed, false);

                // ASSERT BEFORE
                Assert.AreEqual(0, candidates.Count);
                Assert.AreEqual("C1v(1.0,,FaultyBefore)", ComponentsToStringWithResult(installed));

                // ACTION AFTER
                pm.ExecuteOnAfter(candidates, installed, false);

                // ASSERT AFTER
                Assert.AreEqual(0, candidates.Count);
                Assert.AreEqual("C1v(1.0,,FaultyBefore)", ComponentsToStringWithResult(installed));
                Assert.AreEqual("[C1: 1.0] OnBeforeActionStarts.|" +
                                "[C1: 1.0] ExecutionErrorOnBefore.",
                    string.Join("|", log.Select(x => x.ToString(false))));
                Assert.AreEqual("ExecutionErrorOnBefore C1: 1.0", ErrorsToString(pm.Errors));
                Assert.AreEqual(2, packages.Count);
                Assert.AreEqual("1, C1: Install Unfinished, 1.0", PackagesToString(packages[0]));
                Assert.AreEqual("1, C1: Install FaultyBefore, 1.0", PackagesToString(packages[1]));
            });
        }
        public void Patching_Exec_PatchOne_Success()
        {
            NoRepoIntegrationTest(() =>
            {
                var packages = new List<Package[]>();
                var log = new List<PatchExecutionLogRecord>();
                void Log(PatchExecutionLogRecord record) { packages.Add(LoadPackages()); log.Add(record); }
                var executed = new List<ISnPatch>();
                void Exec(PatchExecutionContext ctx) { executed.Add(ctx.CurrentPatch); }

                var installed = new List<SnComponentDescriptor>();
                var candidates = new List<ISnPatch>
            {
                Inst("C1", "v1.0", Exec, Exec),
                Patch("C1", "1.0 <= v < 2.0", "v2.0", Exec, Exec),
            };

                // ACTION BEFORE
                var pm = new PatchManager(null, Log);
                pm.ExecuteOnBefore(candidates, installed, false);

                // ASSERT BEFORE
                Assert.AreEqual(2, candidates.Count);
                Assert.AreEqual("C1v(2.0,,SuccessfulBefore)", ComponentsToStringWithResult(installed));

                // ACTION AFTER
                pm.ExecuteOnAfter(candidates, installed, false);

                // ASSERT AFTER
                Assert.AreEqual(0, candidates.Count);
                Assert.AreEqual("C1v2.0(2.0,2.0,Successful)", ComponentsToStringWithResult(installed));
                Assert.AreEqual("[C1: 1.0] OnBeforeActionStarts.|" +
                                "[C1: 1.0] OnBeforeActionFinished.|" +
                                "[C1: 1.0 <= v < 2.0 --> 2.0] OnBeforeActionStarts.|" +
                                "[C1: 1.0 <= v < 2.0 --> 2.0] OnBeforeActionFinished.|" +
                                "[C1: 1.0] OnAfterActionStarts.|" +
                                "[C1: 1.0] OnAfterActionFinished.|" +
                                "[C1: 1.0 <= v < 2.0 --> 2.0] OnAfterActionStarts.|" +
                                "[C1: 1.0 <= v < 2.0 --> 2.0] OnAfterActionFinished.",
                    string.Join("|", log.Select(x => x.ToString(false))));
                Assert.AreEqual("", ErrorsToString(pm.Errors));
                Assert.AreEqual(8, packages.Count);
                Assert.AreEqual("1, C1: Install SuccessfulBefore, 1.0|" +
                                "2, C1: Patch SuccessfulBefore, 2.0", PackagesToString(packages[4]));
                Assert.AreEqual("1, C1: Install Successful, 1.0|" +
                                "2, C1: Patch SuccessfulBefore, 2.0", PackagesToString(packages[5]));
                Assert.AreEqual("1, C1: Install Successful, 1.0|" +
                                "2, C1: Patch SuccessfulBefore, 2.0", PackagesToString(packages[6]));
                Assert.AreEqual("1, C1: Install Successful, 1.0|" +
                                "2, C1: Patch Successful, 2.0", PackagesToString(packages[7]));
            });
        }
        public void Patching_Exec_PatchOne_Faulty()
        {
            NoRepoIntegrationTest(() =>
            {
                var packages = new List<Package[]>();
                var log = new List<PatchExecutionLogRecord>();
                void Log(PatchExecutionLogRecord record) { packages.Add(LoadPackages()); log.Add(record); }
                var executed = new List<ISnPatch>();
                void Exec(PatchExecutionContext ctx) { executed.Add(ctx.CurrentPatch); }

                var installed = new List<SnComponentDescriptor>();
                var candidates = new List<ISnPatch>
            {
                Inst("C1", "v1.0", Exec, Exec),
                Patch("C1", "1.0 <= v < 2.0", "v2.0", Exec, Error),
            };

                // ACTION BEFORE
                var pm = new PatchManager(null, Log);
                pm.ExecuteOnBefore(candidates, installed, false);

                // ASSERT BEFORE
                Assert.AreEqual(2, candidates.Count);
                Assert.AreEqual("C1v(2.0,,SuccessfulBefore)", ComponentsToStringWithResult(installed));

                // ACTION AFTER
                pm.ExecuteOnAfter(candidates, installed, false);

                // ASSERT AFTER
                Assert.AreEqual(0, candidates.Count);
                Assert.AreEqual("C1v1.0(2.0,2.0,Faulty)", ComponentsToStringWithResult(installed));
                Assert.AreEqual("[C1: 1.0] OnBeforeActionStarts.|" +
                                "[C1: 1.0] OnBeforeActionFinished.|" +
                                "[C1: 1.0 <= v < 2.0 --> 2.0] OnBeforeActionStarts.|" +
                                "[C1: 1.0 <= v < 2.0 --> 2.0] OnBeforeActionFinished.|" +
                                "[C1: 1.0] OnAfterActionStarts.|" +
                                "[C1: 1.0] OnAfterActionFinished.|" +
                                "[C1: 1.0 <= v < 2.0 --> 2.0] OnAfterActionStarts.|" +
                                "[C1: 1.0 <= v < 2.0 --> 2.0] ExecutionError.",
                    string.Join("|", log.Select(x => x.ToString(false))));
                Assert.AreEqual("ExecutionErrorOnAfter C1: 1.0 <= v < 2.0 --> 2.0", ErrorsToString(pm.Errors));
                Assert.AreEqual(8, packages.Count);
                Assert.AreEqual("1, C1: Install SuccessfulBefore, 1.0|2, C1: Patch SuccessfulBefore, 2.0", PackagesToString(packages[4]));
                Assert.AreEqual("1, C1: Install Successful, 1.0|2, C1: Patch SuccessfulBefore, 2.0", PackagesToString(packages[5]));
                Assert.AreEqual("1, C1: Install Successful, 1.0|2, C1: Patch SuccessfulBefore, 2.0", PackagesToString(packages[6]));
                Assert.AreEqual("1, C1: Install Successful, 1.0|2, C1: Patch Faulty, 2.0", PackagesToString(packages[7]));
            });
        }
        public void Patching_Exec_SkipPatch_FaultyInstaller()
        {
            NoRepoIntegrationTest(() =>
            {
                // Faulty execution blocks the following patches on the same component.
                var packages = new List<Package[]>();
                var log = new List<PatchExecutionLogRecord>();
                void Log(PatchExecutionLogRecord record) { packages.Add(LoadPackages()); log.Add(record); }
                var executed = new List<ISnPatch>();
                void Exec(PatchExecutionContext ctx) { executed.Add(ctx.CurrentPatch); }

                var installed = new List<SnComponentDescriptor>();
                var candidates = new List<ISnPatch>
            {
                Inst("C1", "v1.0",Exec, Error),
                Patch("C1", "1.0 <= v < 2.0", "v2.0",Exec, Exec),
            };

                // ACTION BEFORE
                var pm = new PatchManager(null, Log);
                pm.ExecuteOnBefore(candidates, installed, false);

                // ASSERT BEFORE
                Assert.AreEqual(2, candidates.Count);
                Assert.AreEqual("C1v(2.0,,SuccessfulBefore)", ComponentsToStringWithResult(installed));

                // ACTION AFTER
                pm.ExecuteOnAfter(candidates, installed, false);

                // ASSERT AFTER
                Assert.AreEqual(0, candidates.Count);
                Assert.AreEqual("C1v(2.0,1.0,Faulty)", ComponentsToStringWithResult(installed));
                Assert.AreEqual("[C1: 1.0] OnBeforeActionStarts.|" +
                                "[C1: 1.0] OnBeforeActionFinished.|" +
                                "[C1: 1.0 <= v < 2.0 --> 2.0] OnBeforeActionStarts.|" +
                                "[C1: 1.0 <= v < 2.0 --> 2.0] OnBeforeActionFinished.|" +
                                "[C1: 1.0] OnAfterActionStarts.|" +
                                "[C1: 1.0] ExecutionError.|" +
                                "[C1: 1.0 <= v < 2.0 --> 2.0] CannotExecuteMissingVersion.",
                    string.Join("|", log.Select(x => x.ToString(false))));
                Assert.AreEqual("ExecutionErrorOnAfter C1: 1.0|" +
                                "MissingVersion C1: 1.0 <= v < 2.0 --> 2.0", ErrorsToString(pm.Errors));
                Assert.AreEqual(7, packages.Count);
                Assert.AreEqual("1, C1: Install SuccessfulBefore, 1.0|2, C1: Patch SuccessfulBefore, 2.0", PackagesToString(packages[3]));
                Assert.AreEqual("1, C1: Install SuccessfulBefore, 1.0|2, C1: Patch SuccessfulBefore, 2.0", PackagesToString(packages[4]));
                Assert.AreEqual("1, C1: Install Faulty, 1.0|2, C1: Patch SuccessfulBefore, 2.0", PackagesToString(packages[5]));
                Assert.AreEqual("1, C1: Install Faulty, 1.0|2, C1: Patch SuccessfulBefore, 2.0", PackagesToString(packages[6]));
            });
        }
        public void Patching_Exec_SkipPatch_FaultySnPatch()
        {
            NoRepoIntegrationTest(() =>
            {
                // Faulty execution blocks the following patches on the same component.
                var packages = new List<Package[]>();
                var log = new List<PatchExecutionLogRecord>();
                void Log(PatchExecutionLogRecord record) { packages.Add(LoadPackages()); log.Add(record); }
                var executed = new List<ISnPatch>();
                void Exec(PatchExecutionContext ctx) { executed.Add(ctx.CurrentPatch); }

                var installed = new List<SnComponentDescriptor>();
                var candidates = new List<ISnPatch>
            {
                Inst("C1", "v1.0", Exec, Exec),
                Patch("C1", "1.0 <= v < 2.0", "v2.0",Exec, Error),
                Patch("C1", "2.0 <= v < 3.0", "v3.0", Exec, Exec),
            };

                // ACTION BEFORE
                var pm = new PatchManager(null, Log);
                pm.ExecuteOnBefore(candidates, installed, false);

                // ASSERT BEFORE
                Assert.AreEqual(3, candidates.Count);
                Assert.AreEqual("C1v(3.0,,SuccessfulBefore)", ComponentsToStringWithResult(installed));

                // ACTION AFTER
                pm.ExecuteOnAfter(candidates, installed, false);

                // ASSERT AFTER
                Assert.AreEqual(0, candidates.Count);
                Assert.AreEqual("C1v1.0(3.0,2.0,Faulty)", ComponentsToStringWithResult(installed));
                Assert.AreEqual("[C1: 1.0] OnBeforeActionStarts.|" +
                                "[C1: 1.0] OnBeforeActionFinished.|" +
                                "[C1: 1.0 <= v < 2.0 --> 2.0] OnBeforeActionStarts.|" +
                                "[C1: 1.0 <= v < 2.0 --> 2.0] OnBeforeActionFinished.|" +
                                "[C1: 2.0 <= v < 3.0 --> 3.0] OnBeforeActionStarts.|" +
                                "[C1: 2.0 <= v < 3.0 --> 3.0] OnBeforeActionFinished.|" +
                                "[C1: 1.0] OnAfterActionStarts.|" +
                                "[C1: 1.0] OnAfterActionFinished.|" +
                                "[C1: 1.0 <= v < 2.0 --> 2.0] OnAfterActionStarts.|" +
                                "[C1: 1.0 <= v < 2.0 --> 2.0] ExecutionError.|" +
                                "[C1: 2.0 <= v < 3.0 --> 3.0] CannotExecuteMissingVersion.",
                    string.Join("|", log.Select(x => x.ToString(false))));
                Assert.AreEqual("ExecutionErrorOnAfter C1: 1.0 <= v < 2.0 --> 2.0|" +
                                "MissingVersion C1: 2.0 <= v < 3.0 --> 3.0", ErrorsToString(pm.Errors));
                Assert.AreEqual(11, packages.Count);
                Assert.AreEqual("1, C1: Install Successful, 1.0|2, C1: Patch SuccessfulBefore, 2.0|3, C1: Patch SuccessfulBefore, 3.0", PackagesToString(packages[7]));
                Assert.AreEqual("1, C1: Install Successful, 1.0|2, C1: Patch SuccessfulBefore, 2.0|3, C1: Patch SuccessfulBefore, 3.0", PackagesToString(packages[8]));
                Assert.AreEqual("1, C1: Install Successful, 1.0|2, C1: Patch Faulty, 2.0|3, C1: Patch SuccessfulBefore, 3.0", PackagesToString(packages[9]));
                Assert.AreEqual("1, C1: Install Successful, 1.0|2, C1: Patch Faulty, 2.0|3, C1: Patch SuccessfulBefore, 3.0", PackagesToString(packages[10]));
            });
        }

        public void Patching_Exec_SkipPatch_MoreFaultyChains()
        {
            NoRepoIntegrationTest(() =>
            {
                // Faulty execution blocks the following patches on the same component.
                var packages = new List<Package[]>();
                var log = new List<PatchExecutionLogRecord>();
                void Log(PatchExecutionLogRecord record) { packages.Add(LoadPackages()); log.Add(record); }
                var executed = new List<ISnPatch>();
                void Exec(PatchExecutionContext ctx) { executed.Add(ctx.CurrentPatch); }

                var installed = new List<SnComponentDescriptor>();
                var candidates = new List<ISnPatch>
            {
                // Problem in the installer
                Inst("C1", "v1.0", Exec, Error),
                Patch("C1", "1.0 <= v < 2.0", "v2.0", Exec, Exec),
                Patch("C1", "2.0 <= v < 3.0", "v3.0", Exec, Exec),
                // Problem in a middle patch
                Inst("C2", "v1.0", Exec, Exec),
                Patch("C2", "1.0 <= v < 2.0", "v2.0", Exec, Error),
                Patch("C2", "2.0 <= v < 3.0", "v3.0", Exec, Exec),
                // There is no problem
                Inst("C3", "v1.0", Exec, Exec),
                Patch("C3", "1.0 <= v < 2.0", "v2.0", Exec, Exec),
                Patch("C3", "2.0 <= v < 3.0", "v3.0", Exec, Exec),
            };

                // ACTION BEFORE
                var pm = new PatchManager(null, Log);
                pm.ExecuteOnBefore(candidates, installed, false);

                // ASSERT BEFORE
                Assert.AreEqual(9, candidates.Count);
                Assert.AreEqual("C1v(3.0,,SuccessfulBefore) C2v(3.0,,SuccessfulBefore) C3v(3.0,,SuccessfulBefore)", ComponentsToStringWithResult(installed));

                // ACTION AFTER
                pm.ExecuteOnAfter(candidates, installed, false);

                // ASSERT AFTER
                Assert.AreEqual(0, candidates.Count);
                Assert.AreEqual("C1v(3.0,1.0,Faulty) C2v1.0(3.0,2.0,Faulty) C3v3.0(3.0,3.0,Successful)", ComponentsToStringWithResult(installed));
                Assert.AreEqual("[C1: 1.0] OnBeforeActionStarts.|" +
                                "[C1: 1.0] OnBeforeActionFinished.|" +
                                "[C1: 1.0 <= v < 2.0 --> 2.0] OnBeforeActionStarts.|" +
                                "[C1: 1.0 <= v < 2.0 --> 2.0] OnBeforeActionFinished.|" +
                                "[C1: 2.0 <= v < 3.0 --> 3.0] OnBeforeActionStarts.|" +
                                "[C1: 2.0 <= v < 3.0 --> 3.0] OnBeforeActionFinished.|" +
                                "[C2: 1.0] OnBeforeActionStarts.|" +
                                "[C2: 1.0] OnBeforeActionFinished.|" +
                                "[C2: 1.0 <= v < 2.0 --> 2.0] OnBeforeActionStarts.|" +
                                "[C2: 1.0 <= v < 2.0 --> 2.0] OnBeforeActionFinished.|" +
                                "[C2: 2.0 <= v < 3.0 --> 3.0] OnBeforeActionStarts.|" +
                                "[C2: 2.0 <= v < 3.0 --> 3.0] OnBeforeActionFinished.|" +
                                "[C3: 1.0] OnBeforeActionStarts.|" +
                                "[C3: 1.0] OnBeforeActionFinished.|" +
                                "[C3: 1.0 <= v < 2.0 --> 2.0] OnBeforeActionStarts.|" +
                                "[C3: 1.0 <= v < 2.0 --> 2.0] OnBeforeActionFinished.|" +
                                "[C3: 2.0 <= v < 3.0 --> 3.0] OnBeforeActionStarts.|" +
                                "[C3: 2.0 <= v < 3.0 --> 3.0] OnBeforeActionFinished.|" +
                                "[C1: 1.0] OnAfterActionStarts.|" +
                                "[C1: 1.0] ExecutionError.|" +
                                "[C2: 1.0] OnAfterActionStarts.|" +
                                "[C2: 1.0] OnAfterActionFinished.|" +
                                "[C2: 1.0 <= v < 2.0 --> 2.0] OnAfterActionStarts.|" +
                                "[C2: 1.0 <= v < 2.0 --> 2.0] ExecutionError.|" +
                                "[C3: 1.0] OnAfterActionStarts.|" +
                                "[C3: 1.0] OnAfterActionFinished.|" +
                                "[C3: 1.0 <= v < 2.0 --> 2.0] OnAfterActionStarts.|" +
                                "[C3: 1.0 <= v < 2.0 --> 2.0] OnAfterActionFinished.|" +
                                "[C3: 2.0 <= v < 3.0 --> 3.0] OnAfterActionStarts.|" +
                                "[C3: 2.0 <= v < 3.0 --> 3.0] OnAfterActionFinished.|" +
                                "[C1: 1.0 <= v < 2.0 --> 2.0] CannotExecuteMissingVersion.|" +
                                "[C1: 2.0 <= v < 3.0 --> 3.0] CannotExecuteMissingVersion.|" +
                                "[C2: 2.0 <= v < 3.0 --> 3.0] CannotExecuteMissingVersion.",
                    string.Join("|", log.Select(x => x.ToString(false))));
                Assert.AreEqual("ExecutionErrorOnAfter C1: 1.0|" +
                                "ExecutionErrorOnAfter C2: 1.0 <= v < 2.0 --> 2.0|" +
                                "MissingVersion C1: 1.0 <= v < 2.0 --> 2.0|" +
                                "MissingVersion C1: 2.0 <= v < 3.0 --> 3.0|" +
                                "MissingVersion C2: 2.0 <= v < 3.0 --> 3.0", ErrorsToString(pm.Errors));
                Assert.AreEqual(33, packages.Count);
                Assert.AreEqual("1, C1: Install Faulty, 1.0|" +
                                "2, C1: Patch SuccessfulBefore, 2.0|" +
                                "3, C1: Patch SuccessfulBefore, 3.0|" +
                                "4, C2: Install Successful, 1.0|" +
                                "5, C2: Patch Faulty, 2.0|" +
                                "6, C2: Patch SuccessfulBefore, 3.0|" +
                                "7, C3: Install Successful, 1.0|" +
                                "8, C3: Patch Successful, 2.0|" +
                                "9, C3: Patch Successful, 3.0", PackagesToString(packages[32]));
            });
        }

        public void Patching_Exec_WaitForDependency_WaitingBeforeAndAfter()
        {
            NoRepoIntegrationTest(() =>
            {
                // Faulty execution blocks the following patches on the same component.
                var packages = new List<Package[]>();
                var log = new List<PatchExecutionLogRecord>();
                void Log(PatchExecutionLogRecord record) { packages.Add(LoadPackages()); log.Add(record); }
                var executed = new List<ISnPatch>();
                void Exec(PatchExecutionContext ctx) { executed.Add(ctx.CurrentPatch); }

                var installed = new List<SnComponentDescriptor>();
                var candidates = new List<ISnPatch>
            {
                Inst("C1", "v1.0", Exec, Exec),
                Patch("C1", "1.0 <= v < 2.0", "v2.0",
                    new[] {Dep("C2", "3.0 <= v")}, Exec, Exec),
                Patch("C1", "2.0 <= v < 3.0", "v3.0", Exec, Exec),

                Inst("C2", "v1.0", Exec, Exec),
                Patch("C2", "1.0 <= v < 2.0", "v2.0", Exec, Exec),
                Patch("C2", "2.0 <= v < 3.0", "v3.0", Exec, Exec),
            };

                // ACTION BEFORE
                var pm = new PatchManager(null, Log);
                pm.ExecuteOnBefore(candidates, installed, false);

                // ASSERT BEFORE
                Assert.AreEqual(6, candidates.Count);
                Assert.AreEqual("C1v(3.0,,SuccessfulBefore) C2v(3.0,,SuccessfulBefore)",
                    ComponentsToStringWithResult(installed));

                // ACTION AFTER
                pm.ExecuteOnAfter(candidates, installed, false);

                // ASSERT AFTER
                Assert.AreEqual(0, candidates.Count);
                Assert.AreEqual("C1v3.0(3.0,3.0,Successful) C2v3.0(3.0,3.0,Successful)",
                    ComponentsToStringWithResult(installed));
                Assert.AreEqual("[C1: 1.0] OnBeforeActionStarts.|" +
                                "[C1: 1.0] OnBeforeActionFinished.|" +
                                "[C2: 1.0] OnBeforeActionStarts.|" +
                                "[C2: 1.0] OnBeforeActionFinished.|" +
                                "[C2: 1.0 <= v < 2.0 --> 2.0] OnBeforeActionStarts.|" +
                                "[C2: 1.0 <= v < 2.0 --> 2.0] OnBeforeActionFinished.|" +
                                "[C2: 2.0 <= v < 3.0 --> 3.0] OnBeforeActionStarts.|" +
                                "[C2: 2.0 <= v < 3.0 --> 3.0] OnBeforeActionFinished.|" +
                                "[C1: 1.0 <= v < 2.0 --> 2.0] OnBeforeActionStarts.|" +
                                "[C1: 1.0 <= v < 2.0 --> 2.0] OnBeforeActionFinished.|" +
                                "[C1: 2.0 <= v < 3.0 --> 3.0] OnBeforeActionStarts.|" +
                                "[C1: 2.0 <= v < 3.0 --> 3.0] OnBeforeActionFinished.|" +
                                "[C1: 1.0] OnAfterActionStarts.|" +
                                "[C1: 1.0] OnAfterActionFinished.|" +
                                "[C2: 1.0] OnAfterActionStarts.|" +
                                "[C2: 1.0] OnAfterActionFinished.|" +
                                "[C2: 1.0 <= v < 2.0 --> 2.0] OnAfterActionStarts.|" +
                                "[C2: 1.0 <= v < 2.0 --> 2.0] OnAfterActionFinished.|" +
                                "[C2: 2.0 <= v < 3.0 --> 3.0] OnAfterActionStarts.|" +
                                "[C2: 2.0 <= v < 3.0 --> 3.0] OnAfterActionFinished.|" +
                                "[C1: 1.0 <= v < 2.0 --> 2.0] OnAfterActionStarts.|" +
                                "[C1: 1.0 <= v < 2.0 --> 2.0] OnAfterActionFinished.|" +
                                "[C1: 2.0 <= v < 3.0 --> 3.0] OnAfterActionStarts.|" +
                                "[C1: 2.0 <= v < 3.0 --> 3.0] OnAfterActionFinished.",
                    string.Join("|", log.Select(x => x.ToString(false))));
                Assert.AreEqual("", ErrorsToString(pm.Errors));
                Assert.AreEqual(24, packages.Count);
                Assert.AreEqual("1, C1: Install Successful, 1.0|" +
                                "2, C2: Install Successful, 1.0|" +
                                "3, C2: Patch Successful, 2.0|" +
                                "4, C2: Patch Successful, 3.0|" +
                                "5, C1: Patch Successful, 2.0|" +
                                "6, C1: Patch Successful, 3.0",
                    PackagesToString(packages[23]));
            });
        }
        public void Patching_Exec_InstallerIsLast()
        {
            NoRepoIntegrationTest(() =>
            {
                // Faulty execution blocks the following patches on the same component.
                var packages = new List<Package[]>();
                var log = new List<PatchExecutionLogRecord>();
                void Log(PatchExecutionLogRecord record) { packages.Add(LoadPackages()); log.Add(record); }
                var executed = new List<ISnPatch>();
                void Exec(PatchExecutionContext ctx) { executed.Add(ctx.CurrentPatch); }

                var installed = new List<SnComponentDescriptor>();
                var candidates = new List<ISnPatch>
                {
                    Patch("C1", "1.0 <= v < 2.0", "v3.0", Exec),
                    Patch("C1", "2.0 <= v <= 2.0", "v3.0", Exec),
                    Patch("C1", "2.0 <= v < 3.0", "v3.0", Exec),
                    Inst("C1", "v3.0", Exec),
                };

                // ACTION BEFORE
                var pm = new PatchManager(null, Log);
                pm.ExecuteOnBefore(candidates, installed, false);

                // ASSERT BEFORE
                Assert.AreEqual(4, candidates.Count);
                Assert.AreEqual("C1v(3.0,,SuccessfulBefore)", ComponentsToStringWithResult(installed));

                // ACTION AFTER
                pm.ExecuteOnAfter(candidates, installed, false);

                // ASSERT AFTER
                Assert.AreEqual(0, candidates.Count);
                Assert.AreEqual("C1v3.0(3.0,3.0,Successful)",
                    ComponentsToStringWithResult(installed));
                Assert.AreEqual("[C1: 3.0] OnBeforeActionStarts.|" +
                                "[C1: 3.0] OnBeforeActionFinished.|" +
                                "[C1: 3.0] OnAfterActionStarts.|" +
                                "[C1: 3.0] OnAfterActionFinished.",
                    string.Join("|", log.Select(x => x.ToString(false))));
                Assert.AreEqual("", ErrorsToString(pm.Errors));
                Assert.AreEqual(4, packages.Count);
                Assert.AreEqual("1, C1: Install Successful, 3.0",
                    PackagesToString(packages[3]));
            });
        }

        /* ===================================================================== EXECUTION VS VERSIONINFO TESTS */

        public void Patching_Exec_ComponentLifeCycleVsVersionInfo()
        {
            NoRepoIntegrationTest(() =>
            {
                void Exec(PatchExecutionContext ctx) { /* do nothing but register the fact of execution */ }

                AssertVersionInfo("C1", null, new string[0]);
                var installed = new List<SnComponentDescriptor>();

                // ACTION-1
                var candidates = new List<ISnPatch>
                {
                    Inst("C1", "v1.0", Exec, Exec),
                };
                var patchManager = new PatchManager(null, null);
                patchManager.ExecuteOnBefore(candidates, installed, false);
                patchManager.ExecuteOnAfter(candidates, installed, false);

                // ASSERT-1
                AssertVersionInfo("C1", "1.0", new[] { "1.0" });


                // ACTION-2
                candidates = new List<ISnPatch>
                {
                    Inst("C1", "v2.0", Exec, Exec),
                    Patch("C1", "1.0 <= v < 2.0", "v2.0", Exec, Exec),
                };
                patchManager = new PatchManager(null, null);
                patchManager.ExecuteOnBefore(candidates, installed, false);
                patchManager.ExecuteOnAfter(candidates, installed, false);

                // ASSERT-2
                AssertVersionInfo("C1", "2.0", new[] { "1.0", "2.0" });
            });
        }
        private void AssertVersionInfo(string componentId,
            string expectedComponentVersion, string[] expectedPackageVersions)
        {
            var versionInfo = RepositoryVersionInfo.Instance;

            var packages = versionInfo.InstalledPackages.Where(x => x.ComponentId == componentId).ToArray();
            if (expectedPackageVersions.Length == 0)
            {
                Assert.AreEqual(0, packages.Length);
            }
            else
            {
                var expected = string.Join(", ", expectedPackageVersions);
                var actual = string.Join(", ", packages.Select(x => x.ComponentVersion.ToString()));
                Assert.AreEqual(expected, actual);

                Assert.AreEqual(PackageType.Install, packages[0].PackageType);
                for (int i = 1; i < packages.Length; i++)
                    Assert.AreEqual(PackageType.Patch, packages[i].PackageType);
            }

            var component = versionInfo.Components.SingleOrDefault(x => x.ComponentId == componentId);
            if (expectedComponentVersion == null)
            {
                Assert.IsNull(component);
            }
            else
            {
                Assert.IsNotNull(component, "Component not found.");
                Assert.AreEqual(expectedComponentVersion, component.Version.ToString());
            }
        }

        /* ======================================================================= CONDITIONAL EXECUTION TESTS */

        // Patch vary component versions conditionally
        public void Patching_Exec_ConditionalActions_a()
        {
            NoRepoIntegrationTest(() =>
            {
                var log = new List<PatchExecutionLogRecord>();
                void Log(PatchExecutionLogRecord record) { log.Add(record); }
                void Before(PatchExecutionContext ctx)
                {
                    if (ctx.ComponentVersionIsEqual("7.4.0.2"))
                        ctx.Log("Update database in in 7.4.0.2.");
                }
                void After(PatchExecutionContext ctx)
                {
                    if (ctx.ComponentVersionIsLower("7.3.0"))
                        ctx.Log("Import new or modified content in 7.3.0.");
                    if (ctx.ComponentVersionIsLower("7.6.0"))
                        ctx.Log("Import new or modified content in 7.6.0.");
                }

                var installed = new List<SnComponentDescriptor>
            {
                Comp("C1", "v7.1.0")
            };
                var candidates = new List<ISnPatch>
            {
                Patch("C1", "7.1.0 <= v", "v7.7.0", Before, After),
            };

                // ACTION BEFORE
                var pm = new PatchManager(null, Log);
                pm.ExecuteOnBefore(candidates, installed, false);

                // ASSERT BEFORE
                Assert.AreEqual(1, candidates.Count);
                Assert.AreEqual("C1v7.1.0(7.7.0,,SuccessfulBefore)", ComponentsToStringWithResult(installed));

                // ACTION AFTER
                pm.ExecuteOnAfter(candidates, installed, false);

                // ASSERT AFTER
                Assert.AreEqual(0, candidates.Count);
                Assert.AreEqual("C1v7.7.0(7.7.0,7.7.0,Successful)", ComponentsToStringWithResult(installed));
                Assert.AreEqual("[C1: 7.1.0 <= v --> 7.7.0] OnBeforeActionStarts.|" +
                                "[C1: 7.1.0 <= v --> 7.7.0] OnBeforeActionFinished.|" +
                                "[C1: 7.1.0 <= v --> 7.7.0] OnAfterActionStarts.|" +
                                "[C1: 7.1.0 <= v --> 7.7.0] ExecutingOnAfter. Import new or modified content in 7.3.0.|" +
                                "[C1: 7.1.0 <= v --> 7.7.0] ExecutingOnAfter. Import new or modified content in 7.6.0.|" +
                                "[C1: 7.1.0 <= v --> 7.7.0] OnAfterActionFinished.",
                    string.Join("|", log.Select(x => x.ToString())));
                Assert.AreEqual("", ErrorsToString(pm.Errors));
            });
        }
        public void Patching_Exec_ConditionalActions_b()
        {
            NoRepoIntegrationTest(() =>
            {
                var log = new List<PatchExecutionLogRecord>();
                void Log(PatchExecutionLogRecord record) { log.Add(record); }
                void Before(PatchExecutionContext ctx)
                {
                    if (ctx.ComponentVersionIsEqual("7.4.0.2"))
                        ctx.Log("Update database in in 7.4.0.2.");
                }
                void After(PatchExecutionContext ctx)
                {
                    if (ctx.ComponentVersionIsLower("7.3.0"))
                        ctx.Log("Import new or modified content in 7.3.0.");
                    if (ctx.ComponentVersionIsLower("7.6.0"))
                        ctx.Log("Import new or modified content in 7.6.0.");
                }

                var installed = new List<SnComponentDescriptor>
            {
                Comp("C1", "v7.4.0.2")
            };
                var candidates = new List<ISnPatch>
            {
                Patch("C1", "7.1.0 <= v", "v7.7.0", Before, After),
            };

                // ACTION BEFORE
                var pm = new PatchManager(null, Log);
                pm.ExecuteOnBefore(candidates, installed, false);

                // ASSERT BEFORE
                Assert.AreEqual(1, candidates.Count);
                Assert.AreEqual("C1v7.4.0.2(7.7.0,,SuccessfulBefore)", ComponentsToStringWithResult(installed));

                // ACTION AFTER
                pm.ExecuteOnAfter(candidates, installed, false);

                // ASSERT AFTER
                Assert.AreEqual(0, candidates.Count);
                Assert.AreEqual("C1v7.7.0(7.7.0,7.7.0,Successful)", ComponentsToStringWithResult(installed));
                Assert.AreEqual("[C1: 7.1.0 <= v --> 7.7.0] OnBeforeActionStarts.|" +
                                "[C1: 7.1.0 <= v --> 7.7.0] ExecutingOnBefore. Update database in in 7.4.0.2.|" +
                                "[C1: 7.1.0 <= v --> 7.7.0] OnBeforeActionFinished.|" +
                                "[C1: 7.1.0 <= v --> 7.7.0] OnAfterActionStarts.|" +
                                "[C1: 7.1.0 <= v --> 7.7.0] ExecutingOnAfter. Import new or modified content in 7.6.0.|" +
                                "[C1: 7.1.0 <= v --> 7.7.0] OnAfterActionFinished.",
                    string.Join("|", log.Select(x => x.ToString())));
                Assert.AreEqual("", ErrorsToString(pm.Errors));
            });
        }
        public void Patching_Exec_ConditionalActions_c()
        {
            NoRepoIntegrationTest(() =>
            {
                var log = new List<PatchExecutionLogRecord>();
                void Log(PatchExecutionLogRecord record) { log.Add(record); }
                void Before(PatchExecutionContext ctx)
                {
                    if (ctx.ComponentVersionIsEqual("7.4.0.2"))
                        ctx.Log("Update database in in 7.4.0.2.");
                }
                void After(PatchExecutionContext ctx)
                {
                    if (ctx.ComponentVersionIsLower("7.3.0"))
                        ctx.Log("Import new or modified content in 7.3.0.");
                    if (ctx.ComponentVersionIsLower("7.6.0"))
                        ctx.Log("Import new or modified content in 7.6.0.");
                }

                var installed = new List<SnComponentDescriptor>
            {
                Comp("C1", "v7.5")
            };
                var candidates = new List<ISnPatch>
            {
                Patch("C1", "7.1.0 <= v", "v7.7.0", Before, After),
            };

                // ACTION BEFORE
                var pm = new PatchManager(null, Log);
                pm.ExecuteOnBefore(candidates, installed, false);

                // ASSERT BEFORE
                Assert.AreEqual(1, candidates.Count);
                Assert.AreEqual("C1v7.5(7.7.0,,SuccessfulBefore)", ComponentsToStringWithResult(installed));

                // ACTION AFTER
                pm.ExecuteOnAfter(candidates, installed, false);

                // ASSERT AFTER
                Assert.AreEqual(0, candidates.Count);
                Assert.AreEqual("C1v7.7.0(7.7.0,7.7.0,Successful)", ComponentsToStringWithResult(installed));
                Assert.AreEqual("[C1: 7.1.0 <= v --> 7.7.0] OnBeforeActionStarts.|" +
                                "[C1: 7.1.0 <= v --> 7.7.0] OnBeforeActionFinished.|" +
                                "[C1: 7.1.0 <= v --> 7.7.0] OnAfterActionStarts.|" +
                                "[C1: 7.1.0 <= v --> 7.7.0] ExecutingOnAfter. Import new or modified content in 7.6.0.|" +
                                "[C1: 7.1.0 <= v --> 7.7.0] OnAfterActionFinished.",
                    string.Join("|", log.Select(x => x.ToString())));
                Assert.AreEqual("", ErrorsToString(pm.Errors));
            });
        }

        // Patch vary component versions conditionally #2
        private void AllConditionsBefore(PatchExecutionContext ctx)
        {
            if (ctx.ComponentVersionIsHigherOrEqual("2.0"))
                ctx.Log("Before >=2.0");
            if (ctx.ComponentVersionIsHigher("2.0"))
                ctx.Log("Before >2.0");
            if (ctx.ComponentVersionIsEqual("2.5"))
                ctx.Log("Before =2.5");
            if (ctx.ComponentVersionIsLower("3.0"))
                ctx.Log("Before <3.0");
            if (ctx.ComponentVersionIsLowerOrEqual("3.0"))
                ctx.Log("Before <=3.0");
        }
        private void AllConditionsAfter(PatchExecutionContext ctx)
        {
            if (ctx.ComponentVersionIsHigherOrEqual("2.0"))
                ctx.Log("After >=2.0");
            if (ctx.ComponentVersionIsHigher("2.0"))
                ctx.Log("After >2.0");
            if (ctx.ComponentVersionIsEqual("2.5"))
                ctx.Log("After =2.5");
            if (ctx.ComponentVersionIsLower("3.0"))
                ctx.Log("After <3.0");
            if (ctx.ComponentVersionIsLowerOrEqual("3.0"))
                ctx.Log("After <=3.0");
        }

        public void Patching_Exec_ConditionalActions_AllConditions_a()
        {
            NoRepoIntegrationTest(() =>
            {
                var log = new List<PatchExecutionLogRecord>();
                void Log(PatchExecutionLogRecord record) { log.Add(record); }

                var installed = new List<SnComponentDescriptor> { Comp("C1", "v1.0") };
                var candidates = new List<ISnPatch>
                {
                    Patch("C1", "1.0 <= v", "v4.0", AllConditionsBefore, AllConditionsAfter),
                };

                // ACTION
                var pm = new PatchManager(null, Log);
                pm.ExecuteOnBefore(candidates, installed, false);
                pm.ExecuteOnAfter(candidates, installed, false);

                // ASSERT
                Assert.AreEqual("", ErrorsToString(pm.Errors));
                Assert.AreEqual("C1v4.0(4.0,4.0,Successful)", ComponentsToStringWithResult(installed));
                Assert.AreEqual("Before <3.0|Before <=3.0|" +
                                "After <3.0|After <=3.0",
                    string.Join("|", log.Where(x =>
                            x.EventType == PatchExecutionEventType.ExecutingOnBefore ||
                            x.EventType == PatchExecutionEventType.ExecutingOnAfter)
                        .Select(x => x.Message)));
            });
        }
        public void Patching_Exec_ConditionalActions_AllConditions_b()
        {
            NoRepoIntegrationTest(() =>
            {
                var log = new List<PatchExecutionLogRecord>();
                void Log(PatchExecutionLogRecord record) { log.Add(record); }

                var installed = new List<SnComponentDescriptor> { Comp("C1", "v2.0") };
                var candidates = new List<ISnPatch>
                {
                    Patch("C1", "1.0 <= v", "v4.0", AllConditionsBefore, AllConditionsAfter),
                };

                // ACTION
                var pm = new PatchManager(null, Log);
                pm.ExecuteOnBefore(candidates, installed, false);
                pm.ExecuteOnAfter(candidates, installed, false);

                // ASSERT
                Assert.AreEqual("", ErrorsToString(pm.Errors));
                Assert.AreEqual("C1v4.0(4.0,4.0,Successful)", ComponentsToStringWithResult(installed));
                Assert.AreEqual("Before >=2.0|Before <3.0|Before <=3.0|" +
                                "After >=2.0|After <3.0|After <=3.0",
                    string.Join("|", log.Where(x =>
                            x.EventType == PatchExecutionEventType.ExecutingOnBefore ||
                            x.EventType == PatchExecutionEventType.ExecutingOnAfter)
                        .Select(x => x.Message)));
            });
        }
        public void Patching_Exec_ConditionalActions_AllConditions_c()
        {
            NoRepoIntegrationTest(() =>
            {
                var log = new List<PatchExecutionLogRecord>();
                void Log(PatchExecutionLogRecord record) { log.Add(record); }

                var installed = new List<SnComponentDescriptor> { Comp("C1", "v2.5") };
                var candidates = new List<ISnPatch>
                {
                    Patch("C1", "1.0 <= v", "v4.0", AllConditionsBefore, AllConditionsAfter),
                };

                // ACTION
                var pm = new PatchManager(null, Log);
                pm.ExecuteOnBefore(candidates, installed, false);
                pm.ExecuteOnAfter(candidates, installed, false);

                // ASSERT
                Assert.AreEqual("", ErrorsToString(pm.Errors));
                Assert.AreEqual("C1v4.0(4.0,4.0,Successful)", ComponentsToStringWithResult(installed));
                Assert.AreEqual("Before >=2.0|Before >2.0|Before =2.5|Before <3.0|Before <=3.0|" +
                                "After >=2.0|After >2.0|After =2.5|After <3.0|After <=3.0",
                    string.Join("|", log.Where(x =>
                            x.EventType == PatchExecutionEventType.ExecutingOnBefore ||
                            x.EventType == PatchExecutionEventType.ExecutingOnAfter)
                        .Select(x => x.Message)));
            });
        }
        public void Patching_Exec_ConditionalActions_AllConditions_d()
        {
            NoRepoIntegrationTest(() =>
            {
                var log = new List<PatchExecutionLogRecord>();
                void Log(PatchExecutionLogRecord record) { log.Add(record); }

                var installed = new List<SnComponentDescriptor> { Comp("C1", "v3.0") };
                var candidates = new List<ISnPatch>
                {
                    Patch("C1", "1.0 <= v", "v4.0", AllConditionsBefore, AllConditionsAfter),
                };

                // ACTION
                var pm = new PatchManager(null, Log);
                pm.ExecuteOnBefore(candidates, installed, false);
                pm.ExecuteOnAfter(candidates, installed, false);

                // ASSERT
                Assert.AreEqual("", ErrorsToString(pm.Errors));
                Assert.AreEqual("C1v4.0(4.0,4.0,Successful)", ComponentsToStringWithResult(installed));
                Assert.AreEqual("Before >=2.0|Before >2.0|Before <=3.0|" +
                                "After >=2.0|After >2.0|After <=3.0",
                    string.Join("|", log.Where(x =>
                            x.EventType == PatchExecutionEventType.ExecutingOnBefore ||
                            x.EventType == PatchExecutionEventType.ExecutingOnAfter)
                        .Select(x => x.Message)));
            });
        }
        public void Patching_Exec_ConditionalActions_AllConditions_e()
        {
            NoRepoIntegrationTest(() =>
            {
                var log = new List<PatchExecutionLogRecord>();
                void Log(PatchExecutionLogRecord record) { log.Add(record); }

                var installed = new List<SnComponentDescriptor> { Comp("C1", "v3.5") };
                var candidates = new List<ISnPatch>
                {
                    Patch("C1", "1.0 <= v", "v4.0", AllConditionsBefore, AllConditionsAfter),
                };

                // ACTION
                var pm = new PatchManager(null, Log);
                pm.ExecuteOnBefore(candidates, installed, false);
                pm.ExecuteOnAfter(candidates, installed, false);

                // ASSERT
                Assert.AreEqual("", ErrorsToString(pm.Errors));
                Assert.AreEqual("C1v4.0(4.0,4.0,Successful)", ComponentsToStringWithResult(installed));
                Assert.AreEqual("Before >=2.0|Before >2.0|" +
                                "After >=2.0|After >2.0",
                    string.Join("|", log.Where(x =>
                            x.EventType == PatchExecutionEventType.ExecutingOnBefore ||
                            x.EventType == PatchExecutionEventType.ExecutingOnAfter)
                        .Select(x => x.Message)));
            });
        }

        /* ===================================================================== TOOLS */

        private void SavePackage(ISnPatch patch, ExecutionResult result, bool insertOnly = false)
        {
            PackageManager.SavePackage(Manifest.Create(patch), result, null, insertOnly);
        }

        protected Package[] LoadPackages()
        {
            var dataProvider = DataStore.DataProvider.GetExtension<IPackagingDataProviderExtension>();
            return dataProvider.LoadInstalledPackagesAsync(CancellationToken.None)
                .ConfigureAwait(false).GetAwaiter().GetResult().ToArray();
        }

        /// <summary>
        /// Creates a successfully executed package for test purposes. PackageType = PackageType.Patch.
        /// </summary>
        /// <param name="id">ComponentId</param>
        /// <param name="version">Version after successful execution</param>
        /// <param name="dependencies">Dependency array. Use null if there is no dependencies.</param>
        /// <returns></returns>
        protected Package Package(string id, string version, Dependency[] dependencies)
        {
            var package = new Package
            {
                ComponentId = id,
                ComponentVersion = Version.Parse(version.TrimStart('v')),
                Description = $"{id}-Description",
                ExecutionDate = DateTime.Now.AddDays(-1),
                ReleaseDate = DateTime.Now.AddDays(-2),
                ExecutionError = null,
                ExecutionResult = ExecutionResult.Successful,
                PackageType = PackageType.Patch,
            };

            package.Manifest = Manifest.Create(package, dependencies, false).ToXmlString();

            return package;
        }

        /// <summary>
        /// Creates a SnComponentDescriptor for test purposes.
        /// </summary>
        /// <param name="id">ComponentId</param>
        /// <param name="version">Last saved version.</param>
        /// <returns></returns>
        protected SnComponentDescriptor Comp(string id, string version)
        {
            return new SnComponentDescriptor(id, Version.Parse(version.TrimStart('v')), "", null);
        }

        protected ComponentInstaller Inst(string id, string version)
        {
            return new ComponentInstaller
            {
                ComponentId = id,
                Version = Version.Parse(version.TrimStart('v')),
                Dependencies = null,
            };
        }
        protected ComponentInstaller Inst(string id, string version, Dependency[] dependencies)
        {
            return new ComponentInstaller
            {
                ComponentId = id,
                Version = Version.Parse(version.TrimStart('v')),
                Dependencies = dependencies,
            };
        }
        protected ComponentInstaller Inst(string id, string version,
            Action<PatchExecutionContext> action)
        {
            return new ComponentInstaller
            {
                ComponentId = id,
                Version = Version.Parse(version.TrimStart('v')),
                Dependencies = null,
                Action = action
            };
        }
        protected ComponentInstaller Inst(string id, string version, Dependency[] dependencies,
            Action<PatchExecutionContext> action)
        {
            return new ComponentInstaller
            {
                ComponentId = id,
                Version = Version.Parse(version.TrimStart('v')),
                Dependencies = dependencies,
                Action = action
            };
        }
        protected ComponentInstaller Inst(string id, string version, Action<PatchExecutionContext> actionBefore,
            Action<PatchExecutionContext> action)
        {
            return new ComponentInstaller
            {
                ComponentId = id,
                Version = Version.Parse(version.TrimStart('v')),
                Dependencies = null,
                ActionBeforeStart = actionBefore,
                Action = action
            };
        }
        protected ComponentInstaller Inst(string id, string version, Dependency[] dependencies,
            Action<PatchExecutionContext> actionBefore, Action<PatchExecutionContext> action)
        {
            return new ComponentInstaller
            {
                ComponentId = id,
                Version = Version.Parse(version.TrimStart('v')),
                Dependencies = dependencies,
                ActionBeforeStart = actionBefore,
                Action = action
            };
        }

        /// <summary>
        /// Creates a patch for test purposes.
        /// </summary>
        /// <param name="id">ComponentId</param>
        /// <param name="version">Target version</param>
        /// <param name="boundary">Complex source version. Example: "1.1 &lt;= v &lt;= 1.1"</param>
        /// <returns></returns>
        protected SnPatch Patch(string id, string boundary, string version)
        {
            return Patch(id, boundary, version, null, null);
        }
        /// <summary>
        /// Creates a patch for test purposes.
        /// </summary>
        /// <param name="id">ComponentId</param>
        /// <param name="version">Target version</param>
        /// <param name="boundary">Complex source version. Example: "1.1 &lt;= v &lt;= 1.1"</param>
        /// <param name="dependencies">Dependency array. Use null if there is no dependencies.</param>
        /// <returns></returns>
        protected SnPatch Patch(string id, string boundary, string version, Dependency[] dependencies)
        {
            return new SnPatch
            {
                ComponentId = id,
                Version = version == null ? null : Version.Parse(version.TrimStart('v')),
                Boundary = ParseBoundary(boundary),
                Dependencies = dependencies
            };
        }
        /// <summary>
        /// Creates a patch for test purposes.
        /// </summary>
        /// <param name="id">ComponentId</param>
        /// <param name="version">Target version</param>
        /// <param name="boundary">Complex source version. Example: "1.1 &lt;= v &lt;= 1.1"</param>
        /// <param name="action">Function of execution</param>
        /// <returns></returns>
        protected SnPatch Patch(string id, string boundary, string version, Action<PatchExecutionContext> action)
        {
            return Patch(id, boundary, version, null, action);
        }
        protected SnPatch Patch(string id, string boundary, string version, Dependency[] dependencies,
            Action<PatchExecutionContext> actionBefore, Action<PatchExecutionContext> action)
        {
            return new SnPatch
            {
                ComponentId = id,
                Version = version == null ? null : Version.Parse(version.TrimStart('v')),
                Boundary = ParseBoundary(boundary),
                Dependencies = dependencies,
                ActionBeforeStart = actionBefore,
                Action = action
            };
        }
        protected SnPatch Patch(string id, string boundary, string version, Action<PatchExecutionContext> actionBefore,
            Action<PatchExecutionContext> action)
        {
            return new SnPatch
            {
                ComponentId = id,
                Version = version == null ? null : Version.Parse(version.TrimStart('v')),
                Boundary = ParseBoundary(boundary),
                ActionBeforeStart = actionBefore,
                Action = action
            };
        }

        /// <summary>
        /// Creates a Dependency for test purposes.
        /// </summary>
        /// <param name="id">ComponentId</param>
        /// <param name="boundary">Complex source version. Example: "1.1 &lt;= v &lt;= 1.1"</param>
        /// <returns></returns>
        protected Dependency Dep(string id, string boundary)
        {
            return new Dependency
            {
                Id = id,
                Boundary = ParseBoundary(boundary)
            };
        }

        protected VersionBoundary ParseBoundary(string src)
        {
            // "1.0 <= v <  2.0"

            var a = src.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var boundary = new VersionBoundary();

            if (a.Length == 3)
            {
                if (a[0] == "v")
                {
                    boundary.MaxVersion = Version.Parse(a[2]);
                    boundary.MaxVersionIsExclusive = a[1] == "<";

                    boundary.MinVersion = Version.Parse("0.0");
                    boundary.MinVersionIsExclusive = false;
                }
                else if (a[2] == "v")
                {
                    boundary.MinVersion = Version.Parse(a[0]);
                    boundary.MinVersionIsExclusive = a[1] == "<";

                    boundary.MaxVersion = new Version(int.MaxValue, int.MaxValue);
                    boundary.MaxVersionIsExclusive = false;
                }
                else
                {
                    throw new FormatException($"Invalid Boundary: {src}");
                }
            }
            else if (a.Length == 5 && a[2] == "v")
            {
                boundary.MinVersion = Version.Parse(a[0]);
                boundary.MinVersionIsExclusive = a[1] == "<";

                boundary.MaxVersion = Version.Parse(a[4]);
                boundary.MaxVersionIsExclusive = a[3] == "<";
            }
            else
            {
                throw new FormatException($"Invalid Boundary: {src}");
            }

            return boundary;
        }


        protected string ComponentsToString(SnComponentDescriptor[] components)
        {
            return string.Join(" ", components.OrderBy(x => x.ComponentId)
                .Select(x => $"{x.ComponentId}v{x.Version}"));
        }
        protected string ComponentsToStringWithResult(IEnumerable<SnComponentDescriptor> components)
        {
            return string.Join(" ", components.OrderBy(x => x.ComponentId)
                .Select(x => x.ToString()));
        }
        protected string PatchesToString(IEnumerable<ISnPatch> executables)
        {
            return string.Join(" ", executables.Select(x =>
                $"{x.ComponentId}{(x.Type == PackageType.Install ? "i" : "p")}{x.Version}"));
        }
        protected string PackagesToString(Package[] packages)
        {
            return string.Join("|", packages.Select(p => p.ToString()));
        }

        private void Error(PatchExecutionContext context) => throw new Exception("Err");
        private string ErrorsToString(IEnumerable<PatchExecutionError> errors)
        {
            return string.Join("|", errors.Select(x => x.ToString()));
        }

        internal static Manifest ParseManifestHead(string manifestXml)
        {
            var xml = new XmlDocument();
            xml.LoadXml(manifestXml);
            var manifest = new Manifest();
            Manifest.ParseHead(xml, manifest);
            return manifest;
        }
        internal static Manifest ParseManifest(string manifestXml, int currentPhase)
        {
            var xml = new XmlDocument();
            xml.LoadXml(manifestXml);
            return Manifest.Parse(xml, currentPhase, true, new PackageParameter[0]);
        }


    }
}
