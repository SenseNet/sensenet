using System;
using SenseNet.Tools.Configuration;

// ReSharper disable once CheckNamespace
namespace SenseNet.Configuration
{
    [Obsolete("Use LoggingOptions instead from the service collection.", true)]
    public class Logging : SnConfig
    {
        private const string SectionName = "sensenet/logging";

        public static bool DownloadCounterEnabled { get; internal set; } = GetValue<bool>(SectionName, "DownloadCounterEnabled");
        public static bool AuditEnabled { get; internal set; } = GetValue<bool>(SectionName, "AuditEnabled", true);
        public static string EventLogName { get; internal set; } = GetValue(SectionName, "EventLogName", "SenseNet");
        public static string EventLogSourceName { get; internal set; } = GetValue(SectionName, "EventLogSourceName", "SenseNetInstrumentation");
    }

    /// <summary>
    /// Options for configuring the logging.
    /// </summary>
    [OptionsClass(sectionName: "sensenet:logging")]
    public class LoggingOptions
    {
        /// <summary>
        /// Gets or sets the state of the main switch of the DownloadCounter feature. Default: false (off).
        /// </summary>
        public bool DownloadCounterEnabled { get; set; }
        /// <summary>
        /// Gets or sets the state of the main switch of the Audit logging feature. Default: true (on).
        /// </summary>
        public bool AuditEnabled { get; set; } = true;
        /// <summary>
        /// Gets or sets the EventLog name (Windows only). Default: "SenseNet".
        /// </summary>
        public string EventLogName { get; set; } = "SenseNet";
        /// <summary>
        /// Gets or sets the EventLogSource name (Windows only). Default: "SenseNetInstrumentation".
        /// </summary>
        public string EventLogSourceName { get; set; } =  "SenseNetInstrumentation";
    }
}
