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
            builder.Install("1.0", "2020-10-20", "MyComp desc").Execute();

            // ASSERT
            Assert.AreEqual("MyComp: 1.0", PatchesToString(builder));
            var installer = builder.GetPatches()[0] as ComponentInstaller;
            Assert.IsNotNull(installer);
            Assert.IsNull(installer.Dependencies);
            Assert.IsNull(installer.Execute);

            // MORE TESTS
            // version: "v1.0"
            builder = new PatchBuilder(new TestComponent());
            builder.Install("v1.0", "2020-10-20", "MyComp desc").Execute();
            Assert.AreEqual("MyComp: 1.0", PatchesToString(builder));
            // version: "V1.0"
            builder = new PatchBuilder(new TestComponent());
            builder.Install("V1.0", "2020-10-20", "MyComp desc").Execute();
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
            }
        }

        /* ================================================================ PATCH TESTS */

        [TestMethod]
        public void Patching_Builder_Patch_Minimal()
        {
            var builder = new PatchBuilder(new TestComponent());

            // ACTION
            builder.Patch("1.0", "2.0", "2020-10-20", "desc").Execute();

            // ASSERT
            Assert.AreEqual("MyComp: 1.0 <= v < 2.0 --> 2.0", PatchesToString(builder));
            var patch = builder.GetPatches()[0] as SnPatch;
            Assert.IsNotNull(patch);
            Assert.IsNull(patch.Dependencies);
            Assert.IsNull(patch.Execute);
            Assert.AreEqual("1.0", patch.Boundary.MinVersion.ToString());
            Assert.AreEqual("2.0", patch.Boundary.MaxVersion.ToString());
            Assert.IsFalse(patch.Boundary.MinVersionIsExclusive);
            Assert.IsTrue(patch.Boundary.MaxVersionIsExclusive);

            // MORE TESTS
            // versions: "v2.0"
            builder = new PatchBuilder(new TestComponent());
            builder.Patch("v1.0", "v2.0", "2020-10-20", "desc").Execute();
            Assert.AreEqual("MyComp: 1.0 <= v < 2.0 --> 2.0", PatchesToString(builder));
            // versions: "V2.0"
            builder = new PatchBuilder(new TestComponent());
            builder.Patch("V1.0", "V2.0", "2020-10-20", "desc").Execute();
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
            }
        }

        /* ================================================================ DEPENDENCY TESTS */

        [TestMethod]
        public void Patching_Builder_Dependency_Direct()
        {
            var builder = new PatchBuilder(new TestComponent());

            // ACTION
            builder.Patch("1.0", "2.0", "2020-10-20", "desc")
                .DependsFrom("C1", "1.0")
                .DependsFrom("C2", "2.3.0.15")
                .Execute();

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
                .DependsFrom(depBuilder)
                .Execute();

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
                .DependsFrom("C1", "1.0")
                .DependsFrom("C2", "2.0")
                .DependsFrom(depBuilder)
                .Execute();

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
                    .DependsFrom("C1", "1.0")
                    .DependsFrom("MyComp", "2.0")
                    .DependsFrom(depBuilder)
                    .Execute();
                Assert.Fail();
            }
            catch (InvalidPatchException e)
            {
                // ASSERT
                Assert.AreEqual(PatchErrorCode.SelfDependency, e.ErrorCode);
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
                    .DependsFrom("C1", "1.0")
                    .DependsFrom("C2", "2.0")
                    .DependsFrom(depBuilder)
                    .Execute();
                Assert.Fail();
            }
            catch (InvalidPatchException e)
            {
                // ASSERT
                Assert.AreEqual(PatchErrorCode.SelfDependency, e.ErrorCode);
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
                    .DependsFrom("C1", "1.0")
                    .DependsFrom("C2", "2.0")
                    .DependsFrom(depBuilder)
                    .Execute();
                Assert.Fail();
            }
            catch (InvalidPatchException e)
            {
                // ASSERT
                Assert.AreEqual(PatchErrorCode.DuplicatedDependency, e.ErrorCode);
            }
        }

        private string DependenciesToString(IEnumerable<Dependency> dependencies)
        {
            return string.Join(" | ", dependencies.Select(d => d.ToString()));
        }

        /* ================================================================ COMBINATION TESTS */
        [TestMethod]
        public void Patching_Builder_LowInstallerHigherPatches()
        {
            var builder = new PatchBuilder(new TestComponent());

            // ACTION
            builder
                .Install("1.0", "2020-10-20", "desc").Execute()
                .Patch("1.0", "2.0", "2020-10-20", "desc").Execute()
                .Patch("2.0", "3.0", "2020-10-21", "desc").Execute();

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
                .Patch("1.0", "2.0", "2020-10-20", "desc").Execute()
                .Patch("2.0", "3.0", "2020-10-21", "desc").Execute()
                .Install("3.0", "2020-10-20", "desc").Execute();

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
    }
}
