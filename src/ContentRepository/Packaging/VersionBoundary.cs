using System;

// ReSharper disable once CheckNamespace
namespace SenseNet.Packaging
{
    /// <summary>
    /// Determines a version interval.
    /// </summary>
    public class VersionBoundary
    {
        /// <summary>
        /// The lower limit of the interval.
        /// </summary>
        public Version MinVersion { get; set; }
        /// <summary>
        /// The upper limit of the interval.
        /// </summary>
        public Version MaxVersion { get; set; }
        /// <summary>
        /// Gets or sets the value that specifies whether the <c>MinVersion</c> is in the interval or not.
        /// If false, the <c>MinVersion</c> is in the interval.
        /// If true, the <c>MinVersion</c> is not in the interval.
        /// </summary>
        public bool MinVersionIsExclusive { get; set; }
        /// <summary>
        /// Gets or sets the value that specifies whether the <c>MaxVersion</c> is in the interval or not.
        /// If false, the <c>MaxVersion</c> is in the interval.
        /// If true, the <c>MaxVersion</c> is not in the interval.
        /// </summary>
        public bool MaxVersionIsExclusive { get; set; }
    }
}
