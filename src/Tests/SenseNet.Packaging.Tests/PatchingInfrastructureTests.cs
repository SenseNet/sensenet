using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.Packaging.Tests
{
    [TestClass]
    public class PatchingInfrastructureTests : PatchingTestBase
    {
        [TestMethod]
        public void Patching_System_ManifestToXml()
        {
            var src = @"<?xml version='1.0' encoding='utf-8'?>
<Package type='Install'>
  <Id>C7</Id>
  <ReleaseDate>2017-01-01</ReleaseDate>
  <Version>1.0</Version>
  <Description>Description text</Description>
  <Dependencies>
    <Dependency id='C1' version='1.0' />
    <Dependency id='C2' minVersion='1.0' />
    <Dependency id='C3' minVersionExclusive='1.0' />
    <Dependency id='C4' maxVersion='2.0' />
    <Dependency id='C5' maxVersionExclusive='2.0' />
    <Dependency id='C6' minVersion='1.0' maxVersionExclusive='2.0' />
  </Dependencies>
</Package>".Replace('\'', '"');

            var xml = new XmlDocument();
            xml.LoadXml(src);
            var manifest = Manifest.Parse(xml);

            var actual = manifest.ToXmlString();

            Assert.AreEqual(actual, src);
        }

        [TestMethod]
        public void Patching_System_CreatePackageFromInstaller()
        {
            var installer = new ComponentInstaller
            {
                ComponentId = "C7",
                Version = new Version(1, 0),
                Description = "C7 description",
                ReleaseDate = new DateTime(2345, 07, 31),
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
            var expectedManifest = @"<?xml version='1.0' encoding='utf-8'?>
<Package type='Install'>
  <Id>C7</Id>
  <ReleaseDate>2345-07-31</ReleaseDate>
  <Version>1.0</Version>
  <Description>C7 description</Description>
  <Dependencies>
    <Dependency id='C1' version='1.0' />
    <Dependency id='C2' minVersion='1.0' />
    <Dependency id='C3' minVersionExclusive='1.0' />
    <Dependency id='C4' maxVersion='2.0' />
    <Dependency id='C5' maxVersionExclusive='2.0' />
    <Dependency id='C6' minVersion='1.0' maxVersionExclusive='2.0' />
  </Dependencies>
</Package>".Replace('\'', '"');

            // ACTION
            var pkg = PatchManager.CreatePackage(installer);

            // ASSERT
            Assert.AreEqual("C7", pkg.ComponentId);
            Assert.AreEqual(new Version(1, 0), pkg.ComponentVersion);
            Assert.AreEqual("C7 description", pkg.Description);
            Assert.AreEqual(new DateTime(2345, 07, 31), pkg.ReleaseDate);
            Assert.AreEqual(expectedManifest, pkg.Manifest);
        }
        [TestMethod]
        public void Patching_System_CreatePackageFromPatch()
        {
            var patch = new SnPatch
            {
                ComponentId = "C7",
                Version = new Version(2, 0),
                Description = "C7 description",
                ReleaseDate = new DateTime(2345, 07, 31),
                Boundary = ParseBoundary("1.0 <= v < 2.0"),
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
            var expectedManifest = @"<?xml version='1.0' encoding='utf-8'?>
<Package type='Patch'>
  <Id>C7</Id>
  <ReleaseDate>2345-07-31</ReleaseDate>
  <Version>2.0</Version>
  <Description>C7 description</Description>
  <Dependencies>
    <Dependency id='C7' minVersion='1.0' maxVersionExclusive='2.0' />
    <Dependency id='C1' version='1.0' />
    <Dependency id='C2' minVersion='1.0' />
    <Dependency id='C3' minVersionExclusive='1.0' />
    <Dependency id='C4' maxVersion='2.0' />
    <Dependency id='C5' maxVersionExclusive='2.0' />
    <Dependency id='C6' minVersion='1.0' maxVersionExclusive='2.0' />
  </Dependencies>
</Package>".Replace('\'', '"');

            // ACTION
            var pkg = PatchManager.CreatePackage(patch);

            // ASSERT
            Assert.AreEqual("C7", pkg.ComponentId);
            Assert.AreEqual(new Version(2, 0), pkg.ComponentVersion);
            Assert.AreEqual("C7 description", pkg.Description);
            Assert.AreEqual(new DateTime(2345, 07, 31), pkg.ReleaseDate);
            Assert.AreEqual(expectedManifest, pkg.Manifest);
        }

        [TestMethod]
        public void Patching_System_CreateInstallerFromPackage()
        {
            var package = new Package
            {
                Id = 42,
                ComponentId = "C7",
                PackageType = PackageType.Install,
                Description = "C7 description",
                ReleaseDate = new DateTime(2020, 07, 31),
                ComponentVersion = new Version(1, 0),
                ExecutionDate = new DateTime(2020, 08, 12),
                ExecutionResult = ExecutionResult.Faulty,
                ExecutionError = new Exception("Very informative error message."),
                Manifest = @"<?xml version='1.0' encoding='utf-8'?>
<Package type='Install'>
  <Id>C7</Id>
  <ReleaseDate>2020-07-31</ReleaseDate>
  <Version>1.0</Version>
  <Description>C7 description</Description>
  <Dependencies>
    <Dependency id='C1' version='1.0' />
    <Dependency id='C2' minVersion='1.0' />
    <Dependency id='C3' minVersionExclusive='1.0' />
    <Dependency id='C4' maxVersion='2.0' />
    <Dependency id='C5' maxVersionExclusive='2.0' />
    <Dependency id='C6' minVersion='1.0' maxVersionExclusive='2.0' />
  </Dependencies>
</Package>".Replace('\'', '"')
            };

            // ACTION
            var patch = PatchManager.CreatePatch(package);

            // ASSERT
            var installer = patch as ComponentInstaller;
            Assert.IsNotNull(installer);
            Assert.AreEqual(42, installer.Id);
            Assert.AreEqual("C7", installer.ComponentId);
            Assert.AreEqual(new Version(1, 0), installer.Version);
            Assert.AreEqual("C7 description", installer.Description);
            Assert.AreEqual(new DateTime(2020, 07, 31), installer.ReleaseDate);
            var dep = string.Join(",", installer.Dependencies).Replace(" ", "");
            Assert.AreEqual("C1:1.0<=v<=1.0,C2:1.0<=v,C3:1.0<v,C4:v<=2.0,C5:v<2.0,C6:1.0<=v<2.0", dep);
            Assert.AreEqual(new DateTime(2020, 08, 12), installer.ExecutionDate);
            Assert.AreEqual(ExecutionResult.Faulty, installer.ExecutionResult);
            Assert.IsNotNull(installer.ExecutionError);
            Assert.AreEqual("Very informative error message.", installer.ExecutionError.Message);
        }
        [TestMethod]
        public void Patching_System_CreatePatchFromPackage()
        {
            var package = new Package
            {
                Id = 42,
                ComponentId = "C7",
                PackageType = PackageType.Patch,
                Description = "C7 description",
                ReleaseDate = new DateTime(2020, 07, 31),
                ComponentVersion = new Version(2, 0),
                ExecutionDate = new DateTime(2020, 08, 12),
                ExecutionResult = ExecutionResult.Faulty,
                ExecutionError = new Exception("Very informative error message."),
                Manifest = @"<?xml version='1.0' encoding='utf-8'?>
<Package type='Patch'>
  <Id>C7</Id>
  <ReleaseDate>2020-07-31</ReleaseDate>
  <Version>2.0</Version>
  <Description>C7 description</Description>
  <Dependencies>
    <Dependency id='C7' minVersion='1.0' maxVersionExclusive='2.0' />
    <Dependency id='C1' version='1.0' />
    <Dependency id='C2' minVersion='1.0' />
    <Dependency id='C3' minVersionExclusive='1.0' />
    <Dependency id='C4' maxVersion='2.0' />
    <Dependency id='C5' maxVersionExclusive='2.0' />
    <Dependency id='C6' minVersion='1.0' maxVersionExclusive='2.0' />
  </Dependencies>
</Package>".Replace('\'', '"')
            };

            // ACTION
            var patch = PatchManager.CreatePatch(package);

            // ASSERT
            var snPatch = patch as SnPatch;
            Assert.IsNotNull(snPatch);
            Assert.AreEqual(42, snPatch.Id);
            Assert.AreEqual("C7", snPatch.ComponentId);
            Assert.AreEqual(new Version(2, 0), snPatch.Version);
            Assert.AreEqual("C7 description", snPatch.Description);
            Assert.AreEqual(new DateTime(2020, 07, 31), snPatch.ReleaseDate);
            Assert.AreEqual("1.0 <= v < 2.0", snPatch.Boundary.ToString());
            var dep = string.Join(",", snPatch.Dependencies).Replace(" ", "");
            Assert.AreEqual("C1:1.0<=v<=1.0,C2:1.0<=v,C3:1.0<v,C4:v<=2.0,C5:v<2.0,C6:1.0<=v<2.0", dep);
            Assert.AreEqual(new DateTime(2020, 08, 12), snPatch.ExecutionDate);
            Assert.AreEqual(ExecutionResult.Faulty, snPatch.ExecutionResult);
            Assert.IsNotNull(snPatch.ExecutionError);
            Assert.AreEqual("Very informative error message.", snPatch.ExecutionError.Message);
        }

        [TestMethod]
        public void Patching_System_SaveAndReloadFaultyInstaller()
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

            // SAVE
            PackageManager.SavePackage(Manifest.Create(installer), null, false,
                new PackagingException(PackagingExceptionType.DependencyNotFound));

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
            Assert.AreEqual(ExecutionResult.Faulty, patches[0].ExecutionResult);
            Assert.IsNotNull(patches[0].ExecutionError);
            Assert.AreEqual(PackagingExceptionType.DependencyNotFound, ((PackagingException)patches[0].ExecutionError).ErrorType);
        }
        [TestMethod]
        public void Patching_System_ReSaveAndReloadInstaller()
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
        }
        [TestMethod]
        public void Patching_System_SaveAndReloadSnPatch()
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
            var patch = (SnPatch)patches[0];
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
        }

        [TestMethod]
        public void Patching_System_SaveAndReload_Installer_FaultyBefore()
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
        }
        [TestMethod]
        public void Patching_System_SaveAndReload_Installer_SuccessfulBefore()
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
        }
        [TestMethod]
        public void Patching_System_SaveAndReload_SnPatch_FaultyBefore()
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
        }
        [TestMethod]
        public void Patching_System_SaveAndReload_SnPatch_SuccessfulBefore()
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
        }

        [TestMethod]
        public void Patching_System_InstalledComponents()
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
        }
        [TestMethod]
        public void Patching_System_InstalledComponents_Descriptions()
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
        }

        [TestMethod]
        public void Patching_System_LoadInstalledComponents()
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
        }

        [TestMethod]
        public void Patching_System_PatchSorter_InstallerSmallerThanPatch()
        {
            var candidates = new List<ISnPatch>
            {
                Patch("C1", "1.0 < v", "1.1"),
                Inst("C1", "2.0"),
                Inst("C2", "2.0"),
            };
            // ACTION
            PatchManager.SortCandidates(candidates);
            // ASSERT
            Assert.AreEqual("C1i2.0 C1p1.1 C2i2.0", PatchesToString(candidates.ToArray()));
        }
        [TestMethod]
        public void Patching_System_PatchSorter_NameAndVersion()
        {
            var candidates = new List<ISnPatch>
            {
                Patch("C2", "1.0 < v", "2.0"),
                Patch("C1", "2.0 < v", "3.0"),
                Inst("C2", "1.0"),
                Inst("C1", "2.0"),
            };
            // ACTION
            PatchManager.SortCandidates(candidates);
            // ASSERT
            Assert.AreEqual("C1i2.0 C1p3.0 C2i1.0 C2p2.0", PatchesToString(candidates.ToArray()));
        }
        [TestMethod]
        public void Patching_System_PatchSorter_Boundary()
        {
            var p0 = Patch("C1", "5.0 < v", "10.0");
            var p1 = Patch("C1", "5.0 < v <= 6.0", "10.0");
            var p2 = Patch("C1", "5.0 < v < 6.0", "10.0");
            var p3 = Patch("C1", "4.0 < v < 7.0", "10.0");
            var p4 = Patch("C1", "4.0 < v < 6.0", "10.0");
            var p5 = Patch("C1", "3.0 < v < 4.0", "10.0");
            var p6 = Patch("C1", "2.0 < v < 4.0", "10.0");
            var p7 = Patch("C1", "2.0 <= v < 4.0", "10.0");
            var p8 = Patch("C1", "v < 4.0", "10.0");
            
            var candidates = new List<ISnPatch> { p0, p1, p2, p3, p4, p5, p6, p7, p8 };
            // ACTION
            PatchManager.SortCandidates(candidates);
            // ASSERT
            Assert.AreEqual(p8, candidates[0]);
            Assert.AreEqual(p7, candidates[1]);
            Assert.AreEqual(p6, candidates[2]);
            Assert.AreEqual(p5, candidates[3]);
            Assert.AreEqual(p4, candidates[4]);
            Assert.AreEqual(p3, candidates[5]);
            Assert.AreEqual(p2, candidates[6]);
            Assert.AreEqual(p1, candidates[7]);
            Assert.AreEqual(p0, candidates[8]);
        }

        /* ======================================================================= TOOLS */

        private void SavePackage(ISnPatch patch, ExecutionResult result)
        {
            PackageManager.SavePackage(Manifest.Create(patch), result, null);
        }
    }
}
