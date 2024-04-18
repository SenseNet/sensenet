using SenseNet.ContentRepository;
using System;
using SenseNet.Tools.Configuration;

// ReSharper disable once CheckNamespace
// ReSharper disable RedundantTypeArgumentsOfMethod
namespace SenseNet.Configuration
{
    [Obsolete("Use VersioningOptions instead from the service collection.", true)]
    public class Versioning : SnConfig
    {
        private const string SectionName = "sensenet/versioning";

        public static CheckInCommentsMode CheckInCommentsMode { get; internal set; } = GetValue<CheckInCommentsMode>(SectionName, "CheckInComments", CheckInCommentsMode.Recommended);
    }

    /// <summary>
    /// Options for configuring the packaging.
    /// </summary>
    [OptionsClass(sectionName: "sensenet:versioning")]
    public class VersioningOptions : SnConfig
    {
        public CheckInCommentsMode CheckInCommentsMode { get; set; } = CheckInCommentsMode.Recommended;
    }
}
