using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using SenseNet.Configuration;

namespace SenseNet.ContentRepository.Storage.Data
{
    [Obsolete("After V6.5 PATCH 9: Look for the values in the SenseNet.Configuration namespace instead.")]
    public static class RepositoryConfiguration
    {
        #region SECTION: BlobStorage, data, common

        [Obsolete("After V6.5 PATCH 9: Use Configuration.BlobStorage.FileStreamEnabled instead.")]
        public static bool FileStreamEnabled => Configuration.BlobStorage.FileStreamEnabled;
        [Obsolete("After V6.5 PATCH 9: Use Configuration.BlobStorage.MinimumSizeForFileStreamInBytes instead.")]
        public static int MinimumSizeForFileStreamInBytes => Configuration.BlobStorage.MinimumSizeForFileStreamInBytes;
        [Obsolete("After V6.5 PATCH 9: Use Configuration.BlobStorage.MinimumSizeForBlobProviderInBytes instead.")]
        public static int MinimumSizeForBlobProviderInBytes => Configuration.BlobStorage.MinimumSizeForBlobProviderInBytes;

        [Obsolete("After V6.5 PATCH 9: Use Configuration.Common.IsWebEnvironment instead.")]
        public static bool IsWebEnvironment => Configuration.Common.IsWebEnvironment;

        [Obsolete("After V6.5 PATCH 9: Use Configuration.Data.SqlCommandTimeout instead.")]
        public static int SqlCommandTimeout => Configuration.Data.SqlCommandTimeout;
        [Obsolete("After V6.5 PATCH 9: Use Configuration.Data.TransactionTimeout instead.")]
        public static double TransactionTimeout => Configuration.Data.TransactionTimeout;
        [Obsolete("After V6.5 PATCH 9: Use Configuration.Data.LongTransactionTimeout instead.")]
        public static double LongTransactionTimeout => Configuration.Data.LongTransactionTimeout;

        [Obsolete("After V6.5 PATCH 9: Use Configuration.BlobStorage.BinaryBufferSize instead.")]
        public static int BinaryBufferSize => Configuration.BlobStorage.BinaryBufferSize;
        [Obsolete("After V6.5 PATCH 9: Use Configuration.BlobStorage.BinaryChunkSize instead.")]
        public static int BinaryChunkSize => Configuration.BlobStorage.BinaryChunkSize;
        [Obsolete("After V6.5 PATCH 9: Use Configuration.BlobStorage.BinaryCacheSize instead.")]
        public static int BinaryCacheSize => Configuration.BlobStorage.BinaryCacheSize;

        #endregion

        #region Constants

        public const int AdministratorUserId = 1;
        public const int StartupUserId = -2;
        public const int SystemUserId = -1;
        public const int PortalRootId = 2;
        public const int PortalOrgUnitId = 5;
        public const int VisitorUserId = 6;
        public const int AdministratorsGroupId = 7;
        public const int EveryoneGroupId = 8;
        public const int OwnersGroupId = 9;
        public const string OperatorsGroupPath = "/Root/IMS/BuiltIn/Portal/Operators";
        public static readonly int SomebodyUserId = 10;

        public const int MaximumPathLength = 450;

        [Obsolete("After V6.5 PATCH 9: Use Constants.SpecialGroupPaths instead.")]
        public static string[] SpecialGroupPaths => Identifiers.SpecialGroupPaths;

        #endregion

        #region SECTION: ConnectionStrings MOVED

        public static string ConnectionString => ConnectionStrings.ConnectionString;
        public static string SecurityDatabaseConnectionString => ConnectionStrings.SecurityDatabaseConnectionString;
        public static string SignalRDatabaseConnectionString => ConnectionStrings.SignalRDatabaseConnectionString;

        #endregion

        #region SECTION: Providers MOVED

        public static string DataProviderClassName => Providers.DataProviderClassName;
        public static string AccessProviderClassName => Providers.AccessProviderClassName;
        public static string ContentNamingProviderClassName => Providers.ContentNamingProviderClassName;
        public static string TaskManagerClassName => Providers.TaskManagerClassName;
        public static string PasswordHashProviderClassName => Providers.PasswordHashProviderClassName;
        public static string OutdatedPasswordHashProviderClassName => Providers.OutdatedPasswordHashProviderClassName;
        public static string SkinManagerClassName => Providers.SkinManagerClassName;

        #endregion

        #region SECTION: WebApplication MOVED

        [Obsolete("Use SenseNet.Configuration.WebApplication.SignalRSqlEnabled instead.")]
        public static bool SignalRSqlEnabled => false;
        [Obsolete("Use SenseNet.Configuration.RepositoryEnvironment.BackwardCompatibilityDefaultValues instead.")]
        public static bool BackwardCompatibilityDefaultValues => RepositoryEnvironment.BackwardCompatibilityDefaultValues;
        [Obsolete("Use SenseNet.Configuration.RepositoryEnvironment.BackwardCompatibilityXmlNamespaces instead.")]
        public static bool BackwardCompatibilityXmlNamespaces => RepositoryEnvironment.BackwardCompatibilityXmlNamespaces;

        [Obsolete("Use SenseNet.Configuration.IdentityManagement.BuiltInDomainName instead.")]
        public static readonly string BuiltInDomainName = "BuiltIn";
        [Obsolete("Use SenseNet.Configuration.IdentityManagement.DefaultDomain instead.")]
        public static string DefaultDomain => IdentityManagement.DefaultDomain;

        #endregion

        #region SECTION: Indexing MOVED

        public static int LuceneMergeFactor => Indexing.LuceneMergeFactor;
        public static double LuceneRAMBufferSizeMB => Indexing.LuceneRAMBufferSizeMB;
        public static int LuceneMaxMergeDocs => Indexing.LuceneMaxMergeDocs;

        public static int LuceneLockDeleteRetryInterval => Indexing.LuceneLockDeleteRetryInterval;
        public static int IndexLockFileWaitForRemovedTimeout => Indexing.IndexLockFileWaitForRemovedTimeout;
        public static string IndexLockFileRemovedNotificationEmail => Indexing.IndexLockFileRemovedNotificationEmail;

        public static int IndexHealthMonitorRunningPeriod => Indexing.IndexHealthMonitorRunningPeriod;

        public static int IndexHistoryItemLimit => Indexing.IndexHistoryItemLimit;
        public static double CommitDelayInSeconds => Indexing.CommitDelayInSeconds;
        public static int DelayedCommitCycleMaxCount => Indexing.DelayedCommitCycleMaxCount;

        #endregion

        #region SECTION: Notification MOVED

        public static string NotificationSender => Notification.NotificationSender;

        #endregion

        #region SECTION: Cache MOVED

        public static Cache.CacheContentAfterSaveOption CacheContentAfterSaveMode => Cache.CacheContentAfterSaveMode;

        public static int NodeIdDependencyEventPartitions => Cache.NodeIdDependencyEventPartitions;
        public static int NodeTypeDependencyEventPartitions => Cache.NodeTypeDependencyEventPartitions;
        public static int PathDependencyEventPartitions => Cache.PathDependencyEventPartitions;
        public static int PortletDependencyEventPartitions => Cache.PortletDependencyEventPartitions;

        #endregion

        #region SECTION: Messaging MOVED

        public static string MessageQueueName => Messaging.MessageQueueName;
        public static int MessageRetentionTime => Messaging.MessageRetentionTime;
        public static int MessageProcessorThreadCount => Messaging.MessageProcessorThreadCount;
        public static int MessageProcessorThreadMaxMessages => Messaging.MessageProcessorThreadMaxMessages;
        public static int DelayRequestsOnHighMessageCountUpperLimit => Messaging.DelayRequestsOnHighMessageCountUpperLimit;
        public static int DelayRequestsOnHighMessageCountLowerLimit => Messaging.DelayRequestsOnHighMessageCountLowerLimit;
        public static int MsmqIndexDocumentSizeLimit => Messaging.MsmqIndexDocumentSizeLimit;
        public static int ClusterChannelMonitorInterval => Messaging.ClusterChannelMonitorInterval;
        public static int ClusterChannelMonitorTimeout => Messaging.ClusterChannelMonitorTimeout;

        #endregion

        #region SECTION: Logging MOVED

        public static bool PerformanceCountersEnabled => Logging.PerformanceCountersEnabled;
        public static CounterCreationDataCollection CustomPerformanceCounters => Logging.CustomPerformanceCounters;

        #endregion

        #region SECTION: Tracing MOVED

        public static string[] StartupTraceCategories => Tracing.StartupTraceCategories;

        #endregion

        #region SECTION: Security MOVED

        public static bool EnablePasswordHashMigration => Configuration.Security.EnablePasswordHashMigration;
        public static int PasswordHistoryFieldMaxLength => Configuration.Security.PasswordHistoryFieldMaxLength;
        
        public static int SecuritActivityTimeoutInSeconds => Configuration.Security.SecuritActivityTimeoutInSeconds;
        public static int SecuritActivityLifetimeInMinutes => Configuration.Security.SecuritActivityLifetimeInMinutes;
        public static int SecurityDatabaseCommandTimeoutInSeconds => Configuration.Security.SecurityDatabaseCommandTimeoutInSeconds;
        public static int SecurityMonitorRunningPeriodInSeconds => Configuration.Security.SecurityMonitorRunningPeriodInSeconds;

        #endregion

        #region SECTION: Cryptography MOVED

        public static string CertificateThumbprint => Cryptography.CertificateThumbprint;

        #endregion Working modes


        #region Helper methods

        private static string GetStringValue(string sectionName, string key, string defaultValue = null)
        {
            string configValue = null;

            var section = ConfigurationManager.GetSection(sectionName) as NameValueCollection;
            if (section != null)
                configValue = section[key];
            
            // backward compatibility: fallback to the appsettings section
            if (configValue == null)
                configValue = ConfigurationManager.AppSettings[key];

            return configValue ?? defaultValue;
        }

        [Obsolete("After V6.5 PATCH 9: Use the SnConfig API instead.", true)]
        public static T GetValue<T>(string sectionName, string key, T defaultValue = default(T))
        {
            var configString = GetStringValue(sectionName, key);
            if (string.IsNullOrEmpty(configString))
                return defaultValue;

            try
            {
                return Convert<T>(configString);
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }
        private static T Convert<T>(string value)
        {
            if (typeof(T).IsEnum)
                return (T)Enum.Parse(typeof(T), value);

            return (T)System.Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
        }

        #endregion
    }
}