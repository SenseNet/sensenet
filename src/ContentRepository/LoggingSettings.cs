using System;
using System.Linq;
using SenseNet.Communication.Messaging;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.Diagnostics;

namespace SenseNet.ContentRepository
{
    [ContentHandler]
    public class LoggingSettings : Settings
    {
        // ================================================================================= Constructors

        public LoggingSettings(Node parent) : this(parent, null) { }
        public LoggingSettings(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected LoggingSettings(NodeToken nt) : base(nt) { }

        // ================================================================================= Overrides

        private const string TRACESETTINGS_UPDATED_KEY = "TraceUpdated";

        public override void Save(NodeSaveSettings settings)
        {
            base.Save(settings);

            UpdateCategories();
        }

        protected override void OnDeletedPhysically(object sender, NodeEventArgs e)
        {
            base.OnDeletedPhysically(sender, e);

            UpdateCategories();
        }

        private void UpdateCategories()
        {
            // We store a flag in the shared node data to avoid updating tracing settings too frequently.
            // This flag will disappear when this settings content gets invalidated in the cache.
            if (!Convert.ToBoolean(GetCachedData(TRACESETTINGS_UPDATED_KEY)))
            {
                // set this flag to update values only once
                SetCachedData(TRACESETTINGS_UPDATED_KEY, true);

                // update flags on all connected servers
                new UpdateCategoriesDistributedAction().Execute();
            }
        }

        [Serializable]
        public class UpdateCategoriesDistributedAction : DistributedAction
        {
            public override void DoAction(bool onRemote, bool isFromMe)
            {
                if (onRemote && isFromMe)
                    return;

                SnTraceConfigurator.UpdateCategories();
            }
        }

        public static class SnTraceConfigurator
        {
            private const string SETTINGS_NAME = "Logging";
            private const string SETTINGS_PREFIX = "Trace.";
            public static void UpdateCategories()
            {
                foreach (var category in SnTrace.Categories)
                    category.Enabled = Settings.GetValue(SETTINGS_NAME, SETTINGS_PREFIX + category.Name, null, false);

                SnLog.WriteInformation("Trace settings were updated.", EventId.RepositoryRuntime,
                    properties: SnTrace.Categories.ToDictionary(c => c.Name, c => (object)c.Enabled.ToString()));
            }

            public static void UpdateStartupCategories()
            {
                foreach (var category in SnTrace.Categories)
                    category.Enabled = Tracing.StartupTraceCategories.Contains(category.Name);

                SnLog.WriteInformation("Trace settings were updated (for STARTUP).", EventId.RepositoryRuntime,
                    properties: SnTrace.Categories.ToDictionary(c => c.Name, c => (object)c.Enabled.ToString()));
            }

            /// <summary>
            /// Enables trace categories. It only switches the provided categories ON,
            /// it does not switch off the ones that are not listed.
            /// </summary>
            internal static void UpdateCategories(string[] categoryNames)
            {
                if (categoryNames == null)
                {
                    SnTrace.DisableAll();
                    return;
                }

                // do not switch off any category, only switch ON the listed ones
                foreach (var category in SnTrace.Categories.Where(c => categoryNames.Contains(c.Name)))
                {
                    category.Enabled = true;
                }

                SnTrace.System.Write("Trace settings were updated. Enabled: {0}", string.Join(", ", SnTrace.Categories
                    .Where(c => c.Enabled).Select(c => c.Name)));
            }
        }
    }
}
