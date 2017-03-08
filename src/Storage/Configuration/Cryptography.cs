// ReSharper disable once CheckNamespace
namespace SenseNet.Configuration
{
    public class Cryptography : SnConfig
    {
        private const string SectionName = "sensenet/cryptography";

        public static string CertificateThumbprint { get; internal set; } = GetString(SectionName, "CertificateThumbprint", string.Empty);
    }
}
