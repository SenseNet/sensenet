// ReSharper disable once CheckNamespace
namespace SenseNet.Configuration
{
    /// <summary>
    /// Determines domain handling in the login algorithms.
    /// Can be configured under the key  sensenet/identityManagement/DomainUsagePolicy.
    /// The default is "NoDomain".
    /// </summary>
    public enum DomainUsagePolicy
    {
        /// <summary>
        /// The domain is optional if the login name is system-wide unique.
        /// If not, <see cref="MissingDomainException"/>  will be thrown.
        /// </summary>
        NoDomain,
        /// <summary>
        /// The default domain will be used if the domain name is not present during logging in.
        /// The default is optionally configured under the key sensenet/identityManagement/DefaultDomain.
        /// The general default is "BuiltIn".
        /// </summary>
        DefaultDomain,
        /// <summary>
        /// This is the most strict policy: the domain name is always required.
        /// </summary>
        MandatoryDomain
    }
    public class IdentityManagement : SnConfig
    {
        private const string SectionName = "sensenet/identityManagement";

        public static readonly string BuiltInDomainName = "BuiltIn";
        public static DomainUsagePolicy DomainUsagePolicy { get; set; } =
            GetValue<DomainUsagePolicy>(SectionName, nameof(DomainUsagePolicy), DomainUsagePolicy.NoDomain);
        public static string DefaultDomain { get; set; } = GetString(SectionName, "DefaultDomain", BuiltInDomainName);
        public static bool UserProfilesEnabled { get; set; } = GetValue<bool>(SectionName, "UserProfilesEnabled");
    }
}
