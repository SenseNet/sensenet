﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.Packaging.Tests
{
    [TestClass]
    public class PatchingExecutionTests : PatchingTestBase
    {
        [TestMethod]
        public void PatchingExecSim_Install_1New()
        {
            var patches = new ISnPatch[]
            {
                Inst("C1", "v1.0", null, null),
            };

            var installed = new SnComponentDescriptor[0];
            var context = new PatchExecutionContext(null, null);
            var pm = new PatchManager(context);
            var executables = pm.GetExecutablePatches(patches, installed, context, out var after).ToArray();

            Assert.AreEqual(1, after.Length);
            Assert.AreEqual(1, executables.Length);
        }
        [TestMethod]
        public void PatchingExecSim_Install_1New1Skip()
        {
            var installed = new[]
            {
                Comp("C1", "v1.0")
            };
            var patches = new ISnPatch[]
            {
                // Will be skipped. The different version does not cause any error because this patch is irrelevant.
                Inst("C1", "v1.1", null, null),
                Inst("C2", "v2.3", null, null),
            };

            // ACTION
            var context = new PatchExecutionContext(null, null);
            var pm = new PatchManager(context);
            var executables = pm.GetExecutablePatches(patches, installed, context, out var after).ToArray();

            // ASSERT
            Assert.AreEqual(2, after.Length);
            Assert.AreEqual("C1", after[0].ComponentId);
            Assert.AreEqual(new Version(1, 0), after[0].Version);
            Assert.AreEqual("C2", after[1].ComponentId);
            Assert.AreEqual(new Version(2, 3), after[1].Version);

            Assert.AreEqual(1, executables.Length);
            Assert.AreEqual("C2", executables[0].ComponentId);
            Assert.AreEqual(new Version(2, 3), executables[0].Version);
        }
        [TestMethod]
        public void PatchingExecSim_Install_Duplicates()
        {
            var installed = new[]
            {
                Comp("C1", "v1.0")
            };
            var patches = new ISnPatch[]
            {
                // Will be skipped. The same id does not cause any error because this patch is irrelevant.
                Inst("C1", "v1.0", null, null),
                Inst("C1", "v1.1", null, null),
                // Would be executable but the same id causes an error.
                Inst("C2", "v1.0", null, null),
                Inst("C3", "v1.0", null, null),
                Inst("C3", "v2.3", null, null),
                Inst("C4", "v1.0", null, null),
                Inst("C4", "v2.3", null, null),
                Inst("C4", "v1.0", null, null),
            };

            // ACTION
            var context = new PatchExecutionContext(null, null);
            var pm = new PatchManager(context);
            var executables = pm.GetExecutablePatches(patches, installed, context, out var after).ToArray();

            // ASSERT
            Assert.AreEqual(1, after.Length);
            Assert.AreEqual(0, executables.Length);
            var errors = context.Errors;
            Assert.AreEqual(2, errors.Count);
            Assert.IsTrue(errors[0].Message.Contains("C3"));
            Assert.AreEqual(PatchExecutionErrorType.DuplicatedInstaller, errors[0].ErrorType);
            Assert.AreEqual(2, errors[0].FaultyPatches.Length);
            Assert.IsTrue(errors[1].Message.Contains("C4"));
            Assert.AreEqual(PatchExecutionErrorType.DuplicatedInstaller, errors[1].ErrorType);
            Assert.AreEqual(3, errors[1].FaultyPatches.Length);
        }
        [TestMethod]
        public void PatchingExecSim_Install_Dependency()
        {
            // Test the right installer execution order if there is a dependency among the installers.
            var installed = new SnComponentDescriptor[0];
            var patches = new ISnPatch[]
            {
                Inst("C2", "v1.0", new[] {Dep("C1", "1.0 <= v")}, null),
                Inst("C1", "v1.0", null, null),
            };

            // ACTION
            var context = new PatchExecutionContext(null, null);
            var pm = new PatchManager(context);
            var executables = pm.GetExecutablePatches(patches, installed, context, out var after).ToArray();

            // ASSERT
            Assert.AreEqual(2, after.Length);
            Assert.AreEqual("C1,C2", string.Join(",", after.Select(x=>x.ComponentId)));
            Assert.AreEqual(2, executables.Length);
            Assert.AreEqual("C1,C2", string.Join(",", executables.Select(x => x.ComponentId)));
        }
        [TestMethod]
        public void PatchingExecSim_Install_Dependency2()
        {
            // Test the right installer execution order if there is a dependency among the installers.
            // One dependency is exist, 2 will be installed.
            var installed = new[]
            {
                Comp("C1", "v1.0")
            };
            var patches = new ISnPatch[]
            {
                Inst("C3", "v1.0", new[] {Dep("C1", "1.0 <= v"), Dep("C2", "1.0 <= v")}, null),
                Inst("C4", "v1.0", new[] {Dep("C3", "1.0 <= v"), Dep("C2", "1.0 <= v")}, null),
                Inst("C2", "v1.0", new[] {Dep("C1", "1.0 <= v")}, null),
            };

            // ACTION
            var context = new PatchExecutionContext(null, null);
            var pm = new PatchManager(context);
            var executables = pm.GetExecutablePatches(patches, installed, context, out var after).ToArray();

            // ASSERT
            Assert.AreEqual(4, after.Length);
            Assert.AreEqual("C1,C2,C3,C4", string.Join(",", after.Select(x => x.ComponentId)));
            Assert.AreEqual(3, executables.Length);
            Assert.AreEqual("C2,C3,C4", string.Join(",", executables.Select(x => x.ComponentId)));
        }
        [TestMethod]
        public void PatchingExecSim_Install_Dependencies_Circular()
        {
            // Test the right installer execution order if there is a dependency among the installers.
            // One dependency is exist, 2 will be installed.
            var installed = new[]
            {
                Comp("C1", "v1.0")
            };
            var patches = new ISnPatch[]
            {
                Inst("C2", "v1.0", new[] {Dep("C3", "1.0 <= v")}, null),
                Inst("C3", "v1.0", new[] {Dep("C4", "1.0 <= v")}, null),
                Inst("C4", "v1.0", new[] {Dep("C2", "1.0 <= v")}, null),
            };

            // ACTION
            var context = new PatchExecutionContext(null, null);
            var pm = new PatchManager(context);
            var executables = pm.GetExecutablePatches(patches, installed, context, out var after).ToArray();

            // ASSERT
            Assert.AreEqual(1, after.Length);
            Assert.AreEqual("C1", string.Join(",", after.Select(x => x.ComponentId)));
            Assert.AreEqual(0, executables.Length);
            // Circular dependency causes error but the reason is not recognized yet.
            Assert.AreEqual(3, context.Errors.Count);
            Assert.AreEqual(PatchExecutionErrorType.CannotInstall, context.Errors[0].ErrorType);
            Assert.AreEqual(PatchExecutionErrorType.CannotInstall, context.Errors[1].ErrorType);
            Assert.AreEqual(PatchExecutionErrorType.CannotInstall, context.Errors[2].ErrorType);
            Assert.AreEqual("C2", context.Errors[0].FaultyPatch.ComponentId);
            Assert.AreEqual("C3", context.Errors[1].FaultyPatch.ComponentId);
            Assert.AreEqual("C4", context.Errors[2].FaultyPatch.ComponentId);
        }

        /* ======================================================== COMPLEX INSTALL & PATCHING SIMULATION TESTS */

        #region TOOLS for COMPLEX INSTALL & PATCHING TESTS
        private string ComponentsToString(SnComponentDescriptor[] components)
        {
            return string.Join(" ", components.OrderBy(x=>x.ComponentId)
                .Select(x => $"{x.ComponentId}v{x.Version}"));
        }
        private string PatchesToString(ISnPatch[] executables)
        {
            return string.Join(" ", executables.Select(x =>
                $"{x.ComponentId}{(x.Type == PackageType.Install ? "i" : "p")}{x.Version}"));
        }
        private string PackagesToString(Package[] packages)
        {
            return string.Join("|", packages.Select(p => p.ToString()));
        }
        #endregion

        // SIMULATION: Install C1v1.0 and patch it to v3.0 with different pre-installation states 
        [TestMethod]
        public void PatchingExecSim_Patch_C1_a()
        {
            var installed = new SnComponentDescriptor[0];
            var patches = new ISnPatch[]
            {
                Patch("C1", "2.0 <= v <  3.0", "v3.0", null, null),
                Patch("C1", "1.0 <= v <  2.0", "v2.0", null, null),
                Inst("C1", "v1.0", null, null),
            };

            // ACTION
            var context = new PatchExecutionContext(null, null);
            var pm = new PatchManager(context);
            var executables = pm.GetExecutablePatches(patches, installed, context, out var after).ToArray();

            // ASSERT
            Assert.AreEqual(0, context.Errors.Count);
            Assert.AreEqual("C1v3.0", ComponentsToString(after));
            Assert.AreEqual("C1i1.0 C1p2.0 C1p3.0", PatchesToString(executables));
        }
        [TestMethod]
        public void PatchingExecSim_Patch_C1_b()
        {
            var installed = new[]
            {
                Comp("C1", "v1.0")
            };
            var patches = new ISnPatch[]
            {
                Patch("C1", "2.0 <= v <  3.0", "v3.0", null, null),
                Patch("C1", "1.0 <= v <  2.0", "v2.0", null, null),
                Inst("C1", "v1.0", null, null),
            };

            // ACTION
            var context = new PatchExecutionContext(null, null);
            var pm = new PatchManager(context);
            var executables = pm.GetExecutablePatches(patches, installed, context, out var after).ToArray();

            // ASSERT
            Assert.AreEqual(0, context.Errors.Count);
            Assert.AreEqual("C1v3.0", ComponentsToString(after));
            Assert.AreEqual("C1p2.0 C1p3.0", PatchesToString(executables));
        }
        [TestMethod]
        public void PatchingExecSim_Patch_C1b_c()
        {
            var installed = new[]
            {
                Comp("C1", "v2.0")
            };
            var patches = new ISnPatch[]
            {
                Patch("C1", "2.0 <= v <  3.0", "v3.0", null, null),
                Patch("C1", "1.0 <= v <  2.0", "v2.0", null, null),
                Inst("C1", "v1.0", null, null),
            };

            // ACTION
            var context = new PatchExecutionContext(null, null);
            var pm = new PatchManager(context);
            var executables = pm.GetExecutablePatches(patches, installed, context, out var after).ToArray();

            // ASSERT
            Assert.AreEqual(0, context.Errors.Count);
            Assert.AreEqual("C1v3.0", ComponentsToString(after));
            Assert.AreEqual("C1p3.0", PatchesToString(executables));
        }
        [TestMethod]
        public void PatchingExecSim_Patch_C1_d()
        {
            var installed = new[]
            {
                Comp("C1", "v3.0")
            };
            var patches = new ISnPatch[]
            {
                Patch("C1", "2.0 <= v <  3.0", "v3.0", null, null),
                Patch("C1", "1.0 <= v <  2.0", "v2.0", null, null),
                Inst("C1", "v1.0", null, null),
            };

            // ACTION
            var context = new PatchExecutionContext(null, null);
            var pm = new PatchManager(context);
            var executables = pm.GetExecutablePatches(patches, installed, context, out var after).ToArray();

            // ASSERT
            Assert.AreEqual(0, context.Errors.Count);
            Assert.AreEqual("C1v3.0", ComponentsToString(after));
            Assert.AreEqual("", PatchesToString(executables));
        }

        // SIMULATION: Install and patch C2 that depends C1
        [TestMethod]
        public void PatchingExecSim_Patch_C2toC1v2_a()
        {
            var installed = new SnComponentDescriptor[0];
            var patches = new ISnPatch[]
            {
                Inst("C2", "v1.0", new[] {Dep("C1", "2.0 <= v <= 2.0")}, null),
                Inst("C1", "v1.0", null, null),
            };

            // ACTION
            var context = new PatchExecutionContext(null, null);
            var pm = new PatchManager(context);
            var executables = pm.GetExecutablePatches(patches, installed, context, out var after).ToArray();

            // ASSERT
            Assert.AreEqual("CannotInstall C2: 1.0", ErrorsToString(context));
            Assert.AreEqual(patches[0], context.Errors[0].FaultyPatch);
            Assert.AreEqual("C1v1.0", ComponentsToString(after));
            Assert.AreEqual("C1i1.0", PatchesToString(executables));
        }
        [TestMethod]
        public void PatchingExecSim_Patch_C2toC1v2_b()
        {
            var installed = new SnComponentDescriptor[0];
            var patches = new ISnPatch[]
            {
                Patch("C1", "1.0 <= v < 2.0", "v2.0", null, null),
                Inst("C2", "v1.0", new[] {Dep("C1", "2.0 <= v <= 2.0")}, null),
                Inst("C1", "v1.0", null, null),
            };

            // ACTION
            var context = new PatchExecutionContext(null, null);
            var pm = new PatchManager(context);
            var executables = pm.GetExecutablePatches(patches, installed, context, out var after).ToArray();

            // ASSERT
            Assert.AreEqual(0, context.Errors.Count);
            Assert.AreEqual("C1v2.0 C2v1.0", ComponentsToString(after));
            Assert.AreEqual("C1i1.0 C1p2.0 C2i1.0", PatchesToString(executables));
        }

        // SIMULATION: Install and patch C2 that depends C1. Dependency comes with the patch.
        [TestMethod]
        public void PatchingExecSim_Patch_DependencyInPatch()
        {
            var installed = new SnComponentDescriptor[0];
            var patches = new ISnPatch[]
            {
                Patch("C2", "1.0 <= v < 2.0", "v2.0", null, null),
                Patch("C1", "1.0 <= v < 2.0", "v2.0", new[] {Dep("C2", "2.0 <= v <= 2.0")}, null),
                Inst("C2", "v1.0", null, null),
                Inst("C1", "v1.0", null, null),
            };

            // ACTION
            var context = new PatchExecutionContext(null, null);
            var pm = new PatchManager(context);
            var executables = pm.GetExecutablePatches(patches, installed, context, out var after).ToArray();

            // ASSERT
            Assert.AreEqual(0, context.Errors.Count);
            Assert.AreEqual("C1v2.0 C2v2.0", ComponentsToString(after));
            Assert.AreEqual("C1i1.0 C2i1.0 C2p2.0 C1p2.0", PatchesToString(executables));
        }

        // SIMULATION: Install and patch C1 but there is a missing item in the patch chain.
        [TestMethod]
        public void PatchingExecSim_Patch_MissingItemInTheChain_a()
        {
            var installed = new SnComponentDescriptor[0];
            var patches = new ISnPatch[]
            {
                Patch("C1", "3.0 <= v <  4.0", "v4.0", null, null),
                Patch("C1", "1.0 <= v <  2.0", "v2.0", null, null),
                Inst("C1", "v1.0", null, null),
            };

            // ACTION
            var context = new PatchExecutionContext(null, null);
            var pm = new PatchManager(context);
            var executables = pm.GetExecutablePatches(patches, installed, context, out var after).ToArray();

            // ASSERT
            Assert.AreEqual("MissingVersion C1: 3.0 <= v < 4.0 --> 4.0", ErrorsToString(context));
            Assert.AreEqual("C1v2.0", ComponentsToString(after));
            Assert.AreEqual("C1i1.0 C1p2.0", PatchesToString(executables));
        }
        [TestMethod]
        public void PatchingExecSim_Patch_MissingItemInTheChain_b()
        {
            var installed = new[]
            {
                Comp("C1", "v1.0")
            };
            var patches = new ISnPatch[]
            {
                Patch("C1", "3.0 <= v <  4.0", "v4.0", null, null),
                Patch("C1", "1.0 <= v <  2.0", "v2.0", null, null),
                Inst("C1", "v1.0", null, null),
            };

            // ACTION
            var context = new PatchExecutionContext(null, null);
            var pm = new PatchManager(context);
            var executables = pm.GetExecutablePatches(patches, installed, context, out var after).ToArray();

            // ASSERT
            Assert.AreEqual("MissingVersion C1: 3.0 <= v < 4.0 --> 4.0", ErrorsToString(context));
            Assert.AreEqual("C1v2.0", ComponentsToString(after));
            Assert.AreEqual("C1p2.0", PatchesToString(executables));
        }
        [TestMethod]
        public void PatchingExecSim_Patch_MissingItemInTheChain_c()
        {
            var installed = new[]
            {
                Comp("C1", "v2.0")
            };
            var patches = new ISnPatch[]
            {
                Patch("C1", "3.0 <= v <  4.0", "v4.0", null, null),
                Patch("C1", "1.0 <= v <  2.0", "v2.0", null, null),
                Inst("C1", "v1.0", null, null),
            };

            // ACTION
            var context = new PatchExecutionContext(null, null);
            var pm = new PatchManager(context);
            var executables = pm.GetExecutablePatches(patches, installed, context, out var after).ToArray();

            // ASSERT
            Assert.AreEqual("MissingVersion C1: 3.0 <= v < 4.0 --> 4.0", ErrorsToString(context));
            Assert.AreEqual("C1v2.0", ComponentsToString(after));
            Assert.AreEqual("", PatchesToString(executables));
        }
        [TestMethod]
        public void PatchingExecSim_Patch_MissingItemInTheChain_d()
        {
            var installed = new[]
            {
                Comp("C1", "v3.0")
            };
            var patches = new ISnPatch[]
            {
                Patch("C1", "3.0 <= v <  4.0", "v4.0", null, null),
                Patch("C1", "1.0 <= v <  2.0", "v2.0", null, null),
                Inst("C1", "v1.0", null, null),
            };

            // ACTION
            var context = new PatchExecutionContext(null, null);
            var pm = new PatchManager(context);
            var executables = pm.GetExecutablePatches(patches, installed, context, out var after).ToArray();

            // ASSERT
            Assert.AreEqual(0, context.Errors.Count);
            Assert.AreEqual("C1v4.0", ComponentsToString(after));
            Assert.AreEqual("C1p4.0", PatchesToString(executables));
        }

        /* ======================================================== COMPLEX INSTALL & PATCHING EXECUTION TESTS */

        [TestMethod]
        public void PatchingExec_InstallOne_Success()
        {
            var packages = new List<Package[]>();
            var log = new List<PatchExecutionLogRecord>();
            void LogMessage(PatchExecutionLogRecord record)
            {
                packages.Add(LoadPackages());
                log.Add(record);
            }

            var executed = new List<ISnPatch>();
            void Execute(PatchExecutionContext peContext) => executed.Add(peContext.CurrentPatch);

            var installed = new SnComponentDescriptor[0];
            var patches = new ISnPatch[]
            {
                Inst("C1", "v1.0", null, Execute),
            };

            // ACTION
            var context = new PatchExecutionContext(null, LogMessage);
            var pm = new PatchManager(context);
            pm.ExecuteRelevantPatches(patches, installed, context);

            // ASSERT
            Assert.AreEqual(0, context.Errors.Count);
            Assert.AreEqual(2, log.Count);
            Assert.AreEqual("C1i1.0", PatchesToString(executed.ToArray()));
            Assert.AreEqual("[C1: 1.0] ExecutionStart.", log[0].ToString());
            Assert.AreEqual("[C1: 1.0] ExecutionFinished. Successful", log[1].ToString());
            Assert.AreEqual("1, C1: Install Unfinished, 1.0", PackagesToString(packages[0]));
            Assert.AreEqual("1, C1: Install Successful, 1.0", PackagesToString(packages[1]));

        }
        [TestMethod]
        public void PatchingExec_InstallOne_Faulty()
        {
            var packages = new List<Package[]>();
            var log = new List<PatchExecutionLogRecord>();
            void LogMessage(PatchExecutionLogRecord record)
            {
                packages.Add(LoadPackages());
                log.Add(record);
            }

            var installed = new SnComponentDescriptor[0];
            var patches = new ISnPatch[]
            {
                Inst("C1", "v1.0", null, Error),
            };

            // ACTION
            var context = new PatchExecutionContext(null, LogMessage);
            var pm = new PatchManager(context);
            pm.ExecuteRelevantPatches(patches, installed, context);

            // ASSERT
            Assert.AreEqual("ErrorInExecution C1: 1.0", ErrorsToString(context));
            Assert.AreEqual(3, log.Count);
            Assert.AreEqual("[C1: 1.0] ExecutionStart.", log[0].ToString());
            Assert.AreEqual("[C1: 1.0] ExecutionError. Err", log[1].ToString());
            Assert.AreEqual("[C1: 1.0] ExecutionFinished. Faulty", log[2].ToString());
            Assert.AreEqual("1, C1: Install Unfinished, 1.0", PackagesToString(packages[0]));
            Assert.AreEqual("1, C1: Install Unfinished, 1.0", PackagesToString(packages[1]));
            Assert.AreEqual("1, C1: Install Faulty, 1.0", PackagesToString(packages[2]));
        }
        [TestMethod]
        public void PatchingExec_PatchOne_Success()
        {
            var packages = new List<Package[]>();
            var log = new List<PatchExecutionLogRecord>();
            void LogMessage(PatchExecutionLogRecord record)
            {
                packages.Add(LoadPackages());
                log.Add(record);
            }

            var executed = new List<ISnPatch>();
            void Execute(PatchExecutionContext peContext) => executed.Add(peContext.CurrentPatch);

            var installed = new SnComponentDescriptor[0];
            var patches = new ISnPatch[]
            {
                Patch("C1", "1.0 <= v < 2.0", "v2.0", Execute),
                Inst("C1", "v1.0", null, Execute),
            };

            // ACTION
            var context = new PatchExecutionContext(null, LogMessage);
            var pm = new PatchManager(context);
            pm.ExecuteRelevantPatches(patches, installed, context);

            // ASSERT
            Assert.AreEqual(0, context.Errors.Count);
            Assert.AreEqual(4, log.Count);
            Assert.AreEqual("C1i1.0 C1p2.0", PatchesToString(executed.ToArray()));
            Assert.AreEqual("[C1: 1.0] ExecutionStart.", log[0].ToString());
            Assert.AreEqual("[C1: 1.0] ExecutionFinished. Successful", log[1].ToString());
            Assert.AreEqual("[C1: 1.0 <= v < 2.0 --> 2.0] ExecutionStart.", log[2].ToString());
            Assert.AreEqual("[C1: 1.0 <= v < 2.0 --> 2.0] ExecutionFinished. Successful", log[3].ToString());
            Assert.AreEqual(4, packages.Count);
            Assert.AreEqual("1, C1: Install Unfinished, 1.0", PackagesToString(packages[0]));
            Assert.AreEqual("1, C1: Install Successful, 1.0", PackagesToString(packages[1]));
            Assert.AreEqual("1, C1: Install Successful, 1.0|2, C1: Patch Unfinished, 2.0", PackagesToString(packages[2]));
            Assert.AreEqual("1, C1: Install Successful, 1.0|2, C1: Patch Successful, 2.0", PackagesToString(packages[3]));
        }
        [TestMethod]
        public void PatchingExec_PatchOne_Faulty()
        {
            var packages = new List<Package[]>();
            var log = new List<PatchExecutionLogRecord>();
            void LogMessage(PatchExecutionLogRecord record)
            {
                packages.Add(LoadPackages());
                log.Add(record);
            }

            var executed = new List<ISnPatch>();
            void Execute(PatchExecutionContext peContext) => executed.Add(peContext.CurrentPatch);

            var installed = new SnComponentDescriptor[0];
            var patches = new ISnPatch[]
            {
                Patch("C1", "1.0 <= v < 2.0", "v2.0", null, Error),
                Inst("C1", "v1.0", null, Execute),
            };

            // ACTION
            var context = new PatchExecutionContext(null, LogMessage);
            var pm = new PatchManager(context);
            pm.ExecuteRelevantPatches(patches, installed, context);

            // ASSERT
            Assert.AreEqual("ErrorInExecution C1: 1.0 <= v < 2.0 --> 2.0", ErrorsToString(context));
            Assert.AreEqual(5, log.Count);
            Assert.AreEqual("C1i1.0", PatchesToString(executed.ToArray()));
            Assert.AreEqual("[C1: 1.0] ExecutionStart.", log[0].ToString());
            Assert.AreEqual("[C1: 1.0] ExecutionFinished. Successful", log[1].ToString());
            Assert.AreEqual("[C1: 1.0 <= v < 2.0 --> 2.0] ExecutionStart.", log[2].ToString());
            Assert.AreEqual("[C1: 1.0 <= v < 2.0 --> 2.0] ExecutionError. Err", log[3].ToString());
            Assert.AreEqual("[C1: 1.0 <= v < 2.0 --> 2.0] ExecutionFinished. Faulty", log[4].ToString());
            Assert.AreEqual(5, packages.Count);
            Assert.AreEqual("1, C1: Install Unfinished, 1.0", PackagesToString(packages[0]));
            Assert.AreEqual("1, C1: Install Successful, 1.0", PackagesToString(packages[1]));
            Assert.AreEqual("1, C1: Install Successful, 1.0|2, C1: Patch Unfinished, 2.0", PackagesToString(packages[2]));
            Assert.AreEqual("1, C1: Install Successful, 1.0|2, C1: Patch Unfinished, 2.0", PackagesToString(packages[3]));
            Assert.AreEqual("1, C1: Install Successful, 1.0|2, C1: Patch Faulty, 2.0", PackagesToString(packages[4]));
        }
        [TestMethod]
        public void PatchingExec_SkipPatch_FaultyInstaller()
        {
            // Faulty execution blocks the following patches on the same component.
            var packages = new List<Package[]>();
            var log = new List<PatchExecutionLogRecord>();
            void LogMessage(PatchExecutionLogRecord record)
            {
                packages.Add(LoadPackages());
                log.Add(record);
            }

            var executed = new List<ISnPatch>();
            void Execute(PatchExecutionContext peContext) => executed.Add(peContext.CurrentPatch);

            var patches = new ISnPatch[]
            {
                Patch("C1", "1.0 <= v < 2.0", "v2.0", Execute),
                Inst("C1", "v1.0", null, Error),
            };

            // ACTION
            var context = new PatchExecutionContext(null, LogMessage);
            var pm = new PatchManager(context);
            pm.ExecuteRelevantPatches(patches, context);

            // ASSERT
            Assert.AreEqual("ErrorInExecution C1: 1.0, MissingVersion C1: 1.0 <= v < 2.0 --> 2.0",
                ErrorsToString(context));
            Assert.AreEqual(4, log.Count);
            Assert.AreEqual("", PatchesToString(executed.ToArray()));
            Assert.AreEqual("[C1: 1.0] ExecutionStart.", log[0].ToString());
            Assert.AreEqual("[C1: 1.0] ExecutionError. Err", log[1].ToString());
            Assert.AreEqual("[C1: 1.0] ExecutionFinished. Faulty", log[2].ToString());
            Assert.AreEqual("[C1: 1.0 <= v < 2.0 --> 2.0] CannotExecuteMissingVersion.", log[3].ToString());
            Assert.AreEqual(4, packages.Count);
            Assert.AreEqual("1, C1: Install Unfinished, 1.0", PackagesToString(packages[0]));
            Assert.AreEqual("1, C1: Install Unfinished, 1.0", PackagesToString(packages[1]));
            Assert.AreEqual("1, C1: Install Faulty, 1.0", PackagesToString(packages[2]));
            Assert.AreEqual("1, C1: Install Faulty, 1.0", PackagesToString(packages[3]));
        }
        [TestMethod]
        public void PatchingExec_SkipPatch_FaultySnPatch()
        {
            // Faulty execution blocks the following patches on the same component.
            var packages = new List<Package[]>();
            var log = new List<PatchExecutionLogRecord>();
            void LogMessage(PatchExecutionLogRecord record)
            {
                packages.Add(LoadPackages());
                log.Add(record);
            }

            var executed = new List<ISnPatch>();
            void Execute(PatchExecutionContext peContext) => executed.Add(peContext.CurrentPatch);

            var patches = new ISnPatch[]
            {
                Patch("C1", "2.0 <= v < 3.0", "v3.0", Execute),
                Patch("C1", "1.0 <= v < 2.0", "v2.0", Error),
                Inst("C1", "v1.0", null, Execute),
            };

            // ACTION
            var context = new PatchExecutionContext(null, LogMessage);
            var pm = new PatchManager(context);
            pm.ExecuteRelevantPatches(patches, context);

            // ASSERT
            Assert.AreEqual("ErrorInExecution C1: 1.0 <= v < 2.0 --> 2.0, " +
                            "MissingVersion C1: 2.0 <= v < 3.0 --> 3.0",
                string.Join(", ", context.Errors.Select(x=>x.ToString())));
            Assert.AreEqual("C1i1.0", PatchesToString(executed.ToArray()));
            Assert.AreEqual(6, log.Count);
            Assert.AreEqual("[C1: 1.0] ExecutionStart.", log[0].ToString());
            Assert.AreEqual("[C1: 1.0] ExecutionFinished. Successful", log[1].ToString());
            Assert.AreEqual("[C1: 1.0 <= v < 2.0 --> 2.0] ExecutionStart.", log[2].ToString());
            Assert.AreEqual("[C1: 1.0 <= v < 2.0 --> 2.0] ExecutionError. Err", log[3].ToString());
            Assert.AreEqual("[C1: 1.0 <= v < 2.0 --> 2.0] ExecutionFinished. Faulty", log[4].ToString());
            Assert.AreEqual("[C1: 2.0 <= v < 3.0 --> 3.0] CannotExecuteMissingVersion.", log[5].ToString());
            Assert.AreEqual(6, packages.Count);
            Assert.AreEqual("1, C1: Install Unfinished, 1.0", PackagesToString(packages[0]));
            Assert.AreEqual("1, C1: Install Successful, 1.0", PackagesToString(packages[1]));
            Assert.AreEqual("1, C1: Install Successful, 1.0|2, C1: Patch Unfinished, 2.0", PackagesToString(packages[2]));
            Assert.AreEqual("1, C1: Install Successful, 1.0|2, C1: Patch Unfinished, 2.0", PackagesToString(packages[3]));
            Assert.AreEqual("1, C1: Install Successful, 1.0|2, C1: Patch Faulty, 2.0", PackagesToString(packages[4]));
            Assert.AreEqual("1, C1: Install Successful, 1.0|2, C1: Patch Faulty, 2.0", PackagesToString(packages[5]));
        }

        [TestMethod]
        public void PatchingExec_SkipPatch_MoreFaultyChains()
        {
            // Faulty execution blocks the following patches on the same component.
            var packages = new List<Package[]>();
            var log = new List<PatchExecutionLogRecord>();
            void LogMessage(PatchExecutionLogRecord record)
            {
                packages.Add(LoadPackages());
                log.Add(record);
            }

            var executed = new List<ISnPatch>();
            void Execute(PatchExecutionContext peContext) => executed.Add(peContext.CurrentPatch);

            var patches = new ISnPatch[]
            {
                // Problem in the installer
                Patch("C1", "2.0 <= v < 3.0", "v3.0", Execute),
                Patch("C1", "1.0 <= v < 2.0", "v2.0", Execute),
                Inst("C1", "v1.0", null, Error),
                // Problem in a middle patch
                Patch("C2", "2.0 <= v < 3.0", "v3.0", Execute),
                Patch("C2", "1.0 <= v < 2.0", "v2.0", Error),
                Inst("C2", "v1.0", null, Execute),
                // There is no problem
                Patch("C3", "2.0 <= v < 3.0", "v3.0", Execute),
                Patch("C3", "1.0 <= v < 2.0", "v2.0", Execute),
                Inst("C3", "v1.0", null, Execute),
            };

            // ACTION
            var context = new PatchExecutionContext(null, LogMessage);
            var pm = new PatchManager(context);
            pm.ExecuteRelevantPatches(patches, context);

            // ASSERT
            Assert.AreEqual(5, context.Errors.Count);
            Assert.AreEqual("ErrorInExecution C1: 1.0, " +
                            "MissingVersion C1: 1.0 <= v < 2.0 --> 2.0, " +
                            "MissingVersion C1: 2.0 <= v < 3.0 --> 3.0, " +
                            "ErrorInExecution C2: 1.0 <= v < 2.0 --> 2.0, " +
                            "MissingVersion C2: 2.0 <= v < 3.0 --> 3.0",
                ErrorsToString(context));

            Assert.AreEqual("C2i1.0 C3i1.0 C3p2.0 C3p3.0", PatchesToString(executed.ToArray()));
            Assert.AreEqual(17, log.Count);
            Assert.AreEqual("[C1: 1.0] ExecutionStart.", log[0].ToString());
            Assert.AreEqual("[C1: 1.0] ExecutionError. Err", log[1].ToString());
            Assert.AreEqual("[C1: 1.0] ExecutionFinished. Faulty", log[2].ToString());
            Assert.AreEqual("[C1: 1.0 <= v < 2.0 --> 2.0] CannotExecuteMissingVersion.", log[3].ToString());
            Assert.AreEqual("[C1: 2.0 <= v < 3.0 --> 3.0] CannotExecuteMissingVersion.", log[4].ToString());
            Assert.AreEqual("[C2: 1.0] ExecutionStart.", log[5].ToString());
            Assert.AreEqual("[C2: 1.0] ExecutionFinished. Successful", log[6].ToString());
            Assert.AreEqual("[C3: 1.0] ExecutionStart.", log[7].ToString());
            Assert.AreEqual("[C3: 1.0] ExecutionFinished. Successful", log[8].ToString());
            Assert.AreEqual("[C2: 1.0 <= v < 2.0 --> 2.0] ExecutionStart.", log[9].ToString());
            Assert.AreEqual("[C2: 1.0 <= v < 2.0 --> 2.0] ExecutionError. Err", log[10].ToString());
            Assert.AreEqual("[C2: 1.0 <= v < 2.0 --> 2.0] ExecutionFinished. Faulty", log[11].ToString());
            Assert.AreEqual("[C2: 2.0 <= v < 3.0 --> 3.0] CannotExecuteMissingVersion.", log[12].ToString());
            Assert.AreEqual("[C3: 1.0 <= v < 2.0 --> 2.0] ExecutionStart.", log[13].ToString());
            Assert.AreEqual("[C3: 1.0 <= v < 2.0 --> 2.0] ExecutionFinished. Successful", log[14].ToString());
            Assert.AreEqual("[C3: 2.0 <= v < 3.0 --> 3.0] ExecutionStart.", log[15].ToString());
            Assert.AreEqual("[C3: 2.0 <= v < 3.0 --> 3.0] ExecutionFinished. Successful", log[16].ToString());
            Assert.AreEqual(17, packages.Count);
            Assert.AreEqual("1, C1: Install Faulty, 1.0|" +
                            "2, C2: Install Successful, 1.0|" +
                            "3, C3: Install Successful, 1.0|" +
                            "4, C2: Patch Faulty, 2.0|" +
                            "5, C3: Patch Successful, 2.0|" +
                            "6, C3: Patch Unfinished, 3.0", PackagesToString(packages[15]));
            Assert.AreEqual("1, C1: Install Faulty, 1.0|" +
                            "2, C2: Install Successful, 1.0|" +
                            "3, C3: Install Successful, 1.0|" +
                            "4, C2: Patch Faulty, 2.0|" +
                            "5, C3: Patch Successful, 2.0|" +
                            "6, C3: Patch Successful, 3.0", PackagesToString(packages[16]));
        }

        /* ======================================================== Tools */

        private void Error(PatchExecutionContext context) => throw new Exception("Err");

        private string ErrorsToString(PatchExecutionContext context)
        {
            return string.Join(", ", context.Errors.Select(x => x.ToString()));
        }
    }
}
