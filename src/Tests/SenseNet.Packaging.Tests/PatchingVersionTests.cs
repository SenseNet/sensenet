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
            throw new NotImplementedException();
            // Input                       Error
            // --------------------------- ---------------------
            // (??: 1.0 <= v <= 1.0, v1.2) Missing id
            // (C1: 1.0 <= v <= 1.0, ????) Missing target version
            // (C1: 1.0 <= v <= 1.0, v0.9) Downgrade
        }
        [TestMethod]
        public void Patching_Version_Check_BoundaryAutoFill()
        {
            throw new NotImplementedException();
            // PRE CHECK: Patching_Version_Check_BoundaryAutoFill
            // Input                       Output
            // --------------------------- ---------------------
            // (C1:        v <= 1.0, v1.2) (C1: 0.0.0.0 <= v <= 1.0, v1.2)
            // (C1: 1.0 <= v       , v1.2) (C1: 0.0.0.0 <= v <  1.2, v1.2)
        }
        [TestMethod]
        public void Patching_Version_Check_BoundaryOverlapped_OnePatch()
        {
            throw new NotImplementedException();
            // Patch                       Error
            // --------------------------- ---------------------
            // (C1: 1.0 <= v <= 1.0, v1.2) ok
            // (C1: 1.0 <= v <  1.0, v1.2) Invalid interval
            // (C1: 1.0 <  v <= 1.0, v1.2) Invalid interval
            // (C1: 1.0 <  v <  1.0, v1.2) Invalid interval
            // (C1: 1.1 <= v <= 1.0, v1.2) Max is less than min
            // (C1: 1.1 <= v <  1.0, v1.2) Max is less than min
            // (C1: 1.1 <  v <= 1.0, v1.2) Max is less than min
            // (C1: 1.1 <  v <  1.0, v1.2) Max is less than min
        }
        [TestMethod]
        public void Patching_Version_Check_BoundaryOverlapped_MorePatches()
        {
            throw new NotImplementedException();
            // Patch1                      Patch2                      Error
            // --------------------------- --------------------------- ---------------------
            // (C1: 1.1 <= v <= 1.1, v1.3) (C1: 1.2 <= v <= 1.2, v1.3) Targets are the same
            // (C1: 1.1 <= v <= 1.1, v1.3) (C1: 1.1 <= v <= 1.1, v1.4) Sources are the same
            // 
            // (C1: 1.0 <= v <  2.0, v2.0) (C1: 2.0 <= v <  3.0, v3.0) ok
            // (C1: 1.0 <= v <  2.0, v2.0) (C1: 1.9 <= v <  3.0, v3.0) Overlapped
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
