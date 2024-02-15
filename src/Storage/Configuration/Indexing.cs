// ReSharper disable once CheckNamespace
// ReSharper disable RedundantTypeArgumentsOfMethod

using System;
using SenseNet.Tools.Configuration;

namespace SenseNet.Configuration
{
    [OptionsClass("sensenet:indexing")]
    public class IndexingOptions
    {
        public static string IndexDirectoryPath { get; set; } = "";
        /// <summary>
        /// Gets or sets the periodicity of executing lost indexing tasks in seconds. Default: 60.
        /// </summary>
        public static int IndexHealthMonitorRunningPeriod { get; set; } = 60;
        //public static int IndexHistoryItemLimit { get; set; } = 1000000;
        //public static double CommitDelayInSeconds { get; set; } = 2.0d;
        //public static int DelayedCommitCycleMaxCount { get; set; } = 10;
        //public static int IndexingPausedTimeout { get; set; } = 60;
        public static int IndexingActivityTimeoutInSeconds { get; set; } = 120;
        public static int IndexingActivityQueueMaxLength { get; set; } = 500;
        public static int TextExtractTimeout { get; set; } = 300;
        /// <summary>
        /// Gets or sets the periodicity of deleting old IndexingActivities. Default: 10 minutes.
        /// </summary>
        public static int IndexingActivityDeletionPeriodInMinutes { get; set; } = 10;
        /// <summary>
        /// Gets or sets the age threshold for IndexingActivities that are periodically deleted.
        /// The default age threshold is set to 480 (8 hours).
        /// </summary>
        public static int IndexingActivityMaxAgeInMinutes { get; set; } = 480;
    }
    public class Indexing : SnConfig
    {
        public static readonly string DefaultLocalIndexDirectory = "App_Data\\LocalIndex";
        private const string SectionName = "sensenet/indexing";

        private static string _indexDirectoryPath;
        /// <summary>
        /// Do not use this property directly. Use SearchManager.IndexDirectoryPath instead.
        /// </summary>
        public static string IndexDirectoryFullPath
        {
            get
            {
                if (_indexDirectoryPath == null)
                {
                    var configValue = GetString(SectionName, "IndexDirectoryPath", $"..\\{DefaultLocalIndexDirectory}");
                    var assemblyPath = System.Reflection.Assembly.GetExecutingAssembly().CodeBase
                        .Replace("file:///", "")
                        .Replace("file://", "//")
                        .Replace("/", "\\");
                    var directoryPath = System.IO.Path.GetDirectoryName(assemblyPath) ?? string.Empty;

                    _indexDirectoryPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(directoryPath, configValue));
                }
                return _indexDirectoryPath;
            }
        }

        public static string IndexDirectoryPath { get; internal set; } = GetString(SectionName, "IndexDirectoryPath", string.Empty);

        /// <summary>
        /// Do not use this property directly. Use SearchManager.IsOuterEngineEnabled instead.
        /// </summary>
        public static bool IsOuterSearchEngineEnabled { get; set; } = GetValue<bool>(SectionName, "EnableOuterSearchEngine", true);

        #region Moved to Lucene29 configuration

        [Obsolete("Use properties in the Lucene29 configuration class instead.", true)]
        public static int LuceneMergeFactor { get; internal set; }
        [Obsolete("Use properties in the Lucene29 configuration class instead.", true)]
        public static double LuceneRAMBufferSizeMB { get; internal set; }
        [Obsolete("Use properties in the Lucene29 configuration class instead.", true)]
        public static int LuceneMaxMergeDocs { get; internal set; }
        [Obsolete("Use properties in the Lucene29 configuration class instead.", true)]
        public static int LuceneLockDeleteRetryInterval { get; internal set; } =
            GetInt(SectionName, "LuceneLockDeleteRetryInterval", 60);
        [Obsolete("Use properties in the Lucene29 configuration class instead.", true)]
        public static int IndexLockFileWaitForRemovedTimeout { get; internal set; } =
            GetInt(SectionName, "IndexLockFileWaitForRemovedTimeout", 120);
        [Obsolete("Use properties in the Lucene29 configuration class instead.", true)]
        public static string IndexLockFileRemovedNotificationEmail { get; internal set; } = GetString(SectionName, 
            "IndexLockFileRemovedNotificationEmail", string.Empty);

        #endregion

        /// <summary>
        /// Periodicity of executing lost indexing tasks in seconds. Default: 60 (1 minutes), minimum: 1.
        /// </summary>
        public static int IndexHealthMonitorRunningPeriod { get; internal set; } = GetInt(SectionName, 
            "IndexHealthMonitorRunningPeriod", 60, 1);

        /// <summary>
        /// Max number of cached items in indexing history. Default is 1000000.
        /// </summary>
        public static int IndexHistoryItemLimit { get; internal set; } = GetInt(SectionName, "IndexHistoryItemLimit", 1000000);
        public static double CommitDelayInSeconds { get; internal set; } = GetDouble(SectionName, "CommitDelayInSeconds", 2);
        public static int DelayedCommitCycleMaxCount { get; internal set; } = GetInt(SectionName, "DelayedCommitCycleMaxCount", 10);

        public static int IndexingPausedTimeout { get; internal set; } = GetInt(SectionName, "IndexingPausedTimeout", 60);
        public static int IndexingActivityTimeoutInSeconds { get; internal set; } = GetInt(SectionName, "IndexingActivityTimeoutInSeconds", 120);
        public static int IndexingActivityQueueMaxLength { get; internal set; } = GetInt(SectionName, "IndexingActivityQueueMaxLength", 500);
        public static int TextExtractTimeout { get; internal set; } = GetInt(SectionName, "TextExtractTimeout", 300);

        public static int IndexingActivityDeletionPeriodInMinutes { get; internal set; } = GetInt(SectionName, "IndexingActivityDeletionPeriodInMinutes", 10);
        public static int IndexingActivityMaxAgeInMinutes { get; internal set; } = GetInt(SectionName, "IndexingActivityMaxAgeInMinutes", 8 * 60);

        private static bool? GetNullableBool(string key)
        {
            var textValue = GetString(SectionName, key);
            bool boolValue;
            if (string.IsNullOrEmpty(textValue) || !bool.TryParse(textValue, out boolValue))
                return null;

            return boolValue;
        }
    }
}
