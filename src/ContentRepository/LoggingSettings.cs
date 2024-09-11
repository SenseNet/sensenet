using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using STT =System.Threading.Tasks;
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

        public override async STT.Task SaveAsync(NodeSaveSettings settings, CancellationToken cancel)
        {
            await base.SaveAsync(settings, cancel).ConfigureAwait(false);
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
                // (no need to wait for this method)
                _ = new UpdateCategoriesDistributedAction().ExecuteAsync(CancellationToken.None);
            }
        }

        [Serializable]
        public class UpdateCategoriesDistributedAction : DistributedAction
        {
            public override string TraceMessage => null;

            public override STT.Task DoActionAsync(bool onRemote, bool isFromMe, CancellationToken cancellationToken)
            {
                if (onRemote && isFromMe)
                    return STT.Task.CompletedTask;

                SnTraceConfigurator.UpdateCategoriesBySettings();

                return STT.Task.CompletedTask;
            }
        }

        public static class SnTraceConfigurator
        {
            private const string SETTINGS_NAME = "Logging";
            private const string SETTINGS_PREFIX = "Trace.";
            private static Dictionary<string, bool> _basicCategories;

            public static void UpdateCategoriesBySettings()
            {
                if (_basicCategories == null || _basicCategories.Count == 0)
                    _basicCategories = SnTrace.Categories.ToDictionary(c => c.Name, c => false);

                foreach (var category in SnTrace.Categories)
                {
                    var value = Settings.GetValue<bool?>(SETTINGS_NAME, SETTINGS_PREFIX + category.Name, null);
                    category.Enabled = value ?? _basicCategories[category.Name];
                }

                WriteInformation("settings");
            }

            public static void ConfigureCategories()
            {
                foreach (var category in SnTrace.Categories)
                    category.Enabled = Tracing.StartupTraceCategories.Contains(category.Name);

                WriteInformation("configuration");

                UpdateBasicCategories();
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
                }
                else
                {
                    // do not switch off any category, only switch ON the listed ones
                    foreach (var category in SnTrace.Categories.Where(c => categoryNames.Contains(c.Name)))
                    {
                        category.Enabled = true;
                    }
                }

                UpdateBasicCategories();

                WriteInformation("assembly");
            }

            private static void UpdateBasicCategories()
            {
                _basicCategories = SnTrace.Categories.ToDictionary(c => c.Name, c => c.Enabled);
            }

            private static void WriteInformation(string source)
            {
                var logger = Providers.Instance?.Services.GetService<ILogger<LoggingSettings>>();
                logger?.LogInformation($"Trace settings were updated (from {source}). " +
                                       $"Enabled: {CategoriesToString(true)}. " +
                                       $"Disabled: {CategoriesToString(false)}");
            }
            private static string CategoriesToString(bool isEnabled)
            {
                return string.Join(", ", SnTrace.Categories
                    .Where(x => x.Enabled == isEnabled)
                    .Select(x => x.Name));
            }
        }
    }
}
