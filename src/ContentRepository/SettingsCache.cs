using SenseNet.ContentRepository.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.Diagnostics;

namespace SenseNet.ContentRepository
{
    internal sealed class SettingsCache : TreeCache<Settings>
    {
        private static object _sync = new object();
        private static SettingsCache __instance;
        public static SettingsCache Instance
        {
            get
            {
                if (__instance == null)
                    lock (_sync)
                        __instance = (SettingsCache)GetInstance(typeof(SettingsCache));
                return __instance;
            }
        }

        protected override void InstanceChanged()
        {
            lock (_sync)
                __instance = null;
        }

        internal static readonly string SETTINGSCONTAINERNAME = Repository.SettingsFolderName.ToLowerInvariant(); // "Settings";
        internal static readonly string EXTENSION = "settings";

        public static T GetSettingsByName<T>(string settingsName, string contextPath) where T : Settings
        {
            if (contextPath != null && !contextPath.Equals(Repository.RootPath, StringComparison.InvariantCultureIgnoreCase))
            {
                var name = settingsName.ToLowerInvariant();
                var tnode = Instance.FindNearestItem(contextPath,
                    p => RepositoryPath.Combine(p, SETTINGSCONTAINERNAME, name + "." + EXTENSION) // path transformer func
                    );
                if (tnode != null)
                    return Node.Load<T>(tnode.Id);
            }
            return null;
        }

        protected override List<TNode> LoadItems()
        {
            var settings = Tools.LoadItemsByContentType("Settings");

            SnTrace.System.Write("Settings tree cache reloaded: {0} items.", settings.Count);

            return settings;
        }
    }
}
