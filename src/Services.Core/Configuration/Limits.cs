using System;
using System.Collections.Generic;
using System.Text;
using SenseNet.Configuration;

// ReSharper disable once CheckNamespace
namespace SenseNet.Configuration
{
    public class Limits : SnConfig
    {
        private const string SectionName = "sensenet/limits";

        /// <summary>
        /// Gets maximum size of response in bytes. Default: 7340032 (7 MB)
        /// </summary>
        public static int MaxResponseLength { get; }
            = GetInt(SectionName, "MaxResponseLength", 7340032);
    }
}
