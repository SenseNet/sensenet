using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.Packaging.Tests
{
    [TestClass]
    public class PatchingExecutionTests : PatchingTestBase
    {
        /* ======================================================== SIMPLE INSTALLER TESTS */

        [TestMethod]
        public void Patching_ExecSim_Install_1New()
        {
            var log = new List<PatchExecutionLogRecord>();
            void Log(PatchExecutionLogRecord record) { log.Add(record); }
            void Exec(PatchExecutionContext peContext) { }

            var installed = new List<SnComponentDescriptor>();
            var candidates = new List<ISnPatch>
            {
                Inst("C1", "v1.0", Exec, Exec),
            };

            // ACTION BEFORE
            var pm = new PatchManager(null, Log);
            pm.ExecuteOnBefore(candidates, installed, true);

            // ASSERT BEFORE
            Assert.AreEqual(1, candidates.Count);
            Assert.AreEqual("C1v(1.0,,SuccessfulBefore)", ComponentsToStringWithResult(installed));

            // ACTION AFTER
            pm.ExecuteOnAfter(candidates, installed, true);

            // ASSERT AFTER
            Assert.AreEqual(0, candidates.Count);
            Assert.AreEqual("C1v1.0(1.0,1.0,Successful)", ComponentsToStringWithResult(installed));
            Assert.AreEqual("[C1: 1.0] OnBeforeActionStarts.|" +
                            "[C1: 1.0] OnBeforeActionFinished.|" +
                            "[C1: 1.0] OnAfterActionStarts.|" +
                            "[C1: 1.0] OnAfterActionFinished.",
                string.Join("|", log.Select(x => x.ToString(false))));
        }
        [TestMethod]
        public void Patching_ExecSim_Install_1New1Skip()
        {
            void Exec(PatchExecutionContext peContext) { }

            var installed = new List<SnComponentDescriptor>()
            {
                Comp("C1", "v1.0")
            };
            var candidates = new List<ISnPatch>
            {
                Inst("C1", "v1.0", Exec, Exec),
                Inst("C2", "v2.3", Exec, Exec),
            };

            // ACTION BEFORE
            var pm = new PatchManager(null, null);
            pm.ExecuteOnBefore(candidates, installed, true);

            // ASSERT BEFORE
            Assert.AreEqual(2, candidates.Count);
            Assert.AreEqual("C1v1.0(,,Successful) C2v(2.3,,SuccessfulBefore)", ComponentsToStringWithResult(installed));

            // ACTION AFTER
            pm.ExecuteOnAfter(candidates, installed, true);

            // ASSERT AFTER
            Assert.AreEqual(0, candidates.Count);
            Assert.AreEqual("C1v1.0(,,Successful) C2v2.3(2.3,2.3,Successful)", ComponentsToStringWithResult(installed));
        }

        [TestMethod]
        public void Patching_ExecSim_Install_Duplicates()
        {
            void Exec(PatchExecutionContext peContext) { }

            var installed = new List<SnComponentDescriptor>()
            {
                Comp("C1", "v1.0")
            };
            var candidates = new List<ISnPatch>
            {
                // Will be skipped. The same id does not cause any error because this patch is irrelevant.
                Inst("C1", "v1.0", Exec, Exec),
                Inst("C1", "v1.1", Exec, Exec),
                // Would be executable but the same id causes an error.
                Inst("C2", "v1.0", Exec, Exec),
                Inst("C3", "v1.0", Exec, Exec),
                Inst("C3", "v2.3", Exec, Exec),
                Inst("C4", "v1.0", Exec, Exec),
                Inst("C4", "v2.3", Exec, Exec),
                Inst("C4", "v1.0", Exec, Exec),
            };

            // ACTION BEFORE
            var pm = new PatchManager(null, null);
            pm.ExecuteOnBefore(candidates, installed, true);

            // ASSERT BEFORE
            Assert.AreEqual(1, candidates.Count);
            Assert.AreEqual("C1v1.0(,,Successful) C2v(1.0,,SuccessfulBefore)", ComponentsToStringWithResult(installed));
            var errors = pm.Errors;
            Assert.AreEqual(3, errors.Count);
            Assert.IsTrue(errors[0].Message.Contains("C1"));
            Assert.IsTrue(errors[1].Message.Contains("C3"));
            Assert.IsTrue(errors[2].Message.Contains("C4"));
            Assert.AreEqual("DuplicatedInstaller C1: 1.0; C1: 1.1|" +
                            "DuplicatedInstaller C3: 1.0; C3: 2.3|" +
                            "DuplicatedInstaller C4: 1.0; C4: 1.0; C4: 2.3", ErrorsToString(errors));

            // ACTION AFTER
            pm.ExecuteOnAfter(candidates, installed, true);

            // ASSERT AFTER
            Assert.AreEqual(0, candidates.Count);
            Assert.AreEqual("C1v1.0(,,Successful) C2v1.0(1.0,1.0,Successful)", ComponentsToStringWithResult(installed));
        }
        [TestMethod]
        public void Patching_ExecSim_Install_Duplicates_OnlyAfter()
        {
            void Exec(PatchExecutionContext peContext) { }

            var installed = new List<SnComponentDescriptor>()
            {
                Comp("C1", "v1.0")
            };
            var candidates = new List<ISnPatch>
            {
                // Will be skipped. The same id does not cause any error because this patch is irrelevant.
                Inst("C1", "v1.0", Exec),
                Inst("C1", "v1.1", Exec),
                // Would be executable but the same id causes an error.
                Inst("C2", "v1.0", Exec),
                Inst("C3", "v1.0", Exec),
                Inst("C3", "v2.3", Exec),
                Inst("C4", "v1.0", Exec),
                Inst("C4", "v2.3", Exec),
                Inst("C4", "v1.0", Exec),
            };

            // ACTION BEFORE
            var pm = new PatchManager(null, null);
            pm.ExecuteOnBefore(candidates, installed, true);

            // ASSERT BEFORE
            Assert.AreEqual(8, candidates.Count);
            Assert.AreEqual("C1v1.0(,,Successful)", ComponentsToStringWithResult(installed));
            var errors = pm.Errors;
            Assert.AreEqual(0, errors.Count);

            // ACTION AFTER
            pm.ExecuteOnAfter(candidates, installed, true);

            // ASSERT AFTER
            Assert.AreEqual(0, candidates.Count);
            Assert.AreEqual("C1v1.0(,,Successful) C2v1.0(1.0,1.0,Successful)", ComponentsToStringWithResult(installed));
            Assert.AreEqual("DuplicatedInstaller C1: 1.0; C1: 1.1|" +
                            "DuplicatedInstaller C3: 1.0; C3: 2.3|" +
                            "DuplicatedInstaller C4: 1.0; C4: 1.0; C4: 2.3", ErrorsToString(errors));
        }
        [TestMethod]
        public void Patching_ExecSim_Install_Dependency()
        {
            // Test the right installer execution order if there is a dependency among the installers.

            var log = new List<PatchExecutionLogRecord>();
            void Log(PatchExecutionLogRecord record) { log.Add(record); }
            void Exec(PatchExecutionContext peContext) { }

            var installed = new List<SnComponentDescriptor>();
            var candidates = new List<ISnPatch>
            {
                Inst("C1", "v1.0", new[] {Dep("C2", "1.0 <= v")},Exec, Exec),
                Inst("C2", "v1.0", Exec, Exec),
            };

            // ACTION BEFORE
            var pm = new PatchManager(null, Log);
            pm.ExecuteOnBefore(candidates, installed, true);

            // ASSERT BEFORE
            Assert.AreEqual(2, candidates.Count);
            Assert.AreEqual("C1v(1.0,,SuccessfulBefore) C2v(1.0,,SuccessfulBefore)", ComponentsToStringWithResult(installed));

            // ACTION AFTER
            pm.ExecuteOnAfter(candidates, installed, true);

            // ASSERT AFTER
            Assert.AreEqual(0, candidates.Count);
            Assert.AreEqual("C1v1.0(1.0,1.0,Successful) C2v1.0(1.0,1.0,Successful)", ComponentsToStringWithResult(installed));
            Assert.AreEqual("[C2: 1.0] OnBeforeActionStarts.|" +
                            "[C2: 1.0] OnBeforeActionFinished.|" +
                            "[C1: 1.0] OnBeforeActionStarts.|" +
                            "[C1: 1.0] OnBeforeActionFinished.|" +
                            "[C2: 1.0] OnAfterActionStarts.|" +
                            "[C2: 1.0] OnAfterActionFinished.|" +
                            "[C1: 1.0] OnAfterActionStarts.|" +
                            "[C1: 1.0] OnAfterActionFinished.",
                string.Join("|", log.Select(x => x.ToString(false))));
        }
        [TestMethod]
        public void Patching_ExecSim_Install_Dependency2()
        {
            // Test the right installer execution order if there is a dependency among the installers.
            // One dependency is exist, 2 will be installed.

            var log = new List<PatchExecutionLogRecord>();
            void Log(PatchExecutionLogRecord record) { log.Add(record); }
            void Exec(PatchExecutionContext peContext) { }

            var installed = new List<SnComponentDescriptor>
            {
                Comp("C1", "v1.0")
            };
            var candidates = new List<ISnPatch>
            {
                Inst("C2", "v1.0", new[] {Dep("C1", "1.0 <= v")}, Exec, Exec),
                Inst("C3", "v1.0", new[] {Dep("C4", "1.0 <= v"), Dep("C2", "1.0 <= v")}, Exec, Exec),
                Inst("C4", "v1.0", new[] {Dep("C1", "1.0 <= v"), Dep("C2", "1.0 <= v")}, Exec, Exec),
            };

            // ACTION BEFORE
            var pm = new PatchManager(null, Log);
            pm.ExecuteOnBefore(candidates, installed, true);

            // ASSERT BEFORE
            Assert.AreEqual(3, candidates.Count);
            Assert.AreEqual("C1v1.0(,,Successful) C2v(1.0,,SuccessfulBefore) " +
                            "C3v(1.0,,SuccessfulBefore) C4v(1.0,,SuccessfulBefore)", ComponentsToStringWithResult(installed));

            // ACTION AFTER
            pm.ExecuteOnAfter(candidates, installed, true);

            // ASSERT AFTER
            Assert.AreEqual(0, candidates.Count);
            Assert.AreEqual("C1v1.0(,,Successful) C2v1.0(1.0,1.0,Successful) " +
                            "C3v1.0(1.0,1.0,Successful) C4v1.0(1.0,1.0,Successful)", ComponentsToStringWithResult(installed));
            Assert.AreEqual("[C2: 1.0] OnBeforeActionStarts.|" +
                            "[C2: 1.0] OnBeforeActionFinished.|" +
                            "[C4: 1.0] OnBeforeActionStarts.|" +
                            "[C4: 1.0] OnBeforeActionFinished.|" +
                            "[C3: 1.0] OnBeforeActionStarts.|" +
                            "[C3: 1.0] OnBeforeActionFinished.|" +
                            "[C2: 1.0] OnAfterActionStarts.|" +
                            "[C2: 1.0] OnAfterActionFinished.|" +
                            "[C4: 1.0] OnAfterActionStarts.|" +
                            "[C4: 1.0] OnAfterActionFinished.|" +
                            "[C3: 1.0] OnAfterActionStarts.|" +
                            "[C3: 1.0] OnAfterActionFinished.",
                string.Join("|", log.Select(x => x.ToString(false))));
        }
        [TestMethod]
        public void Patching_ExecSim_Install_Dependency_Empty()
        {
            // Test the right installer execution order if there is a dependency among the installers.
            // One dependency is exist, 2 will be installed.

            var log = new List<PatchExecutionLogRecord>();
            void Log(PatchExecutionLogRecord record) { log.Add(record); }
            void Exec(PatchExecutionContext peContext) { }

            var installed = new List<SnComponentDescriptor>();
            var candidates = new List<ISnPatch>
            {
                Inst("C1", "v1.0", new Dependency[0], Exec, Exec),
                Patch("C1", "1.0 <= v", "2.0", new Dependency[0], Exec, Exec),
            };

            // ACTION BEFORE
            var pm = new PatchManager(null, Log);
            pm.ExecuteOnBefore(candidates, installed, true);

            // ASSERT BEFORE
            Assert.AreEqual(2, candidates.Count);
            Assert.AreEqual("C1v(2.0,,SuccessfulBefore)", ComponentsToStringWithResult(installed));

            // ACTION AFTER
            pm.ExecuteOnAfter(candidates, installed, true);

            // ASSERT AFTER
            Assert.AreEqual(0, candidates.Count);
            Assert.AreEqual("C1v2.0(2.0,2.0,Successful)", ComponentsToStringWithResult(installed));
            Assert.AreEqual("[C1: 1.0] OnBeforeActionStarts.|" +
                            "[C1: 1.0] OnBeforeActionFinished.|" +
                            "[C1: 1.0 <= v --> 2.0] OnBeforeActionStarts.|" +
                            "[C1: 1.0 <= v --> 2.0] OnBeforeActionFinished.|" +
                            "[C1: 1.0] OnAfterActionStarts.|" +
                            "[C1: 1.0] OnAfterActionFinished.|" +
                            "[C1: 1.0 <= v --> 2.0] OnAfterActionStarts.|" +
                            "[C1: 1.0 <= v --> 2.0] OnAfterActionFinished.",
                string.Join("|", log.Select(x => x.ToString(false))));
        }
        [TestMethod]
        public void Patching_ExecSim_Install_Dependencies_Circular()
        {
            var log = new List<PatchExecutionLogRecord>();
            void Log(PatchExecutionLogRecord record) { log.Add(record); }
            void Exec(PatchExecutionContext peContext) { }

            var installed = new List<SnComponentDescriptor>
            {
                Comp("C1", "v1.0")
            };
            var candidates = new List<ISnPatch>
            {
                Inst("C2", "v1.0", new[] {Dep("C3", "1.0 <= v")}, Exec, Exec),
                Inst("C3", "v1.0", new[] {Dep("C4", "1.0 <= v")}, Exec, Exec),
                Inst("C4", "v1.0", new[] {Dep("C2", "1.0 <= v")}, Exec, Exec),
            };

            // ACTION BEFORE
            var pm = new PatchManager(null, Log);
            pm.ExecuteOnBefore(candidates, installed, true);

            // ASSERT BEFORE
            Assert.AreEqual(3, candidates.Count);
            Assert.AreEqual("C1v1.0(,,Successful)", ComponentsToStringWithResult(installed));

            // ACTION AFTER
            pm.ExecuteOnAfter(candidates, installed, true);

            // ASSERT AFTER
            Assert.AreEqual(0, candidates.Count);
            Assert.AreEqual("C1v1.0(,,Successful)", ComponentsToStringWithResult(installed));
            Assert.AreEqual("[C2: 1.0] CannotExecuteOnBefore.|" +
                            "[C3: 1.0] CannotExecuteOnBefore.|" +
                            "[C4: 1.0] CannotExecuteOnBefore.|" +
                            "[C2: 1.0] CannotExecuteOnAfter.|" +
                            "[C3: 1.0] CannotExecuteOnAfter.|" +
                            "[C4: 1.0] CannotExecuteOnAfter.",
                string.Join("|", log.Select(x => x.ToString(false))));

            // ASSERT
            // Circular dependency causes error but the reason is not recognized yet.
            Assert.AreEqual("CannotExecuteOnBefore C2: 1.0|" +
                            "CannotExecuteOnBefore C3: 1.0|" +
                            "CannotExecuteOnBefore C4: 1.0|" +
                            "CannotExecuteOnAfter C2: 1.0|" +
                            "CannotExecuteOnAfter C3: 1.0|" +
                            "CannotExecuteOnAfter C4: 1.0", ErrorsToString(pm.Errors));
        }

        /* ======================================================== COMPLEX INSTALL & PATCHING SIMULATION TESTS */

        [TestMethod]
        public void Patching_ExecSim_Patch_WithoutInstaller()
        {
            var log = new List<PatchExecutionLogRecord>();
            void Log(PatchExecutionLogRecord record) { log.Add(record); }
            void Exec(PatchExecutionContext peContext) { }

            var installed = new List<SnComponentDescriptor>();
            var candidates = new List<ISnPatch>
            {
                Inst("C2", "v1.0", Exec, Exec),
                Patch("C1", "1.0 <= v <  2.0", "v2.0", Exec, Exec),
            };

            // ACTION BEFORE
            var pm = new PatchManager(null, Log);
            pm.ExecuteOnBefore(candidates, installed, true);

            // ASSERT BEFORE
            Assert.AreEqual(2, candidates.Count);
            Assert.AreEqual("C2v(1.0,,SuccessfulBefore)", ComponentsToStringWithResult(installed));
            Assert.AreEqual("MissingVersion C1: 1.0 <= v < 2.0 --> 2.0", ErrorsToString(pm.Errors));

            // ACTION AFTER
            pm.ExecuteOnAfter(candidates, installed, true);

            // ASSERT AFTER
            Assert.AreEqual(0, candidates.Count);
            Assert.AreEqual("C2v1.0(1.0,1.0,Successful)", ComponentsToStringWithResult(installed));
            Assert.AreEqual("[C2: 1.0] OnBeforeActionStarts.|" +
                            "[C2: 1.0] OnBeforeActionFinished.|" +
                            "[C1: 1.0 <= v < 2.0 --> 2.0] CannotExecuteMissingVersion.|" +
                            "[C2: 1.0] OnAfterActionStarts.|" +
                            "[C2: 1.0] OnAfterActionFinished.|" +
                            "[C1: 1.0 <= v < 2.0 --> 2.0] CannotExecuteMissingVersion.",
                string.Join("|", log.Select(x => x.ToString(false))));
            Assert.AreEqual("MissingVersion C1: 1.0 <= v < 2.0 --> 2.0|" +
                            "MissingVersion C1: 1.0 <= v < 2.0 --> 2.0", ErrorsToString(pm.Errors));
        }

        // SIMULATION: Install C1v1.0 and patch it to v3.0 with different pre-installation states 
        [TestMethod]
        public void Patching_ExecSim_Patch_C1_a()
        {
            var log = new List<PatchExecutionLogRecord>();
            void Log(PatchExecutionLogRecord record) { log.Add(record); }
            void Exec(PatchExecutionContext peContext) { }

            var installed = new List<SnComponentDescriptor>();
            var candidates = new List<ISnPatch>
            {
                Inst("C1", "v1.0", Exec, Exec),
                Patch("C1", "1.0 <= v <  2.0", "v2.0", Exec, Exec),
                Patch("C1", "2.0 <= v <  3.0", "v3.0", Exec, Exec),
            };

            // ACTION BEFORE
            var pm = new PatchManager(null, Log);
            pm.ExecuteOnBefore(candidates, installed, true);

            // ASSERT BEFORE
            Assert.AreEqual(3, candidates.Count);
            Assert.AreEqual("C1v(3.0,,SuccessfulBefore)", ComponentsToStringWithResult(installed));

            // ACTION AFTER
            pm.ExecuteOnAfter(candidates, installed, true);

            // ASSERT AFTER
            Assert.AreEqual(0, candidates.Count);
            Assert.AreEqual("C1v3.0(3.0,3.0,Successful)", ComponentsToStringWithResult(installed));
            Assert.AreEqual("[C1: 1.0] OnBeforeActionStarts.|" +
                            "[C1: 1.0] OnBeforeActionFinished.|" +
                            "[C1: 1.0 <= v < 2.0 --> 2.0] OnBeforeActionStarts.|" +
                            "[C1: 1.0 <= v < 2.0 --> 2.0] OnBeforeActionFinished.|" +
                            "[C1: 2.0 <= v < 3.0 --> 3.0] OnBeforeActionStarts.|" +
                            "[C1: 2.0 <= v < 3.0 --> 3.0] OnBeforeActionFinished.|" +
                            "[C1: 1.0] OnAfterActionStarts.|" +
                            "[C1: 1.0] OnAfterActionFinished.|" +
                            "[C1: 1.0 <= v < 2.0 --> 2.0] OnAfterActionStarts.|" +
                            "[C1: 1.0 <= v < 2.0 --> 2.0] OnAfterActionFinished.|" +
                            "[C1: 2.0 <= v < 3.0 --> 3.0] OnAfterActionStarts.|" +
                            "[C1: 2.0 <= v < 3.0 --> 3.0] OnAfterActionFinished.",
                string.Join("|", log.Select(x => x.ToString(false))));
            Assert.AreEqual("", ErrorsToString(pm.Errors));
        }
        [TestMethod]
        public void Patching_ExecSim_Patch_C1_b()
        {
            var log = new List<PatchExecutionLogRecord>();
            void Log(PatchExecutionLogRecord record) { log.Add(record); }
            void Exec(PatchExecutionContext peContext) { }

            var installed = new List<SnComponentDescriptor>()
            {
                Comp("C1", "v1.0")
            };
            var candidates = new List<ISnPatch>
            {
                Inst("C1", "v1.0", Exec, Exec),
                Patch("C1", "1.0 <= v <  2.0", "v2.0", Exec, Exec),
                Patch("C1", "2.0 <= v <  3.0", "v3.0", Exec, Exec),
            };

            // ACTION BEFORE
            var pm = new PatchManager(null, Log);
            pm.ExecuteOnBefore(candidates, installed, true);

            // ASSERT BEFORE
            Assert.AreEqual(3, candidates.Count);
            Assert.AreEqual("C1v1.0(3.0,,SuccessfulBefore)", ComponentsToStringWithResult(installed));

            // ACTION AFTER
            pm.ExecuteOnAfter(candidates, installed, true);

            // ASSERT AFTER
            Assert.AreEqual(0, candidates.Count);
            Assert.AreEqual("C1v3.0(3.0,3.0,Successful)", ComponentsToStringWithResult(installed));
            Assert.AreEqual("[C1: 1.0 <= v < 2.0 --> 2.0] OnBeforeActionStarts.|" +
                            "[C1: 1.0 <= v < 2.0 --> 2.0] OnBeforeActionFinished.|" +
                            "[C1: 2.0 <= v < 3.0 --> 3.0] OnBeforeActionStarts.|" +
                            "[C1: 2.0 <= v < 3.0 --> 3.0] OnBeforeActionFinished.|" +
                            "[C1: 1.0 <= v < 2.0 --> 2.0] OnAfterActionStarts.|" +
                            "[C1: 1.0 <= v < 2.0 --> 2.0] OnAfterActionFinished.|" +
                            "[C1: 2.0 <= v < 3.0 --> 3.0] OnAfterActionStarts.|" +
                            "[C1: 2.0 <= v < 3.0 --> 3.0] OnAfterActionFinished.",
                string.Join("|", log.Select(x => x.ToString(false))));
            Assert.AreEqual("", ErrorsToString(pm.Errors));
        }
        [TestMethod]
        public void Patching_ExecSim_Patch_C1_c()
        {
            var log = new List<PatchExecutionLogRecord>();
            void Log(PatchExecutionLogRecord record) { log.Add(record); }
            void Exec(PatchExecutionContext peContext) { }

            var installed = new List<SnComponentDescriptor>()
            {
                Comp("C1", "v2.0")
            };
            var candidates = new List<ISnPatch>
            {
                Inst("C1", "v1.0", Exec, Exec),
                Patch("C1", "1.0 <= v <  2.0", "v2.0", Exec, Exec),
                Patch("C1", "2.0 <= v <  3.0", "v3.0", Exec, Exec),
            };

            // ACTION BEFORE
            var pm = new PatchManager(null, Log);
            pm.ExecuteOnBefore(candidates, installed, true);

            // ASSERT BEFORE
            Assert.AreEqual(3, candidates.Count);
            Assert.AreEqual("C1v2.0(3.0,,SuccessfulBefore)", ComponentsToStringWithResult(installed));

            // ACTION AFTER
            pm.ExecuteOnAfter(candidates, installed, true);

            // ASSERT AFTER
            Assert.AreEqual(0, candidates.Count);
            Assert.AreEqual("C1v3.0(3.0,3.0,Successful)", ComponentsToStringWithResult(installed));
            Assert.AreEqual("[C1: 2.0 <= v < 3.0 --> 3.0] OnBeforeActionStarts.|" +
                            "[C1: 2.0 <= v < 3.0 --> 3.0] OnBeforeActionFinished.|" +
                            "[C1: 2.0 <= v < 3.0 --> 3.0] OnAfterActionStarts.|" +
                            "[C1: 2.0 <= v < 3.0 --> 3.0] OnAfterActionFinished.",
                string.Join("|", log.Select(x => x.ToString(false))));
            Assert.AreEqual("", ErrorsToString(pm.Errors));
        }
        [TestMethod]
        public void Patching_ExecSim_Patch_C1_d()
        {
            var log = new List<PatchExecutionLogRecord>();
            void Log(PatchExecutionLogRecord record) { log.Add(record); }
            void Exec(PatchExecutionContext peContext) { }

            var installed = new List<SnComponentDescriptor>()
            {
                Comp("C1", "v3.0")
            };
            var candidates = new List<ISnPatch>
            {
                Inst("C1", "v1.0", Exec, Exec),
                Patch("C1", "1.0 <= v <  2.0", "v2.0", Exec, Exec),
                Patch("C1", "2.0 <= v <  3.0", "v3.0", Exec, Exec),
            };

            // ACTION BEFORE
            var pm = new PatchManager(null, Log);
            pm.ExecuteOnBefore(candidates, installed, true);

            // ASSERT BEFORE
            Assert.AreEqual(3, candidates.Count);
            Assert.AreEqual("C1v3.0(,,Successful)", ComponentsToStringWithResult(installed));

            // ACTION AFTER
            pm.ExecuteOnAfter(candidates, installed, true);

            // ASSERT AFTER
            Assert.AreEqual(0, candidates.Count);
            Assert.AreEqual("C1v3.0(,,Successful)", ComponentsToStringWithResult(installed));
            Assert.AreEqual("",
                string.Join("|", log.Select(x => x.ToString(false))));
            Assert.AreEqual("", ErrorsToString(pm.Errors));
        }

        // SIMULATION: Install and patch C2 that depends C1
        [TestMethod]
        public void Patching_ExecSim_Patch_C2toC1v2_a()
        {
            var log = new List<PatchExecutionLogRecord>();
            void Log(PatchExecutionLogRecord record) { log.Add(record); }
            void Exec(PatchExecutionContext peContext) { }

            var installed = new List<SnComponentDescriptor>();
            var candidates = new List<ISnPatch>
            {
                Inst("C1", "v1.0", new[] {Dep("C2", "2.0 <= v <= 2.0")}, Exec, Exec),
                Inst("C2", "v1.0", Exec, Exec),
            };

            // ACTION BEFORE
            var pm = new PatchManager(null, Log);
            pm.ExecuteOnBefore(candidates, installed, true);

            // ASSERT BEFORE
            Assert.AreEqual(2, candidates.Count);
            Assert.AreEqual("C2v(1.0,,SuccessfulBefore)", ComponentsToStringWithResult(installed));

            // ACTION AFTER
            pm.ExecuteOnAfter(candidates, installed, true);

            // ASSERT AFTER
            Assert.AreEqual(0, candidates.Count);
            Assert.AreEqual("C2v1.0(1.0,1.0,Successful)", ComponentsToStringWithResult(installed));
            Assert.AreEqual("[C2: 1.0] OnBeforeActionStarts.|" +
                            "[C2: 1.0] OnBeforeActionFinished.|" +
                            "[C1: 1.0] CannotExecuteOnBefore.|" +
                            "[C2: 1.0] OnAfterActionStarts.|" +
                            "[C2: 1.0] OnAfterActionFinished.|" +
                            "[C1: 1.0] CannotExecuteOnAfter.",
                string.Join("|", log.Select(x => x.ToString(false))));
            Assert.AreEqual("CannotExecuteOnBefore C1: 1.0|" +
                            "CannotExecuteOnAfter C1: 1.0", ErrorsToString(pm.Errors));
        }
        [TestMethod]
        public void Patching_ExecSim_Patch_C2toC1v2_b()
        {
            var log = new List<PatchExecutionLogRecord>();
            void Log(PatchExecutionLogRecord record) { log.Add(record); }
            void Exec(PatchExecutionContext peContext) { }

            var installed = new List<SnComponentDescriptor>();
            var candidates = new List<ISnPatch>
            {
                Inst("C1", "v1.0", new[] {Dep("C2", "2.0 <= v <= 2.0")}, Exec, Exec),
                Inst("C2", "v1.0", Exec, Exec),
                Patch("C2", "1.0 <= v < 2.0", "v2.0", Exec, Exec),
            };

            // ACTION BEFORE
            var pm = new PatchManager(null, Log);
            pm.ExecuteOnBefore(candidates, installed, true);

            // ASSERT BEFORE
            Assert.AreEqual(3, candidates.Count);
            Assert.AreEqual("C1v(1.0,,SuccessfulBefore) C2v(2.0,,SuccessfulBefore)", ComponentsToStringWithResult(installed));

            // ACTION AFTER
            pm.ExecuteOnAfter(candidates, installed, true);

            // ASSERT AFTER
            Assert.AreEqual(0, candidates.Count);
            Assert.AreEqual("C1v1.0(1.0,1.0,Successful) C2v2.0(2.0,2.0,Successful)", ComponentsToStringWithResult(installed));
            Assert.AreEqual("[C2: 1.0] OnBeforeActionStarts.|" +
                            "[C2: 1.0] OnBeforeActionFinished.|" +
                            "[C2: 1.0 <= v < 2.0 --> 2.0] OnBeforeActionStarts.|" +
                            "[C2: 1.0 <= v < 2.0 --> 2.0] OnBeforeActionFinished.|" +
                            "[C1: 1.0] OnBeforeActionStarts.|" +
                            "[C1: 1.0] OnBeforeActionFinished.|" +
                            "[C2: 1.0] OnAfterActionStarts.|" +
                            "[C2: 1.0] OnAfterActionFinished.|" +
                            "[C2: 1.0 <= v < 2.0 --> 2.0] OnAfterActionStarts.|" +
                            "[C2: 1.0 <= v < 2.0 --> 2.0] OnAfterActionFinished.|" +
                            "[C1: 1.0] OnAfterActionStarts.|" +
                            "[C1: 1.0] OnAfterActionFinished.",
                string.Join("|", log.Select(x => x.ToString(false))));
            Assert.AreEqual("", ErrorsToString(pm.Errors));
        }

        // SIMULATION: Install and patch C2 that depends C1. Dependency comes with the patch.
        [TestMethod]
        public void Patching_ExecSim_Patch_DependencyInPatch()
        {
            var log = new List<PatchExecutionLogRecord>();
            void Log(PatchExecutionLogRecord record) { log.Add(record); }
            void Exec(PatchExecutionContext peContext) { }

            var installed = new List<SnComponentDescriptor>();
            var candidates = new List<ISnPatch>
            {
                Inst("C1", "v1.0", Exec, Exec),
                Patch("C1", "1.0 <= v < 2.0", "v2.0", new[] {Dep("C2", "2.0 <= v <= 2.0")}, Exec, Exec),
                Inst("C2", "v1.0", Exec, Exec),
                Patch("C2", "1.0 <= v < 2.0", "v2.0", Exec, Exec),
            };

            // ACTION BEFORE
            var pm = new PatchManager(null, Log);
            pm.ExecuteOnBefore(candidates, installed, true);

            // ASSERT BEFORE
            Assert.AreEqual(4, candidates.Count);
            Assert.AreEqual("C1v(2.0,,SuccessfulBefore) C2v(2.0,,SuccessfulBefore)", ComponentsToStringWithResult(installed));

            // ACTION AFTER
            pm.ExecuteOnAfter(candidates, installed, true);

            // ASSERT AFTER
            Assert.AreEqual(0, candidates.Count);
            Assert.AreEqual("C1v2.0(2.0,2.0,Successful) C2v2.0(2.0,2.0,Successful)", ComponentsToStringWithResult(installed));
            Assert.AreEqual("[C1: 1.0] OnBeforeActionStarts.|" +
                            "[C1: 1.0] OnBeforeActionFinished.|" +
                            "[C2: 1.0] OnBeforeActionStarts.|" +
                            "[C2: 1.0] OnBeforeActionFinished.|" +
                            "[C2: 1.0 <= v < 2.0 --> 2.0] OnBeforeActionStarts.|" +
                            "[C2: 1.0 <= v < 2.0 --> 2.0] OnBeforeActionFinished.|" +
                            "[C1: 1.0 <= v < 2.0 --> 2.0] OnBeforeActionStarts.|" +
                            "[C1: 1.0 <= v < 2.0 --> 2.0] OnBeforeActionFinished.|" +
                            "[C1: 1.0] OnAfterActionStarts.|" +
                            "[C1: 1.0] OnAfterActionFinished.|" +
                            "[C2: 1.0] OnAfterActionStarts.|" +
                            "[C2: 1.0] OnAfterActionFinished.|" +
                            "[C2: 1.0 <= v < 2.0 --> 2.0] OnAfterActionStarts.|" +
                            "[C2: 1.0 <= v < 2.0 --> 2.0] OnAfterActionFinished.|" +
                            "[C1: 1.0 <= v < 2.0 --> 2.0] OnAfterActionStarts.|" +
                            "[C1: 1.0 <= v < 2.0 --> 2.0] OnAfterActionFinished.",
                string.Join("|", log.Select(x => x.ToString(false))));
            Assert.AreEqual("", ErrorsToString(pm.Errors));
        }

        // SIMULATION: Install and patch C1 but there is a missing item in the patch chain.
        [TestMethod]
        public void Patching_ExecSim_Patch_MissingItemInTheChain_a()
        {
            var log = new List<PatchExecutionLogRecord>();
            void Log(PatchExecutionLogRecord record) { log.Add(record); }
            void Exec(PatchExecutionContext peContext) { }

            var installed = new List<SnComponentDescriptor>();
            var candidates = new List<ISnPatch>
            {
                Inst("C1", "v1.0", Exec, Exec),
                Patch("C1", "1.0 <= v <  2.0", "v2.0", Exec, Exec),
                Patch("C1", "3.0 <= v <  4.0", "v4.0", Exec, Exec),
            };

            // ACTION BEFORE
            var pm = new PatchManager(null, Log);
            pm.ExecuteOnBefore(candidates, installed, true);

            // ASSERT BEFORE
            Assert.AreEqual(3, candidates.Count);
            Assert.AreEqual("C1v(2.0,,SuccessfulBefore)", ComponentsToStringWithResult(installed));

            // ACTION AFTER
            pm.ExecuteOnAfter(candidates, installed, true);

            // ASSERT AFTER
            Assert.AreEqual(0, candidates.Count);
            Assert.AreEqual("C1v2.0(2.0,2.0,Successful)", ComponentsToStringWithResult(installed));
            Assert.AreEqual("[C1: 1.0] OnBeforeActionStarts.|" +
                            "[C1: 1.0] OnBeforeActionFinished.|" +
                            "[C1: 1.0 <= v < 2.0 --> 2.0] OnBeforeActionStarts.|" +
                            "[C1: 1.0 <= v < 2.0 --> 2.0] OnBeforeActionFinished.|" +
                            "[C1: 3.0 <= v < 4.0 --> 4.0] CannotExecuteMissingVersion.|" +
                            "[C1: 1.0] OnAfterActionStarts.|" +
                            "[C1: 1.0] OnAfterActionFinished.|" +
                            "[C1: 1.0 <= v < 2.0 --> 2.0] OnAfterActionStarts.|" +
                            "[C1: 1.0 <= v < 2.0 --> 2.0] OnAfterActionFinished.|" +
                            "[C1: 3.0 <= v < 4.0 --> 4.0] CannotExecuteMissingVersion.",
                string.Join("|", log.Select(x => x.ToString(false))));
            Assert.AreEqual("MissingVersion C1: 3.0 <= v < 4.0 --> 4.0|" +
                            "MissingVersion C1: 3.0 <= v < 4.0 --> 4.0", ErrorsToString(pm.Errors));
        }
        [TestMethod]
        public void Patching_ExecSim_Patch_MissingItemInTheChain_b()
        {
            var log = new List<PatchExecutionLogRecord>();
            void Log(PatchExecutionLogRecord record) { log.Add(record); }
            void Exec(PatchExecutionContext peContext) { }

            var installed = new List<SnComponentDescriptor>()
            {
                Comp("C1", "v1.0")
            };
            var candidates = new List<ISnPatch>
            {
                Inst("C1", "v1.0", Exec, Exec),
                Patch("C1", "1.0 <= v <  2.0", "v2.0", Exec, Exec),
                Patch("C1", "3.0 <= v <  4.0", "v4.0", Exec, Exec),
            };

            // ACTION BEFORE
            var pm = new PatchManager(null, Log);
            pm.ExecuteOnBefore(candidates, installed, true);

            // ASSERT BEFORE
            Assert.AreEqual(3, candidates.Count);
            Assert.AreEqual("C1v1.0(2.0,,SuccessfulBefore)", ComponentsToStringWithResult(installed));

            // ACTION AFTER
            pm.ExecuteOnAfter(candidates, installed, true);

            // ASSERT AFTER
            Assert.AreEqual(0, candidates.Count);
            Assert.AreEqual("C1v2.0(2.0,2.0,Successful)", ComponentsToStringWithResult(installed));
            Assert.AreEqual("[C1: 1.0 <= v < 2.0 --> 2.0] OnBeforeActionStarts.|" +
                            "[C1: 1.0 <= v < 2.0 --> 2.0] OnBeforeActionFinished.|" +
                            "[C1: 3.0 <= v < 4.0 --> 4.0] CannotExecuteMissingVersion.|" +
                            "[C1: 1.0 <= v < 2.0 --> 2.0] OnAfterActionStarts.|" +
                            "[C1: 1.0 <= v < 2.0 --> 2.0] OnAfterActionFinished.|" +
                            "[C1: 3.0 <= v < 4.0 --> 4.0] CannotExecuteMissingVersion.",
                string.Join("|", log.Select(x => x.ToString(false))));
            Assert.AreEqual("MissingVersion C1: 3.0 <= v < 4.0 --> 4.0|" +
                            "MissingVersion C1: 3.0 <= v < 4.0 --> 4.0", ErrorsToString(pm.Errors));
        }
        [TestMethod]
        public void Patching_ExecSim_Patch_MissingItemInTheChain_c()
        {
            var log = new List<PatchExecutionLogRecord>();
            void Log(PatchExecutionLogRecord record) { log.Add(record); }
            void Exec(PatchExecutionContext peContext) { }

            var installed = new List<SnComponentDescriptor>()
            {
                Comp("C1", "v2.0")
            };
            var candidates = new List<ISnPatch>
            {
                Inst("C1", "v1.0", Exec, Exec),
                Patch("C1", "1.0 <= v <  2.0", "v2.0", Exec, Exec),
                Patch("C1", "3.0 <= v <  4.0", "v4.0", Exec, Exec),
            };

            // ACTION BEFORE
            var pm = new PatchManager(null, Log);
            pm.ExecuteOnBefore(candidates, installed, true);

            // ASSERT BEFORE
            Assert.AreEqual(3, candidates.Count);
            Assert.AreEqual("C1v2.0(,,Successful)", ComponentsToStringWithResult(installed));

            // ACTION AFTER
            pm.ExecuteOnAfter(candidates, installed, true);

            // ASSERT AFTER
            Assert.AreEqual(0, candidates.Count);
            Assert.AreEqual("C1v2.0(,,Successful)", ComponentsToStringWithResult(installed));
            Assert.AreEqual("[C1: 3.0 <= v < 4.0 --> 4.0] CannotExecuteMissingVersion.|" +
                            "[C1: 3.0 <= v < 4.0 --> 4.0] CannotExecuteMissingVersion.",
                string.Join("|", log.Select(x => x.ToString(false))));
            Assert.AreEqual("MissingVersion C1: 3.0 <= v < 4.0 --> 4.0|" +
                            "MissingVersion C1: 3.0 <= v < 4.0 --> 4.0", ErrorsToString(pm.Errors));
        }
        [TestMethod]
        public void Patching_ExecSim_Patch_MissingItemInTheChain_d()
        {
            var log = new List<PatchExecutionLogRecord>();
            void Log(PatchExecutionLogRecord record) { log.Add(record); }
            void Exec(PatchExecutionContext peContext) { }

            var installed = new List<SnComponentDescriptor>()
            {
                Comp("C1", "v3.0")
            };
            var candidates = new List<ISnPatch>
            {
                Inst("C1", "v1.0", Exec, Exec),
                Patch("C1", "1.0 <= v <  2.0", "v2.0", Exec, Exec),
                Patch("C1", "3.0 <= v <  4.0", "v4.0", Exec, Exec),
            };

            // ACTION BEFORE
            var pm = new PatchManager(null, Log);
            pm.ExecuteOnBefore(candidates, installed, true);

            // ASSERT BEFORE
            Assert.AreEqual(3, candidates.Count);
            Assert.AreEqual("C1v3.0(4.0,,SuccessfulBefore)", ComponentsToStringWithResult(installed));

            // ACTION AFTER
            pm.ExecuteOnAfter(candidates, installed, true);

            // ASSERT AFTER
            Assert.AreEqual(0, candidates.Count);
            Assert.AreEqual("C1v4.0(4.0,4.0,Successful)", ComponentsToStringWithResult(installed));
            Assert.AreEqual("[C1: 3.0 <= v < 4.0 --> 4.0] OnBeforeActionStarts.|" +
                            "[C1: 3.0 <= v < 4.0 --> 4.0] OnBeforeActionFinished.|" +
                            "[C1: 3.0 <= v < 4.0 --> 4.0] OnAfterActionStarts.|" +
                            "[C1: 3.0 <= v < 4.0 --> 4.0] OnAfterActionFinished.",
                string.Join("|", log.Select(x => x.ToString(false))));
            Assert.AreEqual("", ErrorsToString(pm.Errors));
        }

        [TestMethod]
        public void Patching_ExecSim_Install_SelfDependency()
        {
            var log = new List<PatchExecutionLogRecord>();
            void Log(PatchExecutionLogRecord record) { log.Add(record); }
            void Exec(PatchExecutionContext peContext) { }

            var installed = new List<SnComponentDescriptor>();
            var candidates = new List<ISnPatch>
            {
                Inst("C1", "v1.0", new[] {Dep("C1", "0.9 <= v")}, Exec, Exec),
                Inst("C2", "v1.0", Exec, Exec),
            };

            // ACTION BEFORE
            var pm = new PatchManager(null, Log);
            pm.ExecuteOnBefore(candidates, installed, true);

            // ASSERT BEFORE
            Assert.AreEqual(2, candidates.Count);
            Assert.AreEqual("C2v(1.0,,SuccessfulBefore)", ComponentsToStringWithResult(installed));

            // ACTION AFTER
            pm.ExecuteOnAfter(candidates, installed, true);

            // ASSERT AFTER
            Assert.AreEqual(0, candidates.Count);
            Assert.AreEqual("C2v1.0(1.0,1.0,Successful)", ComponentsToStringWithResult(installed));
            Assert.AreEqual("[C2: 1.0] OnBeforeActionStarts.|" +
                            "[C2: 1.0] OnBeforeActionFinished.|" +
                            "[C2: 1.0] OnAfterActionStarts.|" +
                            "[C2: 1.0] OnAfterActionFinished.",
                string.Join("|", log.Select(x => x.ToString(false))));
            Assert.AreEqual("SelfDependencyForbidden C1: 1.0|" +
                            "SelfDependencyForbidden C1: 1.0",
                ErrorsToString(pm.Errors));
        }
        [TestMethod]
        public void Patching_ExecSim_Patch_SelfDependency()
        {
            var log = new List<PatchExecutionLogRecord>();
            void Log(PatchExecutionLogRecord record) { log.Add(record); }
            void Exec(PatchExecutionContext peContext) { }

            var installed = new List<SnComponentDescriptor>();
            var candidates = new List<ISnPatch>
            {
                Inst("C1", "v1.0", Exec, Exec),
                Patch("C1", "1.0 <= v", "2.0",
                    new[] {Dep("C1", "0.9 <= v")}, Exec, Exec),
                Inst("C2", "v1.0", Exec, Exec),
            };

            // ACTION BEFORE
            var pm = new PatchManager(null, Log);
            pm.ExecuteOnBefore(candidates, installed, true);

            // ASSERT BEFORE
            Assert.AreEqual(3, candidates.Count);
            Assert.AreEqual("C1v(1.0,,SuccessfulBefore) C2v(1.0,,SuccessfulBefore)", ComponentsToStringWithResult(installed));

            // ACTION AFTER
            pm.ExecuteOnAfter(candidates, installed, true);

            // ASSERT AFTER
            Assert.AreEqual(0, candidates.Count);
            Assert.AreEqual("C1v1.0(1.0,1.0,Successful) C2v1.0(1.0,1.0,Successful)", ComponentsToStringWithResult(installed));
            Assert.AreEqual("[C1: 1.0] OnBeforeActionStarts.|" +
                            "[C1: 1.0] OnBeforeActionFinished.|" +
                            "[C2: 1.0] OnBeforeActionStarts.|" +
                            "[C2: 1.0] OnBeforeActionFinished.|" +
                            "[C1: 1.0] OnAfterActionStarts.|" +
                            "[C1: 1.0] OnAfterActionFinished.|" +
                            "[C2: 1.0] OnAfterActionStarts.|" +
                            "[C2: 1.0] OnAfterActionFinished.",
                string.Join("|", log.Select(x => x.ToString(false))));
            Assert.AreEqual("SelfDependencyForbidden C1: 1.0 <= v --> 2.0|" +
                            "SelfDependencyForbidden C1: 1.0 <= v --> 2.0",
                ErrorsToString(pm.Errors));
        }

        [TestMethod]
        public void Patching_ExecSim_Patch_OnlyLowerPatch()
        {
            var log = new List<PatchExecutionLogRecord>();
            void Log(PatchExecutionLogRecord record) { log.Add(record); }
            void Exec(PatchExecutionContext peContext) { }

            var installed = new List<SnComponentDescriptor>
            {
                Comp("C1", "1.0")
            };
            var candidates = new List<ISnPatch>
            {
                Patch("C1", "1.0 <= v <  3.0", "v3.0", Exec, Exec),
                Patch("C1", "2.0 <= v <  3.0", "v3.0", Exec, Exec),
            };

            // ACTION BEFORE
            var pm = new PatchManager(null, Log);
            pm.ExecuteOnBefore(candidates, installed, true);

            // ASSERT BEFORE
            Assert.AreEqual(2, candidates.Count);
            Assert.AreEqual("C1v1.0(3.0,,SuccessfulBefore)", ComponentsToStringWithResult(installed));

            // ACTION AFTER
            pm.ExecuteOnAfter(candidates, installed, true);

            // ASSERT AFTER
            Assert.AreEqual(0, candidates.Count);
            Assert.AreEqual("C1v3.0(3.0,3.0,Successful)", ComponentsToStringWithResult(installed));
            Assert.AreEqual("[C1: 1.0 <= v < 3.0 --> 3.0] OnBeforeActionStarts.|" +
                            "[C1: 1.0 <= v < 3.0 --> 3.0] OnBeforeActionFinished.|" +
                            "[C1: 1.0 <= v < 3.0 --> 3.0] OnAfterActionStarts.|" +
                            "[C1: 1.0 <= v < 3.0 --> 3.0] OnAfterActionFinished.",
                string.Join("|", log.Select(x => x.ToString(false))));
            Assert.AreEqual("", ErrorsToString(pm.Errors));
        }

        /* ============================================================================= FIX UNFINISHED STATES */
        /* --------------------------------------------------------------------------- Action before and after */

        [TestMethod]
        public void Patching_ExecSim_Install_Fix_BA_Unfinished()
        {
            var log = new List<PatchExecutionLogRecord>();
            void Log(PatchExecutionLogRecord record) { log.Add(record); }
            void Exec(PatchExecutionContext peContext) { }

            var installed = new List<SnComponentDescriptor>()
            {
                Comp("C1", "v1.0"),
            };
            installed[0].State = ExecutionResult.Unfinished;
            installed[0].TempVersionBefore = installed[0].Version;
            installed[0].Version = null;
            Assert.AreEqual("C1v(1.0,,Unfinished)", ComponentsToStringWithResult(installed));
            var candidates = new List<ISnPatch>
            {
                Inst("C1", "v1.0", Exec, Exec),
            };

            // ACTION BEFORE
            var pm = new PatchManager(null, Log);
            pm.ExecuteOnBefore(candidates, installed, true);

            // ASSERT BEFORE
            Assert.AreEqual(1, candidates.Count);
            Assert.AreEqual("C1v(1.0,,SuccessfulBefore)", ComponentsToStringWithResult(installed));

            // ACTION AFTER
            pm.ExecuteOnAfter(candidates, installed, true);

            // ASSERT AFTER
            Assert.AreEqual(0, candidates.Count);
            Assert.AreEqual("C1v1.0(1.0,1.0,Successful)", ComponentsToStringWithResult(installed));
            Assert.AreEqual("[C1: 1.0] OnBeforeActionStarts.|" +
                            "[C1: 1.0] OnBeforeActionFinished.|" +
                            "[C1: 1.0] OnAfterActionStarts.|" +
                            "[C1: 1.0] OnAfterActionFinished.",
                string.Join("|", log.Select(x => x.ToString(false))));
            Assert.AreEqual("", ErrorsToString(pm.Errors));
        }
        [TestMethod]
        public void Patching_ExecSim_Install_Fix_BA_SuccessfulBefore()
        {
            // In this text only the OnAfter action will be executed

            var log = new List<PatchExecutionLogRecord>();
            void Log(PatchExecutionLogRecord record) { log.Add(record); }
            void Exec(PatchExecutionContext peContext) { }

            var comp = Comp("C1", "v1.0");
            var installed = new List<SnComponentDescriptor> { comp };
            comp.State = ExecutionResult.SuccessfulBefore;
            comp.TempVersionBefore = comp.Version;
            comp.Version = null;
            Assert.AreEqual("C1v(1.0,,SuccessfulBefore)", ComponentsToStringWithResult(installed));
            var candidates = new List<ISnPatch>
            {
                Inst("C1", "v1.0", Exec, Exec),
            };

            // ACTION BEFORE
            var pm = new PatchManager(null, Log);
            pm.ExecuteOnBefore(candidates, installed, true);

            // ASSERT BEFORE
            Assert.AreEqual(1, candidates.Count);
            Assert.AreEqual("C1v(1.0,,SuccessfulBefore)", ComponentsToStringWithResult(installed));

            // ACTION AFTER
            pm.ExecuteOnAfter(candidates, installed, true);

            // ASSERT AFTER
            Assert.AreEqual(0, candidates.Count);
            Assert.AreEqual("C1v1.0(1.0,1.0,Successful)", ComponentsToStringWithResult(installed));
            Assert.AreEqual("[C1: 1.0] OnAfterActionStarts.|" +
                            "[C1: 1.0] OnAfterActionFinished.",
                string.Join("|", log.Select(x => x.ToString(false))));
            Assert.AreEqual("", ErrorsToString(pm.Errors));
        }
        [TestMethod]
        public void Patching_ExecSim_Install_Fix_BA_FaultyBefore()
        {
            // In this text OnBefore and OnAfter action will be executed

            var log = new List<PatchExecutionLogRecord>();
            void Log(PatchExecutionLogRecord record) { log.Add(record); }
            void Exec(PatchExecutionContext peContext) { }

            var comp = Comp("C1", "v1.0");
            var installed = new List<SnComponentDescriptor> { comp };
            comp.State = ExecutionResult.FaultyBefore;
            comp.TempVersionBefore = comp.Version;
            comp.Version = null;
            Assert.AreEqual("C1v(1.0,,FaultyBefore)", ComponentsToStringWithResult(installed));
            var candidates = new List<ISnPatch>
            {
                Inst("C1", "v1.0", Exec, Exec),
            };

            // ACTION BEFORE
            var pm = new PatchManager(null, Log);
            pm.ExecuteOnBefore(candidates, installed, true);

            // ASSERT BEFORE
            Assert.AreEqual(1, candidates.Count);
            Assert.AreEqual("C1v(1.0,,SuccessfulBefore)", ComponentsToStringWithResult(installed));

            // ACTION AFTER
            pm.ExecuteOnAfter(candidates, installed, true);

            // ASSERT AFTER
            Assert.AreEqual(0, candidates.Count);
            Assert.AreEqual("C1v1.0(1.0,1.0,Successful)", ComponentsToStringWithResult(installed));
            Assert.AreEqual("[C1: 1.0] OnBeforeActionStarts.|" +
                            "[C1: 1.0] OnBeforeActionFinished.|" +
                            "[C1: 1.0] OnAfterActionStarts.|" +
                            "[C1: 1.0] OnAfterActionFinished.",
                string.Join("|", log.Select(x => x.ToString(false))));
            Assert.AreEqual("", ErrorsToString(pm.Errors));
        }
        [TestMethod]
        public void Patching_ExecSim_Install_Fix_BA_Faulty()
        {
            // In this text only the OnAfter action will be executed

            var log = new List<PatchExecutionLogRecord>();
            void Log(PatchExecutionLogRecord record) { log.Add(record); }
            void Exec(PatchExecutionContext peContext) { }

            var comp = Comp("C1", "v1.0");
            var installed = new List<SnComponentDescriptor> { comp };
            comp.State = ExecutionResult.Faulty;
            comp.TempVersionBefore = comp.Version;
            comp.TempVersionAfter = comp.Version;
            comp.Version = null;
            Assert.AreEqual("C1v(1.0,1.0,Faulty)", ComponentsToStringWithResult(installed));
            var candidates = new List<ISnPatch>
            {
                Inst("C1", "v1.0", Exec, Exec),
            };

            // ACTION BEFORE
            var pm = new PatchManager(null, Log);
            pm.ExecuteOnBefore(candidates, installed, true);

            // ASSERT BEFORE
            Assert.AreEqual(1, candidates.Count);
            Assert.AreEqual("C1v(1.0,1.0,Faulty)", ComponentsToStringWithResult(installed));

            // ACTION AFTER
            pm.ExecuteOnAfter(candidates, installed, true);

            // ASSERT AFTER
            Assert.AreEqual(0, candidates.Count);
            Assert.AreEqual("C1v1.0(1.0,1.0,Successful)", ComponentsToStringWithResult(installed));
            Assert.AreEqual("[C1: 1.0] OnAfterActionStarts.|" +
                            "[C1: 1.0] OnAfterActionFinished.",
                string.Join("|", log.Select(x => x.ToString(false))));
            Assert.AreEqual("", ErrorsToString(pm.Errors));
        }

        [TestMethod]
        public void Patching_ExecSim_Patch_Fix_BA_Unfinished()
        {
            var log = new List<PatchExecutionLogRecord>();
            void Log(PatchExecutionLogRecord record) { log.Add(record); }
            void Exec(PatchExecutionContext peContext) { }

            var installed = new List<SnComponentDescriptor>()
            {
                Comp("C1", "v1.0"),
            };
            installed[0].State = ExecutionResult.Unfinished;
            installed[0].TempVersionBefore = new Version(2, 0);
            Assert.AreEqual("C1v1.0(2.0,,Unfinished)", ComponentsToStringWithResult(installed));
            var candidates = new List<ISnPatch>
            {
                Patch("C1", "1.0 <= v < 2.0", "v2.0", Exec, Exec),
            };

            // ACTION BEFORE
            var pm = new PatchManager(null, Log);
            pm.ExecuteOnBefore(candidates, installed, true);

            // ASSERT BEFORE
            Assert.AreEqual(1, candidates.Count);
            Assert.AreEqual("C1v1.0(2.0,,SuccessfulBefore)", ComponentsToStringWithResult(installed));

            // ACTION AFTER
            pm.ExecuteOnAfter(candidates, installed, true);

            // ASSERT AFTER
            Assert.AreEqual(0, candidates.Count);
            Assert.AreEqual("C1v2.0(2.0,2.0,Successful)", ComponentsToStringWithResult(installed));
            Assert.AreEqual("[C1: 1.0 <= v < 2.0 --> 2.0] OnBeforeActionStarts.|" +
                            "[C1: 1.0 <= v < 2.0 --> 2.0] OnBeforeActionFinished.|" +
                            "[C1: 1.0 <= v < 2.0 --> 2.0] OnAfterActionStarts.|" +
                            "[C1: 1.0 <= v < 2.0 --> 2.0] OnAfterActionFinished.",
                string.Join("|", log.Select(x => x.ToString(false))));
            Assert.AreEqual("", ErrorsToString(pm.Errors));
        }
        [TestMethod]
        public void Patching_ExecSim_Patch_Fix_BA_SuccessfulBefore()
        {
            var log = new List<PatchExecutionLogRecord>();
            void Log(PatchExecutionLogRecord record) { log.Add(record); }
            void Exec(PatchExecutionContext peContext) { }

            var comp = Comp("C1", "v1.0");
            var installed = new List<SnComponentDescriptor> { comp };
            comp.State = ExecutionResult.SuccessfulBefore;
            comp.TempVersionBefore = new Version(2, 0);
            Assert.AreEqual("C1v1.0(2.0,,SuccessfulBefore)", ComponentsToStringWithResult(installed));
            var candidates = new List<ISnPatch>
            {
                Patch("C1", "1.0 <= v < 2.0", "v2.0", Exec, Exec),
            };

            // ACTION BEFORE
            var pm = new PatchManager(null, Log);
            pm.ExecuteOnBefore(candidates, installed, true);

            // ASSERT BEFORE
            Assert.AreEqual(1, candidates.Count);
            Assert.AreEqual("C1v1.0(2.0,,SuccessfulBefore)", ComponentsToStringWithResult(installed));

            // ACTION AFTER
            pm.ExecuteOnAfter(candidates, installed, true);

            // ASSERT AFTER
            Assert.AreEqual(0, candidates.Count);
            Assert.AreEqual("C1v2.0(2.0,2.0,Successful)", ComponentsToStringWithResult(installed));
            Assert.AreEqual("[C1: 1.0 <= v < 2.0 --> 2.0] OnAfterActionStarts.|" +
                            "[C1: 1.0 <= v < 2.0 --> 2.0] OnAfterActionFinished.",
                string.Join("|", log.Select(x => x.ToString(false))));
            Assert.AreEqual("", ErrorsToString(pm.Errors));
        }
        [TestMethod]
        public void Patching_ExecSim_Patch_Fix_BA_FaultyBefore()
        {
            var log = new List<PatchExecutionLogRecord>();
            void Log(PatchExecutionLogRecord record) { log.Add(record); }
            void Exec(PatchExecutionContext peContext) { }

            var comp = Comp("C1", "v1.0");
            var installed = new List<SnComponentDescriptor> { comp };
            comp.State = ExecutionResult.FaultyBefore;
            comp.TempVersionBefore = new Version(2, 0);
            Assert.AreEqual("C1v1.0(2.0,,FaultyBefore)", ComponentsToStringWithResult(installed));
            var candidates = new List<ISnPatch>
            {
                Patch("C1", "1.0 <= v < 2.0", "v2.0", Exec, Exec),
            };

            // ACTION BEFORE
            var pm = new PatchManager(null, Log);
            pm.ExecuteOnBefore(candidates, installed, true);

            // ASSERT BEFORE
            Assert.AreEqual(1, candidates.Count);
            Assert.AreEqual("C1v1.0(2.0,,SuccessfulBefore)", ComponentsToStringWithResult(installed));

            // ACTION AFTER
            pm.ExecuteOnAfter(candidates, installed, true);

            // ASSERT AFTER
            Assert.AreEqual(0, candidates.Count);
            Assert.AreEqual("C1v2.0(2.0,2.0,Successful)", ComponentsToStringWithResult(installed));
            Assert.AreEqual("[C1: 1.0 <= v < 2.0 --> 2.0] OnBeforeActionStarts.|" +
                            "[C1: 1.0 <= v < 2.0 --> 2.0] OnBeforeActionFinished.|" +
                            "[C1: 1.0 <= v < 2.0 --> 2.0] OnAfterActionStarts.|" +
                            "[C1: 1.0 <= v < 2.0 --> 2.0] OnAfterActionFinished.",
                string.Join("|", log.Select(x => x.ToString(false))));
            Assert.AreEqual("", ErrorsToString(pm.Errors));
        }
        [TestMethod]
        public void Patching_ExecSim_Patch_Fix_BA_Faulty()
        {
            var log = new List<PatchExecutionLogRecord>();
            void Log(PatchExecutionLogRecord record) { log.Add(record); }
            void Exec(PatchExecutionContext peContext) { }

            var comp = Comp("C1", "v1.0");
            var installed = new List<SnComponentDescriptor> { comp };
            comp.State = ExecutionResult.Faulty;
            comp.TempVersionBefore = new Version(2, 0);
            comp.TempVersionAfter = new Version(2, 0);
            Assert.AreEqual("C1v1.0(2.0,2.0,Faulty)", ComponentsToStringWithResult(installed));
            var candidates = new List<ISnPatch>
            {
                Patch("C1", "1.0 <= v < 2.0", "v2.0", Exec, Exec),
            };

            // ACTION BEFORE
            var pm = new PatchManager(null, Log);
            pm.ExecuteOnBefore(candidates, installed, true);

            // ASSERT BEFORE
            Assert.AreEqual(1, candidates.Count);
            Assert.AreEqual("C1v1.0(2.0,2.0,Faulty)", ComponentsToStringWithResult(installed));

            // ACTION AFTER
            pm.ExecuteOnAfter(candidates, installed, true);

            // ASSERT AFTER
            Assert.AreEqual(0, candidates.Count);
            Assert.AreEqual("C1v2.0(2.0,2.0,Successful)", ComponentsToStringWithResult(installed));
            Assert.AreEqual("[C1: 1.0 <= v < 2.0 --> 2.0] OnAfterActionStarts.|" +
                            "[C1: 1.0 <= v < 2.0 --> 2.0] OnAfterActionFinished.",
                string.Join("|", log.Select(x => x.ToString(false))));
            Assert.AreEqual("", ErrorsToString(pm.Errors));
        }

        /* --------------------------------------------------------------------------- Action only after */

        [TestMethod]
        public void Patching_ExecSim_Install_Fix_A_Unfinished()
        {
            var log = new List<PatchExecutionLogRecord>();
            void Log(PatchExecutionLogRecord record) { log.Add(record); }
            void Exec(PatchExecutionContext peContext) { }

            var installed = new List<SnComponentDescriptor>()
            {
                Comp("C1", "v1.0"),
            };
            installed[0].State = ExecutionResult.Unfinished;
            installed[0].TempVersionBefore = installed[0].Version;
            installed[0].Version = null;
            Assert.AreEqual("C1v(1.0,,Unfinished)", ComponentsToStringWithResult(installed));
            var candidates = new List<ISnPatch>
            {
                Inst("C1", "v1.0", Exec),
            };

            // ACTION BEFORE
            var pm = new PatchManager(null, Log);
            pm.ExecuteOnBefore(candidates, installed, true);

            // ASSERT BEFORE
            Assert.AreEqual(1, candidates.Count);
            Assert.AreEqual("C1v(1.0,,Unfinished)", ComponentsToStringWithResult(installed));

            // ACTION AFTER
            pm.ExecuteOnAfter(candidates, installed, true);

            // ASSERT AFTER
            Assert.AreEqual(0, candidates.Count);
            Assert.AreEqual("C1v1.0(1.0,1.0,Successful)", ComponentsToStringWithResult(installed));
            Assert.AreEqual("[C1: 1.0] OnAfterActionStarts.|" +
                            "[C1: 1.0] OnAfterActionFinished.",
                string.Join("|", log.Select(x => x.ToString(false))));
            Assert.AreEqual("", ErrorsToString(pm.Errors));
        }
        [TestMethod]
        public void Patching_ExecSim_Install_Fix_A_SuccessfulBefore()
        {
            // In this text only the OnAfter action will be executed

            var log = new List<PatchExecutionLogRecord>();
            void Log(PatchExecutionLogRecord record) { log.Add(record); }
            void Exec(PatchExecutionContext peContext) { }

            var comp = Comp("C1", "v1.0");
            var installed = new List<SnComponentDescriptor> { comp };
            comp.State = ExecutionResult.SuccessfulBefore;
            comp.TempVersionBefore = comp.Version;
            comp.Version = null;
            Assert.AreEqual("C1v(1.0,,SuccessfulBefore)", ComponentsToStringWithResult(installed));
            var candidates = new List<ISnPatch>
            {
                Inst("C1", "v1.0", Exec),
            };

            // ACTION BEFORE
            var pm = new PatchManager(null, Log);
            pm.ExecuteOnBefore(candidates, installed, true);

            // ASSERT BEFORE
            Assert.AreEqual(1, candidates.Count);
            Assert.AreEqual("C1v(1.0,,SuccessfulBefore)", ComponentsToStringWithResult(installed));

            // ACTION AFTER
            pm.ExecuteOnAfter(candidates, installed, true);

            // ASSERT AFTER
            Assert.AreEqual(0, candidates.Count);
            Assert.AreEqual("C1v1.0(1.0,1.0,Successful)", ComponentsToStringWithResult(installed));
            Assert.AreEqual("[C1: 1.0] OnAfterActionStarts.|" +
                            "[C1: 1.0] OnAfterActionFinished.",
                string.Join("|", log.Select(x => x.ToString(false))));
            Assert.AreEqual("", ErrorsToString(pm.Errors));
        }
        [TestMethod]
        public void Patching_ExecSim_Install_Fix_A_FaultyBefore()
        {
            // In this text OnBefore and OnAfter action will be executed

            var log = new List<PatchExecutionLogRecord>();
            void Log(PatchExecutionLogRecord record) { log.Add(record); }
            void Exec(PatchExecutionContext peContext) { }

            var comp = Comp("C1", "v1.0");
            var installed = new List<SnComponentDescriptor> { comp };
            comp.State = ExecutionResult.FaultyBefore;
            comp.TempVersionBefore = comp.Version;
            comp.Version = null;
            Assert.AreEqual("C1v(1.0,,FaultyBefore)", ComponentsToStringWithResult(installed));
            var candidates = new List<ISnPatch>
            {
                Inst("C1", "v1.0", Exec),
            };

            // ACTION BEFORE
            var pm = new PatchManager(null, Log);
            pm.ExecuteOnBefore(candidates, installed, true);

            // ASSERT BEFORE
            Assert.AreEqual(1, candidates.Count);
            Assert.AreEqual("C1v(1.0,,FaultyBefore)", ComponentsToStringWithResult(installed));

            // ACTION AFTER
            pm.ExecuteOnAfter(candidates, installed, true);

            // ASSERT AFTER
            Assert.AreEqual(0, candidates.Count);
            Assert.AreEqual("C1v1.0(1.0,1.0,Successful)", ComponentsToStringWithResult(installed));
            Assert.AreEqual("[C1: 1.0] OnAfterActionStarts.|" +
                            "[C1: 1.0] OnAfterActionFinished.",
                string.Join("|", log.Select(x => x.ToString(false))));
            Assert.AreEqual("", ErrorsToString(pm.Errors));
        }
        [TestMethod]
        public void Patching_ExecSim_Install_Fix_A_Faulty()
        {
            // In this text only the OnAfter action will be executed

            var log = new List<PatchExecutionLogRecord>();
            void Log(PatchExecutionLogRecord record) { log.Add(record); }
            void Exec(PatchExecutionContext peContext) { }

            var comp = Comp("C1", "v1.0");
            var installed = new List<SnComponentDescriptor> { comp };
            comp.State = ExecutionResult.Faulty;
            comp.TempVersionBefore = comp.Version;
            comp.TempVersionAfter = comp.Version;
            comp.Version = null;
            Assert.AreEqual("C1v(1.0,1.0,Faulty)", ComponentsToStringWithResult(installed));
            var candidates = new List<ISnPatch>
            {
                Inst("C1", "v1.0", Exec),
            };

            // ACTION BEFORE
            var pm = new PatchManager(null, Log);
            pm.ExecuteOnBefore(candidates, installed, true);

            // ASSERT BEFORE
            Assert.AreEqual(1, candidates.Count);
            Assert.AreEqual("C1v(1.0,1.0,Faulty)", ComponentsToStringWithResult(installed));

            // ACTION AFTER
            pm.ExecuteOnAfter(candidates, installed, true);

            // ASSERT AFTER
            Assert.AreEqual(0, candidates.Count);
            Assert.AreEqual("C1v1.0(1.0,1.0,Successful)", ComponentsToStringWithResult(installed));
            Assert.AreEqual("[C1: 1.0] OnAfterActionStarts.|" +
                            "[C1: 1.0] OnAfterActionFinished.",
                string.Join("|", log.Select(x => x.ToString(false))));
            Assert.AreEqual("", ErrorsToString(pm.Errors));
        }

        [TestMethod]
        public void Patching_ExecSim_Patch_Fix_A_Unfinished()
        {
            var log = new List<PatchExecutionLogRecord>();
            void Log(PatchExecutionLogRecord record) { log.Add(record); }
            void Exec(PatchExecutionContext peContext) { }

            var installed = new List<SnComponentDescriptor>()
            {
                Comp("C1", "v1.0"),
            };
            installed[0].State = ExecutionResult.Unfinished;
            installed[0].TempVersionBefore = new Version(2, 0);
            Assert.AreEqual("C1v1.0(2.0,,Unfinished)", ComponentsToStringWithResult(installed));
            var candidates = new List<ISnPatch>
            {
                Patch("C1", "1.0 <= v < 2.0", "v2.0", Exec),
            };

            // ACTION BEFORE
            var pm = new PatchManager(null, Log);
            pm.ExecuteOnBefore(candidates, installed, true);

            // ASSERT BEFORE
            Assert.AreEqual(1, candidates.Count);
            Assert.AreEqual("C1v1.0(2.0,,Unfinished)", ComponentsToStringWithResult(installed));

            // ACTION AFTER
            pm.ExecuteOnAfter(candidates, installed, true);

            // ASSERT AFTER
            Assert.AreEqual(0, candidates.Count);
            Assert.AreEqual("C1v2.0(2.0,2.0,Successful)", ComponentsToStringWithResult(installed));
            Assert.AreEqual("[C1: 1.0 <= v < 2.0 --> 2.0] OnAfterActionStarts.|" +
                            "[C1: 1.0 <= v < 2.0 --> 2.0] OnAfterActionFinished.",
                string.Join("|", log.Select(x => x.ToString(false))));
            Assert.AreEqual("", ErrorsToString(pm.Errors));
        }
        [TestMethod]
        public void Patching_ExecSim_Patch_Fix_A_SuccessfulBefore()
        {
            var log = new List<PatchExecutionLogRecord>();
            void Log(PatchExecutionLogRecord record) { log.Add(record); }
            void Exec(PatchExecutionContext peContext) { }

            var comp = Comp("C1", "v1.0");
            var installed = new List<SnComponentDescriptor> { comp };
            comp.State = ExecutionResult.SuccessfulBefore;
            comp.TempVersionBefore = new Version(2, 0);
            Assert.AreEqual("C1v1.0(2.0,,SuccessfulBefore)", ComponentsToStringWithResult(installed));
            var candidates = new List<ISnPatch>
            {
                Patch("C1", "1.0 <= v < 2.0", "v2.0", Exec),
            };

            // ACTION BEFORE
            var pm = new PatchManager(null, Log);
            pm.ExecuteOnBefore(candidates, installed, true);

            // ASSERT BEFORE
            Assert.AreEqual(1, candidates.Count);
            Assert.AreEqual("C1v1.0(2.0,,SuccessfulBefore)", ComponentsToStringWithResult(installed));

            // ACTION AFTER
            pm.ExecuteOnAfter(candidates, installed, true);

            // ASSERT AFTER
            Assert.AreEqual(0, candidates.Count);
            Assert.AreEqual("C1v2.0(2.0,2.0,Successful)", ComponentsToStringWithResult(installed));
            Assert.AreEqual("[C1: 1.0 <= v < 2.0 --> 2.0] OnAfterActionStarts.|" +
                            "[C1: 1.0 <= v < 2.0 --> 2.0] OnAfterActionFinished.",
                string.Join("|", log.Select(x => x.ToString(false))));
            Assert.AreEqual("", ErrorsToString(pm.Errors));
        }
        [TestMethod]
        public void Patching_ExecSim_Patch_Fix_A_FaultyBefore()
        {
            var log = new List<PatchExecutionLogRecord>();
            void Log(PatchExecutionLogRecord record) { log.Add(record); }
            void Exec(PatchExecutionContext peContext) { }

            var comp = Comp("C1", "v1.0");
            var installed = new List<SnComponentDescriptor> { comp };
            comp.State = ExecutionResult.FaultyBefore;
            comp.TempVersionBefore = new Version(2, 0);
            Assert.AreEqual("C1v1.0(2.0,,FaultyBefore)", ComponentsToStringWithResult(installed));
            var candidates = new List<ISnPatch>
            {
                Patch("C1", "1.0 <= v < 2.0", "v2.0", Exec),
            };

            // ACTION BEFORE
            var pm = new PatchManager(null, Log);
            pm.ExecuteOnBefore(candidates, installed, true);

            // ASSERT BEFORE
            Assert.AreEqual(1, candidates.Count);
            Assert.AreEqual("C1v1.0(2.0,,FaultyBefore)", ComponentsToStringWithResult(installed));

            // ACTION AFTER
            pm.ExecuteOnAfter(candidates, installed, true);

            // ASSERT AFTER
            Assert.AreEqual(0, candidates.Count);
            Assert.AreEqual("C1v2.0(2.0,2.0,Successful)", ComponentsToStringWithResult(installed));
            Assert.AreEqual("[C1: 1.0 <= v < 2.0 --> 2.0] OnAfterActionStarts.|" +
                            "[C1: 1.0 <= v < 2.0 --> 2.0] OnAfterActionFinished.",
                string.Join("|", log.Select(x => x.ToString(false))));
            Assert.AreEqual("", ErrorsToString(pm.Errors));
        }
        [TestMethod]
        public void Patching_ExecSim_Patch_Fix_A_Faulty()
        {
            var log = new List<PatchExecutionLogRecord>();
            void Log(PatchExecutionLogRecord record) { log.Add(record); }
            void Exec(PatchExecutionContext peContext) { }

            var comp = Comp("C1", "v1.0");
            var installed = new List<SnComponentDescriptor> { comp };
            comp.State = ExecutionResult.Faulty;
            comp.TempVersionBefore = new Version(2, 0);
            comp.TempVersionAfter = new Version(2, 0);
            Assert.AreEqual("C1v1.0(2.0,2.0,Faulty)", ComponentsToStringWithResult(installed));
            var candidates = new List<ISnPatch>
            {
                Patch("C1", "1.0 <= v < 2.0", "v2.0", Exec),
            };

            // ACTION BEFORE
            var pm = new PatchManager(null, Log);
            pm.ExecuteOnBefore(candidates, installed, true);

            // ASSERT BEFORE
            Assert.AreEqual(1, candidates.Count);
            Assert.AreEqual("C1v1.0(2.0,2.0,Faulty)", ComponentsToStringWithResult(installed));

            // ACTION AFTER
            pm.ExecuteOnAfter(candidates, installed, true);

            // ASSERT AFTER
            Assert.AreEqual(0, candidates.Count);
            Assert.AreEqual("C1v2.0(2.0,2.0,Successful)", ComponentsToStringWithResult(installed));
            Assert.AreEqual("[C1: 1.0 <= v < 2.0 --> 2.0] OnAfterActionStarts.|" +
                            "[C1: 1.0 <= v < 2.0 --> 2.0] OnAfterActionFinished.",
                string.Join("|", log.Select(x => x.ToString(false))));
            Assert.AreEqual("", ErrorsToString(pm.Errors));
        }

        /* ======================================================== COMPLEX INSTALL & PATCHING EXECUTION TESTS */

        [TestMethod]
        public void Patching_Exec_NoAction()
        {
            // Faulty execution blocks the following patches on the same component.
            var packages = new List<Package[]>();
            var log = new List<PatchExecutionLogRecord>();
            void Log(PatchExecutionLogRecord record) { packages.Add(LoadPackages()); log.Add(record); }
            var executed = new List<ISnPatch>();
            void Exec(PatchExecutionContext ctx) { executed.Add(ctx.CurrentPatch); }

            var installed = new List<SnComponentDescriptor>();
            var candidates = new List<ISnPatch>
            {
                Patch("C1", "1.0 <= v < 2.0", "v2.0"),
                Inst("C1", "v2.0"),
            };

            // ACTION BEFORE
            var pm = new PatchManager(null, Log);
            pm.ExecuteOnBefore(candidates, installed, false);

            // ASSERT BEFORE
            Assert.AreEqual(2, candidates.Count);
            Assert.AreEqual("", ComponentsToStringWithResult(installed));

            // ACTION AFTER
            pm.ExecuteOnAfter(candidates, installed, false);

            // ASSERT AFTER
            Assert.AreEqual(0, candidates.Count);
            Assert.AreEqual("C1v2.0(2.0,2.0,Successful)",
                ComponentsToStringWithResult(installed));
            Assert.AreEqual("[C1: 2.0] OnAfterActionStarts.|[C1: 2.0] OnAfterActionFinished.",
                string.Join("|", log.Select(x => x.ToString(false))));
            Assert.AreEqual("", ErrorsToString(pm.Errors));
            Assert.AreEqual(2, packages.Count);
            Assert.AreEqual("1, C1: Install Successful, 2.0",
                PackagesToString(packages[1]));
        }
        [TestMethod]
        public void Patching_Exec_NoAfterAction()
        {
            // Faulty execution blocks the following patches on the same component.
            var packages = new List<Package[]>();
            var log = new List<PatchExecutionLogRecord>();
            void Log(PatchExecutionLogRecord record) { packages.Add(LoadPackages()); log.Add(record); }
            var executed = new List<ISnPatch>();
            void Exec(PatchExecutionContext ctx) { executed.Add(ctx.CurrentPatch); }

            var installed = new List<SnComponentDescriptor>();
            var candidates = new List<ISnPatch>
            {
                Patch("C1", "1.0 <= v < 2.0", "v2.0", Exec, null),
                Inst("C1", "v2.0", Exec, null),
            };

            // ACTION BEFORE
            var pm = new PatchManager(null, Log);
            pm.ExecuteOnBefore(candidates, installed, false);

            // ASSERT BEFORE
            Assert.AreEqual(2, candidates.Count);
            Assert.AreEqual("C1v(2.0,,SuccessfulBefore)", ComponentsToStringWithResult(installed));

            // ACTION AFTER
            pm.ExecuteOnAfter(candidates, installed, false);

            // ASSERT AFTER
            Assert.AreEqual(0, candidates.Count);
            Assert.AreEqual("C1v2.0(2.0,2.0,Successful)",
                ComponentsToStringWithResult(installed));
            Assert.AreEqual("[C1: 2.0] OnBeforeActionStarts.|" +
                            "[C1: 2.0] OnBeforeActionFinished.|" +
                            "[C1: 2.0] OnAfterActionStarts.|" +
                            "[C1: 2.0] OnAfterActionFinished.",
                string.Join("|", log.Select(x => x.ToString(false))));
            Assert.AreEqual("", ErrorsToString(pm.Errors));
            Assert.AreEqual(4, packages.Count);
            Assert.AreEqual("1, C1: Install Successful, 2.0",
                PackagesToString(packages[3]));
        }

        [TestMethod]
        public void Patching_Exec_InstallOne_Success()
        {
            var packages = new List<Package[]>();
            var log = new List<PatchExecutionLogRecord>();
            void Log(PatchExecutionLogRecord record) { packages.Add(LoadPackages()); log.Add(record); }
            var executed = new List<ISnPatch>();
            void Exec(PatchExecutionContext ctx) { executed.Add(ctx.CurrentPatch); }

            var installed = new List<SnComponentDescriptor>();
            var candidates = new List<ISnPatch>
            {
                Inst("C1", "v1.0", Exec, Exec),
            };

            // ACTION BEFORE
            var pm = new PatchManager(null, Log);
            pm.ExecuteOnBefore(candidates, installed, false);

            // ASSERT BEFORE
            Assert.AreEqual(1, candidates.Count);
            Assert.AreEqual("C1v(1.0,,SuccessfulBefore)", ComponentsToStringWithResult(installed));

            // ACTION AFTER
            pm.ExecuteOnAfter(candidates, installed, false);

            // ASSERT AFTER
            Assert.AreEqual(0, candidates.Count);
            Assert.AreEqual("C1v1.0(1.0,1.0,Successful)", ComponentsToStringWithResult(installed));
            Assert.AreEqual("[C1: 1.0] OnBeforeActionStarts.|" +
                            "[C1: 1.0] OnBeforeActionFinished.|" +
                            "[C1: 1.0] OnAfterActionStarts.|" +
                            "[C1: 1.0] OnAfterActionFinished.",
                string.Join("|", log.Select(x => x.ToString(false))));
            Assert.AreEqual("", ErrorsToString(pm.Errors));
            Assert.AreEqual(4, packages.Count);
            Assert.AreEqual("1, C1: Install Unfinished, 1.0", PackagesToString(packages[0]));
            Assert.AreEqual("1, C1: Install SuccessfulBefore, 1.0", PackagesToString(packages[1]));
            Assert.AreEqual("1, C1: Install SuccessfulBefore, 1.0", PackagesToString(packages[2]));
            Assert.AreEqual("1, C1: Install Successful, 1.0", PackagesToString(packages[3]));

        }
        [TestMethod]
        public void Patching_Exec_InstallOne_FaultyBefore()
        {
            var packages = new List<Package[]>();
            var log = new List<PatchExecutionLogRecord>();
            void Log(PatchExecutionLogRecord record) { packages.Add(LoadPackages()); log.Add(record); }
            var executed = new List<ISnPatch>();
            void Exec(PatchExecutionContext ctx) { executed.Add(ctx.CurrentPatch); }


            var installed = new List<SnComponentDescriptor>();
            var candidates = new List<ISnPatch>
            {
                Inst("C1", "v1.0", Error, Exec),
            };

            // ACTION BEFORE
            var pm = new PatchManager(null, Log);
            pm.ExecuteOnBefore(candidates, installed, false);

            // ASSERT BEFORE
            Assert.AreEqual(0, candidates.Count);
            Assert.AreEqual("C1v(1.0,,FaultyBefore)", ComponentsToStringWithResult(installed));

            // ACTION AFTER
            pm.ExecuteOnAfter(candidates, installed, false);

            // ASSERT AFTER
            Assert.AreEqual(0, candidates.Count);
            Assert.AreEqual("C1v(1.0,,FaultyBefore)", ComponentsToStringWithResult(installed));
            Assert.AreEqual("[C1: 1.0] OnBeforeActionStarts.|" +
                            "[C1: 1.0] ExecutionErrorOnBefore.",
                string.Join("|", log.Select(x => x.ToString(false))));
            Assert.AreEqual("ExecutionErrorOnBefore C1: 1.0", ErrorsToString(pm.Errors));
            Assert.AreEqual(2, packages.Count);
            Assert.AreEqual("1, C1: Install Unfinished, 1.0", PackagesToString(packages[0]));
            Assert.AreEqual("1, C1: Install FaultyBefore, 1.0", PackagesToString(packages[1]));
        }
        [TestMethod]
        public void Patching_Exec_PatchOne_Success()
        {
            var packages = new List<Package[]>();
            var log = new List<PatchExecutionLogRecord>();
            void Log(PatchExecutionLogRecord record) { packages.Add(LoadPackages()); log.Add(record); }
            var executed = new List<ISnPatch>();
            void Exec(PatchExecutionContext ctx) { executed.Add(ctx.CurrentPatch); }

            var installed = new List<SnComponentDescriptor>();
            var candidates = new List<ISnPatch>
            {
                Inst("C1", "v1.0", Exec, Exec),
                Patch("C1", "1.0 <= v < 2.0", "v2.0", Exec, Exec),
            };

            // ACTION BEFORE
            var pm = new PatchManager(null, Log);
            pm.ExecuteOnBefore(candidates, installed, false);

            // ASSERT BEFORE
            Assert.AreEqual(2, candidates.Count);
            Assert.AreEqual("C1v(2.0,,SuccessfulBefore)", ComponentsToStringWithResult(installed));

            // ACTION AFTER
            pm.ExecuteOnAfter(candidates, installed, false);

            // ASSERT AFTER
            Assert.AreEqual(0, candidates.Count);
            Assert.AreEqual("C1v2.0(2.0,2.0,Successful)", ComponentsToStringWithResult(installed));
            Assert.AreEqual("[C1: 1.0] OnBeforeActionStarts.|" +
                            "[C1: 1.0] OnBeforeActionFinished.|" +
                            "[C1: 1.0 <= v < 2.0 --> 2.0] OnBeforeActionStarts.|" +
                            "[C1: 1.0 <= v < 2.0 --> 2.0] OnBeforeActionFinished.|" +
                            "[C1: 1.0] OnAfterActionStarts.|" +
                            "[C1: 1.0] OnAfterActionFinished.|" +
                            "[C1: 1.0 <= v < 2.0 --> 2.0] OnAfterActionStarts.|" +
                            "[C1: 1.0 <= v < 2.0 --> 2.0] OnAfterActionFinished.",
                string.Join("|", log.Select(x => x.ToString(false))));
            Assert.AreEqual("", ErrorsToString(pm.Errors));
            Assert.AreEqual(8, packages.Count);
            Assert.AreEqual("1, C1: Install SuccessfulBefore, 1.0|" +
                            "2, C1: Patch SuccessfulBefore, 2.0", PackagesToString(packages[4]));
            Assert.AreEqual("1, C1: Install Successful, 1.0|" +
                            "2, C1: Patch SuccessfulBefore, 2.0", PackagesToString(packages[5]));
            Assert.AreEqual("1, C1: Install Successful, 1.0|" +
                            "2, C1: Patch SuccessfulBefore, 2.0", PackagesToString(packages[6]));
            Assert.AreEqual("1, C1: Install Successful, 1.0|" +
                            "2, C1: Patch Successful, 2.0", PackagesToString(packages[7]));
        }
        [TestMethod]
        public void Patching_Exec_PatchOne_Faulty()
        {
            var packages = new List<Package[]>();
            var log = new List<PatchExecutionLogRecord>();
            void Log(PatchExecutionLogRecord record) { packages.Add(LoadPackages()); log.Add(record); }
            var executed = new List<ISnPatch>();
            void Exec(PatchExecutionContext ctx) { executed.Add(ctx.CurrentPatch); }

            var installed = new List<SnComponentDescriptor>();
            var candidates = new List<ISnPatch>
            {
                Inst("C1", "v1.0", Exec, Exec),
                Patch("C1", "1.0 <= v < 2.0", "v2.0", Exec, Error),
            };

            // ACTION BEFORE
            var pm = new PatchManager(null, Log);
            pm.ExecuteOnBefore(candidates, installed, false);

            // ASSERT BEFORE
            Assert.AreEqual(2, candidates.Count);
            Assert.AreEqual("C1v(2.0,,SuccessfulBefore)", ComponentsToStringWithResult(installed));

            // ACTION AFTER
            pm.ExecuteOnAfter(candidates, installed, false);

            // ASSERT AFTER
            Assert.AreEqual(0, candidates.Count);
            Assert.AreEqual("C1v1.0(2.0,2.0,Faulty)", ComponentsToStringWithResult(installed));
            Assert.AreEqual("[C1: 1.0] OnBeforeActionStarts.|" +
                            "[C1: 1.0] OnBeforeActionFinished.|" +
                            "[C1: 1.0 <= v < 2.0 --> 2.0] OnBeforeActionStarts.|" +
                            "[C1: 1.0 <= v < 2.0 --> 2.0] OnBeforeActionFinished.|" +
                            "[C1: 1.0] OnAfterActionStarts.|" +
                            "[C1: 1.0] OnAfterActionFinished.|" +
                            "[C1: 1.0 <= v < 2.0 --> 2.0] OnAfterActionStarts.|" +
                            "[C1: 1.0 <= v < 2.0 --> 2.0] ExecutionError.",
                string.Join("|", log.Select(x => x.ToString(false))));
            Assert.AreEqual("ExecutionErrorOnAfter C1: 1.0 <= v < 2.0 --> 2.0", ErrorsToString(pm.Errors));
            Assert.AreEqual(8, packages.Count);
            Assert.AreEqual("1, C1: Install SuccessfulBefore, 1.0|2, C1: Patch SuccessfulBefore, 2.0", PackagesToString(packages[4]));
            Assert.AreEqual("1, C1: Install Successful, 1.0|2, C1: Patch SuccessfulBefore, 2.0", PackagesToString(packages[5]));
            Assert.AreEqual("1, C1: Install Successful, 1.0|2, C1: Patch SuccessfulBefore, 2.0", PackagesToString(packages[6]));
            Assert.AreEqual("1, C1: Install Successful, 1.0|2, C1: Patch Faulty, 2.0", PackagesToString(packages[7]));
        }
        [TestMethod]
        public void Patching_Exec_SkipPatch_FaultyInstaller()
        {
            // Faulty execution blocks the following patches on the same component.
            var packages = new List<Package[]>();
            var log = new List<PatchExecutionLogRecord>();
            void Log(PatchExecutionLogRecord record) { packages.Add(LoadPackages()); log.Add(record); }
            var executed = new List<ISnPatch>();
            void Exec(PatchExecutionContext ctx) { executed.Add(ctx.CurrentPatch); }

            var installed = new List<SnComponentDescriptor>();
            var candidates = new List<ISnPatch>
            {
                Inst("C1", "v1.0",Exec, Error),
                Patch("C1", "1.0 <= v < 2.0", "v2.0",Exec, Exec),
            };

            // ACTION BEFORE
            var pm = new PatchManager(null, Log);
            pm.ExecuteOnBefore(candidates, installed, false);

            // ASSERT BEFORE
            Assert.AreEqual(2, candidates.Count);
            Assert.AreEqual("C1v(2.0,,SuccessfulBefore)", ComponentsToStringWithResult(installed));

            // ACTION AFTER
            pm.ExecuteOnAfter(candidates, installed, false);

            // ASSERT AFTER
            Assert.AreEqual(0, candidates.Count);
            Assert.AreEqual("C1v(2.0,1.0,Faulty)", ComponentsToStringWithResult(installed));
            Assert.AreEqual("[C1: 1.0] OnBeforeActionStarts.|" +
                            "[C1: 1.0] OnBeforeActionFinished.|" +
                            "[C1: 1.0 <= v < 2.0 --> 2.0] OnBeforeActionStarts.|" +
                            "[C1: 1.0 <= v < 2.0 --> 2.0] OnBeforeActionFinished.|" +
                            "[C1: 1.0] OnAfterActionStarts.|" +
                            "[C1: 1.0] ExecutionError.|" +
                            "[C1: 1.0 <= v < 2.0 --> 2.0] CannotExecuteMissingVersion.",
                string.Join("|", log.Select(x => x.ToString(false))));
            Assert.AreEqual("ExecutionErrorOnAfter C1: 1.0|" +
                            "MissingVersion C1: 1.0 <= v < 2.0 --> 2.0", ErrorsToString(pm.Errors));
            Assert.AreEqual(7, packages.Count);
            Assert.AreEqual("1, C1: Install SuccessfulBefore, 1.0|2, C1: Patch SuccessfulBefore, 2.0", PackagesToString(packages[3]));
            Assert.AreEqual("1, C1: Install SuccessfulBefore, 1.0|2, C1: Patch SuccessfulBefore, 2.0", PackagesToString(packages[4]));
            Assert.AreEqual("1, C1: Install Faulty, 1.0|2, C1: Patch SuccessfulBefore, 2.0", PackagesToString(packages[5]));
            Assert.AreEqual("1, C1: Install Faulty, 1.0|2, C1: Patch SuccessfulBefore, 2.0", PackagesToString(packages[6]));
        }
        [TestMethod]
        public void Patching_Exec_SkipPatch_FaultySnPatch()
        {
            // Faulty execution blocks the following patches on the same component.
            var packages = new List<Package[]>();
            var log = new List<PatchExecutionLogRecord>();
            void Log(PatchExecutionLogRecord record) { packages.Add(LoadPackages()); log.Add(record); }
            var executed = new List<ISnPatch>();
            void Exec(PatchExecutionContext ctx) { executed.Add(ctx.CurrentPatch); }

            var installed = new List<SnComponentDescriptor>();
            var candidates = new List<ISnPatch>
            {
                Inst("C1", "v1.0", Exec, Exec),
                Patch("C1", "1.0 <= v < 2.0", "v2.0",Exec, Error),
                Patch("C1", "2.0 <= v < 3.0", "v3.0", Exec, Exec),
            };

            // ACTION BEFORE
            var pm = new PatchManager(null, Log);
            pm.ExecuteOnBefore(candidates, installed, false);

            // ASSERT BEFORE
            Assert.AreEqual(3, candidates.Count);
            Assert.AreEqual("C1v(3.0,,SuccessfulBefore)", ComponentsToStringWithResult(installed));

            // ACTION AFTER
            pm.ExecuteOnAfter(candidates, installed, false);

            // ASSERT AFTER
            Assert.AreEqual(0, candidates.Count);
            Assert.AreEqual("C1v1.0(3.0,2.0,Faulty)", ComponentsToStringWithResult(installed));
            Assert.AreEqual("[C1: 1.0] OnBeforeActionStarts.|" +
                            "[C1: 1.0] OnBeforeActionFinished.|" +
                            "[C1: 1.0 <= v < 2.0 --> 2.0] OnBeforeActionStarts.|" +
                            "[C1: 1.0 <= v < 2.0 --> 2.0] OnBeforeActionFinished.|" +
                            "[C1: 2.0 <= v < 3.0 --> 3.0] OnBeforeActionStarts.|" +
                            "[C1: 2.0 <= v < 3.0 --> 3.0] OnBeforeActionFinished.|" +
                            "[C1: 1.0] OnAfterActionStarts.|" +
                            "[C1: 1.0] OnAfterActionFinished.|" +
                            "[C1: 1.0 <= v < 2.0 --> 2.0] OnAfterActionStarts.|" +
                            "[C1: 1.0 <= v < 2.0 --> 2.0] ExecutionError.|" +
                            "[C1: 2.0 <= v < 3.0 --> 3.0] CannotExecuteMissingVersion.",
                string.Join("|", log.Select(x => x.ToString(false))));
            Assert.AreEqual("ExecutionErrorOnAfter C1: 1.0 <= v < 2.0 --> 2.0|" +
                            "MissingVersion C1: 2.0 <= v < 3.0 --> 3.0", ErrorsToString(pm.Errors));
            Assert.AreEqual(11, packages.Count);
            Assert.AreEqual("1, C1: Install Successful, 1.0|2, C1: Patch SuccessfulBefore, 2.0|3, C1: Patch SuccessfulBefore, 3.0", PackagesToString(packages[7]));
            Assert.AreEqual("1, C1: Install Successful, 1.0|2, C1: Patch SuccessfulBefore, 2.0|3, C1: Patch SuccessfulBefore, 3.0", PackagesToString(packages[8]));
            Assert.AreEqual("1, C1: Install Successful, 1.0|2, C1: Patch Faulty, 2.0|3, C1: Patch SuccessfulBefore, 3.0", PackagesToString(packages[9]));
            Assert.AreEqual("1, C1: Install Successful, 1.0|2, C1: Patch Faulty, 2.0|3, C1: Patch SuccessfulBefore, 3.0", PackagesToString(packages[10]));
        }

        [TestMethod]
        public void Patching_Exec_SkipPatch_MoreFaultyChains()
        {
            // Faulty execution blocks the following patches on the same component.
            var packages = new List<Package[]>();
            var log = new List<PatchExecutionLogRecord>();
            void Log(PatchExecutionLogRecord record) { packages.Add(LoadPackages()); log.Add(record); }
            var executed = new List<ISnPatch>();
            void Exec(PatchExecutionContext ctx) { executed.Add(ctx.CurrentPatch); }

            var installed = new List<SnComponentDescriptor>();
            var candidates = new List<ISnPatch>
            {
                // Problem in the installer
                Inst("C1", "v1.0", Exec, Error),
                Patch("C1", "1.0 <= v < 2.0", "v2.0", Exec, Exec),
                Patch("C1", "2.0 <= v < 3.0", "v3.0", Exec, Exec),
                // Problem in a middle patch
                Inst("C2", "v1.0", Exec, Exec),
                Patch("C2", "1.0 <= v < 2.0", "v2.0", Exec, Error),
                Patch("C2", "2.0 <= v < 3.0", "v3.0", Exec, Exec),
                // There is no problem
                Inst("C3", "v1.0", Exec, Exec),
                Patch("C3", "1.0 <= v < 2.0", "v2.0", Exec, Exec),
                Patch("C3", "2.0 <= v < 3.0", "v3.0", Exec, Exec),
            };

            // ACTION BEFORE
            var pm = new PatchManager(null, Log);
            pm.ExecuteOnBefore(candidates, installed, false);

            // ASSERT BEFORE
            Assert.AreEqual(9, candidates.Count);
            Assert.AreEqual("C1v(3.0,,SuccessfulBefore) C2v(3.0,,SuccessfulBefore) C3v(3.0,,SuccessfulBefore)", ComponentsToStringWithResult(installed));

            // ACTION AFTER
            pm.ExecuteOnAfter(candidates, installed, false);

            // ASSERT AFTER
            Assert.AreEqual(0, candidates.Count);
            Assert.AreEqual("C1v(3.0,1.0,Faulty) C2v1.0(3.0,2.0,Faulty) C3v3.0(3.0,3.0,Successful)", ComponentsToStringWithResult(installed));
            Assert.AreEqual("[C1: 1.0] OnBeforeActionStarts.|" +
                            "[C1: 1.0] OnBeforeActionFinished.|" +
                            "[C1: 1.0 <= v < 2.0 --> 2.0] OnBeforeActionStarts.|" +
                            "[C1: 1.0 <= v < 2.0 --> 2.0] OnBeforeActionFinished.|" +
                            "[C1: 2.0 <= v < 3.0 --> 3.0] OnBeforeActionStarts.|" +
                            "[C1: 2.0 <= v < 3.0 --> 3.0] OnBeforeActionFinished.|" +
                            "[C2: 1.0] OnBeforeActionStarts.|" +
                            "[C2: 1.0] OnBeforeActionFinished.|" +
                            "[C2: 1.0 <= v < 2.0 --> 2.0] OnBeforeActionStarts.|" +
                            "[C2: 1.0 <= v < 2.0 --> 2.0] OnBeforeActionFinished.|" +
                            "[C2: 2.0 <= v < 3.0 --> 3.0] OnBeforeActionStarts.|" +
                            "[C2: 2.0 <= v < 3.0 --> 3.0] OnBeforeActionFinished.|" +
                            "[C3: 1.0] OnBeforeActionStarts.|" +
                            "[C3: 1.0] OnBeforeActionFinished.|" +
                            "[C3: 1.0 <= v < 2.0 --> 2.0] OnBeforeActionStarts.|" +
                            "[C3: 1.0 <= v < 2.0 --> 2.0] OnBeforeActionFinished.|" +
                            "[C3: 2.0 <= v < 3.0 --> 3.0] OnBeforeActionStarts.|" +
                            "[C3: 2.0 <= v < 3.0 --> 3.0] OnBeforeActionFinished.|" +
                            "[C1: 1.0] OnAfterActionStarts.|" +
                            "[C1: 1.0] ExecutionError.|" +
                            "[C2: 1.0] OnAfterActionStarts.|" +
                            "[C2: 1.0] OnAfterActionFinished.|" +
                            "[C2: 1.0 <= v < 2.0 --> 2.0] OnAfterActionStarts.|" +
                            "[C2: 1.0 <= v < 2.0 --> 2.0] ExecutionError.|" +
                            "[C3: 1.0] OnAfterActionStarts.|" +
                            "[C3: 1.0] OnAfterActionFinished.|" +
                            "[C3: 1.0 <= v < 2.0 --> 2.0] OnAfterActionStarts.|" +
                            "[C3: 1.0 <= v < 2.0 --> 2.0] OnAfterActionFinished.|" +
                            "[C3: 2.0 <= v < 3.0 --> 3.0] OnAfterActionStarts.|" +
                            "[C3: 2.0 <= v < 3.0 --> 3.0] OnAfterActionFinished.|" +
                            "[C1: 1.0 <= v < 2.0 --> 2.0] CannotExecuteMissingVersion.|" +
                            "[C1: 2.0 <= v < 3.0 --> 3.0] CannotExecuteMissingVersion.|" +
                            "[C2: 2.0 <= v < 3.0 --> 3.0] CannotExecuteMissingVersion.",
                string.Join("|", log.Select(x => x.ToString(false))));
            Assert.AreEqual("ExecutionErrorOnAfter C1: 1.0|" +
                            "ExecutionErrorOnAfter C2: 1.0 <= v < 2.0 --> 2.0|" +
                            "MissingVersion C1: 1.0 <= v < 2.0 --> 2.0|" +
                            "MissingVersion C1: 2.0 <= v < 3.0 --> 3.0|" +
                            "MissingVersion C2: 2.0 <= v < 3.0 --> 3.0", ErrorsToString(pm.Errors));
            Assert.AreEqual(33, packages.Count);
            Assert.AreEqual("1, C1: Install Faulty, 1.0|" +
                            "2, C1: Patch SuccessfulBefore, 2.0|" +
                            "3, C1: Patch SuccessfulBefore, 3.0|" +
                            "4, C2: Install Successful, 1.0|" +
                            "5, C2: Patch Faulty, 2.0|" +
                            "6, C2: Patch SuccessfulBefore, 3.0|" +
                            "7, C3: Install Successful, 1.0|" +
                            "8, C3: Patch Successful, 2.0|" +
                            "9, C3: Patch Successful, 3.0", PackagesToString(packages[32]));
        }

        [TestMethod]
        public void Patching_Exec_WaitForDependency_WaitingBeforeAndAfter()
        {
            // Faulty execution blocks the following patches on the same component.
            var packages = new List<Package[]>();
            var log = new List<PatchExecutionLogRecord>();
            void Log(PatchExecutionLogRecord record) { packages.Add(LoadPackages()); log.Add(record); }
            var executed = new List<ISnPatch>();
            void Exec(PatchExecutionContext ctx) { executed.Add(ctx.CurrentPatch); }

            var installed = new List<SnComponentDescriptor>();
            var candidates = new List<ISnPatch>
            {
                Inst("C1", "v1.0", Exec, Exec),
                Patch("C1", "1.0 <= v < 2.0", "v2.0", 
                    new[] {Dep("C2", "3.0 <= v")}, Exec, Exec),
                Patch("C1", "2.0 <= v < 3.0", "v3.0", Exec, Exec),

                Inst("C2", "v1.0", Exec, Exec),
                Patch("C2", "1.0 <= v < 2.0", "v2.0", Exec, Exec),
                Patch("C2", "2.0 <= v < 3.0", "v3.0", Exec, Exec),
            };

            // ACTION BEFORE
            var pm = new PatchManager(null, Log);
            pm.ExecuteOnBefore(candidates, installed, false);

            // ASSERT BEFORE
            Assert.AreEqual(6, candidates.Count);
            Assert.AreEqual("C1v(3.0,,SuccessfulBefore) C2v(3.0,,SuccessfulBefore)",
                ComponentsToStringWithResult(installed));

            // ACTION AFTER
            pm.ExecuteOnAfter(candidates, installed, false);

            // ASSERT AFTER
            Assert.AreEqual(0, candidates.Count);
            Assert.AreEqual("C1v3.0(3.0,3.0,Successful) C2v3.0(3.0,3.0,Successful)",
                ComponentsToStringWithResult(installed));
            Assert.AreEqual("[C1: 1.0] OnBeforeActionStarts.|" +
                            "[C1: 1.0] OnBeforeActionFinished.|" +
                            "[C2: 1.0] OnBeforeActionStarts.|" +
                            "[C2: 1.0] OnBeforeActionFinished.|" +
                            "[C2: 1.0 <= v < 2.0 --> 2.0] OnBeforeActionStarts.|" +
                            "[C2: 1.0 <= v < 2.0 --> 2.0] OnBeforeActionFinished.|" +
                            "[C2: 2.0 <= v < 3.0 --> 3.0] OnBeforeActionStarts.|" +
                            "[C2: 2.0 <= v < 3.0 --> 3.0] OnBeforeActionFinished.|" +
                            "[C1: 1.0 <= v < 2.0 --> 2.0] OnBeforeActionStarts.|" +
                            "[C1: 1.0 <= v < 2.0 --> 2.0] OnBeforeActionFinished.|" +
                            "[C1: 2.0 <= v < 3.0 --> 3.0] OnBeforeActionStarts.|" +
                            "[C1: 2.0 <= v < 3.0 --> 3.0] OnBeforeActionFinished.|" +
                            "[C1: 1.0] OnAfterActionStarts.|" +
                            "[C1: 1.0] OnAfterActionFinished.|" +
                            "[C2: 1.0] OnAfterActionStarts.|" +
                            "[C2: 1.0] OnAfterActionFinished.|" +
                            "[C2: 1.0 <= v < 2.0 --> 2.0] OnAfterActionStarts.|" +
                            "[C2: 1.0 <= v < 2.0 --> 2.0] OnAfterActionFinished.|" +
                            "[C2: 2.0 <= v < 3.0 --> 3.0] OnAfterActionStarts.|" +
                            "[C2: 2.0 <= v < 3.0 --> 3.0] OnAfterActionFinished.|" +
                            "[C1: 1.0 <= v < 2.0 --> 2.0] OnAfterActionStarts.|" +
                            "[C1: 1.0 <= v < 2.0 --> 2.0] OnAfterActionFinished.|" +
                            "[C1: 2.0 <= v < 3.0 --> 3.0] OnAfterActionStarts.|" +
                            "[C1: 2.0 <= v < 3.0 --> 3.0] OnAfterActionFinished.",
                string.Join("|", log.Select(x => x.ToString(false))));
            Assert.AreEqual("", ErrorsToString(pm.Errors));
            Assert.AreEqual(24, packages.Count);
            Assert.AreEqual("1, C1: Install Successful, 1.0|" +
                            "2, C2: Install Successful, 1.0|" +
                            "3, C2: Patch Successful, 2.0|" +
                            "4, C2: Patch Successful, 3.0|" +
                            "5, C1: Patch Successful, 2.0|" +
                            "6, C1: Patch Successful, 3.0",
                PackagesToString(packages[23]));
        }
        [TestMethod]
        public void Patching_Exec_InstallerIsLast()
        {
            // Faulty execution blocks the following patches on the same component.
            var packages = new List<Package[]>();
            var log = new List<PatchExecutionLogRecord>();
            void Log(PatchExecutionLogRecord record) { packages.Add(LoadPackages()); log.Add(record); }
            var executed = new List<ISnPatch>();
            void Exec(PatchExecutionContext ctx) { executed.Add(ctx.CurrentPatch); }

            var installed = new List<SnComponentDescriptor>();
            var candidates = new List<ISnPatch>
            {
                Patch("C1", "1.0 <= v < 2.0", "v3.0", Exec),
                Patch("C1", "2.0 <= v <= 2.0", "v3.0", Exec),
                Patch("C1", "2.0 <= v < 3.0", "v3.0", Exec),
                Inst("C1", "v3.0", Exec),
            };

            // ACTION BEFORE
            var pm = new PatchManager(null, Log);
            pm.ExecuteOnBefore(candidates, installed, false);

            // ASSERT BEFORE
            Assert.AreEqual(4, candidates.Count);
            Assert.AreEqual("", ComponentsToStringWithResult(installed));

            // ACTION AFTER
            pm.ExecuteOnAfter(candidates, installed, false);

            // ASSERT AFTER
            Assert.AreEqual(0, candidates.Count);
            Assert.AreEqual("C1v3.0(3.0,3.0,Successful)",
                ComponentsToStringWithResult(installed));
            Assert.AreEqual("[C1: 3.0] OnAfterActionStarts.|[C1: 3.0] OnAfterActionFinished.",
                string.Join("|", log.Select(x => x.ToString(false))));
            Assert.AreEqual("", ErrorsToString(pm.Errors));
            Assert.AreEqual(2, packages.Count);
            Assert.AreEqual("1, C1: Install Successful, 3.0",
                PackagesToString(packages[1]));
        }

        /* ======================================================================= CONDITIONAL EXECUTION TESTS */

        // Patch vary component versions conditionally
        [TestMethod]
        public void Patching_Exec_ConditionalActions_a()
        {
            var log = new List<PatchExecutionLogRecord>();
            void Log(PatchExecutionLogRecord record) { log.Add(record); }
            void Before(PatchExecutionContext ctx)
            {
                if (ctx.ComponentVersionIsEqual("7.4.0.2"))
                    ctx.Log("Update database in in 7.4.0.2.");
            }
            void After(PatchExecutionContext ctx)
            {
                if (ctx.ComponentVersionIsLower("7.3.0"))
                    ctx.Log("Import new or modified content in 7.3.0.");
                if (ctx.ComponentVersionIsLower("7.6.0"))
                    ctx.Log("Import new or modified content in 7.6.0.");
            }

            var installed = new List<SnComponentDescriptor>
            {
                Comp("C1", "v7.1.0")
            };
            var candidates = new List<ISnPatch>
            {
                Patch("C1", "7.1.0 <= v", "v7.7.0", Before, After),
            };

            // ACTION BEFORE
            var pm = new PatchManager(null, Log);
            pm.ExecuteOnBefore(candidates, installed, false);

            // ASSERT BEFORE
            Assert.AreEqual(1, candidates.Count);
            Assert.AreEqual("C1v7.1.0(7.7.0,,SuccessfulBefore)", ComponentsToStringWithResult(installed));

            // ACTION AFTER
            pm.ExecuteOnAfter(candidates, installed, false);

            // ASSERT AFTER
            Assert.AreEqual(0, candidates.Count);
            Assert.AreEqual("C1v7.7.0(7.7.0,7.7.0,Successful)", ComponentsToStringWithResult(installed));
            Assert.AreEqual("[C1: 7.1.0 <= v --> 7.7.0] OnBeforeActionStarts.|" +
                            "[C1: 7.1.0 <= v --> 7.7.0] OnBeforeActionFinished.|" +
                            "[C1: 7.1.0 <= v --> 7.7.0] OnAfterActionStarts.|" +
                            "[C1: 7.1.0 <= v --> 7.7.0] ExecutingOnAfter. Import new or modified content in 7.3.0.|" +
                            "[C1: 7.1.0 <= v --> 7.7.0] ExecutingOnAfter. Import new or modified content in 7.6.0.|" +
                            "[C1: 7.1.0 <= v --> 7.7.0] OnAfterActionFinished.",
                string.Join("|", log.Select(x => x.ToString())));
            Assert.AreEqual("", ErrorsToString(pm.Errors));
        }
        [TestMethod]
        public void Patching_Exec_ConditionalActions_b()
        {
            var log = new List<PatchExecutionLogRecord>();
            void Log(PatchExecutionLogRecord record) { log.Add(record); }
            void Before(PatchExecutionContext ctx)
            {
                if (ctx.ComponentVersionIsEqual("7.4.0.2"))
                    ctx.Log("Update database in in 7.4.0.2.");
            }
            void After(PatchExecutionContext ctx)
            {
                if (ctx.ComponentVersionIsLower("7.3.0"))
                    ctx.Log("Import new or modified content in 7.3.0.");
                if (ctx.ComponentVersionIsLower("7.6.0"))
                    ctx.Log("Import new or modified content in 7.6.0.");
            }

            var installed = new List<SnComponentDescriptor>
            {
                Comp("C1", "v7.4.0.2")
            };
            var candidates = new List<ISnPatch>
            {
                Patch("C1", "7.1.0 <= v", "v7.7.0", Before, After),
            };

            // ACTION BEFORE
            var pm = new PatchManager(null, Log);
            pm.ExecuteOnBefore(candidates, installed, false);

            // ASSERT BEFORE
            Assert.AreEqual(1, candidates.Count);
            Assert.AreEqual("C1v7.4.0.2(7.7.0,,SuccessfulBefore)", ComponentsToStringWithResult(installed));

            // ACTION AFTER
            pm.ExecuteOnAfter(candidates, installed, false);

            // ASSERT AFTER
            Assert.AreEqual(0, candidates.Count);
            Assert.AreEqual("C1v7.7.0(7.7.0,7.7.0,Successful)", ComponentsToStringWithResult(installed));
            Assert.AreEqual("[C1: 7.1.0 <= v --> 7.7.0] OnBeforeActionStarts.|" +
                            "[C1: 7.1.0 <= v --> 7.7.0] ExecutingOnBefore. Update database in in 7.4.0.2.|" +
                            "[C1: 7.1.0 <= v --> 7.7.0] OnBeforeActionFinished.|" +
                            "[C1: 7.1.0 <= v --> 7.7.0] OnAfterActionStarts.|" +
                            "[C1: 7.1.0 <= v --> 7.7.0] ExecutingOnAfter. Import new or modified content in 7.6.0.|" +
                            "[C1: 7.1.0 <= v --> 7.7.0] OnAfterActionFinished.",
                string.Join("|", log.Select(x => x.ToString())));
            Assert.AreEqual("", ErrorsToString(pm.Errors));
        }
        [TestMethod]
        public void Patching_Exec_ConditionalActions_c()
        {
            var log = new List<PatchExecutionLogRecord>();
            void Log(PatchExecutionLogRecord record) { log.Add(record); }
            void Before(PatchExecutionContext ctx)
            {
                if (ctx.ComponentVersionIsEqual("7.4.0.2"))
                    ctx.Log("Update database in in 7.4.0.2.");
            }
            void After(PatchExecutionContext ctx)
            {
                if (ctx.ComponentVersionIsLower("7.3.0"))
                    ctx.Log("Import new or modified content in 7.3.0.");
                if (ctx.ComponentVersionIsLower("7.6.0"))
                    ctx.Log("Import new or modified content in 7.6.0.");
            }

            var installed = new List<SnComponentDescriptor>
            {
                Comp("C1", "v7.5")
            };
            var candidates = new List<ISnPatch>
            {
                Patch("C1", "7.1.0 <= v", "v7.7.0", Before, After),
            };

            // ACTION BEFORE
            var pm = new PatchManager(null, Log);
            pm.ExecuteOnBefore(candidates, installed, false);

            // ASSERT BEFORE
            Assert.AreEqual(1, candidates.Count);
            Assert.AreEqual("C1v7.5(7.7.0,,SuccessfulBefore)", ComponentsToStringWithResult(installed));

            // ACTION AFTER
            pm.ExecuteOnAfter(candidates, installed, false);

            // ASSERT AFTER
            Assert.AreEqual(0, candidates.Count);
            Assert.AreEqual("C1v7.7.0(7.7.0,7.7.0,Successful)", ComponentsToStringWithResult(installed));
            Assert.AreEqual("[C1: 7.1.0 <= v --> 7.7.0] OnBeforeActionStarts.|" +
                            "[C1: 7.1.0 <= v --> 7.7.0] OnBeforeActionFinished.|" +
                            "[C1: 7.1.0 <= v --> 7.7.0] OnAfterActionStarts.|" +
                            "[C1: 7.1.0 <= v --> 7.7.0] ExecutingOnAfter. Import new or modified content in 7.6.0.|" +
                            "[C1: 7.1.0 <= v --> 7.7.0] OnAfterActionFinished.",
                string.Join("|", log.Select(x => x.ToString())));
            Assert.AreEqual("", ErrorsToString(pm.Errors));
        }

        /* ======================================================== Tools */

        private void Error(PatchExecutionContext context) => throw new Exception("Err");
        private string ErrorsToString(IEnumerable<PatchExecutionError> errors)
        {
            return string.Join("|", errors.Select(x => x.ToString()));
        }
    }
}
