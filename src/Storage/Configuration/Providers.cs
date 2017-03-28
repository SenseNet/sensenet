using SenseNet.Communication.Messaging;
using SenseNet.ContentRepository.Storage.Data.SqlClient;
using SenseNet.Security.Messaging;

// ReSharper disable once CheckNamespace
// ReSharper disable RedundantTypeArgumentsOfMethod
namespace SenseNet.Configuration
{
    public class Providers : SnConfig
    {
        private const string SectionName = "sensenet/providers";

        public static string DataProviderClassName { get; internal set; } = GetProvider("DataProvider", typeof(SqlProvider).FullName);
        public static string AccessProviderClassName { get; internal set; } = GetProvider("AccessProvider",
            "SenseNet.ContentRepository.Security.UserAccessProvider");
        public static string ContentNamingProviderClassName { get; internal set; } = GetProvider("ContentNamingProvider");
        public static string TaskManagerClassName { get; internal set; } = GetProvider("TaskManager");
        public static string PasswordHashProviderClassName { get; internal set; } = GetProvider("PasswordHashProvider",
            typeof(ContentRepository.Storage.Security.SenseNetPasswordHashProvider).FullName);
        public static string OutdatedPasswordHashProviderClassName { get; internal set; } = GetProvider("OutdatedPasswordHashProvider",
            typeof(ContentRepository.Storage.Security.Sha256PasswordHashProviderWithoutSalt).FullName);
        public static string SkinManagerClassName { get; internal set; } = GetProvider("SkinManager", "SenseNet.Portal.SkinManager");
        public static string DirectoryProviderClassName { get; internal set; } = GetProvider("DirectoryProvider");
        public static string SecurityMessageProviderClassName { get; internal set; } = GetProvider("SecurityMessageProvider", 
            typeof(DefaultMessageProvider).FullName);
        public static string DocumentPreviewProviderClassName { get; internal set; } = GetProvider("DocumentPreviewProvider",
            "SenseNet.Preview.DefaultDocumentPreviewProvider");
        public static string ClusterChannelProviderClassName { get; internal set; } = GetProvider("ClusterChannelProvider",
            typeof(VoidChannel).FullName);

        public static bool RepositoryPathProviderEnabled { get; internal set; } = GetValue<bool>(SectionName, "RepositoryPathProviderEnabled", true);

        private static string GetProvider(string key, string defaultValue = null)
        {
            return GetString(SectionName, key, defaultValue);
        }
    }
}
