using System;

// ReSharper disable once CheckNamespace
namespace SenseNet.Configuration
{
    public class Tracing : SnConfig
    {
        private const string SectionName = "sensenet/tracing";

        public static string[] StartupTraceCategories { get; } = GetString(SectionName, "StartupTraceCategories", string.Empty)
            .Split(new[] {',', ';', ' '}, StringSplitOptions.RemoveEmptyEntries);
    }
}
