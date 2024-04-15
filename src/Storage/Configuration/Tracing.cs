using SenseNet.Tools.Configuration;
using System;

// ReSharper disable once CheckNamespace
namespace SenseNet.Configuration
{
    [Obsolete("Use TracingOptions instead from the service collection.", true)]
    public class Tracing : SnConfig
    {
        private const string SectionName = "sensenet/tracing";

        public static string[] StartupTraceCategories { get; } = GetString(SectionName, "StartupTraceCategories", string.Empty)
            .Split(new[] {',', ';', ' '}, StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Options for configuring the cryptography service.
    /// </summary>
    [OptionsClass(sectionName: "sensenet:tracing")]
    public class TracingOptions : SnConfig
    {
        /// <summary>
        /// Gets or sets comma, semicolon or space separated list of the trace categories
        /// in the startup sequence of the repository. Use GetStartupTraceCategories()
        /// method to access the detailed list
        /// </summary>
        public string StartupTraceCategories { get; set; } = string.Empty;

        private readonly char[] _separators = new[] {',', ';', ' '};
        private string[] _startupCategories;
        /// <summary>
        /// Returns a string arra containing the trace categories
        /// in the startup sequence of the repository.
        /// </summary>
        public string[] GetStartupTraceCategories()
        {
            return _startupCategories ??= string.IsNullOrEmpty(StartupTraceCategories)
                ? Array.Empty<string>()
                : StartupTraceCategories.Split(_separators, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
