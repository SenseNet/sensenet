using System.Collections.Generic;

// ReSharper disable once CheckNamespace
// ReSharper disable RedundantTypeArgumentsOfMethod
namespace SenseNet.Configuration
{
    public class CacheConfiguration : SnConfig
    {
        private const string SectionName = "sensenet/cache";

        public enum CacheContentAfterSaveOption
        {
            None = 0,
            Containers,
            All
        }

        public static CacheContentAfterSaveOption CacheContentAfterSaveMode { get; internal set; } =
            GetValue(SectionName, "CacheContentAfterSaveMode", CacheContentAfterSaveOption.All);

        public static int NodeIdDependencyEventPartitions { get; internal set; } =
            GetInt(SectionName, "NodeIdDependencyEventPartitions", 400);

        public static int NodeTypeDependencyEventPartitions { get; internal set; } =
            GetInt(SectionName, "NodeTypeDependencyEventPartitions", 400);

        public static int PathDependencyEventPartitions { get; internal set; } =
            GetInt(SectionName, "PathDependencyEventPartitions", 400);

        public static int PortletDependencyEventPartitions { get; internal set; } =
            GetInt(SectionName, "PortletDependencyEventPartitions", 400);

        public static double SlidingExpirationSeconds { get; internal set; } =
            GetDouble(SectionName, "SlidingExpirationSeconds", 0);
        public static double AbsoluteExpirationSeconds { get; internal set; } =
            GetDouble(SectionName, "AbsoluteExpirationSeconds", 120);

        public static string ResizedImagesCacheFolder { get; internal set; } =
            GetString(SectionName, "ResizedImagesCacheFolder", "/ResizedImages");

        public static List<string> AdminGroupPathsForLoggedInUserCache { get; internal set; } =
            GetListOrEmpty<string>(SectionName, "AdminGroupPathsForLoggedInUserCache", new List<string>
            {
                "/Root/IMS/BuiltIn/Portal/Administrators"
            });
    }
}
