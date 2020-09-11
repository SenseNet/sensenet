using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SenseNet.Packaging.Tests
{
    [TestClass]
    public class PatchingPlayerTests : PatchingTestBase
    {
        [TestMethod]
        public void PatchingPlayer_()
        {
            var pkg = Package("C1", "v1.0", new[] {Dep("C2", "1.0 <= v")});

            Assert.Fail();
        }
    }
}
