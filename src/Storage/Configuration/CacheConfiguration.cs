using SenseNet.Tools.Configuration;
using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
// ReSharper disable RedundantTypeArgumentsOfMethod
namespace SenseNet.Configuration
{
    public enum CacheContentAfterSaveOption
    {
        None = 0,
        Containers,
        All
    }

    [Obsolete("Use CacheOptions instead from the service collection.", true)]
    public class CacheConfiguration : SnConfig
    {
        private const string SectionName = "sensenet/cache";

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

    /// <summary>
    /// Options for cache.
    /// </summary>
    [OptionsClass(sectionName: "sensenet:cache")]
    public class CacheOptions
    {
        /// <summary>
        /// Gets or sets a value that determines whether content should be cached after saving.
        /// Available values: None, Containers, All (default)
        /// </summary>
        public CacheContentAfterSaveOption CacheContentAfterSaveMode { get; set; } =  CacheContentAfterSaveOption.All;

        public int NodeIdDependencyEventPartitions { get; set; } = 400;
        public int NodeTypeDependencyEventPartitions { get; set; } = 400;
        public int PathDependencyEventPartitions { get; set; } = 400;
        public int PortletDependencyEventPartitions { get; set; } = 400;

        [Obsolete("This value is not used in this version.")]
        public double SlidingExpirationSeconds { get; set; } = 0;
        [Obsolete("This value is not used in this version.")]
        public double AbsoluteExpirationSeconds { get; set; } = 120;

        [Obsolete("Do not use this property anymore.", true)]
        public string ResizedImagesCacheFolder { get; set; } = "/ResizedImages";
        [Obsolete("Do not use this property anymore.", true)]
        public string AdminGroupPathForLoggedInUserCache { get; set; } ="/Root/IMS/BuiltIn/Portal/Administrators";
    }
}
