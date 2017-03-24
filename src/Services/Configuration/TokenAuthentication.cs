namespace SenseNet.Configuration
{
    public class TokenAuthentication: SnConfig
    {
        private const string SectionName = "sensenet/tokenAuthentication";

        /// <summary>
        /// Gets a strongly typed value (for example enums that are not available in the lower layers) 
        /// from the current section from configuration.
        /// </summary>
        public static T GetValue<T>(string key, T defaultValue = default(T))
        {
            return GetValue<T>(SectionName, key, defaultValue);
        }

        public static string SymmetricKeySecret { get;  set; } = GetString(SectionName, "SymmetricKeySecret");
        public static string Audience { get;  set; } = GetString(SectionName, "Audience");
        public static string Issuer { get;  set; } = GetString(SectionName, "Issuer");
        public static string Subject { get;  set; } = GetString(SectionName, "Subject");
        public static string EncriptionAlgorithm { get;  set; } = GetString(SectionName, "EncriptionAlgorithm");
        public static int AccessLifeTimeInMinutes { get;  set; } = GetValue<int>(SectionName, "AccessLifeTimeInMinutes");
        public static int RefreshLifeTimeInMinutes { get;  set; } = GetValue<int>(SectionName, "RefreshLifeTimeInMinutes");
        public static int ClockSkewInMinutes { get;  set; } = GetValue<int>(SectionName, "ClockSkewInMinutes");
    }
}