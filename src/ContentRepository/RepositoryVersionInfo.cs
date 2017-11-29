using System;
using System.Collections.Generic;
using IO = System.IO;
using System.Linq;
using SenseNet.ContentRepository.Storage;
using System.Reflection;
using SenseNet.Communication.Messaging;
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

        //UNDONE: call this when the repository starts
        // (or during site start?)
        public static void CheckAssemblyVersions()
        {
            foreach (var versionCheckerType in TypeResolver.GetTypesByInterface(typeof(IVersionChecker)))
            {
                var versionChecker = TypeResolver.CreateInstance(versionCheckerType.FullName) as IVersionChecker;
                if (versionChecker?.IsComponentAllowed() ?? true)
                    continue;

                throw new ApplicationException($"Component and assembly version mismatch. Component {versionChecker.ComponentId} is not allowed to run. Please check the available ugrades before starting the repository.");
            }
        }
    }

    //UNDONE: move this to a separate file
    public interface IVersionChecker
    {
        string ComponentId { get; }
        bool IsComponentAllowed();
    }
    
    //UNDONE: move this to the Services library
    public class ServicesVersionChecker : IVersionChecker
    {
        public string ComponentId => "SenseNet.Services";
        public bool IsComponentAllowed()
        {
            //var repoAssembly = RepositoryVersionInfo.Instance.Assemblies.SenseNet.FirstOrDefault(a =>
            //    a.Name.StartsWith("SenseNet.ContentRepository, "));
            //if (repoAssembly == null)
            //    throw new InvalidOperationException("ContentRepository assembly not found.");
            //var version = repoAssembly.Version.Substring()
            
            var assemblyVersion = TypeHandler.GetVersion(Assembly.GetExecutingAssembly());
            var componentVersion = RepositoryVersionInfo.Instance.Components
                .FirstOrDefault(c => c.ComponentId == ComponentId)?.Version;

            //TODO: compare versions. Important: library version may be different from the component version (?).
            // Need to come up with a correct decision-making algorithm here, this may be too restrictive.

            // For example: this is in the source code. The assembly knows perfectly well the component
            // version that it is compatible with. Maybe we should say here 'allowed if component version if bigger than xxxxxxx'.
            // Otherwise we would have to keep dll and snadmin manifest version in sync (which may be a good thing, dunno).

            return componentVersion == assemblyVersion;
        }
    }
}
