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
            // "1.0 <= v <  2.0"
            var boundary = ParseBoundary("1.0 <= v <  2.0");
            throw new NotImplementedException();
        }
        [TestMethod]
        public void Patching_SelfTest_CreatePatch()
        {
            // "C1: 1.0 <= v < 1.0, v1.2"
            var patch = Patch("C1", "1.0 <= v < 1.0,", "v1.2", null);
            throw new NotImplementedException();
        }
        [TestMethod]
        public void Patching_SelfTest_CreateDependency()
        {
            throw new NotImplementedException();
            // "C1: 1.0 <= v <= 1.0"
        }
    }
}
