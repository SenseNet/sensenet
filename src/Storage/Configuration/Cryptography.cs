// ReSharper disable once CheckNamespace
using System;

namespace SenseNet.Configuration
{
    [Obsolete("Use CryptographyOptions instead from the service collection.")]
    public class Cryptography : SnConfig
    {
        private const string SectionName = "sensenet/cryptography";

        public static string CertificateThumbprint { get; internal set; } = GetString(SectionName, "CertificateThumbprint", string.Empty);
    }

    public class CryptographyOptions
    {
        public string CertificateThumbprint { get; set; }
    }
}
