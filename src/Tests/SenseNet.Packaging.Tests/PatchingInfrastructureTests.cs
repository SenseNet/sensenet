﻿using System;
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
        public void PatchingSystem_ManifestToXml()
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
        public void PatchingSystem_CreatePackageFromInstaller()
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
        public void PatchingSystem_CreatePackageFromPatch()
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
        public void PatchingSystem_CreateInstallerFromPackage()
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
        public void PatchingSystem_CreatePatchFromPackage()
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
        public void PatchingSystem_SaveAndReloadFaultyInstaller()
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
        public void PatchingSystem_ReSaveAndReloadInstaller()
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
        public void PatchingSystem_SaveAndReloadSnPatch()
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
        public void PatchingSystem_InstalledComponents()
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
        public void PatchingSystem_InstalledComponents_Descriptions()
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

    }
}
