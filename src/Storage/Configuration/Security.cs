// ReSharper disable once CheckNamespace
using System;

namespace SenseNet.Configuration
{
    public class Security : SnConfig
    {
        private const string SectionName = "sensenet/security";

        public static bool EnablePasswordHashMigration { get; internal set; } = GetValue<bool>(SectionName, "EnablePasswordHashMigration");
        public static int PasswordHistoryFieldMaxLength { get; internal set; } = GetInt(SectionName, "PasswordHistoryFieldMaxLength", 10);

        [Obsolete("Use MessagingOptions instead.", true)]
        public static int SecuritActivityTimeoutInSeconds { get; internal set; } = GetInt(SectionName, "SecuritActivityTimeoutInSeconds", 120);
        [Obsolete("Use MessagingOptions instead.", true)]
        public static int SecuritActivityLifetimeInMinutes { get; internal set; } = GetInt(SectionName, "SecuritActivityLifetimeInMinutes", 25 * 60);
        [Obsolete("Use MessagingOptions instead.", true)]
        public static int SecurityDatabaseCommandTimeoutInSeconds { get; internal set; } = GetInt(SectionName, "SecurityDatabaseCommandTimeoutInSeconds", Data.DbCommandTimeout);
        [Obsolete("Use MessagingOptions instead.", true)]
        public static int SecurityMonitorRunningPeriodInSeconds { get; internal set; } = GetInt(SectionName, "SecurityMonitorPeriodInSeconds", 30);
    }
}
