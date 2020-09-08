using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SenseNet.Packaging.Tests
{
    [TestClass]
    public class PatchingVersionTests : PatchingTestBase
    {
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
                ValidatePatch(Patch(null, "1.0 <= v <= 1.0", "v1.2", null));
                Assert.Fail();
            }
            catch (InvalidPackageException e)
            {
                Assert.AreEqual(e.ErrorType, PackagingExceptionType.MissingComponentId);
            }

            try
            {
                ValidatePatch(Patch(string.Empty, "1.0 <= v <= 1.0", "v1.2", null));
                Assert.Fail();
            }
            catch (InvalidPackageException e)
            {
                Assert.AreEqual(e.ErrorType, PackagingExceptionType.MissingComponentId);
            }

            try
            {
                ValidatePatch(Patch("C1", "1.0 <= v <= 1.0", null, null));
                Assert.Fail();
            }
            catch (InvalidPackageException e)
            {
                Assert.AreEqual(e.ErrorType, PackagingExceptionType.MissingVersion);
            }

            try
            {
                ValidatePatch(Patch("C1", "1.0 <= v <= 1.0", "v0.9", null));
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

            var patch = Patch("C1", "       v <= 1.0", "v1.2", null);
            ValidatePatch(patch);
            Assert.AreEqual(Version.Parse("0.0"), patch.Boundary.MinVersion);
            Assert.IsFalse(patch.Boundary.MinVersionIsExclusive);

            patch = Patch("C1", "1.0 <= v       ", "v1.2", null);
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
                    ValidatePatch(Patch("C1", boundary, "v1.2", null));
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
            ValidatePatch(Patch("C1", "1.0 <= v <= 1.0", "v1.2", null));
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
                Patch("C1", "1.1 <= v <= 1.1", "v1.3", null),
                Patch("C1", "1.2 <= v <= 1.2", "v1.3", null),
                PackagingExceptionType.TargetVersionsAreTheSame);
            CheckError(
                Patch("C1", "1.1 <= v <= 1.1", "v1.3", null),
                Patch("C1", "1.1 <= v <= 1.1", "v1.4", null),
                PackagingExceptionType.SourceVersionsAreTheSame);

            // Patch1                      Patch2                      Error
            // --------------------------- --------------------------- ---------------------
            // (C1: 1.0 <= v <  2.0, v2.0) (C1: 2.0 <  v <  3.0, v3.0) ok
            // (C1: 1.0 <= v <= 2.0, v2.0) (C1: 2.0 <  v <  3.0, v3.0) ok
            // (C1: 1.0 <= v <  2.0, v2.0) (C1: 2.0 <= v <  3.0, v3.0) ok
            // (C1: 1.0 <= v <= 2.0, v2.0) (C1: 2.0 <= v <  3.0, v3.0) Overlapped
            // (C1: 1.0 <= v <  2.0, v2.0) (C1: 1.9 <= v <  3.0, v3.0) Overlapped

            ValidatePatches(
                Patch("C1", "1.0 <= v <  2.0", "v2.0", null),
                Patch("C1", "2.0 <  v <  3.0", "v3.0", null));
            ValidatePatches(
                Patch("C1", "1.0 <= v <= 2.0", "v2.0", null),
                Patch("C1", "2.0 <  v <  3.0", "v3.0", null));
            ValidatePatches(
                Patch("C1", "1.0 <= v <  2.0", "v2.0", null),
                Patch("C1", "2.0 <= v <  3.0", "v3.0", null));
            CheckError(
                Patch("C1", "1.0 <= v <= 2.0", "v2.0", null),
                Patch("C1", "2.0 <= v <  3.0", "v3.0", null),
                PackagingExceptionType.OverlappedIntervals);
            CheckError(
                Patch("C1", "1.0 <= v <  2.0", "v2.0", null),
                Patch("C1", "1.9 <= v <  3.0", "v3.0", null),
                PackagingExceptionType.OverlappedIntervals);
        }
        [TestMethod]
        public void Patching_Version_Check_WrongDependency()
        {
            throw new NotImplementedException();
            // Patch                                                  Error
            // ------------------------------------------------------ ---------------------
            // (C1: 1.0 <= v <= 1.0, v1.2, {DEP C1: 1.0 <= v <= 1.0}) Patch and dependency id are the same
        }

        /* =================================================================== PATCH RELEVANCE TESTS */
        /* Also known as executability test. Relevance is determined by the current state of the     */
        /* components and the patch's' target version and boundary.                                  */

        [TestMethod]
        public void Patching_Version_Relevance_NotInstalledComponent()
        {
            throw new NotImplementedException();
            // Package    Patch                       IsRelevant (will be executed or not)
            // ---------- --------------------------- ----------
            // [C1: v1.0] (C2: 1.0 <= v <= 1.0, v1.1) no
        }
        [TestMethod]
        public void Patching_Version_Relevance_MaxVersionNotDefined()
        {
            throw new NotImplementedException();
            // Package     Patch                IsRelevant
            // ----------- -------------------- ----------
            // [C1:  v1.0] (C1: 1.0 <= v, v1.1) yes
            // [C1:  v1.0] (C1: 1.0 <  v, v1.1) no
            // [C1:  v1.1] (C1: 1.0 <= v, v1.1) no
            // [C1:  v1.1] (C1: 1.0 <= v, v1.2) yes
            // [C1:  v1.1] (C1: 1.0 <  v, v1.2) yes
        }
        [TestMethod]
        public void Patching_Version_Relevance_MinVersionNotDefined()
        {
            throw new NotImplementedException();
            // Package     Patch                IsRelevant
            // ----------- -------------------- ----------
            // [C1:  v1.0] (C1: v <= 1.0, v1.1) yes
            // [C1:  v1.0] (C1: v <= 1.1, v1.2) yes
            // [C1:  v1.1] (C1: v <  1.1, v1.2) no
            // [C1:  v1.2] (C1: v <  1.1, v1.2) yes
            // [C1:  v1.2] (C1: v <= 1.1, v1.2) no
        }
        [TestMethod]
        public void Patching_Version_Relevance_BothVersionsDefined()
        {
            throw new NotImplementedException();
            // Package     Patch                        IsRelevant
            // ----------- ---------------------------- ----------
            // [C1:  v1.0] (C1: 1.1 <= v <= 1.1, v1.3)  no         // v = 1.1
            // [C1:  v1.1] (C1: 1.1 <= v <= 1.1, v1.3)  yes        // v = 1.1
            // [C1:  v1.2] (C1: 1.1 <= v <= 1.1, v1.3)  no         // v = 1.1

            // [C1:  v1.0] (C1: 1.1 <= v <= 1.3, v1.4)  no
            // [C1:  v1.1] (C1: 1.1 <= v <= 1.3, v1.4)  yes
            // [C1:  v1.2] (C1: 1.1 <= v <= 1.3, v1.4)  yes
            // [C1:  v1.3] (C1: 1.1 <= v <= 1.3, v1.4)  yes
            // [C1:  v1.4] (C1: 1.1 <= v <= 1.3, v1.4)  no

            // [C1:  v1.0] (C1: 1.1 <  v <= 1.3, v1.4)  no
            // [C1:  v1.1] (C1: 1.1 <  v <= 1.3, v1.4)  no
            // [C1:  v1.2] (C1: 1.1 <  v <= 1.3, v1.4)  yes
            // [C1:  v1.3] (C1: 1.1 <  v <= 1.3, v1.4)  yes
            // [C1:  v1.4] (C1: 1.1 <  v <= 1.3, v1.4)  no

            // [C1:  v1.0] (C1: 1.1 <= v <  1.3, v1.4)  no
            // [C1:  v1.1] (C1: 1.1 <= v <  1.3, v1.4)  yes
            // [C1:  v1.2] (C1: 1.1 <= v <  1.3, v1.4)  yes
            // [C1:  v1.3] (C1: 1.1 <= v <  1.3, v1.4)  no
            // [C1:  v1.4] (C1: 1.1 <= v <  1.3, v1.4)  no

            // [C1:  v1.0] (C1: 1.1 <  v <  1.3, v1.4)  no
            // [C1:  v1.1] (C1: 1.1 <  v <  1.3, v1.4)  no
            // [C1:  v1.2] (C1: 1.1 <  v <  1.3, v1.4)  yes
            // [C1:  v1.3] (C1: 1.1 <  v <  1.3, v1.4)  no
            // [C1:  v1.4] (C1: 1.1 <  v <  1.3, v1.4)  no
        }
    }
}
