using SenseNet.ContentRepository.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.Diagnostics;

namespace SenseNet.ContentRepository
{
    public sealed class SettingsCache : TreeCache<Settings>
    {
        private static readonly object Sync = new object();
        private static SettingsCache __instance;
        private static bool _noInstanceLogged;

        public static SettingsCache Instance
        {
            get
            {
                if (__instance == null)
                    lock (Sync)
                    {
                        __instance = (SettingsCache) GetInstance(typeof(SettingsCache));
                        if (__instance == null && !_noInstanceLogged)
                        {
                            SnLog.WriteWarning("Settings cache could not be loaded.");
                            _noInstanceLogged = true;
                        }
                    }

                return __instance;
            }
        }

        protected override void InstanceChanged()
        {
            lock (Sync)
                __instance = null;
        }

        internal static readonly string SETTINGSCONTAINERNAME = Repository.SettingsFolderName.ToLowerInvariant(); // "Settings";
        internal static readonly string EXTENSION = "settings";

        public static T GetSettingsByName<T>(string settingsName, string contextPath) where T : Settings
        {
            if (Instance == null)
            {
                SnTrace.System.Write($"Settings {settingsName} could not be loaded because the settings cache is empty.");
                return null;
            }

            if (contextPath != null && !contextPath.Equals(Repository.RootPath, StringComparison.InvariantCultureIgnoreCase))
            {
                var name = settingsName.ToLowerInvariant();
                var sNode = Instance.FindNearestItem(contextPath,
                    p => RepositoryPath.Combine(p, SETTINGSCONTAINERNAME, name + "." + EXTENSION) // path transformer func
                    );
                if (sNode != null)
                    return Node.Load<T>(sNode.Id);
            }
            return null;
        }

        internal Settings[] GetSettings()
        {
            return Items.Keys.Select(Node.Load<Settings>).ToArray();
        }
        protected override void Invalidate()
        {
            base.Invalidate();

            Providers.Instance.OnSettingsReloaded();
        }

        protected override void OnNodeModified(object sender, NodeEventArgs e)
        {
            //UNDONE: make sure settings-config invalidation happens
            // only once but it happens on all web nodes.
            base.OnNodeModified(sender, e);

            if (e.SourceNode is not Settings)
                return;

            // invalidate only if the base method did not do it to avoid double reload
            if (e.OriginalSourcePath == e.SourceNode.Path || !IsSubtreeContaining(e.OriginalSourcePath))
            {
                Providers.Instance.OnSettingsReloaded();
            }
        }

        protected override List<TNode> LoadItems()
        {
            var settings = Tools.LoadItemsByContentType("Settings");

            SnTrace.System.Write("Settings tree cache reloaded: {0} items.", settings.Count);

            return settings;
        }
    }
}
