using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SenseNet.Packaging.Tests
{
    [TestClass]
    public class PatchingSelfTests : PatchingTestBase
    {
        [TestMethod]
        public void Patching_SelfTest_ParseBoundary()
        {
            var boundary = ParseBoundary("1.0 <  v <  2.0");
            Assert.AreEqual(Version.Parse("1.0"), boundary.MinVersion);
            Assert.AreEqual(Version.Parse("2.0"), boundary.MaxVersion);
            Assert.AreEqual(true, boundary.MinVersionIsExclusive);
            Assert.AreEqual(true, boundary.MaxVersionIsExclusive);

            boundary = ParseBoundary("1.0 <= v <  2.0");
            Assert.AreEqual(Version.Parse("1.0"), boundary.MinVersion);
            Assert.AreEqual(Version.Parse("2.0"), boundary.MaxVersion);
            Assert.AreEqual(false, boundary.MinVersionIsExclusive);
            Assert.AreEqual(true, boundary.MaxVersionIsExclusive);

            boundary = ParseBoundary("1.0 <  v <= 2.0");
            Assert.AreEqual(Version.Parse("1.0"), boundary.MinVersion);
            Assert.AreEqual(Version.Parse("2.0"), boundary.MaxVersion);
            Assert.AreEqual(true, boundary.MinVersionIsExclusive);
            Assert.AreEqual(false, boundary.MaxVersionIsExclusive);

            boundary = ParseBoundary("1.0 <= v <= 2.0");
            Assert.AreEqual(Version.Parse("1.0"), boundary.MinVersion);
            Assert.AreEqual(Version.Parse("2.0"), boundary.MaxVersion);
            Assert.AreEqual(false, boundary.MinVersionIsExclusive);
            Assert.AreEqual(false, boundary.MaxVersionIsExclusive);

            boundary = ParseBoundary("v <= 2.0");
            Assert.AreEqual(Version.Parse("0.0"), boundary.MinVersion);
            Assert.AreEqual(Version.Parse("2.0"), boundary.MaxVersion);
            Assert.AreEqual(false, boundary.MinVersionIsExclusive);
            Assert.AreEqual(false, boundary.MaxVersionIsExclusive);

            boundary = ParseBoundary("1.0 <= v");
            Assert.AreEqual(Version.Parse("1.0"), boundary.MinVersion);
            Assert.AreEqual(new Version(int.MaxValue, int.MaxValue), boundary.MaxVersion);
            Assert.AreEqual(false, boundary.MinVersionIsExclusive);
            Assert.AreEqual(false, boundary.MaxVersionIsExclusive);
        }
        [TestMethod]
        public void Patching_SelfTest_CreateDependency()
        {
            // "C1: 1.0 <= v <= 1.0"
            var dependency = Dep("C1", "1.0 <= v <= 1.0");

            Assert.AreEqual("C1", dependency.Id);
            Assert.AreEqual(Version.Parse("1.0"), dependency.Boundary.MinVersion);
            Assert.AreEqual(Version.Parse("1.0"), dependency.Boundary.MaxVersion);
            Assert.AreEqual(false, dependency.Boundary.MinVersionIsExclusive);
            Assert.AreEqual(false, dependency.Boundary.MaxVersionIsExclusive);
        }
        [TestMethod]
        public void Patching_SelfTest_CreatePatch()
        {
            // "C1: 1.0 <= v < 1.0, v1.2"
            var patch = Patch("C1", "1.0 <= v < 1.0", "v1.2", null);

            Assert.AreEqual("C1", patch.ComponentId);
            Assert.AreEqual(Version.Parse("1.2"), patch.Version);
            Assert.AreEqual(Version.Parse("1.0"), patch.Boundary.MinVersion);
            Assert.AreEqual(Version.Parse("1.0"), patch.Boundary.MaxVersion);
            Assert.AreEqual(false, patch.Boundary.MinVersionIsExclusive);
            Assert.AreEqual(true, patch.Boundary.MaxVersionIsExclusive);
        }
    }
}
