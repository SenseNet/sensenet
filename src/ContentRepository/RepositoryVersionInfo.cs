﻿using System;
using System.Collections.Generic;
using IO = System.IO;
using System.Linq;
using SenseNet.ContentRepository.Storage;
using System.Reflection;
using SenseNet.Communication.Messaging;
using SenseNet.Diagnostics;
using SenseNet.Packaging;
using SenseNet.Tools;

namespace SenseNet.ContentRepository
{
    public class AssemblyDetails
    {
        public AssemblyInfo[] SenseNet { get; set; }
        public AssemblyInfo[] Plugins { get; set; }
        public AssemblyInfo[] GAC { get; set; }
        public AssemblyInfo[] Other { get; set; }
        public AssemblyInfo[] Dynamic { get; set; }
    }
    public class RepositoryVersionInfo
    {
        public IEnumerable<ComponentInfo> Components { get; private set; }
        public AssemblyDetails Assemblies { get; private set; }
        public IEnumerable<Package> InstalledPackages{ get; private set;}
        public bool DatabaseAvailable { get; private set; }

        // ============================================================== Static part

        private static readonly RepositoryVersionInfo BeforeInstall = new RepositoryVersionInfo
        {
            Components = new ComponentInfo[0],
            InstalledPackages = new Package[0],
            Assemblies = new AssemblyDetails
            {
                Dynamic = new AssemblyInfo[0],
                GAC = new AssemblyInfo[0],
                Other = new AssemblyInfo[0],
                SenseNet = new AssemblyInfo[0],
                Plugins = new AssemblyInfo[0]
            },
            DatabaseAvailable = false
        };

        private static RepositoryVersionInfo __instance;
        private static object _instanceLock = new object();
        public static RepositoryVersionInfo Instance
        {
            get
            {
                if (__instance == null)
                    lock (_instanceLock)
                        if (__instance == null)
                            __instance = Create();
                return __instance;
            }
        }

        private static RepositoryVersionInfo Create()
        {
            var storage = PackageManager.Storage;
            try
            {
                return Create(
                    storage.LoadInstalledComponents(),
                    storage.LoadInstalledPackages());
            }
            catch
            {
                return RepositoryVersionInfo.BeforeInstall;
            }
        }

        private static RepositoryVersionInfo Create(IEnumerable<ComponentInfo> componentVersions, IEnumerable<Package> packages, bool databaseAvailable = true)
        {
            var asms = TypeHandler.GetAssemblyInfo();

            var sncr = Assembly.GetExecutingAssembly();
            var binPath = IO.Path.GetDirectoryName(TypeHandler.GetCodeBase(sncr));

            var asmDyn = asms.Where(a => a.IsDynamic).ToArray();
            asms = asms.Except(asmDyn).ToArray();
            var asmInBin = asms.Where(a => a.CodeBase.StartsWith(binPath)).ToArray();
            asms = asms.Except(asmInBin).ToArray();
            var asmInGac = asms.Where(a => a.CodeBase.Contains("\\GAC")).ToArray();
            asms = asms.Except(asmInGac).ToArray();

            var asmSn = asmInBin.Where(a => a.Name.StartsWith("SenseNet.")).ToArray();
            var plugins = asmInBin.Except(asmSn).ToArray();

            return new RepositoryVersionInfo
            {
                Components = componentVersions,
                Assemblies = new AssemblyDetails
                {
                    SenseNet = asmSn,
                    Plugins = plugins,
                    GAC = asmInGac,
                    Other = asms,
                    Dynamic = asmDyn,
                },
                InstalledPackages = packages,
                DatabaseAvailable = databaseAvailable
            };
        }

        public static void Reset()
        {
            new RepositoryVersionInfoResetDistributedAction().Execute();
        }
        private static void ResetPrivate()
        {
            __instance = null;
        }

        [Serializable]
        internal sealed class RepositoryVersionInfoResetDistributedAction : DistributedAction
        {
            public override void DoAction(bool onRemote, bool isFromMe)
            {
                if (onRemote && isFromMe)
                    return;
                RepositoryVersionInfo.ResetPrivate();
            }
        }

        public static void CheckComponentVersions()
        {
            var components = TypeResolver.GetTypesByInterface(typeof(ISnComponent)).Where(vct => !vct.IsAbstract)
                .Select(t => TypeResolver.CreateInstance(t.FullName) as ISnComponent)
                .Where(c => c != null)
                .Select(SnComponentInfo.Create)
                .ToArray();
            CheckComponentVersions(components);
        }
        private static void CheckComponentVersions(SnComponentInfo[] components)
        {
#if DEBUG
            CheckComponentVersions(components, false);
#else
            CheckComponentVersions(components, true);
#endif
        }
        internal static void CheckComponentVersions(SnComponentInfo[] components, bool release)
        {
            foreach (var component in components)
            {
                if (string.IsNullOrEmpty(component.ComponentId))
                {
                    SnLog.WriteWarning($"Component class {component.GetType().FullName} is invalid, it does not provide a ComponentId.");
                    continue;
                }

                var componentVersion = Instance.Components.FirstOrDefault(c => c.ComponentId == component.ComponentId)?.Version;

                if (IsComponentAllowed(component, componentVersion))
                {
                    SnTrace.System.Write($"Component {component.ComponentId} is allowed to run (version: {componentVersion})");
                    continue;
                }

                if (release)
                    throw new ApplicationException($"Component and assembly version mismatch. Component {component.ComponentId} is not allowed to run."
                        + $" Installed version: {componentVersion}, expected minimal version: {component.SupportedVersion}."
                        + " Please check assembly versions and available ugrades before starting the repository.");

                SnTrace.System.Write($"Component {component.ComponentId} is allowed to run only in the DEBUG mode."
                        + $" Installed version: {componentVersion}, expected minimal version: {component.SupportedVersion}.");
            }
        }
        internal static bool IsComponentAllowed(SnComponentInfo component, Version installedComponentVersion)
        {
            if (installedComponentVersion == null)
                throw new InvalidOperationException($"{component.ComponentId} component is missing.");

            var assemblyVersion = component.AssemblyVersion ?? TypeHandler.GetVersion(component.GetType().Assembly);

            // To be able to publish code hotfixes, we allow the revision number (the 4th element) 
            // to be higher in the assembly (in case every other part equals). This assumes 
            // that every repository change raises at least the build number (the 3rd one) 
            // in the component's version.
            if (installedComponentVersion.Major != assemblyVersion.Major ||
                installedComponentVersion.Minor != assemblyVersion.Minor ||
                installedComponentVersion.Build != assemblyVersion.Build)
            {
                // Not allowed if the assembly is older than the matched component.
                if (assemblyVersion < installedComponentVersion)
                    return false;

                // Not allowed if the component is older than the assembly can support.
                if (installedComponentVersion < component.SupportedVersion)
                    return false;
            }

            // Call the customized function if there is.
            if (component.IsComponentAllowed != null)
                return component.IsComponentAllowed.Invoke(installedComponentVersion);

            // Everything is fine, assembly is runnable.
            return true;
        }

        internal static SnComponentInfo[] GetAssemblyComponents()
        {
            return TypeResolver.GetTypesByInterface(typeof(ISnComponent)).Where(vct => !vct.IsAbstract)
                .Select(t => TypeResolver.CreateInstance(t.FullName) as ISnComponent)
                .Where(c => c != null)
                .OrderBy(c => c.ComponentId, new SnComponentComparer())
                .Select(SnComponentInfo.Create)
                .ToArray();
        }

        /// <summary>
        /// Compares and sorts components loaded from the assemblies based on the order
        /// found in the database. This is necessary to execute patches in the same
        /// dependency order as they were installed.
        /// </summary>
        internal class SnComponentComparer : IComparer<string>
        {
            private readonly string[] _installedComponents;

            internal SnComponentComparer(string[] installedComponents = null)
            {
                _installedComponents = installedComponents ?? 
                                       Instance.Components.Select(ci => ci.ComponentId).ToArray();
            }

            public int Compare(string componentId1, string componentId2)
            {
                var idx1 = Array.FindIndex(_installedComponents, ic => string.Equals(ic, componentId1));
                var idx2 = Array.FindIndex(_installedComponents, ic => string.Equals(ic, componentId2));

                if (idx1 < 0 && idx2 < 0)
                    return string.Compare(componentId1, componentId2, StringComparison.InvariantCultureIgnoreCase);
                if (idx1 < 0)
                    return 1;
                if (idx2 < 0)
                    return -1;

                return idx1 < idx2 ? -1 : (idx1 == idx2 ? 0 : 1);
            }
        }
    }
}
