// ReSharper disable once CheckNamespace
namespace SenseNet.Configuration
{
    public class IdentityManagement : SnConfig
    {
        private const string SectionName = "sensenet/identityManagement";

        public static readonly string BuiltInDomainName = "BuiltIn";
        public static string DefaultDomain { get; internal set; } = GetString(SectionName, "DefaultDomain", BuiltInDomainName);
        public static bool UserProfilesEnabled { get; internal set; } = GetValue<bool>(SectionName, "UserProfilesEnabled");
    }
}
