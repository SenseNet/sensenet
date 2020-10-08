using System;
using System.Text;

// ReSharper disable once CheckNamespace
namespace SenseNet.Packaging
{
    /// <summary>
    /// Defines a version interval.
    /// </summary>
    public class VersionBoundary
    {
        public static readonly Version DefaultMinVersion = new Version(0, 0);
        public static readonly Version DefaultMaxVersion = new Version(int.MaxValue, int.MaxValue);

        /// <summary>
        /// The lower limit of the interval.
        /// </summary>
        public Version MinVersion { get; set; }
        /// <summary>
        /// The upper limit of the interval.
        /// </summary>
        public Version MaxVersion { get; set; }
        /// <summary>
        /// Gets or sets the value that specifies whether <c>MinVersion</c> is in the interval or not.
        /// If false, <c>MinVersion</c> is in the interval.
        /// If true, <c>MinVersion</c> is not in the interval.
        /// </summary>
        public bool MinVersionIsExclusive { get; set; }
        /// <summary>
        /// Gets or sets the value that specifies whether <c>MaxVersion</c> is in the interval or not.
        /// If false, <c>MaxVersion</c> is in the interval.
        /// If true, <c>MaxVersion</c> is not in the interval.
        /// </summary>
        public bool MaxVersionIsExclusive { get; set; }

        /// <summary>
        /// Returns true if the defined interval contains the given <paramref name="version"/>.
        /// </summary>
        /// <param name="version">The tested version.</param>
        /// <returns></returns>
        public bool ContainsVersion(Version version)
        {
            if (version == null)
                return false;
            if ((MinVersion != null && version < MinVersion) || (MaxVersion != null && version > MaxVersion))
                return false;
            if (MinVersion != null && MinVersionIsExclusive && version == MinVersion)
                return false;
            if (MaxVersion != null && MaxVersionIsExclusive && version == MaxVersion)
                return false;
            return true;
        }

        /// <summary>
        /// String representation of this VersionBoundary instance.
        /// </summary>
        public override string ToString()
        {
            var sb = new StringBuilder();

            if (MinVersion != null && (MinVersion.Major > 0 || MinVersion.Minor > 0))
            {
                sb.Append(MinVersion);
                sb.Append(MinVersionIsExclusive ? " < " : " <= ");
            }

            sb.Append("v");
            
            if (MaxVersion != null && (MaxVersion.Major < int.MaxValue || MaxVersion.Minor < int.MaxValue))
            {
                sb.Append(MaxVersionIsExclusive ? " < " : " <= ");
                sb.Append(MaxVersion);
            }

            return sb.ToString();
        }
    }
}
