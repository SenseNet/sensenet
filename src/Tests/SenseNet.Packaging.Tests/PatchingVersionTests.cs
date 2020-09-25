using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.Packaging.Tests
{
    [TestClass]
    public class PatchingVersionTests : PatchingTestBase
    {
        [TestMethod]
        public void Patching_Version_Check_IsInInterval()
        {
            bool IsInInterval(string boundarySrc, string version)
            {
                return ParseBoundary(boundarySrc).IsInInterval(Version.Parse(version.TrimStart('v')));
            }

            Assert.IsFalse(IsInInterval("2.0 <= v <= 2.0", "v1.9"));
            Assert.IsTrue(IsInInterval("2.0 <= v <= 2.0", "v2.0"));
            Assert.IsFalse(IsInInterval("2.0 <= v <= 2.0", "v2.0.0"));
            Assert.IsFalse(IsInInterval("2.0 <= v <= 3.0", "v1.9"));
            Assert.IsTrue(IsInInterval("2.0 <= v <= 3.0", "v2.5"));
            Assert.IsFalse(IsInInterval("2.0 <= v <= 3.0", "v3.0.0"));
            Assert.IsTrue(IsInInterval("2.0 <= v <= 3.0", "v2.0"));
            Assert.IsFalse(IsInInterval("2.0 <  v <= 3.0", "v2.0"));
            Assert.IsFalse(IsInInterval("2.0 <= v <  3.0", "v3.0"));
            Assert.IsTrue(IsInInterval("2.0 <= v <= 3.0", "v3.0"));

            var max = int.MaxValue;
            var boundary = new VersionBoundary {MinVersion = new Version(2, 3)}; // 2.3 <= v
            Assert.IsFalse(boundary.IsInInterval(new Version(1, 0)));
            Assert.IsFalse(boundary.IsInInterval(new Version(2, 2, max, max)));
            Assert.IsTrue(boundary.IsInInterval(new Version(2, 3)));
            Assert.IsTrue(boundary.IsInInterval(new Version(max, max)));
            boundary.MinVersionIsExclusive = true; // 2.3 < v
            Assert.IsFalse(boundary.IsInInterval(new Version(2, 3)));
            Assert.IsTrue(boundary.IsInInterval(new Version(2, 3,0,1)));

            boundary = new VersionBoundary { MaxVersion = new Version(2, 3) }; // v <= 2.3
            Assert.IsTrue(boundary.IsInInterval(new Version(2, 0)));
            Assert.IsTrue(boundary.IsInInterval(new Version(2, 3)));
            Assert.IsFalse(boundary.IsInInterval(new Version(2, 3, 0, 1)));
            boundary.MaxVersionIsExclusive = true; // v < 2.3
            Assert.IsTrue(boundary.IsInInterval(new Version(2, 2, max, max)));
            Assert.IsFalse(boundary.IsInInterval(new Version(2, 3)));
            Assert.IsFalse(boundary.IsInInterval(new Version(max, max)));
        }

        /* =================================================================== PATCH VALIDITY TESTS */

        [TestMethod]
        public void Patching_Version_Check_WrongIdOrTarget()
        {
            // Input                       Error
            // --------------------------- ---------------------
            // (??: 1.0 <= v <= 1.0, v1.2) Missing id
            // ("": 1.0 <= v <= 1.0, v1.2) Missing id
            // (C1: 1.0 <= v <= 1.0, ????) Missing target version
            // (C1: 1.0 <= v <= 1.0, v0.9) Downgrade

            try
            {
                ValidatePatch(Patch(null, "1.0 <= v <= 1.0", "v1.2"));
                Assert.Fail();
            }
            catch (InvalidPackageException e)
            {
                Assert.AreEqual(e.ErrorType, PackagingExceptionType.MissingComponentId);
            }

            try
            {
                ValidatePatch(Patch(string.Empty, "1.0 <= v <= 1.0", "v1.2"));
                Assert.Fail();
            }
            catch (InvalidPackageException e)
            {
                Assert.AreEqual(e.ErrorType, PackagingExceptionType.MissingComponentId);
            }

            try
            {
                ValidatePatch(Patch("C1", "1.0 <= v <= 1.0", null));
                Assert.Fail();
            }
            catch (InvalidPackageException e)
            {
                Assert.AreEqual(e.ErrorType, PackagingExceptionType.MissingVersion);
            }

            try
            {
                ValidatePatch(Patch("C1", "1.0 <= v <= 1.0", "v0.9"));
                Assert.Fail();
            }
            catch (InvalidPackageException e)
            {
                Assert.AreEqual(e.ErrorType, PackagingExceptionType.TargetVersionTooSmall);
            }
        }

        [TestMethod]
        public void Patching_Version_Check_BoundaryAutoFill()
        {
            // Input                       Output
            // --------------------------- ---------------------
            // (C1:        v <= 1.0, v1.2) (C1: 0.0.0.0 <= v <= 1.0, v1.2)
            // (C1: 1.0 <= v       , v1.2) (C1: 1.0     <= v <  1.2, v1.2)

            var patch = Patch("C1", "       v <= 1.0", "v1.2");
            ValidatePatch(patch);
            Assert.AreEqual(Version.Parse("0.0"), patch.Boundary.MinVersion);
            Assert.IsFalse(patch.Boundary.MinVersionIsExclusive);

            patch = Patch("C1", "1.0 <= v       ", "v1.2");
            ValidatePatch(patch);
            Assert.AreEqual(patch.Version, patch.Boundary.MaxVersion);
            Assert.IsTrue(patch.Boundary.MaxVersionIsExclusive);
        }
        [TestMethod]
        public void Patching_Version_Check_BoundaryOverlapped_OnePatch()
        {
            #region void CheckError(string boundary, PackagingExceptionType expectedErrorType)...
            void CheckError(string boundary, PackagingExceptionType expectedErrorType)
            {
                try
                {
                    ValidatePatch(Patch("C1", boundary, "v1.2"));
                    Assert.Fail("The expected exception was not thrown.");
                }
                catch (InvalidPackageException e)
                {
                    Assert.AreEqual(e.ErrorType, expectedErrorType);
                }
            }
            #endregion

            // Id Patch                       Error
            // -- --------------------------- ---------------------
            //  1 (C1: 1.0 <= v <= 1.0, v1.2) ok
            //  2 (C1: 1.0 <= v <  1.0, v1.2) Invalid interval
            //  3 (C1: 1.0 <  v <= 1.0, v1.2) Invalid interval
            //  4 (C1: 1.0 <  v <  1.0, v1.2) Invalid interval
            //  5 (C1: 1.1 <= v <= 1.0, v1.2) Max is less than min
            //  6 (C1: 1.1 <= v <  1.0, v1.2) Max is less than min
            //  7 (C1: 1.1 <  v <= 1.0, v1.2) Max is less than min
            //  8 (C1: 1.1 <  v <  1.0, v1.2) Max is less than min

            // Case 1: no error
            ValidatePatch(Patch("C1", "1.0 <= v <= 1.0", "v1.2"));
            // Case 2-8: throw InvalidPackageException
            CheckError("1.0 <= v <  1.0", PackagingExceptionType.InvalidInterval);
            CheckError("1.0 <  v <= 1.0", PackagingExceptionType.InvalidInterval);
            CheckError("1.0 <  v <  1.0", PackagingExceptionType.InvalidInterval);
            CheckError("1.1 <= v <= 1.0", PackagingExceptionType.MaxLessThanMin);
            CheckError("1.1 <= v <  1.0", PackagingExceptionType.MaxLessThanMin);
            CheckError("1.1 <  v <= 1.0", PackagingExceptionType.MaxLessThanMin);
            CheckError("1.1 <  v <  1.0", PackagingExceptionType.MaxLessThanMin);
        }
        [TestMethod]
        public void Patching_Version_Check_BoundaryOverlapped_MorePatches()
        {
            void CheckError(SnPatch patch1, SnPatch patch2, PackagingExceptionType expectedErrorType)
            {
                try
                {
                    ValidatePatches(patch1, patch2);
                    Assert.Fail("The expected exception was not thrown.");
                }
                catch (InvalidPackageException e)
                {
                    Assert.AreEqual(e.ErrorType, expectedErrorType);
                }
            }

            // Patch1                      Patch2                      Error
            // --------------------------- --------------------------- ---------------------
            // (C1: 1.1 <= v <= 1.1, v1.3) (C1: 1.2 <= v <= 1.2, v1.3) Targets are the same
            // (C1: 1.1 <= v <= 1.1, v1.3) (C1: 1.1 <= v <= 1.1, v1.4) Sources are the same

            CheckError(
                Patch("C1", "1.1 <= v <= 1.1", "v1.3"),
                Patch("C1", "1.2 <= v <= 1.2", "v1.3"),
                PackagingExceptionType.TargetVersionsAreTheSame);
            CheckError(
                Patch("C1", "1.1 <= v <= 1.1", "v1.3"),
                Patch("C1", "1.1 <= v <= 1.1", "v1.4"),
                PackagingExceptionType.SourceVersionsAreTheSame);

            // Patch1                      Patch2                      Error
            // --------------------------- --------------------------- ---------------------
            // (C1: 1.0 <= v <  2.0, v2.0) (C1: 2.0 <  v <  3.0, v3.0) ok
            // (C1: 1.0 <= v <= 2.0, v2.0) (C1: 2.0 <  v <  3.0, v3.0) ok
            // (C1: 1.0 <= v <  2.0, v2.0) (C1: 2.0 <= v <  3.0, v3.0) ok
            // (C1: 1.0 <= v <= 2.0, v2.0) (C1: 2.0 <= v <  3.0, v3.0) Overlapped
            // (C1: 1.0 <= v <  2.0, v2.0) (C1: 1.9 <= v <  3.0, v3.0) Overlapped

            ValidatePatches(
                Patch("C1", "1.0 <= v <  2.0", "v2.0"),
                Patch("C1", "2.0 <  v <  3.0", "v3.0"));
            ValidatePatches(
                Patch("C1", "1.0 <= v <= 2.0", "v2.0"),
                Patch("C1", "2.0 <  v <  3.0", "v3.0"));
            ValidatePatches(
                Patch("C1", "1.0 <= v <  2.0", "v2.0"),
                Patch("C1", "2.0 <= v <  3.0", "v3.0"));
            CheckError(
                Patch("C1", "1.0 <= v <= 2.0", "v2.0"),
                Patch("C1", "2.0 <= v <  3.0", "v3.0"),
                PackagingExceptionType.OverlappedIntervals);
            CheckError(
                Patch("C1", "1.0 <= v <  2.0", "v2.0"),
                Patch("C1", "1.9 <= v <  3.0", "v3.0"),
                PackagingExceptionType.OverlappedIntervals);
        }
        [TestMethod]
        public void Patching_Version_Check_WrongDependency()
        {
            // Patch                                                  Error
            // ------------------------------------------------------ ---------------------
            // (C1: 1.0 <= v <= 1.0, v1.2, {DEP C1: 1.0 <= v <= 1.0}) Patch and dependency id are the same

            var patch = Patch("C1", "1.0 <= v <= 1.0", "v1.2",
                new[] {Dep("C1", "1.0 <= v <= 1.0")});
            try
            {
                ValidatePatch(patch);
                Assert.Fail("The expected exception was not thrown.");
            }
            catch (InvalidPackageException e)
            {
                Assert.AreEqual(e.ErrorType, PackagingExceptionType.PatchIdAndDependencyIdAreTheSame);
            }
        }

        /* =================================================================== PATCH RELEVANCE TESTS */
        /* Also known as executability test. Relevance is determined by the current state of the     */
        /* components and the patch's' target version and boundary.                                  */

        [TestMethod]
        public void Patching_Version_Relevance_NotInstalledComponent()
        {
            // Package    Patch                       IsRelevant
            // ---------- --------------------------- ----------
            // [C1: v1.0] (C2: 1.0 <= v <= 1.0, v1.1) no

            Assert.IsFalse(IsRelevant("[C1: v1.0]", "(C2: 1.0 <= v <= 1.0, v1.1)"));
        }
        [TestMethod]
        public void Patching_Version_Relevance_MaxVersionNotDefined()
        {
            // Package     Patch                IsRelevant
            // ----------- -------------------- ----------
            // [C1:  v1.0] (C1: 1.0 <= v, v1.1) yes
            // [C1:  v1.0] (C1: 1.0 <  v, v1.1) no
            // [C1:  v1.1] (C1: 1.0 <= v, v1.1) no
            // [C1:  v1.1] (C1: 1.0 <= v, v1.2) yes
            // [C1:  v1.1] (C1: 1.0 <  v, v1.2) yes

            Assert.IsTrue(IsRelevant("[C1: v1.0]", "(C1: 1.0 <= v, v1.1)"));
            Assert.IsFalse(IsRelevant("[C1: v1.0]", "(C1: 1.0 <  v, v1.1)"));
            Assert.IsFalse(IsRelevant("[C1: v1.1]", "(C1: 1.0 <= v, v1.1)"));
            Assert.IsTrue(IsRelevant("[C1: v1.1]", "(C1: 1.0 <= v, v1.2)"));
            Assert.IsTrue(IsRelevant("[C1: v1.1]", "(C1: 1.0 <  v, v1.2)"));
        }
        [TestMethod]
        public void Patching_Version_Relevance_MinVersionNotDefined()
        {
            // Package     Patch                IsRelevant
            // ----------- -------------------- ----------
            // [C1:  v1.0] (C1: v <= 1.0, v1.1) yes
            // [C1:  v1.0] (C1: v <= 1.1, v1.2) yes
            // [C1:  v1.1] (C1: v <  1.1, v1.2) no
            // [C1:  v1.1] (C1: v <= 1.1, v1.2) yes
            // [C1:  v1.2] (C1: v <= 1.1, v1.2) no

            Assert.IsTrue(IsRelevant("[C1:  v1.0]", "(C1: v <= 1.0, v1.1)"));
            Assert.IsTrue(IsRelevant("[C1:  v1.0]", "(C1: v <= 1.1, v1.2)"));
            Assert.IsFalse(IsRelevant("[C1:  v1.1]", "(C1: v <  1.1, v1.2)"));
            Assert.IsTrue(IsRelevant("[C1:  v1.1]", "(C1: v <= 1.1, v1.2)"));
            Assert.IsFalse(IsRelevant("[C1:  v1.2]", "(C1: v <= 1.1, v1.2)"));
        }
        [TestMethod]
        public void Patching_Version_Relevance_BothVersionsDefined()
        {
            // Package     Patch                        IsRelevant
            // ----------- ---------------------------- ----------
            // [C1:  v1.0] (C1: 1.1 <= v <= 1.1, v1.3)  no         // v = 1.1
            // [C1:  v1.1] (C1: 1.1 <= v <= 1.1, v1.3)  yes        // v = 1.1
            // [C1:  v1.2] (C1: 1.1 <= v <= 1.1, v1.3)  no         // v = 1.1
            Assert.IsFalse(IsRelevant("[C1:  v1.0]", "(C1: 1.1 <= v <= 1.1, v1.3)"));
            Assert.IsTrue(IsRelevant("[C1:  v1.1]", "(C1: 1.1 <= v <= 1.1, v1.3)"));
            Assert.IsFalse(IsRelevant("[C1:  v1.2]", "(C1: 1.1 <= v <= 1.1, v1.3)"));

            // Package     Patch                        IsRelevant
            // ----------- ---------------------------- ----------
            // [C1:  v1.0] (C1: 1.1 <= v <= 1.3, v1.4)  no
            // [C1:  v1.1] (C1: 1.1 <= v <= 1.3, v1.4)  yes
            // [C1:  v1.2] (C1: 1.1 <= v <= 1.3, v1.4)  yes
            // [C1:  v1.3] (C1: 1.1 <= v <= 1.3, v1.4)  yes
            // [C1:  v1.4] (C1: 1.1 <= v <= 1.3, v1.4)  no
            Assert.IsFalse(IsRelevant("[C1:  v1.0]", "(C1: 1.1 <= v <= 1.3, v1.4)"));
            Assert.IsTrue(IsRelevant("[C1:  v1.1]", "(C1: 1.1 <= v <= 1.3, v1.4)"));
            Assert.IsTrue(IsRelevant("[C1:  v1.2]", "(C1: 1.1 <= v <= 1.3, v1.4)"));
            Assert.IsTrue(IsRelevant("[C1:  v1.3]", "(C1: 1.1 <= v <= 1.3, v1.4)"));
            Assert.IsFalse(IsRelevant("[C1:  v1.4]", "(C1: 1.1 <= v <= 1.3, v1.4)"));

            // Package     Patch                        IsRelevant
            // ----------- ---------------------------- ----------
            // [C1:  v1.0] (C1: 1.1 <  v <= 1.3, v1.4)  no
            // [C1:  v1.1] (C1: 1.1 <  v <= 1.3, v1.4)  no
            // [C1:  v1.2] (C1: 1.1 <  v <= 1.3, v1.4)  yes
            // [C1:  v1.3] (C1: 1.1 <  v <= 1.3, v1.4)  yes
            // [C1:  v1.4] (C1: 1.1 <  v <= 1.3, v1.4)  no
            Assert.IsFalse(IsRelevant("[C1:  v1.0]", "(C1: 1.1 <  v <= 1.3, v1.4)"));
            Assert.IsFalse(IsRelevant("[C1:  v1.1]", "(C1: 1.1 <  v <= 1.3, v1.4)"));
            Assert.IsTrue(IsRelevant("[C1:  v1.2]", "(C1: 1.1 <  v <= 1.3, v1.4)"));
            Assert.IsTrue(IsRelevant("[C1:  v1.3]", "(C1: 1.1 <  v <= 1.3, v1.4)"));
            Assert.IsFalse(IsRelevant("[C1:  v1.4]", "(C1: 1.1 <  v <= 1.3, v1.4)"));

            // Package     Patch                        IsRelevant
            // ----------- ---------------------------- ----------
            // [C1:  v1.0] (C1: 1.1 <= v <  1.3, v1.4)  no
            // [C1:  v1.1] (C1: 1.1 <= v <  1.3, v1.4)  yes
            // [C1:  v1.2] (C1: 1.1 <= v <  1.3, v1.4)  yes
            // [C1:  v1.3] (C1: 1.1 <= v <  1.3, v1.4)  no
            // [C1:  v1.4] (C1: 1.1 <= v <  1.3, v1.4)  no
            Assert.IsFalse(IsRelevant("[C1:  v1.0]", "(C1: 1.1 <= v <  1.3, v1.4)"));
            Assert.IsTrue(IsRelevant("[C1:  v1.1]", "(C1: 1.1 <= v <  1.3, v1.4)"));
            Assert.IsTrue(IsRelevant("[C1:  v1.2]", "(C1: 1.1 <= v <  1.3, v1.4)"));
            Assert.IsFalse(IsRelevant("[C1:  v1.3]", "(C1: 1.1 <= v <  1.3, v1.4)"));
            Assert.IsFalse(IsRelevant("[C1:  v1.4]", "(C1: 1.1 <= v <  1.3, v1.4)"));

            // Package     Patch                        IsRelevant
            // ----------- ---------------------------- ----------
            // [C1:  v1.0] (C1: 1.1 <  v <  1.3, v1.4)  no
            // [C1:  v1.1] (C1: 1.1 <  v <  1.3, v1.4)  no
            // [C1:  v1.2] (C1: 1.1 <  v <  1.3, v1.4)  yes
            // [C1:  v1.3] (C1: 1.1 <  v <  1.3, v1.4)  no
            // [C1:  v1.4] (C1: 1.1 <  v <  1.3, v1.4)  no
            Assert.IsFalse(IsRelevant("[C1:  v1.0]", "(C1: 1.1 <  v <  1.3, v1.4)"));
            Assert.IsFalse(IsRelevant("[C1:  v1.1]", "(C1: 1.1 <  v <  1.3, v1.4)"));
            Assert.IsTrue(IsRelevant("[C1:  v1.2]", "(C1: 1.1 <  v <  1.3, v1.4)"));
            Assert.IsFalse(IsRelevant("[C1:  v1.3]", "(C1: 1.1 <  v <  1.3, v1.4)"));
            Assert.IsFalse(IsRelevant("[C1:  v1.4]", "(C1: 1.1 <  v <  1.3, v1.4)"));
        }
        private bool IsRelevant(string package, string patch)
        {
            var packages = Packages(package).ToArray();
            var patches = Patches(patch).ToArray();
            var relevantPatches = GetRelevantPatches(packages, patches);
            return relevantPatches.Any(x => x.Id == patches.First().Id);
        }
        private IEnumerable<Package> Packages(params string[] src)
        {
            return src.Select(x =>
            {
                var segments = x.TrimStart('[').TrimEnd(']').Split(':');
                return Package(segments[0].Trim(), segments[1].Trim(), null);
            });
        }
        private IEnumerable<SnPatch> Patches(params string[] src)
        {
            return src.Select(x =>
            {
                var segments = x.TrimStart('(').TrimEnd(')').Split(':');
                var id = segments[0].Trim();
                segments = segments[1].Trim().Split(',');
                return Patch(id, segments[0].Trim(), segments[1].Trim());
            });
        }

    }
}
