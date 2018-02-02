using System;
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
            //TODO: have a pinned list of components in the Providers class
            // so that the instances can be replaced by tests.
            foreach (var componentType in TypeResolver.GetTypesByInterface(typeof(ISnComponent)).Where(vct => !vct.IsAbstract))
            {
                var component = TypeResolver.CreateInstance(componentType.FullName) as ISnComponent;
                if (component == null)
                    continue;

                if (string.IsNullOrEmpty(component.ComponentId))
                {
                    SnLog.WriteWarning($"Component class {component.GetType().FullName} is invalid, it does not provide a ComponentId.");
                    continue;
                }

                var componentVersion = Instance.Components.FirstOrDefault(c => c.ComponentId == component.ComponentId)?.Version;

                if (component.IsComponentAllowed(componentVersion))
                {
                    SnTrace.System.Write($"Component {component.ComponentId} is allowed to run (version: {componentVersion})");
                    continue;
                }

                throw new ApplicationException($"Component and assembly version mismatch. Component {component.ComponentId} (version: {componentVersion}) is not allowed to run. Please check assembly versions and available ugrades before starting the repository.");
            }
        }
    }   
}
