using System;
using System.Linq;
using System.Collections.Generic;

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

        /// <summary>
        /// Transliterates generated names to ASCII using AnyAscii. Default: false.
        /// </summary>
        public static bool Transliterate { get; internal set; } =
            GetValue<bool>(SECTIONNAME, nameof(Transliterate), false);

        /// <summary>
        /// Applies the ordered ReplacementSources/ReplacementTargets pairs to generated names. Default: false.
        /// </summary>
        public static bool ApplyCustomReplacements { get; internal set; } =
            GetValue<bool>(SECTIONNAME, nameof(ApplyCustomReplacements), false);

        /// <summary>
        /// Literal, case-sensitive source strings for custom replacements, in application order.
        /// </summary>
        public static string[] ReplacementSources { get; internal set; } =
            GetReplacementArray(nameof(ReplacementSources));

        /// <summary>
        /// Replacement strings paired by index with ReplacementSources. Empty strings remove matches.
        /// </summary>
        public static string[] ReplacementTargets { get; internal set; } =
            GetReplacementArray(nameof(ReplacementTargets));

        private static string[] GetReplacementArray(string key)
        {
            var values = new List<string>();
            for (var index = 0; ; index++)
            {
                var value = GetString(SECTIONNAME, key + ":" + index);
                if (value == null)
                    return values.ToArray();
                values.Add(value);
            }
        }
    }
}
