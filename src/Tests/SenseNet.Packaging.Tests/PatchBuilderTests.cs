using System.Collections.Generic;
using System.Linq;
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
            builder.Install("1.0", "2020-10-20", "MyComp desc").Action();

            // ASSERT
            Assert.AreEqual("MyComp: 1.0", PatchesToString(builder));
            var installer = builder.GetPatches()[0] as ComponentInstaller;
            Assert.IsNotNull(installer);
            Assert.IsNull(installer.Dependencies);
            Assert.IsNull(installer.Action);

            // MORE TESTS
            // version: "v1.0"
            builder = new PatchBuilder(new TestComponent());
            builder.Install("v1.0", "2020-10-20", "MyComp desc").Action();
            Assert.AreEqual("MyComp: 1.0", PatchesToString(builder));
            // version: "V1.0"
            builder = new PatchBuilder(new TestComponent());
            builder.Install("V1.0", "2020-10-20", "MyComp desc").Action();
            Assert.AreEqual("MyComp: 1.0", PatchesToString(builder));
        }
        [TestMethod]
        public void Patching_Builder_Installer_WrongVersion()
        {
            var builder = new PatchBuilder(new TestComponent());

            try
            {
                // ACTION-1
                builder.Install(null, "2020-10-20", "MyComp desc");
                Assert.Fail();
            }
            catch (InvalidPatchException e)
            {
                // ASSERT-1
                Assert.AreEqual(PatchErrorCode.InvalidVersion, e.ErrorCode);
                Assert.AreEqual("MyComp: 0.0", e.Patch.ToString());
            }

            try
            {
                // ACTION-2
                builder.Install("wrong.wrong", "2020-10-20", "MyComp desc");
                Assert.Fail();
            }
            catch (InvalidPatchException e)
            {
                // ASSERT-2
                Assert.AreEqual(PatchErrorCode.InvalidVersion, e.ErrorCode);
                Assert.AreEqual("MyComp: 0.0", e.Patch.ToString());
            }
        }
        [TestMethod]
        public void Patching_Builder_Installer_WrongReleaseDate()
        {
            var builder = new PatchBuilder(new TestComponent());

            try
            {
                // ACTION-1
                builder.Install("1.0", null, "MyComp desc");
                Assert.Fail();
            }
            catch (InvalidPatchException e)
            {
                // ASSERT-1
                Assert.AreEqual(PatchErrorCode.InvalidDate, e.ErrorCode);
                Assert.AreEqual("MyComp: 1.0", e.Patch.ToString());
            }

            try
            {
                // ACTION-2
                builder.Install("1.0", "wrong", "MyComp desc");
                Assert.Fail();
            }
            catch (InvalidPatchException e)
            {
                // ASSERT-2
                Assert.AreEqual(PatchErrorCode.InvalidDate, e.ErrorCode);
                Assert.AreEqual("MyComp: 1.0", e.Patch.ToString());
            }
        }
        [TestMethod]
        public void Patching_Builder_Installer_WrongDescription()
        {
            var builder = new PatchBuilder(new TestComponent());

            try
            {
                // ACTION-1
                builder.Install("1.0", "2020-10-20", null);
                Assert.Fail();
            }
            catch (InvalidPatchException e)
            {
                // ASSERT-1
                Assert.AreEqual(PatchErrorCode.MissingDescription, e.ErrorCode);
                Assert.AreEqual("MyComp: 1.0", e.Patch.ToString());
            }

            try
            {
                // ACTION-2
                builder.Install("1.0", "2020-10-20", string.Empty);
                Assert.Fail();
            }
            catch (InvalidPatchException e)
            {
                // ASSERT-2
                Assert.AreEqual(PatchErrorCode.MissingDescription, e.ErrorCode);
                Assert.AreEqual("MyComp: 1.0", e.Patch.ToString());
            }
        }

        [TestMethod]
        public void Patching_Builder_Installer_BeforeAfter()
        {
            var builder = new PatchBuilder(new TestComponent());

            // ACTION
            builder.Install("1.0", "2020-10-20", "MyComp desc")
                .ActionBefore().Action()
                .Install("1.0", "2020-10-20", "MyComp desc")
                .ActionBefore().Action()
                .Install("1.0", "2020-10-20", "MyComp desc")
                .ActionBefore().Action()
                .Install("1.0", "2020-10-20", "MyComp desc")
                .ActionBefore().Action();

            // ASSERT
            var installers = builder.GetPatches()
                .Select(x=>(ComponentInstaller)x).ToArray();
            Assert.IsNull(installers[0].ActionBeforeStart);
            Assert.IsNull(installers[0].Action);
            Assert.IsNull(installers[1].ActionBeforeStart);
            Assert.IsNull(installers[1].Action);
            Assert.IsNull(installers[2].ActionBeforeStart);
            Assert.IsNull(installers[2].Action);
            Assert.IsNull(installers[3].ActionBeforeStart);
            Assert.IsNull(installers[3].Action);
        }

        /* ================================================================ PATCH TESTS */

        [TestMethod]
        public void Patching_Builder_Patch_Minimal()
        {
            var builder = new PatchBuilder(new TestComponent());

            // ACTION
            builder.Patch("1.0", "2.0", "2020-10-20", "desc").Action();

            // ASSERT
            Assert.AreEqual("MyComp: 1.0 <= v < 2.0 --> 2.0", PatchesToString(builder));
            var patch = builder.GetPatches()[0] as SnPatch;
            Assert.IsNotNull(patch);
            Assert.IsNull(patch.Dependencies);
            Assert.IsNull(patch.Action);
            Assert.AreEqual("1.0", patch.Boundary.MinVersion.ToString());
            Assert.AreEqual("2.0", patch.Boundary.MaxVersion.ToString());
            Assert.IsFalse(patch.Boundary.MinVersionIsExclusive);
            Assert.IsTrue(patch.Boundary.MaxVersionIsExclusive);

            // MORE TESTS
            // versions: "v2.0"
            builder = new PatchBuilder(new TestComponent());
            builder.Patch("v1.0", "v2.0", "2020-10-20", "desc").Action();
            Assert.AreEqual("MyComp: 1.0 <= v < 2.0 --> 2.0", PatchesToString(builder));
            // versions: "V2.0"
            builder = new PatchBuilder(new TestComponent());
            builder.Patch("V1.0", "V2.0", "2020-10-20", "desc").Action();
            Assert.AreEqual("MyComp: 1.0 <= v < 2.0 --> 2.0", PatchesToString(builder));
        }
        [TestMethod]
        public void Patching_Builder_Patch_WrongVersion()
        {
            var builder = new PatchBuilder(new TestComponent());

            try
            {
                // ACTION-1
                builder.Patch("1.0", null, "2020-10-20", "MyComp desc");
                Assert.Fail();
            }
            catch (InvalidPatchException e)
            {
                // ASSERT-1
                Assert.AreEqual(PatchErrorCode.InvalidVersion, e.ErrorCode);
                Assert.AreEqual("MyComp: v <= 0.0 --> 0.0", e.Patch.ToString());
            }

            try
            {
                // ACTION-2
                builder.Patch("1.0", "wrong.wrong", "2020-10-20", "MyComp desc");
                Assert.Fail();
            }
            catch (InvalidPatchException e)
            {
                // ASSERT-2
                Assert.AreEqual(PatchErrorCode.InvalidVersion, e.ErrorCode);
                Assert.AreEqual("MyComp: v <= 0.0 --> 0.0", e.Patch.ToString());
            }
        }
        [TestMethod]
        public void Patching_Builder_Patch_WrongReleaseDate()
        {
            var builder = new PatchBuilder(new TestComponent());

            try
            {
                // ACTION-1
                builder.Patch("1.0", "2.0", null, "MyComp desc");
                Assert.Fail();
            }
            catch (InvalidPatchException e)
            {
                // ASSERT-1
                Assert.AreEqual(PatchErrorCode.InvalidDate, e.ErrorCode);
                Assert.AreEqual("MyComp: 1.0 <= v < 2.0 --> 2.0", e.Patch.ToString());
            }

            try
            {
                // ACTION-2
                builder.Patch("1.0", "2.0", "wrong", "MyComp desc");
                Assert.Fail();
            }
            catch (InvalidPatchException e)
            {
                // ASSERT-2
                Assert.AreEqual(PatchErrorCode.InvalidDate, e.ErrorCode);
                Assert.AreEqual("MyComp: 1.0 <= v < 2.0 --> 2.0", e.Patch.ToString());
            }
        }
        [TestMethod]
        public void Patching_Builder_Patch_WrongDescription()
        {
            var builder = new PatchBuilder(new TestComponent());

            try
            {
                // ACTION-1
                builder.Patch("1.0", "2.0", "2020-10-20", null);
                Assert.Fail();
            }
            catch (InvalidPatchException e)
            {
                // ASSERT-1
                Assert.AreEqual(PatchErrorCode.MissingDescription, e.ErrorCode);
                Assert.AreEqual("MyComp: 1.0 <= v < 2.0 --> 2.0", e.Patch.ToString());
            }

            try
            {
                // ACTION-2
                builder.Patch("1.0", "2.0", "2020-10-20", string.Empty);
                Assert.Fail();
            }
            catch (InvalidPatchException e)
            {
                // ASSERT-2
                Assert.AreEqual(PatchErrorCode.MissingDescription, e.ErrorCode);
                Assert.AreEqual("MyComp: 1.0 <= v < 2.0 --> 2.0", e.Patch.ToString());
            }
        }

        [TestMethod]
        public void Patching_Builder_Patch_TooSmallTargetVersion()
        {
            var builder = new PatchBuilder(new TestComponent());

            try
            {
                // ACTION 
                builder.Patch("1.0", "1.0", "2020-10-20", "MyComp desc");
                Assert.Fail();
            }
            catch (InvalidPatchException e)
            {
                // ASSERT
                Assert.AreEqual(PatchErrorCode.TooSmallTargetVersion, e.ErrorCode);
                Assert.AreEqual("MyComp: 1.0 <= v < 1.0 --> 1.0", e.Patch.ToString());
            }
        }

        /* ================================================================ DEPENDENCY TESTS */

        [TestMethod]
        public void Patching_Builder_Dependency_Direct()
        {
            var builder = new PatchBuilder(new TestComponent());

            // ACTION
            builder.Patch("1.0", "2.0", "2020-10-20", "desc")
                .DependsOn("C1", "1.0")
                .DependsOn("C2", "2.3.0.15")
                .Action();

            // ASSERT
            var patch = builder.GetPatches()[0] as SnPatch;
            var actual = DependenciesToString(patch?.Dependencies ?? new Dependency[0]);
            Assert.AreEqual("C1: 1.0 <= v | C2: 2.3.0.15 <= v", actual);
        }
        [TestMethod]
        public void Patching_Builder_Dependency_Shared()
        {
            var patchBuilder = new PatchBuilder(new TestComponent());
            var depBuilder = new DependencyBuilder(patchBuilder)
                    .Dependency("C3", "3.0")
                    .Dependency("C4", "4.0");

            // ACTION
            patchBuilder.Patch("1.0", "2.0", "2020-10-20", "desc")
                .DependsOn(depBuilder)
                .Action();

            // ASSERT
            var patch = patchBuilder.GetPatches()[0] as SnPatch;
            var actual = DependenciesToString(patch?.Dependencies ?? new Dependency[0]);
            Assert.AreEqual("C3: 3.0 <= v | C4: 4.0 <= v", actual);
        }
        [TestMethod]
        public void Patching_Builder_Dependency_Mixed()
        {
            var patchBuilder = new PatchBuilder(new TestComponent());
            var depBuilder = new DependencyBuilder(patchBuilder)
                .Dependency("C3", "3.0")
                .Dependency("C4", "4.0");

            // ACTION
            patchBuilder.Patch("1.0", "2.0", "2020-10-20", "desc")
                .DependsOn("C1", "1.0")
                .DependsOn("C2", "2.0")
                .DependsOn(depBuilder)
                .Action();

            // ASSERT
            var patch = patchBuilder.GetPatches()[0] as SnPatch;
            var actual = DependenciesToString(patch?.Dependencies ?? new Dependency[0]);
            Assert.AreEqual("C1: 1.0 <= v | C2: 2.0 <= v | C3: 3.0 <= v | C4: 4.0 <= v", actual);
        }
        [TestMethod]
        public void Patching_Builder_Dependency_SelfDirect()
        {
            var patchBuilder = new PatchBuilder(new TestComponent());
            var depBuilder = new DependencyBuilder(patchBuilder)
                .Dependency("C3", "3.0")
                .Dependency("C4", "4.0");

            try
            {
                // ACTION
                patchBuilder.Patch("1.0", "2.0", "2020-10-20", "desc")
                    .DependsOn("C1", "1.0")
                    .DependsOn("MyComp", "2.0")
                    .DependsOn(depBuilder)
                    .Action();
                Assert.Fail();
            }
            catch (InvalidPatchException e)
            {
                // ASSERT
                Assert.AreEqual(PatchErrorCode.SelfDependency, e.ErrorCode);
                Assert.AreEqual("MyComp: 1.0 <= v < 2.0 --> 2.0", e.Patch.ToString());
            }
        }
        [TestMethod]
        public void Patching_Builder_Dependency_SelfShared()
        {
            var patchBuilder = new PatchBuilder(new TestComponent());
            var depBuilder = new DependencyBuilder(patchBuilder)
                .Dependency("C3", "3.0")
                .Dependency("MyComp", "4.0");

            try
            {
                // ACTION
                patchBuilder.Patch("1.0", "2.0", "2020-10-20", "desc")
                    .DependsOn("C1", "1.0")
                    .DependsOn("C2", "2.0")
                    .DependsOn(depBuilder)
                    .Action();
                Assert.Fail();
            }
            catch (InvalidPatchException e)
            {
                // ASSERT
                Assert.AreEqual(PatchErrorCode.SelfDependency, e.ErrorCode);
                Assert.AreEqual("MyComp: 1.0 <= v < 2.0 --> 2.0", e.Patch.ToString());
            }
        }
        [TestMethod]
        public void Patching_Builder_Dependency_Duplicated()
        {
            var patchBuilder = new PatchBuilder(new TestComponent());
            var depBuilder = new DependencyBuilder(patchBuilder)
                .Dependency("C3", "3.0")
                .Dependency("C2", "4.0");

            try
            {
                // ACTION
                patchBuilder.Patch("1.0", "2.0", "2020-10-20", "desc")
                    .DependsOn("C1", "1.0")
                    .DependsOn("C2", "2.0")
                    .DependsOn(depBuilder)
                    .Action();
                Assert.Fail();
            }
            catch (InvalidPatchException e)
            {
                // ASSERT
                Assert.AreEqual(PatchErrorCode.DuplicatedDependency, e.ErrorCode);
                Assert.AreEqual("MyComp: 1.0 <= v < 2.0 --> 2.0", e.Patch.ToString());
            }
        }

        /* ================================================================ COMBINATION TESTS */

        [TestMethod]
        public void Patching_Builder_LowInstallerHigherPatches()
        {
            var builder = new PatchBuilder(new TestComponent());

            // ACTION
            builder
                .Install("1.0", "2020-10-20", "desc").Action()
                .Patch("1.0", "2.0", "2020-10-20", "desc").Action()
                .Patch("2.0", "3.0", "2020-10-21", "desc").Action();

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
                .Patch("1.0", "2.0", "2020-10-20", "desc").Action()
                .Patch("2.0", "3.0", "2020-10-21", "desc").Action()
                .Install("3.0", "2020-10-20", "desc").Action();

            // ASSERT
            //                       MyComp: 1.0 <= v < 2.0 --> 2.0
            Assert.AreEqual("MyComp: 1.0 <= v < 2.0 --> 2.0 | MyComp: 2.0 <= v < 3.0 --> 3.0 | MyComp: 3.0",
                PatchesToString(builder));
        }

        /* ============================================================ TOOLS */

        private string PatchesToString(PatchBuilder builder)
        {
            return string.Join(" | ", builder.GetPatches());
        }

        private string DependenciesToString(IEnumerable<Dependency> dependencies)
        {
            return string.Join(" | ", dependencies.Select(d => d.ToString()));
        }
    }
}
