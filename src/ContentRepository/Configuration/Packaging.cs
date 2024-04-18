// ReSharper disable once CheckNamespace

using SenseNet.Tools.Configuration;
using System;

namespace SenseNet.Configuration
{
    [Obsolete("Use PackagingOptions instead from the service collection.", true)]
    public class Packaging : SnConfig
    {
        private const string SectionName = "sensenet/packaging";

        public static string[] NetworkTargets { get; internal set; } = GetListOrEmpty<string>(SectionName, "NetworkTargets").ToArray();
        public static string TargetDirectory { get; internal set; } = GetString(SectionName, "TargetDirectory");
        public static string PackageDirectory { get; internal set; } = GetString(SectionName, "PackageDirectory");
    }

    /// <summary>
    /// Options for configuring the packaging.
    /// </summary>
    [OptionsClass(sectionName: "sensenet:packaging")]
    public class PackagingOptions
    {
        public string[] NetworkTargets { get; set; } = Array.Empty<string>();
        public string TargetDirectory { get; set; }
        public string PackageDirectory { get; set; }
    }
}
