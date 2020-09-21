using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.TaskManagement.Core.Configuration;

namespace SenseNet.Packaging.Tests
{
    [TestClass]
    public class PatchBuilderTests : PatchingTestBase
    {
        private class TestComponent : SnComponent
        {
            public override string ComponentId { get; } = "MyComp";
        }

        /* ================================================================ INSTALLER TESTS */

        [TestMethod]
        public void Patching_Builder_Installer_Minimal()
        {
            var builder = new PatchBuilder(new TestComponent());

            // ACTION
            builder.Install("1.0", "2020-10-20", "MyComp desc", (context) => { });

            // ASSERT
            Assert.AreEqual("MyComp: 1.0", PatchesToString(builder));
            var installer = builder.Patches[0] as ComponentInstaller;
            Assert.IsNotNull(installer);
            Assert.IsNull(installer.Dependencies);
            Assert.IsNotNull(installer.Execute);

            // MORE TESTS
            // version: "v1.0"
            builder = new PatchBuilder(new TestComponent());
            builder.Install("v1.0", "2020-10-20", "MyComp desc", (context) => { });
            Assert.AreEqual("MyComp: 1.0", PatchesToString(builder));
            // version: "V1.0"
            builder = new PatchBuilder(new TestComponent());
            builder.Install("V1.0", "2020-10-20", "MyComp desc", (context) => { });
            Assert.AreEqual("MyComp: 1.0", PatchesToString(builder));

        }
        [TestMethod]
        public void Patching_Builder_Installer_WrongVersion()
        {
            var builder = new PatchBuilder(new TestComponent());

            try
            {
                // ACTION-1
                builder.Install(null, "2020-10-20", "MyComp desc", (context) => { });
                Assert.Fail();
            }
            catch (InvalidPatchException e)
            {
                // ASSERT-1
                Assert.AreEqual(PatchErrorCode.InvalidVersion, e.ErrorCode);
            }

            try
            {
                // ACTION-2
                builder.Install("wrong.wrong", "2020-10-20", "MyComp desc", (context) => { });
                Assert.Fail();
            }
            catch (InvalidPatchException e)
            {
                // ASSERT-2
                Assert.AreEqual(PatchErrorCode.InvalidVersion, e.ErrorCode);
            }
        }
        [TestMethod]
        public void Patching_Builder_Installer_WrongReleaseDate()
        {
            var builder = new PatchBuilder(new TestComponent());

            try
            {
                // ACTION-1
                builder.Install("1.0", null, "MyComp desc", (context) => { });
                Assert.Fail();
            }
            catch (InvalidPatchException e)
            {
                // ASSERT-1
                Assert.AreEqual(PatchErrorCode.InvalidDate, e.ErrorCode);
            }

            try
            {
                // ACTION-2
                builder.Install("1.0", "wrong", "MyComp desc", (context) => { });
                Assert.Fail();
            }
            catch (InvalidPatchException e)
            {
                // ASSERT-2
                Assert.AreEqual(PatchErrorCode.InvalidDate, e.ErrorCode);
            }
        }
        [TestMethod]
        public void Patching_Builder_Installer_WrongDescription()
        {
            var builder = new PatchBuilder(new TestComponent());

            try
            {
                // ACTION-1
                builder.Install("1.0", "2020-10-20", null, (context) => { });
                Assert.Fail();
            }
            catch (InvalidPatchException e)
            {
                // ASSERT-1
                Assert.AreEqual(PatchErrorCode.MissingDescription, e.ErrorCode);
            }

            try
            {
                // ACTION-2
                builder.Install("1.0", "2020-10-20", string.Empty, (context) => { });
                Assert.Fail();
            }
            catch (InvalidPatchException e)
            {
                // ASSERT-2
                Assert.AreEqual(PatchErrorCode.MissingDescription, e.ErrorCode);
            }
        }
        [TestMethod]
        public void Patching_Builder_Installer_WrongCallback()
        {
            var builder = new PatchBuilder(new TestComponent());

            try
            {
                // ACTION
                builder.Install("1.0", "2020-10-20", "MyComp desc", null);
                Assert.Fail();
            }
            catch (InvalidPatchException e)
            {
                // ASSERT
                Assert.AreEqual(PatchErrorCode.MissingExecuteAction, e.ErrorCode);
            }
        }

        /* ================================================================ PATCH TESTS */

        [TestMethod]
        public void Patching_Builder_Patch_Minimal()
        {
            var builder = new PatchBuilder(new TestComponent());

            // ACTION
            builder.Patch("2.0", "2020-10-20", "desc", builder.MinVersion("1.0"),
                (context) => { });

            // ASSERT
            Assert.AreEqual("MyComp: 1.0 <= v < 2.0 --> 2.0", PatchesToString(builder));
            var patch = builder.Patches[0] as SnPatch;
            Assert.IsNotNull(patch);
            Assert.IsNull(patch.Dependencies);
            Assert.IsNotNull(patch.Execute);
            Assert.AreEqual("1.0", patch.Boundary.MinVersion.ToString());
            Assert.AreEqual("2.0", patch.Boundary.MaxVersion.ToString());
            Assert.IsFalse(patch.Boundary.MinVersionIsExclusive);
            Assert.IsTrue(patch.Boundary.MaxVersionIsExclusive);

            // MORE TESTS
            // versions: "v2.0"
            builder = new PatchBuilder(new TestComponent());
            builder.Patch("v2.0", "2020-10-20", "desc", builder.MinVersion("v1.0"),
                (context) => { });
            Assert.AreEqual("MyComp: 1.0 <= v < 2.0 --> 2.0", PatchesToString(builder));
            // versions: "V2.0"
            builder = new PatchBuilder(new TestComponent());
            builder.Patch("V2.0", "2020-10-20", "desc", builder.MinVersion("V1.0"),
                (context) => { });
            Assert.AreEqual("MyComp: 1.0 <= v < 2.0 --> 2.0", PatchesToString(builder));
        }
        [TestMethod]
        public void Patching_Builder_Patch_WrongVersion()
        {
            var builder = new PatchBuilder(new TestComponent());

            try
            {
                // ACTION-1
                builder.Patch(null, "2020-10-20", "MyComp desc", 
                    builder.MinVersion("1.0"), (context) => { });
                Assert.Fail();
            }
            catch (InvalidPatchException e)
            {
                // ASSERT-1
                Assert.AreEqual(PatchErrorCode.InvalidVersion, e.ErrorCode);
            }

            try
            {
                // ACTION-2
                builder.Patch("wrong.wrong", "2020-10-20", "MyComp desc",
                    builder.MinVersion("1.0"), (context) => { });
                Assert.Fail();
            }
            catch (InvalidPatchException e)
            {
                // ASSERT-2
                Assert.AreEqual(PatchErrorCode.InvalidVersion, e.ErrorCode);
            }
        }
        [TestMethod]
        public void Patching_Builder_Patch_WrongReleaseDate()
        {
            var builder = new PatchBuilder(new TestComponent());

            try
            {
                // ACTION-1
                builder.Patch("1.0", null, "MyComp desc",
                    builder.MinVersion("1.0"), (context) => { });
                Assert.Fail();
            }
            catch (InvalidPatchException e)
            {
                // ASSERT-1
                Assert.AreEqual(PatchErrorCode.InvalidDate, e.ErrorCode);
            }

            try
            {
                // ACTION-2
                builder.Patch("1.0", "wrong", "MyComp desc",
                    builder.MinVersion("1.0"), (context) => { });
                Assert.Fail();
            }
            catch (InvalidPatchException e)
            {
                // ASSERT-2
                Assert.AreEqual(PatchErrorCode.InvalidDate, e.ErrorCode);
            }
        }
        [TestMethod]
        public void Patching_Builder_Patch_WrongDescription()
        {
            var builder = new PatchBuilder(new TestComponent());

            try
            {
                // ACTION-1
                builder.Patch("2.0", "2020-10-20", null,
                    builder.MinVersion("1.0"), (context) => { });
                Assert.Fail();
            }
            catch (InvalidPatchException e)
            {
                // ASSERT-1
                Assert.AreEqual(PatchErrorCode.MissingDescription, e.ErrorCode);
            }

            try
            {
                // ACTION-2
                builder.Patch("2.0", "2020-10-20", string.Empty,
                    builder.MinVersion("1.0"), (context) => { });
                Assert.Fail();
            }
            catch (InvalidPatchException e)
            {
                // ASSERT-2
                Assert.AreEqual(PatchErrorCode.MissingDescription, e.ErrorCode);
            }
        }

        [TestMethod]
        public void Patching_Builder_Patch_TooSmallTargetVersion()
        {
            var builder = new PatchBuilder(new TestComponent());

            try
            {
                // ACTION 
                builder.Patch("1.0", "2020-10-20", "MyComp desc",
                    builder.MinVersion("1.0"), (context) => { });
                Assert.Fail();
            }
            catch (InvalidPatchException e)
            {
                // ASSERT
                Assert.AreEqual(PatchErrorCode.TooSmallTargetVersion, e.ErrorCode);
            }
        }

        /* ================================================================ COMBINATION TESTS */

        [TestMethod]
        public void Patching_Builder_LowInstallerHigherPatches()
        {
            var builder = new PatchBuilder(new TestComponent());

            // ACTION
            builder
                .Install("1.0", "2020-10-20", "desc", (context) => { })
                .Patch("2.0", "2020-10-20", "desc",
                    builder.MinMaxExVersion("1.0", "2.0"), (context) => { })
                .Patch("3.0", "2020-10-21", "desc",
                    builder.MinMaxExVersion("2.0", "3.0"), (context) => { });

            // ASSERT
            Assert.AreEqual("MyComp: 1.0 | MyComp: 1.0 <= v < 2.0 --> 2.0 | MyComp: 2.0 <= v < 3.0 --> 3.0",
                PatchesToString(builder));
        }
        [TestMethod]
        public void Patching_Builder_HighInstallerLowerPatches()
        {
            var builder = new PatchBuilder(new TestComponent());

            // ACTION
            builder
                .Patch("2.0", "2020-10-20", "desc",
                    builder.MinMaxExVersion("1.0", "2.0"), (context) => { })
                .Patch("3.0", "2020-10-21", "desc",
                    builder.MinMaxExVersion("2.0", "3.0"), (context) => { })
                .Install("3.0", "2020-10-20", "desc", (context) => { });

            // ASSERT
            //                       MyComp: 1.0 <= v < 2.0 --> 2.0
            Assert.AreEqual("MyComp: 1.0 <= v < 2.0 --> 2.0 | MyComp: 2.0 <= v < 3.0 --> 3.0 | MyComp: 3.0",
                PatchesToString(builder));
        }

        /* ============================================================ TOOLS */

        private string PatchesToString(PatchBuilder builder)
        {
            return string.Join(" | ", builder.Patches);
        }
    }
}
