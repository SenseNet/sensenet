// ReSharper disable once CheckNamespace
using SenseNet.Tools.Configuration;
using System;

namespace SenseNet.Configuration
{
    [Obsolete("Use CryptographyOptions instead from the service collection.")]
    public class Cryptography : SnConfig
    {
        private const string SectionName = "sensenet/cryptography";

        public static string CertificateThumbprint { get; internal set; } = GetString(SectionName, "CertificateThumbprint", string.Empty);
    }

    [OptionsClass(sectionName: "sensenet:cryptography")]
    public class CryptographyOptions
    {
        public string CertificateThumbprint { get; set; }
    }
}
