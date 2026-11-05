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
        /// Defaults to common European Latin letters without canonical decomposition.
        /// Configured indexed pairs replace the complete preset.
        /// </summary>
        public static string[] ReplacementSources { get; internal set; } =
            GetReplacementArray(nameof(ReplacementSources), new[]
            {
                "ß", "ẞ",
                "æ", "Æ", "œ", "Œ", "ø", "Ø", "ł", "Ł",
                "đ", "Đ", "ð", "Ð", "þ", "Þ", "ħ", "Ħ",
                "ı", "ĳ", "Ĳ", "ŋ", "Ŋ", "ŧ", "Ŧ", "ſ", "ŀ", "Ŀ"
            });

        /// <summary>
        /// Replacement strings paired by index with ReplacementSources. Empty strings remove matches.
        /// </summary>
        public static string[] ReplacementTargets { get; internal set; } =
            GetReplacementArray(nameof(ReplacementTargets), new[]
            {
                "ss", "SS",
                "ae", "AE", "oe", "OE", "o", "O", "l", "L",
                "d", "D", "d", "D", "th", "TH", "h", "H",
                "i", "ij", "IJ", "n", "N", "t", "T", "s", "l", "L"
            });

        private static string[] GetReplacementArray(string key, string[] defaultValues)
        {
            var values = new List<string>();
            for (var index = 0; ; index++)
            {
                var value = GetString(SECTIONNAME, key + ":" + index);
                if (value == null)
                {
                    // Use the preset only when neither indexed array is configured.
                    // A missing half of a custom pair must still fail validation when enabled.
                    return values.Count == 0 &&
                           GetString(SECTIONNAME, nameof(ReplacementSources) + ":0") == null &&
                           GetString(SECTIONNAME, nameof(ReplacementTargets) + ":0") == null
                        ? defaultValues
                        : values.ToArray();
                }
                values.Add(value);
            }
        }
    }
}
