// ReSharper disable once CheckNamespace
namespace SenseNet.Configuration
{
    public class Actions : SnConfig
    {
        private const string SectionName = "sensenet/actions";

        public static string DefaultActionType { get; internal set; } = GetString(SectionName, "DefaultActionType", "UrlAction");
    }
}
