using System;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
                Id = "C7",
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
            var pkg = PackageManager.CreatePackage(installer);

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
                Id = "C7",
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
            var pkg = PackageManager.CreatePackage(patch);

            // ASSERT
            Assert.AreEqual("C7", pkg.ComponentId);
            Assert.AreEqual(new Version(2, 0), pkg.ComponentVersion);
            Assert.AreEqual("C7 description", pkg.Description);
            Assert.AreEqual(new DateTime(2345, 07, 31), pkg.ReleaseDate);
            Assert.AreEqual(expectedManifest, pkg.Manifest);
        }
    }
}
