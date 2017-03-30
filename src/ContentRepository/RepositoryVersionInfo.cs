using System;
using System.Collections.Generic;
using IO = System.IO;
using System.Linq;
using SenseNet.ContentRepository.Storage;
using System.Reflection;
using SenseNet.Communication.Messaging;
using SenseNet.Packaging;

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

        public IEnumerable<ComponentInfo> Applications { get; private set; } //UNDONE: Rename to Components
        public AssemblyDetails Assemblies { get; private set; }
        public IEnumerable<Package> InstalledPackages{ get; private set;}
        public bool DatabaseAvailable { get; private set; }

        // ============================================================== Static part

        private static readonly RepositoryVersionInfo BeforeInstall = new RepositoryVersionInfo
        {
            Applications = new ComponentInfo[0],
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
                    storage.LoadInstalledApplications(),
                    storage.LoadInstalledPackages());
            }
            catch
            {
                return RepositoryVersionInfo.BeforeInstall;
            }
        }

        private static RepositoryVersionInfo Create(IEnumerable<ComponentInfo> applicationVersions, IEnumerable<Package> packages, bool databaseAvailable = true)
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
                Applications = applicationVersions,
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
    }
}
