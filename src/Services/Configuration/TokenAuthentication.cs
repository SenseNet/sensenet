namespace SenseNet.Configuration
{
    public class TokenAuthentication: SnConfig
    {
        private const string SectionName = "sensenet/tokenAuthentication";

        public static string SymmetricKeySecret { get;  set; } = GetString(SectionName, "SymmetricKeySecret");
        public static string Audience { get;  set; } = GetString(SectionName, "Audience", "client");
        public static string Issuer { get;  set; } = GetString(SectionName, "Issuer", "sensenet-token-service");
        public static string Subject { get;  set; } = GetString(SectionName, "Subject", "sensenet");
        public static string EncriptionAlgorithm { get;  set; } = GetString(SectionName, "EncriptionAlgorithm", "HS512");
        public static int AccessLifeTimeInMinutes { get;  set; } = GetValue<int>(SectionName, "AccessLifeTimeInMinutes", 5);
        public static int RefreshLifeTimeInMinutes { get;  set; } = GetValue<int>(SectionName, "RefreshLifeTimeInMinutes", 1440);
        public static int ClockSkewInMinutes { get;  set; } = GetValue<int>(SectionName, "ClockSkewInMinutes", 1);
    }
}