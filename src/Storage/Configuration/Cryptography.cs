﻿// ReSharper disable once CheckNamespace
using SenseNet.Tools.Configuration;
using System;

namespace SenseNet.Configuration
{
    [Obsolete("Use CryptographyOptions instead from the service collection.", true)]
    public class Cryptography : SnConfig
    {
        private const string SectionName = "sensenet/cryptography";

        public static string CertificateThumbprint { get; internal set; } = GetString(SectionName, "CertificateThumbprint", string.Empty);
    }

    /// <summary>
    /// Options for configuring the cryptography service.
    /// </summary>
    [OptionsClass(sectionName: "sensenet:cryptography")]
    public class CryptographyOptions
    {
        /// <summary>
        /// The thumbprint of the certificate used for encrypting values.
        /// </summary>
        public string CertificateThumbprint { get; set; }
    }
}
