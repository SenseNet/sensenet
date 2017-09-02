// ReSharper disable once CheckNamespace
// ReSharper disable RedundantTypeArgumentsOfMethod
namespace SenseNet.Configuration
{
    public class Indexing : SnConfig
    {
        private const string SectionName = "sensenet/indexing";

        private static string _indexDirectoryPath;
        /// <summary>
        /// Do not use this property directly. Use StorageContext.Search.IndexDirectoryPath instead.
        /// </summary>
        internal static string IndexDirectoryFullPath
        {
            get
            {
                if (_indexDirectoryPath == null)
                {
                    var configValue = GetString(SectionName, "IndexDirectoryPath", "..\\App_Data\\LuceneIndex");
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
        /// Do not use this property directly. Use StorageContext.Search.IsOuterEngineEnabled instead.
        /// </summary>
        internal static bool IsOuterSearchEngineEnabled { get; set; } = GetValue<bool>(SectionName, "EnableOuterSearchEngine", true);
        public static int LuceneMergeFactor { get; internal set; } = GetInt(SectionName, "LuceneMergeFactor", 10);
        public static double LuceneRAMBufferSizeMB { get; internal set; } = GetDouble(SectionName, "LuceneRAMBufferSizeMB", 16.0);
        public static int LuceneMaxMergeDocs { get; internal set; } = GetInt(SectionName, "LuceneMaxMergeDocs", int.MaxValue);

        public static int LuceneLockDeleteRetryInterval { get; internal set; } =
            GetInt(SectionName, "LuceneLockDeleteRetryInterval", 60);
        public static int IndexLockFileWaitForRemovedTimeout { get; internal set; } =
            GetInt(SectionName, "IndexLockFileWaitForRemovedTimeout", 120);
        public static string IndexLockFileRemovedNotificationEmail { get; internal set; } = GetString(SectionName, 
            "IndexLockFileRemovedNotificationEmail", string.Empty);

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
        public static int LuceneActivityTimeoutInSeconds { get; internal set; } = GetInt(SectionName, "LuceneActivityTimeoutInSeconds", 120);
        public static int LuceneActivityQueueMaxLength { get; internal set; } = GetInt(SectionName, "LuceneActivityQueueMaxLength", 500);
        public static int TextExtractTimeout { get; internal set; } = GetInt(SectionName, "TextExtractTimeout", 300);

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
