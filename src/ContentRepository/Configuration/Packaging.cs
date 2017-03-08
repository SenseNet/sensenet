// ReSharper disable once CheckNamespace
namespace SenseNet.Configuration
{
    public class Packaging : SnConfig
    {
        private const string SectionName = "sensenet/packaging";

        public static string[] NetworkTargets { get; internal set; } = GetListOrEmpty<string>(SectionName, "NetworkTargets").ToArray();
        public static string TargetDirectory { get; internal set; } = GetString(SectionName, "TargetDirectory");
    }
}
