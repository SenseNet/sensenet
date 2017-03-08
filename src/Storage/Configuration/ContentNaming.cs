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
            InvalidNameCharsPattern.Replace("\\", "\\\\").Replace("'", "\\'");

        public static char ReplacementChar { get; internal set; } = GetString(SECTIONNAME,
            "ReplacementChar", "-").FirstOrDefault();
    }
}
