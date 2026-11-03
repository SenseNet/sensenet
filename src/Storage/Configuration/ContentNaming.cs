using System;
using System.Linq;

namespace SenseNet.Configuration
{
    public class ContentNaming : SnConfig
    {
        private static readonly string SECTIONNAME = "sensenet/contentNaming";

        public static string InvalidNameCharsPattern { get; internal set; } = GetString(SECTIONNAME,
            "InvalidNameCharsPattern", "[\\$&\\+\\\\,/:;=?@\"<>\\#%{}|^~\\[\\u005D'’`\\*\t\r\n]");

        /// <summary>
        /// Invalid name chars pattern formatted for client side JS code (some special characters escaped)
        /// </summary>
        public static string InvalidNameCharsPatternForClient { get; internal set; } =
            InvalidNameCharsPattern.Replace("\\", "\\\\").Replace("'", "\\'").Replace(Environment.NewLine, string.Empty);

        public static char ReplacementChar { get; internal set; } = GetString(SECTIONNAME,
            "ReplacementChar", "-").FirstOrDefault();

        /// <summary>
        /// Removes decomposable Unicode diacritics when generating a name from a display name.
        /// Default: false.
        /// </summary>
        public static bool RemoveDiacritics { get; internal set; } =
            GetValue<bool>(SECTIONNAME, nameof(RemoveDiacritics), false);

        /// <summary>
        /// Converts generated names to lowercase using invariant casing.
        /// The original extension is preserved. Default: false.
        /// </summary>
        public static bool Lowercase { get; internal set; } =
            GetValue<bool>(SECTIONNAME, nameof(Lowercase), false);
    }
}
