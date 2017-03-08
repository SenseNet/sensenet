// ReSharper disable once CheckNamespace
// ReSharper disable RedundantTypeArgumentsOfMethod
namespace SenseNet.Configuration
{
    public class SystemStart : SnConfig
    {
        private const string SectionName = "sensenet/systemStart";

        public static bool WarmupEnabled { get; internal set; } = GetValue<bool>(SectionName, "WarmupEnabled", true);
        public static string WarmupControlQueryFilter { get; internal set; } = GetString(SectionName, "WarmupControlQueryFilter", string.Empty);
    }
}
